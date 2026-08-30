using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class ResponsiveSpriteBackground : MonoBehaviour
{
    [SerializeField]
    private Camera targetCamera;

    private SpriteRenderer spriteRenderer;

    private int lastScreenWidth;
    private int lastScreenHeight;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }
    }

    private void Start()
    {
        FitToCamera();
    }

    private void Update()
    {
        if (
            Screen.width != lastScreenWidth ||
            Screen.height != lastScreenHeight
        )
        {
            FitToCamera();
        }
    }

    private void FitToCamera()
    {
        if (
            targetCamera == null ||
            spriteRenderer == null ||
            spriteRenderer.sprite == null
        )
        {
            return;
        }

        lastScreenWidth = Screen.width;
        lastScreenHeight = Screen.height;

        float distance = Vector3.Dot(
            transform.position - targetCamera.transform.position,
            targetCamera.transform.forward
        );

        if (distance <= 0f)
        {
            return;
        }

        Vector3 bottomLeft = targetCamera.ViewportToWorldPoint(
            new Vector3(0f, 0f, distance)
        );

        Vector3 topRight = targetCamera.ViewportToWorldPoint(
            new Vector3(1f, 1f, distance)
        );

        float cameraWidth =
            Mathf.Abs(topRight.x - bottomLeft.x);

        float cameraHeight =
            Mathf.Abs(topRight.y - bottomLeft.y);

        Vector2 spriteSize =
            spriteRenderer.sprite.bounds.size;

        float scaleX =
            cameraWidth / spriteSize.x;

        float scaleY =
            cameraHeight / spriteSize.y;

        float finalScale =
            Mathf.Max(scaleX, scaleY);

        transform.localScale = new Vector3(
            finalScale,
            finalScale,
            1f
        );

        Vector3 center = targetCamera.ViewportToWorldPoint(
            new Vector3(0.5f, 0.5f, distance)
        );

        transform.position = new Vector3(
            center.x,
            center.y,
            transform.position.z
        );
    }
}