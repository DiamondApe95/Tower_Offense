using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{

    [Header("Movement Bounds")]
    public float minX = -20f;
    public float maxX = 20f;
    public float minZ = -20f;
    public float maxZ = 20f;


    [Header("Movement")]
    public float moveSpeed = 20f;
    public float zoomSpeed = 10f;
    public float minHeight = 5f;
    public float maxHeight = 30f;

    [Header("Rotation")]
    public float rotationSpeed = 100f;

    private Camera cam;

    private void Awake()
    {
        cam = GetComponentInChildren<Camera>();
    }

    private void Update()
    {
        HandleMovement();
        HandleZoom();
        HandleRotation();
    }

    private void HandleMovement()
    {
        Vector2 input = Vector2.zero;

        if (Keyboard.current != null)
        {
            if (Keyboard.current.wKey.isPressed) input.y += 1;
            if (Keyboard.current.sKey.isPressed) input.y -= 1;
            if (Keyboard.current.aKey.isPressed) input.x -= 1;
            if (Keyboard.current.dKey.isPressed) input.x += 1;
        }

        Vector3 dir = new Vector3(input.x, 0f, input.y);
        Vector3 move = Quaternion.Euler(0, transform.eulerAngles.y, 0) * dir;

        transform.position += move * moveSpeed * Time.deltaTime;
    }

    private void HandleZoom()
    {
        if (Mouse.current == null || cam == null) return;

        float scroll = Mouse.current.scroll.ReadValue().y;
        if (Mathf.Abs(scroll) < 0.01f) return;

        Vector3 pos = cam.transform.localPosition;
        pos.y -= scroll * zoomSpeed * Time.deltaTime;
        pos.y = Mathf.Clamp(pos.y, minHeight, maxHeight);
        cam.transform.localPosition = pos;
    }

    private void HandleRotation()
    {
        if (Mouse.current == null) return;

        if (Mouse.current.rightButton.isPressed)
        {
            float delta = Mouse.current.delta.ReadValue().x;
            transform.Rotate(Vector3.up, delta * rotationSpeed * Time.deltaTime);
        }
    }
}
