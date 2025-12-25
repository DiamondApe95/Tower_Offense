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
    public float edgeScrollSpeed = 15f;
    public float edgeScrollThreshold = 20f; // Pixel vom Bildschirmrand

    [Header("Zoom")]
    public float zoomSpeed = 5f;
    public float minHeight = 5f;
    public float maxHeight = 50f;

    [Header("Rotation")]
    public float rotationSpeed = 100f;

    [Header("Edge Scrolling")]
    public bool enableEdgeScrolling = true;

    private Camera cam;

    private void Awake()
    {
        cam = GetComponent<Camera>();
        if (cam == null)
        {
            cam = GetComponentInChildren<Camera>();
        }
    }

    private void Update()
    {
        HandleKeyboardMovement();
        HandleEdgeScrolling();
        HandleZoom();
        HandleRotation();
        ClampPosition();
    }

    private void HandleKeyboardMovement()
    {
        Vector2 input = Vector2.zero;

        if (Keyboard.current != null)
        {
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) input.y += 1;
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) input.y -= 1;
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) input.x -= 1;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) input.x += 1;
        }

        if (input.sqrMagnitude < 0.01f) return;

        input = input.normalized;
        Vector3 dir = new Vector3(input.x, 0f, input.y);
        Vector3 move = Quaternion.Euler(0, transform.eulerAngles.y, 0) * dir;

        transform.position += move * moveSpeed * Time.deltaTime;
    }

    private void HandleEdgeScrolling()
    {
        if (!enableEdgeScrolling || Mouse.current == null) return;

        Vector2 mousePos = Mouse.current.position.ReadValue();
        Vector2 input = Vector2.zero;

        // Edge-Scrolling: Maus am Bildschirmrand bewegt die Kamera
        if (mousePos.x < edgeScrollThreshold) input.x = -1;
        if (mousePos.x > Screen.width - edgeScrollThreshold) input.x = 1;
        if (mousePos.y < edgeScrollThreshold) input.y = -1;
        if (mousePos.y > Screen.height - edgeScrollThreshold) input.y = 1;

        if (input.sqrMagnitude < 0.01f) return;

        Vector3 dir = new Vector3(input.x, 0f, input.y);
        Vector3 move = Quaternion.Euler(0, transform.eulerAngles.y, 0) * dir;

        transform.position += move * edgeScrollSpeed * Time.deltaTime;
    }

    private void HandleZoom()
    {
        if (Mouse.current == null) return;

        float scroll = Mouse.current.scroll.ReadValue().y;
        if (Mathf.Abs(scroll) < 0.01f) return;

        // Zoom durch Höhe ändern
        Vector3 pos = transform.position;
        pos.y -= scroll * zoomSpeed * 0.1f;
        pos.y = Mathf.Clamp(pos.y, minHeight, maxHeight);
        transform.position = pos;
    }

    private void HandleRotation()
    {
        if (Mouse.current == null) return;

        // Rotation mit mittlerer Maustaste oder Alt + linke Maustaste
        bool canRotate = Mouse.current.middleButton.isPressed;
        if (Keyboard.current != null && Keyboard.current.altKey.isPressed && Mouse.current.leftButton.isPressed)
        {
            canRotate = true;
        }

        if (canRotate)
        {
            float delta = Mouse.current.delta.ReadValue().x;
            transform.Rotate(Vector3.up, delta * rotationSpeed * Time.deltaTime);
        }
    }

    private void ClampPosition()
    {
        Vector3 pos = transform.position;
        pos.x = Mathf.Clamp(pos.x, minX, maxX);
        pos.z = Mathf.Clamp(pos.z, minZ, maxZ);
        transform.position = pos;
    }
}
