using UnityEngine;

public class HorizontalParallax : MonoBehaviour
{
    [SerializeField] private Camera targetCamera;
    [SerializeField, Range(0f, 1f)] private float horizontalMultiplier = 0.2f;

    private Vector3 initialPosition;
    private float initialCameraX;

    private void Awake()
    {
        initialPosition = transform.position;

        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        if (targetCamera != null)
        {
            initialCameraX = targetCamera.transform.position.x;
        }
    }

    private void LateUpdate()
    {
        if (targetCamera == null)
        {
            return;
        }

        Vector3 position = initialPosition;
        position.x += (targetCamera.transform.position.x - initialCameraX) * horizontalMultiplier;
        transform.position = position;
    }
}
