using UnityEngine;

public class TriggerTestMover : MonoBehaviour
{
    [SerializeField] private float speed = 2f;

    private void Start()
    {
        Debug.Log("[TriggerTestMover] Script started on: " + gameObject.name);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.W))
        {
            Debug.Log("[TriggerTestMover] W pressed");
        }

        float x = 0f;
        float z = 0f;

        if (Input.GetKey(KeyCode.A)) x = -1f;
        if (Input.GetKey(KeyCode.D)) x = 1f;
        if (Input.GetKey(KeyCode.W)) z = 1f;
        if (Input.GetKey(KeyCode.S)) z = -1f;

        Vector3 movement = new Vector3(x, 0f, z) * speed * Time.deltaTime;
        transform.Translate(movement, Space.World);
    }
}