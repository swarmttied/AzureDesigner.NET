using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace AzureDesigner.WinUI;

public interface IIconPathSource
{
    string this[string serviceType] { get; }
}

public class IconPathSource : IIconPathSource
{
    const string IconAssetPrefix = "/Assets/Icon-";

    Dictionary<string, string> _iconFileLookup = new(StringComparer.InvariantCultureIgnoreCase)
    {
        { "microsoft.cognitiveservices/accounts/cognitiveservices", "CognitiveServices.svg" },
        { "microsoft.cognitiveservices/accounts/formrecognizer", "FormRecognizer.svg" },
        { "microsoft.cognitiveservices/accounts/openai", "OpenAI.svg" },
        { "microsoft.cognitiveservices/accounts/textanalytics", "TextAnalytics.svg" },
        { "microsoft.insights/actiongroups", "ActionGroups.svg" },
        { "microsoft.insights/components/web", "AppInsights.svg" },
        { "microsoft.portal/dashboards", "Dashboard.svg" },
        { "microsoft.network/privatednszones", "DNSZones.svg" },
        { "microsoft.documentdb/databaseaccounts/globaldocumentdb", "DocumentDB.svg" },
        { "hostedgroups", "HostedGroups.svg" },
        { "microsoft.cloudtest/hostedpools", "HostedPools.svg" },
        { "microsoft.network/networksecuritygroups", "NetworkSecurityGroups.svg" },
        { "microsoft.network/networkinterfaces", "NetworkInterfaces.svg" },
        { "microsoft.network/privateendpoints", "PrivateEndpoints.svg" },
        { "microsoft.network/networkwatchers", "NetworkWatchers.svg" },
        { "microsoft.network/networksecurityperimeters", "NetworkSecurityPerimeters.svg" },
        { "microsoft.cdn/profiles/frontdoor", "FrontDoor.svg" },
        { "microsoft.search/searchservices", "Search.svg" },
        { "microsoft.web/serverfarms/functionapp", "ServerFarms.svg" },
        { "microsoft.web/serverfarms/app", "ServerFarms.svg" },
        { "microsoft.sql/servers/v12.0", "SQLServer.svg" },
        { "microsoft.sql/servers/databases/v12.0,user,vcore", "SQLServerDB.svg" },
        { "Microsoft.Sql/servers/databases/v12.0,user,vcore,serverless", "SQLServerDB.svg" },
        { "microsoft.sql/servers/databases/v12.0,system", "SQLServerDB.svg" },
        { "microsoft.apimanagement/service", "Service.svg" },
        { "microsoft.web/sites/app", "WebApp.svg" },
        { "microsoft.web/sites/app,linux", "WebApp.svg" },
        { "microsoft.web/sites/functionapp", "FunctionApp.svg" },
        { "microsoft.web/sites/functionapp,linux", "FunctionApp.svg" },
        { "microsoft.alertsmanagement/smartdetectoralertrules", "AlertRules.svg" },
        { "microsoft.storage/storageaccounts/storagev2", "StorageV2.svg" },
        { "microsoft.storage/storageaccounts/storage", "Storage.svg" },
        { "templatespecs", "TemplateSpecs.svg" },
        { "microsoft.cognitiveservices/accounts/texttranslation", "TextTranslation.svg" },
        { "microsoft.managedidentity/userassignedidentities", "Identity.svg" },
        { "microsoft.keyvault/vaults", "KeyVault.svg" },
        { "microsoft.network/virtualnetworks", "VirtualNetwork.svg" },
        { "workspaces", "Workspaces.svg" },
        { "workspaces/hub", "WorkspacesHub.svg" },
        { "microsoft.logic/workflows", "LogicApp.svg" },
        { "microsoft.resources/templatespecs", "TemplateSpecs.svg" },
        { "microsoft.resources/templatespecs/versions", "TemplateSpecs.svg" }
    };

    public string this[string serviceType]
    {

        get
        {
            serviceType = serviceType.ToLower().Trim();

            //if (serviceType.Contains("templatespecs"))
            //    serviceType = "templatespecs";
            //else if (_workspaces.Contains(serviceType))
            //    serviceType = "workspaces";
            //else if (serviceType.Contains("serverfarms"))
            //    serviceType = "serverfarms";
            //else if (serviceType.Contains("virutalnetworks"))
            //    serviceType = "virtualnetworks";
            //else if (serviceType.Contains("texttranslation"))
            //    serviceType = "texttranslation";
            //else if (serviceType.Contains("dnszones"))
            //    serviceType = "dnszones";
            //else if (serviceType.Contains("documentdb"))
            //    serviceType = "documentdb";
            //else if (serviceType.Contains("search"))
            //    serviceType = "search";

            string iconFullPath = _iconFileLookup.TryGetValue(serviceType, out var iconfFile)
                            ? $"{IconAssetPrefix}{iconfFile}"
                            : $"{IconAssetPrefix}Default.svg";
            return iconFullPath;
        }
    }
}
