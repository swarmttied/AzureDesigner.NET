using System.Text.RegularExpressions;

namespace SKLIb;

public interface IAIResponseExtractor
{
    string ExtractJson(string aiResponse);
}

/// <summary>
/// Extracts JSON content from AI responses with fallback mechanisms
/// </summary>
public class AIResponseExtractor : IAIResponseExtractor
{
    private readonly Regex _jsonRegex = new(@"(?<=```json)(.*?)(?=```)", RegexOptions.Singleline | RegexOptions.Compiled);
    private readonly Regex _fallbackRegex = new(@"\{.*\}", RegexOptions.Singleline | RegexOptions.Compiled);

    /// <summary>
    /// Extracts JSON content from AI response using multiple extraction strategies
    /// </summary>
    /// <param name="aiResponse">The AI response containing JSON</param>
    /// <returns>Extracted JSON string, or empty string if no JSON found</returns>
    public string ExtractJson(string aiResponse)
    {
        if (string.IsNullOrWhiteSpace(aiResponse))
            return "";

        // First attempt: Extract from ```json...``` code blocks
        var match = _jsonRegex.Match(aiResponse);
        if (match.Success)
        {
            var jsonInResult = match.Value.Trim();

            if (!string.IsNullOrWhiteSpace(jsonInResult))
                return jsonInResult;
        }

        // Fallback: Extract content between first { and last }
        var fallbackMatch = _fallbackRegex.Match(aiResponse);
        if (fallbackMatch.Success)
        {
            return fallbackMatch.Value.Trim();
        }

        return "";
    }
}