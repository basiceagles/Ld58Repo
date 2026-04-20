using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField] private float mouseSensitivity = 100f;
    [SerializeField] private float minWaitAngle = -90f;
    [SerializeField] private float maxWaitAngle = 90f;
    [SerializeField] private Transform playerBody;

    private float xRotation = 0f;

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        float savedSensitivity = PlayerPrefs.GetFloat("Sensitivity", 100f);
        SetSensitivity(savedSensitivity);
        Debug.Log($"[CameraController] Applied Mouse Sensitivity: {mouseSensitivity}");
    }



    private void Update()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, minWaitAngle, maxWaitAngle);

        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        playerBody.Rotate(Vector3.up * mouseX);
    }

    public void SetSensitivity(float value)
    {
        mouseSensitivity = value;
    }
}

