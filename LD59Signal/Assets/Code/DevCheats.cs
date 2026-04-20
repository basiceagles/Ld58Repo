using UnityEngine;

public class DevCheats : MonoBehaviour
{
    public static bool IsActive { get; private set; }

    [SerializeField] private float flySpeed = 25f;
    private CharacterController controller;
    private PlayerController playerController;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        playerController = GetComponent<PlayerController>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1))
        {
            IsActive = !IsActive;
            
            if (playerController != null) playerController.enabled = !IsActive;
            if (controller != null) controller.enabled = !IsActive;
            
            Debug.Log(IsActive ? "<color=yellow>DEV MODE: ON (Flight, Infinite, Fast)</color>" : "<color=yellow>DEV MODE: OFF</color>");
        }

        if (IsActive)
        {
            HandleFlight();
        }
    }

    private void HandleFlight()
    {
        float x = Input.GetAxisRaw("Horizontal");
        float z = Input.GetAxisRaw("Vertical");
        float y = 0;
        if (Input.GetKey(KeyCode.Space)) y = 1;
        if (Input.GetKey(KeyCode.LeftShift)) y = -1;

        Vector3 move = transform.right * x + transform.forward * z + Vector3.up * y;
        transform.position += move.normalized * flySpeed * Time.deltaTime;
    }
}
