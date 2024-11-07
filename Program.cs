using Azure.AI.OpenAI;
using Azure.Identity;
using OpenAI;
using OpenAI.RealtimeConversation;
using System.ClientModel;
using RealtimeInteractiveConsole;
using System.ComponentModel;

//https://github.com/adiazcan/assistant-voice/blob/main/Program.cshttps://github.com/adiazcan/assistant-voice/blob/main/Program.cs
//https://github.com/Azure-Samples/aoai-realtime-audio-sdk/tree/main/dotnet/samples
#pragma warning disable OPENAI002

public class Program
{
    [Description("Invoked when the user says goodbye, expresses being finished, or otherwise seems to want to stop the interaction.")]
    private void FinishConversation()
    {
        //return true;
    }

    public static async Task Main(string[] args)
    {
        var assistant = new Assistant();
        await assistant.Initialize();

    }
}