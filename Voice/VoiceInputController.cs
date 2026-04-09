using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.XR;

public class VoiceInputController : MonoBehaviour
{
    [SerializeField] private MicrophoneRecorder microphoneRecorder;
    [SerializeField] private AzureSpeechSTTClient azureOpenAITranscriptionClient;
    [SerializeField] private VoiceTranscriptProvider voiceTranscriptProvider;
    [SerializeField] private LLMDemoRunner llmDemoRunner;
    [SerializeField] private WorldInfoPanelController worldInfoPanelController;

    [Header("Optional Keyboard Demo")]
    [SerializeField] private bool enableKeyboardDemo = true;
    [SerializeField] private KeyCode holdToTalkKey = KeyCode.V;

    [Header("XR Hold To Talk")]
    [SerializeField] private bool enableXRHoldToTalk = true;
    [SerializeField] private bool preferRightHandTrigger = true;
    [SerializeField] private float xrTriggerThreshold = 0.55f;

    private bool isBusy = false;
    private bool wasHeldLastFrame = false;
    private bool wasXRHeldLastFrame = false;
    private bool hasLoggedMissingXRController = false;

    private void Start()
    {
        Debug.Log("[VoiceInputController] Started");
    }

    private void Update()
    {
        if (enableKeyboardDemo)
        {
            bool held = IsKeyboardHoldToTalkPressed();

            if (held && !wasHeldLastFrame)
            {
                BeginListening();
            }

            if (!held && wasHeldLastFrame)
            {
                StopListening();
            }

            wasHeldLastFrame = held;
        }

        if (enableXRHoldToTalk)
        {
            bool xrHeld = IsXRTriggerHeld();

            if (xrHeld && !wasXRHeldLastFrame)
            {
                Debug.Log("[VoiceInputController] XR trigger hold started");
                BeginListening();
            }

            if (!xrHeld && wasXRHeldLastFrame)
            {
                Debug.Log("[VoiceInputController] XR trigger hold ended");
                StopListening();
            }

            wasXRHeldLastFrame = xrHeld;
        }
    }

    public void BeginListening()
    {
        if (isBusy)
        {
            Debug.Log("[VoiceInputController] Busy, ignore BeginListening.");
            return;
        }

        if (microphoneRecorder == null)
        {
            Debug.LogError("[VoiceInputController] microphoneRecorder is null.");
            return;
        }

        if (voiceTranscriptProvider != null)
        {
            voiceTranscriptProvider.ClearTranscript();
        }

        if (worldInfoPanelController != null)
        {
            worldInfoPanelController.ShowListeningFeedback();
        }

        Debug.Log("[VoiceInputController] BeginListening called");
        microphoneRecorder.StartRecording();
    }

    public void StopListening()
    {
        if (isBusy)
        {
            Debug.Log("[VoiceInputController] Busy, ignore StopListening.");
            return;
        }

        if (microphoneRecorder == null)
        {
            Debug.LogError("[VoiceInputController] microphoneRecorder is null.");
            return;
        }

        if (azureOpenAITranscriptionClient == null)
        {
            Debug.LogError("[VoiceInputController] azureOpenAITranscriptionClient is null.");
            if (worldInfoPanelController != null)
            {
                worldInfoPanelController.ShowErrorFeedback("Transcription service is not connected.");
            }
            return;
        }

        Debug.Log("[VoiceInputController] StopListening called");

        if (worldInfoPanelController != null)
        {
            worldInfoPanelController.ShowProcessingFeedback();
        }

        byte[] wav = microphoneRecorder.StopRecordingAndGetWav();
        if (wav == null || wav.Length == 0)
        {
            Debug.LogWarning("[VoiceInputController] No wav captured.");
            if (worldInfoPanelController != null)
            {
                worldInfoPanelController.ShowErrorFeedback("No voice captured. Try holding the trigger a bit longer.");
            }
            return;
        }

        isBusy = true;

        azureOpenAITranscriptionClient.TranscribeWav(
            wav,
            onSuccess: transcript =>
            {
                Debug.Log("[VoiceInputController] Transcript = " + transcript);

                if (voiceTranscriptProvider != null)
                {
                    voiceTranscriptProvider.SetTranscript(transcript);
                }

                if (llmDemoRunner != null)
                {
                    Debug.Log("[VoiceInputController] Auto-triggering LLM after transcription.");
                    llmDemoRunner.RunDemo();
                }
                else
                {
                    Debug.LogWarning("[VoiceInputController] llmDemoRunner is null.");
                }

                if (worldInfoPanelController != null)
                {
                    worldInfoPanelController.ClearVoiceFeedback();
                }

                isBusy = false;
            },
            onError: error =>
            {
                Debug.LogError("[VoiceInputController] Transcription error:\n" + error);
                if (worldInfoPanelController != null)
                {
                    worldInfoPanelController.ShowErrorFeedback("Transcription failed. Please try again.");
                }
                isBusy = false;
            });
    }

    private bool IsXRTriggerHeld()
    {
        XRController preferredController = null;
        XRController fallbackController = null;

        foreach (InputDevice device in InputSystem.devices)
        {
            if (device is not XRController xrController || !device.added || !device.enabled)
            {
                continue;
            }

            if (preferRightHandTrigger && IsRightHandController(xrController))
            {
                preferredController = xrController;
                break;
            }

            fallbackController ??= xrController;
        }

        XRController controllerToRead = preferredController != null ? preferredController : fallbackController;
        if (controllerToRead == null)
        {
            if (!hasLoggedMissingXRController)
            {
                Debug.LogWarning("[VoiceInputController] No XRController detected by the Input System.");
                hasLoggedMissingXRController = true;
            }

            return false;
        }

        hasLoggedMissingXRController = false;

        AxisControl triggerAxis = controllerToRead.TryGetChildControl<AxisControl>("trigger");
        if (triggerAxis != null && triggerAxis.ReadValue() >= xrTriggerThreshold)
        {
            return true;
        }

        ButtonControl triggerButton = controllerToRead.TryGetChildControl<ButtonControl>("triggerPressed");
        if (triggerButton != null && triggerButton.isPressed)
        {
            return true;
        }

        triggerButton = controllerToRead.TryGetChildControl<ButtonControl>("triggerButton");
        return triggerButton != null && triggerButton.isPressed;
    }

    private static bool IsRightHandController(InputDevice device)
    {
        string usages = string.Join(",", device.usages);
        return usages.Contains("RightHand");
    }

    private bool IsKeyboardHoldToTalkPressed()
    {
        if (Keyboard.current == null)
        {
            return false;
        }

        Key key = holdToTalkKey switch
        {
            KeyCode.V => Key.V,
            KeyCode.Space => Key.Space,
            KeyCode.LeftShift => Key.LeftShift,
            KeyCode.RightShift => Key.RightShift,
            _ => Key.None
        };

        if (key == Key.None)
        {
            Debug.LogWarning("[VoiceInputController] holdToTalkKey is not mapped for the Input System: " + holdToTalkKey);
            return false;
        }

        return Keyboard.current[key].isPressed;
    }
}
