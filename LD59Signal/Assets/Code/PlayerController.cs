using SmallHedge.SoundManager;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float sprintSpeed = 10f;
    [SerializeField] private float gravity = -19.62f;
    [SerializeField] private float jumpHeight = 2f;
    [SerializeField] private float maxSprintTime = 3f;
    [SerializeField] private float sprintRecoveryRate = 1f;
    [SerializeField] private float groundDistance = 0.4f;
    [SerializeField] private LayerMask groundMask;
    [SerializeField] private float CurrentSpeed;

    [Header("Footsteps")]
    [Min(0f)]
    [SerializeField] private float walkFootstepInterval = 0.5f;
    [Min(0f)]
    [SerializeField] private float runFootstepInterval = 0.25f;

    private CharacterController controller;
    private Vector3 velocity;
    private bool isGrounded;

    private float footstepTimer;
    private bool isMoving;

    private float currentSprintTime;
    private bool isSprinting;

    private void Start()
    {
        controller = GetComponent<CharacterController>();
        currentSprintTime = maxSprintTime;
    }

    private void Update()
    {
        HandleGroundCheck();
        HandleMovementInput();
        HandleJumpInput();
        ApplyGravity();

        HandleFootsteps();

        CurrentSpeed = isSprinting ? sprintSpeed : walkSpeed;
    }

    private void HandleGroundCheck()
    {
        bool controllerGrounded = controller.isGrounded;
        bool sphereGrounded = Physics.SphereCast(transform.position, controller.radius * 0.5f, Vector3.down, out _, groundDistance, groundMask);
        isGrounded = controllerGrounded || sphereGrounded;

        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }
    }

    private void HandleMovementInput()
    {
        float x = Input.GetAxisRaw("Horizontal");
        float z = Input.GetAxisRaw("Vertical");

        Vector3 move = transform.right * x + transform.forward * z;
        move.Normalize();

        isMoving = move.sqrMagnitude > 0.001f;

        HandleSprint();

        float currentSpeed = isSprinting ? sprintSpeed : walkSpeed;
        controller.Move(move * currentSpeed * Time.deltaTime);
    }

    private void HandleFootsteps()
    {
        if (!isGrounded || !isMoving)
        {
            footstepTimer = 0f;
            return;
        }

        float interval = isSprinting ? runFootstepInterval : walkFootstepInterval;
        if (interval <= 0f)
        {
            return;
        }

        footstepTimer -= Time.deltaTime;
        if (footstepTimer <= 0f)
        {
            SoundManager.PlaySound(SoundType.FOOTSTEP);
            footstepTimer = interval;
        }
    }

    private void HandleSprint()
    {
        bool isTryingToSprint = Input.GetKey(KeyCode.LeftShift) && Input.GetAxisRaw("Vertical") > 0;

        if (isTryingToSprint)
        {
            if (currentSprintTime > 0)
            {
                isSprinting = true;
                currentSprintTime -= Time.deltaTime;
            }
            else
            {
                isSprinting = false;
            }
        }
        else
        {
            isSprinting = false;
            if (currentSprintTime < maxSprintTime)
            {
                currentSprintTime += Time.deltaTime * sprintRecoveryRate;
            }
        }
        
        currentSprintTime = Mathf.Clamp(currentSprintTime, 0f, maxSprintTime);
    }

    private void HandleJumpInput()
    {
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }
    }

    private void ApplyGravity()
    {
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    public float GetCurrentSprintTime()
    {
        return currentSprintTime;
    }

    public float GetMaxSprintTime()
    {
        return maxSprintTime;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, transform.position + Vector3.down * groundDistance);
    }
}
