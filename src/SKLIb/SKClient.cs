using System.Net;
using System.Text.RegularExpressions;
using Azure.AI.OpenAI;
using Azure.Core;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;



namespace SKLIb
{
    public interface ISKClient
    {
        event EventHandler<SKClient.RateExceededEventArgs>? RateExceeded;
        event EventHandler<SKClient.ResponseEventArgs>? ResponseReceived;
        event EventHandler<FunctionCallEventArgs>? FunctionCalled;
        event EventHandler<SKClient.RequestEventArgs>? RequestSent;
        event EventHandler<ExceptionEventArgs>? ExceptionOccurred;
        Task RunAsync(string prompt);
    }

    public class SKClient : ISKClient
    {
        public class Settings
        {
            public string Endpoint { get; set; } = string.Empty;
            public string Deployment { get; set; } = string.Empty;
            public string Instructions { get; set; } = string.Empty;
            public bool SaveChatHistory { get; set; }
        }

        public class ResponseEventArgs : EventArgs
        {
            public string? Response { get; set; }
            public string[] SqlQueries { get; set; } = Array.Empty<string>();
            public string? JsonResponse { get; set; }
        }

        public class RequestEventArgs : EventArgs
        {
            public string Prompt { get; set; } = string.Empty;
        }

        public class RateExceededEventArgs : EventArgs
        {
            public int WaitTimeInSeconds { get; set; }
        }

        public event EventHandler<RequestEventArgs>? RequestSent;
        public event EventHandler<ResponseEventArgs>? ResponseReceived;
        public event EventHandler<RateExceededEventArgs>? RateExceeded;
        public event EventHandler<FunctionCallEventArgs>? FunctionCalled;
        public event EventHandler<ExceptionEventArgs>? ExceptionOccurred;

        readonly ChatHistory _chatHistory;
        readonly IChatCompletionService _chatService;
        readonly OpenAIPromptExecutionSettings _openAIPromptExecutionSettings;
        readonly IAIResponseExtractor aIResponseExtractor;
        readonly Kernel _kernel;
        readonly bool _saveChatHistory;  // Still contemplating whether to use chat history or NOT 
        readonly AIAgent _agent;

        // Synchronization root to guard ChatHistory access (multi-thread safety)
        private readonly object _chatHistorySync = new();

        public SKClient(Settings settings, TokenCredential credential, object[]? services = null, IAIResponseExtractor aIResponseExtractor = null)
        {
            this.aIResponseExtractor = aIResponseExtractor ?? new AIResponseExtractor();
            var builder = Kernel.CreateBuilder();
            builder.AddAzureOpenAIChatCompletion(
                deploymentName: settings.Deployment,
                endpoint: settings.Endpoint,
                credentials: credential);

            _saveChatHistory = settings.SaveChatHistory;

            _openAIPromptExecutionSettings = new();

            if (services != null)
            {
                foreach (var service in services)
                    builder.Plugins.AddFromObject(service);
                _openAIPromptExecutionSettings.ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions;
            }

            _kernel = builder.Build();

            _chatHistory = new ChatHistory();
            if (!string.IsNullOrEmpty(settings.Instructions))
            {
                lock (_chatHistorySync)
                {
                    _chatHistory.AddSystemMessage(settings.Instructions);
                }
            }
            _chatService = _kernel.GetRequiredService<IChatCompletionService>();



            var client = new AzureOpenAIClient(new Uri(settings.Endpoint), credential);
            var chatClient = client.GetChatClient(settings.Deployment).AsIChatClient();
            //var aiTools = services.Select(o=>o).OfType<AITool>();
            var functionSources = services.Select(o => o)
                                             .OfType<IAIFunctionsSource>();
            IList<AITool> toolList = functionSources.SelectMany(o => o.GetAIFunctions()).ToList();
            _agent = chatClient.CreateAIAgent(tools: toolList);

        }

