using Azure.AI.OpenAI;
using Azure.Identity;
using OpenAI;
using OpenAI.RealtimeConversation;
using System;
using System.ClientModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RealtimeInteractiveConsole
{
    public class Credentials
    {
        public string? AoaiEndpoint { get; set; }
        public string? AoaiDeployment { get; set; }
        public string? AoaiApiKey { get; set; }
        public bool UseEntra { get; set; }
        public string? OaiApiKey { get; set; }
    }

    public class OpenAIConfiguration
    {
        public static RealtimeConversationClient GetClient(Credentials credentials)
        {
            if (credentials.AoaiEndpoint != null && credentials.UseEntra)
            {
                return GetClientForAzureOpenAIWithEntra(credentials.AoaiEndpoint, credentials.AoaiDeployment);
            }
            else if (credentials.AoaiEndpoint != null && credentials.AoaiApiKey != null)
            {
                return GetClientForAzureOpenAIWithKey(credentials.AoaiEndpoint, credentials.AoaiDeployment, credentials.AoaiApiKey);
            }
            else if (credentials.OaiApiKey != null)
            {
                return GetClientForOpenAIWithKey(credentials.OaiApiKey);
            }
            else
            {
                throw new InvalidOperationException("Invalid credentials configuration");
            }
        }

        private static RealtimeConversationClient GetClientForAzureOpenAIWithEntra(string endpoint, string? deployment)
        {
            AzureOpenAIClient client = new(new Uri(endpoint), new DefaultAzureCredential());
            return client.GetRealtimeConversationClient(deployment);
        }

        private static RealtimeConversationClient GetClientForAzureOpenAIWithKey(string endpoint, string? deployment, string apiKey)
        {
            AzureOpenAIClient client = new(new Uri(endpoint), new ApiKeyCredential(apiKey));
            return client.GetRealtimeConversationClient(deployment);
        }

        private static RealtimeConversationClient GetClientForOpenAIWithKey(string apiKey)
        {
            OpenAIClient client = new(new ApiKeyCredential(apiKey));
            return client.GetRealtimeConversationClient("gpt-4o-realtime-preview-2024-10-01");
        }
    }
}
