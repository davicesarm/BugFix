using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// Gira o modelo 3D ao arrastar o dedo/mouse na tela.
// Adicionado automaticamente pelo TranslateCards quando uma carta do tipo ShowModel3D é lida.
public class ModelDragRotate : MonoBehaviour
{
    [Tooltip("Sensibilidade da rotação por pixel arrastado.")]
    [SerializeField]
    private float rotationSpeed = 0.3f;

    [SerializeField]
    private bool invertY = false;

    private bool isDragging;
    private Vector2 lastPointerPosition;

    private void Update()
    {
        if (!TryGetPointerState(out Vector2 currentPosition, out bool pressedThisFrame, out bool isPressed))
        {
            isDragging = false;
            return;
        }

        if (pressedThisFrame)
        {
            isDragging = true;
            lastPointerPosition = currentPosition;
            return;
        }

        if (!isPressed)
        {
            isDragging = false;
            return;
        }

        if (!isDragging)
            return;

        Vector2 delta = currentPosition - lastPointerPosition;
        lastPointerPosition = currentPosition;

        float yaw = delta.x * rotationSpeed;
        float pitch = delta.y * rotationSpeed * (invertY ? 1f : -1f);

        // Gira em torno do eixo vertical do mundo (yaw) e do eixo local horizontal (pitch),
        // como um "arcball" simples — dá pra virar o modelo em qualquer direção.
        transform.Rotate(Vector3.up, -yaw, Space.World);
        transform.Rotate(Vector3.right, pitch, Space.Self);
    }

    private bool TryGetPointerState(out Vector2 position, out bool pressedThisFrame, out bool isPressed)
    {
        position = Vector2.zero;
        pressedThisFrame = false;
        isPressed = false;

#if ENABLE_INPUT_SYSTEM
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
        {
            position = Touchscreen.current.primaryTouch.position.ReadValue();
            pressedThisFrame = Touchscreen.current.primaryTouch.press.wasPressedThisFrame;
            isPressed = true;
            return true;
        }

        if (Mouse.current != null && Mouse.current.leftButton.isPressed)
        {
            position = Mouse.current.position.ReadValue();
            pressedThisFrame = Mouse.current.leftButton.wasPressedThisFrame;
            isPressed = true;
            return true;
        }

        return false;
#else
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            position = touch.position;
            pressedThisFrame = touch.phase == TouchPhase.Began;
            isPressed = true;
            return true;
        }

        if (Input.GetMouseButton(0))
        {
            position = Input.mousePosition;
            pressedThisFrame = Input.GetMouseButtonDown(0);
            isPressed = true;
            return true;
        }

        return false;
#endif
    }
}
