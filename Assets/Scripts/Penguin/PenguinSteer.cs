using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
[DefaultExecutionOrder(50)]
public class PenguinSteer : MonoBehaviour
{
    [SerializeField] float turnSpeedDegPerSec = 280f;
    [SerializeField] float maxRotationDegreesPerSec = 340f;
    [SerializeField] float groundedRayLength = 7f;
    [SerializeField] float raycastHeightOffset = 0.35f;
    [SerializeField] float groundSampleWidth = 0.5f;
    [SerializeField] LayerMask groundMask = ~0;
    [SerializeField] float groundNormalSmooth = 32f;
    [SerializeField] float targetRotationSmooth = 11f;
    [SerializeField] float minVelocityForForward = 0.85f;
    [SerializeField] float forwardFollowLerp = 3.2f;
    [SerializeField] float leanNoseTowardIceDegrees = 0f;

    Rigidbody _rb;
    Vector3 _surfaceFlatForward;
    Vector3 _smoothGroundUp = Vector3.up;
    Quaternion _smoothedTargetRot;
    bool _hasForward;

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        Vector3 f = Vector3.ProjectOnPlane(transform.forward, Vector3.up);
        if (f.sqrMagnitude > 1e-4f)
        {
            _surfaceFlatForward = f.normalized;
            _hasForward = true;
        }

        _smoothGroundUp = transform.up.sqrMagnitude > 0.1f ? transform.up.normalized : Vector3.up;
        _smoothedTargetRot = transform.rotation;
    }

    public static float ReadSteerInput()
    {
        var k = Keyboard.current;
        if (k != null)
        {
            float h = 0f;
            if (k.leftArrowKey.isPressed || k.qKey.isPressed || k.aKey.isPressed)
                h -= 1f;
            if (k.rightArrowKey.isPressed || k.dKey.isPressed)
                h += 1f;
            if (!Mathf.Approximately(h, 0f))
                return Mathf.Clamp(h, -1f, 1f);
        }

        var pad = Gamepad.current;
        if (pad != null)
        {
            float lx = pad.leftStick.x.ReadValue();
            if (Mathf.Abs(lx) > 0.15f)
                return Mathf.Clamp(lx, -1f, 1f);
        }

        return 0f;
    }

    bool TryGroundNormal(Vector3 origin, out Vector3 averagedNormal)
    {
        averagedNormal = Vector3.up;
        if (!Physics.Raycast(origin, Vector3.down, out RaycastHit mid, groundedRayLength, groundMask,
                QueryTriggerInteraction.Ignore))
            return false;

        Vector3 n = mid.normal;
        int count = 1;

        Vector3 tangent = Vector3.Cross(n, Vector3.up);
        if (tangent.sqrMagnitude < 1e-5f)
            tangent = Vector3.Cross(n, Vector3.forward);
        tangent.Normalize();

        float w = groundSampleWidth;
        if (Physics.Raycast(origin + tangent * w, Vector3.down, out RaycastHit h1, groundedRayLength, groundMask,
                QueryTriggerInteraction.Ignore))
        {
            n += h1.normal;
            count++;
        }

        if (Physics.Raycast(origin - tangent * w, Vector3.down, out RaycastHit h2, groundedRayLength, groundMask,
                QueryTriggerInteraction.Ignore))
        {
            n += h2.normal;
            count++;
        }

        averagedNormal = (n / count).normalized;
        return true;
    }

    void FixedUpdate()
    {
        float input = ReadSteerInput();

        Vector3 origin = transform.position + Vector3.up * raycastHeightOffset;
        if (!TryGroundNormal(origin, out Vector3 rawNormal))
        {
            _smoothGroundUp = Vector3.Slerp(_smoothGroundUp, Vector3.up, 5f * Time.fixedDeltaTime);
            if (Mathf.Abs(input) < 0.01f)
                return;
            float yaw = input * turnSpeedDegPerSec * Time.fixedDeltaTime;
            Quaternion target = _rb.rotation * Quaternion.Euler(0f, yaw, 0f);
            _rb.MoveRotation(StepRotation(_rb.rotation, target));
            _rb.angularVelocity = Vector3.zero;
            return;
        }

        _smoothGroundUp = Vector3.Slerp(_smoothGroundUp, rawNormal,
            Mathf.Clamp01(groundNormalSmooth * Time.fixedDeltaTime)).normalized;

        Vector3 up = _smoothGroundUp;
        Vector3 velF = Vector3.ProjectOnPlane(_rb.linearVelocity, up);
        Vector3 tfF = Vector3.ProjectOnPlane(transform.forward, up);
        if (tfF.sqrMagnitude < 1e-5f)
            tfF = Vector3.ProjectOnPlane(Vector3.forward, up);
        tfF.Normalize();

        if (!_hasForward)
        {
            _surfaceFlatForward = tfF;
            _hasForward = true;
        }

        Vector3 hint = velF.sqrMagnitude > minVelocityForForward * minVelocityForForward
            ? velF.normalized
            : _surfaceFlatForward;
        hint = Vector3.ProjectOnPlane(hint, up);
        if (hint.sqrMagnitude < 1e-5f)
            hint = tfF;
        hint.Normalize();

        _surfaceFlatForward = Vector3.ProjectOnPlane(_surfaceFlatForward, up);
        if (_surfaceFlatForward.sqrMagnitude < 1e-5f)
            _surfaceFlatForward = hint;
        _surfaceFlatForward.Normalize();

        if (Mathf.Abs(input) > 0.01f)
        {
            float yaw = input * turnSpeedDegPerSec * Time.fixedDeltaTime;
            _surfaceFlatForward = Quaternion.AngleAxis(yaw, up) * _surfaceFlatForward;
        }
        else
        {
            _surfaceFlatForward = Vector3.Slerp(_surfaceFlatForward, hint, forwardFollowLerp * Time.fixedDeltaTime);
        }

        _surfaceFlatForward = Vector3.ProjectOnPlane(_surfaceFlatForward, up);
        if (_surfaceFlatForward.sqrMagnitude < 1e-5f)
            _surfaceFlatForward = hint;
        _surfaceFlatForward.Normalize();

        if (Mathf.Abs(Vector3.Dot(_surfaceFlatForward, up)) > 0.96f)
            _surfaceFlatForward = Vector3.ProjectOnPlane(tfF, up).normalized;

        Quaternion idealRot = Quaternion.LookRotation(_surfaceFlatForward, up);
        idealRot *= Quaternion.AngleAxis(leanNoseTowardIceDegrees, Vector3.right);
        float blend = Mathf.Clamp01(targetRotationSmooth * Time.fixedDeltaTime);
        _smoothedTargetRot = Quaternion.Slerp(_smoothedTargetRot, idealRot, blend);
        _rb.MoveRotation(StepRotation(_rb.rotation, _smoothedTargetRot));
        _rb.angularVelocity = Vector3.zero;
    }

    Quaternion StepRotation(Quaternion from, Quaternion to)
    {
        float maxDeg = maxRotationDegreesPerSec * Time.fixedDeltaTime;
        return Quaternion.RotateTowards(from, to, maxDeg);
    }
}
