using UnityEngine;

public class CameraFollowsCursor : MonoBehaviour
{
    public float sensitivity = 0.001f;
    public float smoothSpeed = 5f;
    public float maxOffset = 0.3f;
    public bool inverse = true; // Toggle for inverse movement
    
    private Vector3 originalPosition;
    private Vector3 targetOffset;

    void Start()
    {
        originalPosition = transform.position;
    }

    void Update()
    {
        Vector2 screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        Vector2 cursorPosition = Input.mousePosition;
        
        // Calculate normalized offset from center
        Vector2 normalizedOffset = new Vector2(
            (cursorPosition.x - screenCenter.x) / screenCenter.x,
            (cursorPosition.y - screenCenter.y) / screenCenter.y
        );
        
        // Apply inverse toggle
        if (inverse)
        {
            // Inverse movement
            targetOffset = new Vector3(
                -normalizedOffset.x * sensitivity,
                -normalizedOffset.y * sensitivity,
                0
            );
        }
        else
        {
            // Normal movement (same direction as cursor)
            targetOffset = new Vector3(
                normalizedOffset.x * sensitivity,
                normalizedOffset.y * sensitivity,
                0
            );
        }
        
        // Clamp the target offset
        targetOffset.x = Mathf.Clamp(targetOffset.x, -maxOffset, maxOffset);
        targetOffset.y = Mathf.Clamp(targetOffset.y, -maxOffset, maxOffset);
        
        // Smoothly move the camera to the target position
        Vector3 targetPosition = originalPosition + targetOffset;
        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * smoothSpeed);
    }
}