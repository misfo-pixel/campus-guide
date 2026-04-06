using UnityEngine;
using UnityEngine.InputSystem;

public class VoiceInputController : MonoBehaviour
{
    [SerializeField] private MicrophoneRecorder microphoneRecorder;
    [SerializeField] private AzureSpeechSTTClient azureSpeechSTTClient;
    [SerializeField] private VoiceTranscriptProvider voiceTranscriptProvider;

    [Header("Keyboard Demo")]
    [SerializeField] private KeyCode holdToTalkKey = KeyCode.V;

    private bool wasHeldLastFrame = false;

    private void Update()
    {
        bool held = IsHoldToTalkPressed();

        if (held && !wasHeldLastFrame)
        {
            wasHeldLastFrame = true;
            voiceTranscriptProvider.ClearTranscript();
            microphoneRecorder.StartRecording();
            return;
        }

        if (!held && wasHeldLastFrame)
        {
            wasHeldLastFrame = false;
            byte[] wav = microphoneRecorder.StopRecordingAndGetWav();
            azureSpeechSTTClient.TranscribeWav(
                wav,
                onSuccess: transcript =>
                {
                    Debug.Log("[VoiceInputController] Transcript = " + transcript);
                    voiceTranscriptProvider.SetTranscript(transcript);

                    LLMDemoRunner runner = FindFirstObjectByType<LLMDemoRunner>();
                    if (runner != null)
                    {
                        Debug.Log("[VoiceInputController] Auto-triggering LLM after transcription.");
                        runner.RunDemo();
                    }
                    else
                    {
                        Debug.LogWarning("[VoiceInputController] No LLMDemoRunner found.");
                    }
                },
                onError: error =>
                {
                    Debug.LogError("[VoiceInputController] Azure STT error:\n" + error);
                });
            return;
        }

        wasHeldLastFrame = held;
    }

    private bool IsHoldToTalkPressed()
    {
        if (Keyboard.current != null)
        {
            switch (holdToTalkKey)
            {
                case KeyCode.V:
                    return Keyboard.current.vKey.isPressed;
                case KeyCode.Space:
                    return Keyboard.current.spaceKey.isPressed;
                default:
                    break;
            }
        }

        return Input.GetKey(holdToTalkKey);
    }
}
