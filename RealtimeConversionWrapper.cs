using Azure.AI.OpenAI;
using Azure.Identity;
using OpenAI.RealtimeConversation;
using OpenAI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using RealtimeInteractiveConsole.Utilities;
using RealtimeInteractiveConsole.Utilities.RealtimeInteractiveConsole.Utilities;

namespace RealtimeInteractiveConsole
{


    public class RealtimeConversationSessionWrapper
    {
        private readonly RealtimeConversationClient _client;
        private readonly List<ConversationFunctionWrapper> _functionWrappers;

        public RealtimeConversationSessionWrapper(Credentials credentials)
        {
            _client = OpenAIConfiguration.GetClient(credentials);
            _functionWrappers = new List<ConversationFunctionWrapper>();
        }

        public void AddFunctionWrapper(ConversationFunctionWrapper functionWrapper)
        {
            _functionWrappers.Add(functionWrapper);
        }

        public void AddFunction(Func<object> function)
        {
            var wrapper = new ConversationFunctionWrapper(function);
            _functionWrappers.Add(wrapper);
        }

        public async Task StartSessionAsync()
        {
            using RealtimeConversationSession session = await _client.StartConversationSessionAsync();
            await ConfigureSessionAsync(session);
            await ProcessSessionUpdatesAsync(session);
        }

        protected virtual async Task ConfigureSessionAsync(RealtimeConversationSession session)
        {
            var options = new ConversationSessionOptions
            {
                Voice = ConversationVoice.Shimmer,
                InputTranscriptionOptions = new() { Model = "whisper-1" }
            };

            foreach (var wrapper in _functionWrappers)
            {
                options.Tools.Add(wrapper.CreateTool());
            }

            await session.ConfigureSessionAsync(options);
        }

        protected virtual async Task ProcessSessionUpdatesAsync(RealtimeConversationSession session)
        {
            await foreach (ConversationUpdate update in session.ReceiveUpdatesAsync())
            {
                switch (update)
                {
                    case ConversationSessionStartedUpdate:
                        OnSessionStarted(session);
                        break;
                    case ConversationInputSpeechStartedUpdate:
                        OnInputSpeechStarted();
                        break;
                    case ConversationInputSpeechFinishedUpdate:
                        OnInputSpeechFinished();
                        break;
                    case ConversationInputTranscriptionFinishedUpdate transcriptionUpdate:
                        OnInputTranscriptionFinished(transcriptionUpdate);
                        break;
                    case ConversationAudioDeltaUpdate audioUpdate:
                        OnAudioDelta(audioUpdate);
                        break;
                    case ConversationOutputTranscriptionDeltaUpdate outputTranscriptionUpdate:
                        OnOutputTranscriptionDelta(outputTranscriptionUpdate);
                        break;
                    case ConversationItemFinishedUpdate itemFinishedUpdate:
                        await OnItemFinishedAsync(session, itemFinishedUpdate);
                        break;
                    case ConversationErrorUpdate errorUpdate:
                        OnError(errorUpdate);
                        break;
                }
            }
        }

        protected virtual void OnSessionStarted(RealtimeConversationSession session)
        {
            _ = Task.Run(async () =>
            {
                using MicrophoneAudioStream microphoneInput = MicrophoneAudioStream.Start();
                await session.SendAudioAsync(microphoneInput);
            });
        }

        protected virtual void OnInputSpeechStarted() { }

        protected virtual void OnInputSpeechFinished() { }

        protected virtual void OnInputTranscriptionFinished(ConversationInputTranscriptionFinishedUpdate transcriptionUpdate) { }

        protected virtual void OnAudioDelta(ConversationAudioDeltaUpdate audioUpdate) { }

        protected virtual void OnOutputTranscriptionDelta(ConversationOutputTranscriptionDeltaUpdate outputTranscriptionUpdate) { }

        protected virtual async Task OnItemFinishedAsync(RealtimeConversationSession session, ConversationItemFinishedUpdate itemFinishedUpdate)
        {
            foreach (var wrapper in _functionWrappers)
            {
                var conversationItem = await wrapper.InvokeFunctionForConversationAsync(itemFinishedUpdate);
                if (conversationItem != null)
                {
                    await session.AddItemAsync(conversationItem);
                }
            }
        }

        protected virtual void OnError(ConversationErrorUpdate errorUpdate) { }
    }

    public class RealtimeConversationWithVoiceOutput : RealtimeConversationSessionWrapper
    {
        private readonly OutputInstance _output;
        private readonly SpeakerOutput _speakerOutput;

        public RealtimeConversationWithVoiceOutput(Credentials credentials) : base(credentials)
        {
            _output = new OutputInstance();
            _speakerOutput = new SpeakerOutput();
        }

        protected override void OnSessionStarted(RealtimeConversationSession session)
        {
            _output.WriteLine($" <<< Connected: session started");
            base.OnSessionStarted(session);
            _output.WriteLine($" >>> Listening to microphone input");
        }

        protected override void OnInputSpeechStarted()
        {
            _output.WriteLine($" <<< Start of speech detected");
            base.OnInputSpeechStarted();
            _speakerOutput.ClearPlayback();
        }

        protected override void OnInputSpeechFinished()
        {
            _output.WriteLine($" <<< End of speech detected");
            base.OnInputSpeechFinished();
        }

        protected override void OnInputTranscriptionFinished(ConversationInputTranscriptionFinishedUpdate transcriptionUpdate)
        {
            _output.WriteLine($" >>> USER: {transcriptionUpdate.Transcript}");
            base.OnInputTranscriptionFinished(transcriptionUpdate);
        }

        protected override void OnAudioDelta(ConversationAudioDeltaUpdate audioUpdate)
        {
            _speakerOutput.EnqueueForPlayback(audioUpdate.Delta);
            base.OnAudioDelta(audioUpdate);
        }

        protected override void OnOutputTranscriptionDelta(ConversationOutputTranscriptionDeltaUpdate outputTranscriptionUpdate)
        {
            Output.Write(outputTranscriptionUpdate.Delta);
            base.OnOutputTranscriptionDelta(outputTranscriptionUpdate);
        }

        protected override async Task OnItemFinishedAsync(RealtimeConversationSession session, ConversationItemFinishedUpdate itemFinishedUpdate)
        {
            await base.OnItemFinishedAsync(session, itemFinishedUpdate);
            _output.WriteLine($" <<< Function invoked");
        }

        protected override void OnError(ConversationErrorUpdate errorUpdate)
        {
            _output.WriteLine($" <<< ERROR: {errorUpdate.ErrorMessage}");
            base.OnError(errorUpdate);
        }
    }
}