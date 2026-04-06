using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using XRCommonUsages = UnityEngine.XR.CommonUsages;
using XRInputDevice = UnityEngine.XR.InputDevice;
using XRInputDeviceCharacteristics = UnityEngine.XR.InputDeviceCharacteristics;
using XRInputDevices = UnityEngine.XR.InputDevices;

public class LLMDemoRunner : MonoBehaviour
{
    private enum SummonButton
    {
        PrimaryAction,
        Trigger,
        SecondaryButton
    }

    [SerializeField] private DemoContextProvider contextProvider;
    [SerializeField] private OpenAIResponseClient openAIClient;
    [SerializeField] private WorldInfoPanelController worldInfoPanelController;
    [SerializeField] private SpriteActionController spriteActionController;
    [SerializeField] private SummonButton summonButton = SummonButton.PrimaryAction;
    [SerializeField] private float officialFeedWaitTimeoutSeconds = 20f;

    private readonly List<XRInputDevice> xrDevices = new List<XRInputDevice>();
    private bool isWaitingForOfficialFeed;

    private void Start()
    {
        Debug.Log("[LLMDemoRunner] Started on: " + gameObject.name);
    }

    private void Update()
    {
        if (DidPressRunKey())
        {
            Debug.Log("[LLMDemoRunner] Summon input detected");
            RunDemo();
        }
    }

    private bool DidPressRunKey()
    {
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            return true;
        }

        XRInputDevices.GetDevicesWithCharacteristics(
            XRInputDeviceCharacteristics.HeldInHand |
            XRInputDeviceCharacteristics.Controller,
            xrDevices);

        for (int i = 0; i < xrDevices.Count; i++)
        {
            if (WasSummonPressed(xrDevices[i]))
            {
                return true;
            }
        }

        return Input.GetKeyDown(KeyCode.Space);
    }

    private bool WasSummonPressed(XRInputDevice controller)
    {
        switch (summonButton)
        {
            case SummonButton.Trigger:
                return controller.TryGetFeatureValue(XRCommonUsages.triggerButton, out bool triggerPressed) && triggerPressed;

            case SummonButton.SecondaryButton:
                return controller.TryGetFeatureValue(XRCommonUsages.secondaryButton, out bool secondaryPressed) && secondaryPressed;

            default:
                return controller.TryGetFeatureValue(XRCommonUsages.primaryButton, out bool primaryPressed) && primaryPressed;
        }
    }

    public void RunDemo()
    {
        if (contextProvider == null || openAIClient == null || worldInfoPanelController == null || spriteActionController == null)
        {
            Debug.LogWarning("[LLMDemoRunner] Missing references.");
            return;
        }

        if (!contextProvider.IsOfficialFeedReady())
        {
            if (!isWaitingForOfficialFeed)
            {
                Debug.LogWarning("[LLMDemoRunner] Official feed still loading. Waiting to auto-run...");
                StartCoroutine(WaitForOfficialFeedAndRun());
            }

            return;
        }

        RunDemoInternal();
    }

    private System.Collections.IEnumerator WaitForOfficialFeedAndRun()
    {
        isWaitingForOfficialFeed = true;
        float waitedSeconds = 0f;

        while (contextProvider != null && !contextProvider.IsOfficialFeedReady() && waitedSeconds < officialFeedWaitTimeoutSeconds)
        {
            waitedSeconds += Time.deltaTime;
            yield return null;
        }

        isWaitingForOfficialFeed = false;

        if (contextProvider == null)
        {
            yield break;
        }

        if (!contextProvider.IsOfficialFeedReady())
        {
            Debug.LogWarning("[LLMDemoRunner] Official feed did not finish loading in time.");
            yield break;
        }

        Debug.LogWarning("[LLMDemoRunner] Official feed ready. Auto-running now.");
        RunDemoInternal();
    }

    private void RunDemoInternal()
    {
        if (contextProvider == null || openAIClient == null || worldInfoPanelController == null || spriteActionController == null)
        {
            Debug.LogWarning("[LLMDemoRunner] Missing references.");
            return;
        }

        string fullContext = contextProvider.BuildPrompt();
        Debug.Log("[LLMDemoRunner] Prompt:\n" + fullContext);

        openAIClient.RequestResponse(
            fullContext,
            onSuccess: result =>
            {
                Debug.Log("[LLMDemoRunner] LLM success: " + result.title);

                SpriteStateData state = new SpriteStateData
                {
                    Mode = SpriteMode.Info,
                    Title = result.title,
                    Body = result.body,
                    ShowPanel = true
                };

                worldInfoPanelController.ApplyState(state);
                spriteActionController.PlayAction(result.action);
            },
            onError: error =>
            {
                Debug.LogError("[LLMDemoRunner] LLM error:\n" + error);
            }
        );
    }
}