        public async Task RunAsync(string prompt)
        {
            RequestSent?.Invoke(this, new RequestEventArgs { Prompt = prompt });

            if (_saveChatHistory)
            {  // Guard against multi-thread access
                lock (_chatHistorySync)
                {
                    _chatHistory.AddUserMessage(prompt);
                }
            }

            bool success = false;
            IReadOnlyList<ChatMessageContent>? messageContents = null;
            AgentRunResponse agentRunResponse = null;
            int waitTimeInSeconds = 0;
            do
            {
                if (waitTimeInSeconds > 0)
                {
                    await Task.Delay(TimeSpan.FromSeconds(waitTimeInSeconds));
                }

                try
                {
                    // Snapshot of chat history under lock to avoid concurrent mutation during send.
                    ChatHistory localHistory;
                    if (_saveChatHistory)
                    {
                        lock (_chatHistorySync)
                        {
                            // ChatHistory does not implement deep clone, but passing reference is fine if no mutation until response.
                            localHistory = _chatHistory;
                        }
                        //messageContents = await _chatService.GetChatMessageContentsAsync(
                        //                      _chatHistory,
                        //                      _openAIPromptExecutionSettings,
                        //                      kernel: _kernel);


                    }
                    else
                    {
                        //messageContents = await _chatService.GetChatMessageContentsAsync(
                        //                       prompt,
                        //                       _openAIPromptExecutionSettings,
                        //                       kernel: _kernel);

                        agentRunResponse = await _agent.RunAsync(new ChatMessage(ChatRole.User, prompt));
                        //await foreach (var chunk in agentResult)
                        //{
                        //    if (chunk is not null)
                        //    {
                        //        FunctionCalled?.Invoke(this, new FunctionCallEventArgs(chunk.Text));
                        //    }
                        //}
                    }
                    success = true;
                }
                catch (Exception ex)
                {
                    if (ex.Message.Contains("exceeded"))
                    {
                        var waitTime = GetSecondsToWait(ex.Message);
                        waitTimeInSeconds = waitTime;
                        var args = new RateExceededEventArgs { WaitTimeInSeconds = waitTimeInSeconds };
                        RateExceeded?.Invoke(this, args);
                    }
                    else if (ex.Message.Contains("content_filter"))
                    {
                        var msg = "The response was filtered due to the prompt triggering Azure OpenAI's content management policy. Please modify your prompt and retry.";
                        ResponseReceived?.Invoke(this, new ResponseEventArgs { Response = msg });
                    }
                    else if (ex.Message.Contains("400"))
                    {
                        var msg = "Bad request. Please check the prompt and try again.";
                        ResponseReceived?.Invoke(this, new ResponseEventArgs { Response = ex.Message });
                        success = true; // do not retry
                    }
                    else
                    {
                        ExceptionOccurred?.Invoke(this, new ExceptionEventArgs(ex));
                    }
                }
            }
            while (!success);

            string fullMessage = "";
            //if (messageContents != null)
            //{
            //    foreach (var msg in messageContents)
            //    {
            //        fullMessage += msg.Content;
            //    }
            //}

            if (agentRunResponse != null)
            {
                foreach (ChatMessage chatMsg in agentRunResponse.Messages)
                {
                    fullMessage += chatMsg.Text;
                }
            }

            if (_saveChatHistory)
            {
                lock (_chatHistorySync)
                {
                    _chatHistory.AddAssistantMessage(fullMessage);
                }
            }

            ResponseReceived?.Invoke(this, new ResponseEventArgs
            {
                Response = fullMessage,
                JsonResponse = aIResponseExtractor.ExtractJson(fullMessage)
            });
        }

        static string[] ExtractSql(string response)
        {
            string pattern = @"(?<=```sql)(.*?)(?=```)";
            var matches = Regex.Matches(response, pattern, RegexOptions.Singleline);
            string[] sqlQueries = new string[matches.Count];
            for (int i = 0; i < matches.Count; i++)
            {
                sqlQueries[i] = matches[i].Value.Trim();
            }
            return sqlQueries;
        }

        static int GetSecondsToWait(string rateExeededMessage)
        {
            string pattern = @"(?<=Try again in )\d+(?= seconds\.)";
            var match = Regex.Match(rateExeededMessage, pattern);
            return match.Success ? int.Parse(match.Value) : 30;
        }
    }
}
