using UnityEngine;

public class ContextPrintTester : MonoBehaviour
{
    [SerializeField] private InputContextAssembler inputContextAssembler;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (inputContextAssembler != null)
            {
                inputContextAssembler.PrintFullContext();
            }
            else
            {
                Debug.LogWarning("[ContextPrintTester] No InputContextAssembler assigned.");
            }
        }
    }
}