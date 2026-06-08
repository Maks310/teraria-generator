using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerPlanetController : MonoBehaviour
{
    [Header("Planet")]
    public PlanetGenerator planet;
    public Transform cameraPivot;

    [Header("Movement")]
    public float moveSpeed = 8f;
    public float sprintMultiplier = 1.6f;
    public float jumpSpeed = 7f;
    public float gravityStrength = 28f;
    public float orientationSharpness = 10f;
    public float groundCheckDistance = 1.4f;
    public LayerMask groundMask = ~0;

    [Header("Input")]
    public string horizontalAxis = "Horizontal";
    public string verticalAxis = "Vertical";
    public KeyCode sprintKey = KeyCode.LeftShift;
    public KeyCode jumpKey = KeyCode.Space;

    private Rigidbody _rigidbody;
    private bool _grounded;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _rigidbody.useGravity = false;
        _rigidbody.freezeRotation = true;
    }

    private void FixedUpdate()
    {
        if (planet == null)
        {
            return;
        }

        Vector3 up = (transform.position - planet.transform.position).normalized;
        Vector3 gravity = -up * gravityStrength;
        _rigidbody.AddForce(gravity, ForceMode.Acceleration);

        AlignToSurface(up);
        MoveOnTangent(up);
    }

    private void Update()
    {
        if (planet == null)
        {
            return;
        }

        Vector3 up = (transform.position - planet.transform.position).normalized;
        _grounded = Physics.Raycast(transform.position + up * 0.25f, -up, groundCheckDistance, groundMask, QueryTriggerInteraction.Ignore);
        if (_grounded && Input.GetKeyDown(jumpKey))
        {
            _rigidbody.AddForce(up * jumpSpeed, ForceMode.VelocityChange);
        }
    }

    private void AlignToSurface(Vector3 up)
    {
        Quaternion targetRotation = Quaternion.FromToRotation(transform.up, up) * transform.rotation;
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 1f - Mathf.Exp(-orientationSharpness * Time.fixedDeltaTime));
    }

    private void MoveOnTangent(Vector3 up)
    {
        float horizontal = Input.GetAxis(horizontalAxis);
        float vertical = Input.GetAxis(verticalAxis);
        Transform reference = cameraPivot != null ? cameraPivot : transform;
        Vector3 forward = Vector3.ProjectOnPlane(reference.forward, up).normalized;
        Vector3 right = Vector3.ProjectOnPlane(reference.right, up).normalized;
        if (forward.sqrMagnitude < 0.001f)
        {
            forward = Vector3.ProjectOnPlane(transform.forward, up).normalized;
        }
        Vector3 desired = (forward * vertical + right * horizontal);
        desired = Vector3.ClampMagnitude(desired, 1f);
        float speed = Input.GetKey(sprintKey) ? moveSpeed * sprintMultiplier : moveSpeed;

        Vector3 velocity = _rigidbody.velocity;
        Vector3 radialVelocity = Vector3.Project(velocity, up);
        Vector3 tangentVelocity = desired * speed;
        _rigidbody.velocity = tangentVelocity + radialVelocity;
    }
}
