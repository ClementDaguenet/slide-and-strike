using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
[DefaultExecutionOrder(100)]
public class PenguinSlideDrive : MonoBehaviour
{
    [SerializeField] float alongSlopeAcceleration = 6.5f;
    [SerializeField] float groundedRayLength = 8f;
    [SerializeField] float raycastOriginHeight = 0.12f;
    [SerializeField] float groundSampleWidth = 0.48f;
    [SerializeField] LayerMask groundMask = ~0;
    [SerializeField] float maxKmhForIdleNudge = 0.85f;
    [SerializeField] float idleNudgeAcceleration = 5.5f;
    [SerializeField] float steerAssistAcceleration = 34f;
    [SerializeField] float steerVelocityTurnAcceleration = 26f;
    [SerializeField] float steerForwardPullAcceleration = 4.2f;
    [SerializeField] float maxKmhForSteerAssist = 120f;
    [SerializeField] float groundSurfacePadding = 0f;
    [SerializeField] float maxUpwardVelocityWhenNearGround = 2.2f;
    [SerializeField] float groundProximityForClamp = 4.5f;
    [SerializeField] float outwardVelocityDamp = 0.94f;
    [SerializeField] float stickSpring = 0f;
    [SerializeField] float maxStickAcceleration = 0f;
    [SerializeField] float gapIgnoreBelow = 0.05f;
    [SerializeField] float groundedGapForJump = 0.38f;
    [SerializeField] float jumpVelocityChange = 4.8f;
    [SerializeField] float diveForwardVelocityChange = 3.2f;
    [SerializeField] float diveDownVelocityChange = 7f;
    [SerializeField] float jumpGroundClampGrace = 0.18f;

    Rigidbody _rb;
    CapsuleCollider _cap;
    bool _spacePressed;
    bool _grounded;
    bool _diveUsed;
    float _skipGroundClampUntil;
    Vector3 _lastGroundNormal = Vector3.up;

    public float PowerUpAccelerationMultiplier { get; set; } = 1f;
    public float InputSign { get; set; } = 1f;

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _cap = GetComponent<CapsuleCollider>();
    }

    void Update()
    {
        var k = Keyboard.current;
        if (k != null && k.spaceKey.wasPressedThisFrame)
            _spacePressed = true;
    }

    IEnumerator Start()
    {
        _rb.WakeUp();
        yield return null;
        SnapOntoGround();
    }

    void SnapOntoGround()
    {
        Vector3 p = transform.position;
        if (!Physics.Raycast(p + Vector3.up * 120f, Vector3.down, out RaycastHit hit, 260f, groundMask,
                QueryTriggerInteraction.Ignore))
            return;

        if (_cap == null)
            return;

        Vector3 pos = PenguinCapsulePlacement.PivotPositionForBottomAt(transform, _cap, hit.point, hit.normal,
            groundSurfacePadding);
        transform.position = pos;
        _rb.position = pos;
        _rb.linearVelocity = Vector3.zero;
        _rb.angularVelocity = Vector3.zero;
    }

    void FixedUpdate()
    {
        Vector3 origin = transform.position + Vector3.up * raycastOriginHeight;
        if (!TryAverageGround(origin, out RaycastHitData g))
        {
            _grounded = false;
            HandleSpaceAction(Vector3.up, false);
            return;
        }

        Vector3 normal = g.normal;
        _lastGroundNormal = normal;
        _grounded = IsGrounded(g);
        if (_grounded && Time.time >= _skipGroundClampUntil)
            _diveUsed = false;
        bool jumpedOrDived = HandleSpaceAction(normal, _grounded);
        Vector3 grav = Physics.gravity.sqrMagnitude > 1e-6f ? Physics.gravity : new Vector3(0f, -9.81f, 0f);
        Vector3 gPlane = Vector3.ProjectOnPlane(grav, normal);

        Vector3 alongSlide;
        if (gPlane.sqrMagnitude > 1e-6f)
        {
            Vector3 vPlane = Vector3.ProjectOnPlane(_rb.linearVelocity, normal);
            if (vPlane.sqrMagnitude > 1.5f)
                alongSlide = (gPlane.normalized * 0.78f + vPlane.normalized * 0.22f).normalized;
            else
                alongSlide = gPlane.normalized;
            _rb.AddForce(alongSlide * (alongSlopeAcceleration * PowerUpAccelerationMultiplier), ForceMode.Acceleration);
        }
        else
        {
            alongSlide = Vector3.ProjectOnPlane(transform.forward, normal);
            if (alongSlide.sqrMagnitude < 1e-5f)
                alongSlide = Vector3.ProjectOnPlane(Vector3.forward, normal);
            alongSlide.Normalize();
        }

        float kmh = _rb.linearVelocity.magnitude * 3.6f;
        if (kmh < maxKmhForIdleNudge && ReadIdleForwardInput() && alongSlide.sqrMagnitude > 1e-5f)
            _rb.AddForce(alongSlide * idleNudgeAcceleration, ForceMode.Acceleration);

        ApplySteerAssist(normal, kmh);

        if (_grounded && !jumpedOrDived && Time.time >= _skipGroundClampUntil)
        {
            StickDown(g);
            KillLiftAlongGroundNormal(normal);
            CapUpwardSlip(g.avgDistance);
        }
    }

    struct RaycastHitData
    {
        public Vector3 normal;
        public Vector3 surfacePoint;
        public float avgDistance;
    }

    bool TryAverageGround(Vector3 origin, out RaycastHitData data)
    {
        data = default;
        if (!Physics.Raycast(origin, Vector3.down, out RaycastHit mid, groundedRayLength, groundMask,
                QueryTriggerInteraction.Ignore))
            return false;

        Vector3 nSum = mid.normal;
        Vector3 pSum = mid.point;
        float dSum = mid.distance;
        int count = 1;

        Vector3 tangent = Vector3.Cross(mid.normal, Vector3.up);
        if (tangent.sqrMagnitude < 1e-5f)
            tangent = Vector3.Cross(mid.normal, Vector3.forward);
        tangent.Normalize();

        float w = groundSampleWidth;
        if (Physics.Raycast(origin + tangent * w, Vector3.down, out RaycastHit h1, groundedRayLength, groundMask,
                QueryTriggerInteraction.Ignore))
        {
            nSum += h1.normal;
            pSum += h1.point;
            dSum += h1.distance;
            count++;
        }

        if (Physics.Raycast(origin - tangent * w, Vector3.down, out RaycastHit h2, groundedRayLength, groundMask,
                QueryTriggerInteraction.Ignore))
        {
            nSum += h2.normal;
            pSum += h2.point;
            dSum += h2.distance;
            count++;
        }

        data.normal = (nSum / count).normalized;
        data.surfacePoint = pSum / count;
        data.avgDistance = dSum / count;
        return true;
    }

    bool IsGrounded(RaycastHitData g)
    {
        if (_cap == null)
            return g.avgDistance <= groundedGapForJump + raycastOriginHeight;

        Vector3 bottom = PenguinCapsulePlacement.GetWorldBottom(transform, _cap);
        float gap = Vector3.Dot(bottom - g.surfacePoint, g.normal);
        return gap <= groundedGapForJump;
    }

    bool HandleSpaceAction(Vector3 groundNormal, bool grounded)
    {
        if (!_spacePressed)
            return false;

        _spacePressed = false;
        if (grounded)
        {
            Jump(groundNormal);
            return true;
        }

        if (_diveUsed)
            return false;

        Dive();
        return true;
    }

    void Jump(Vector3 groundNormal)
    {
        _diveUsed = false;
        _skipGroundClampUntil = Time.time + jumpGroundClampGrace;

        Vector3 jumpDir = (groundNormal.normalized + Vector3.up * 0.7f).normalized;
        Vector3 v = _rb.linearVelocity;
        float outwardSpeed = Vector3.Dot(v, jumpDir);
        if (outwardSpeed < 0f)
            v -= jumpDir * outwardSpeed;
        _rb.linearVelocity = v;
        _rb.AddForce(jumpDir * jumpVelocityChange, ForceMode.VelocityChange);
    }

    void Dive()
    {
        _diveUsed = true;

        Vector3 forward = Vector3.ProjectOnPlane(transform.forward, _lastGroundNormal);
        if (forward.sqrMagnitude < 1e-5f)
            forward = Vector3.ProjectOnPlane(_rb.linearVelocity, Vector3.up);
        if (forward.sqrMagnitude < 1e-5f)
            forward = transform.forward;
        forward.Normalize();

        _rb.AddForce(forward * diveForwardVelocityChange + Vector3.down * diveDownVelocityChange, ForceMode.VelocityChange);
    }

    void StickDown(RaycastHitData g)
    {
        if (_cap == null)
            return;
        if (stickSpring <= 0f || maxStickAcceleration <= 0f)
            return;
        Vector3 bottom = PenguinCapsulePlacement.GetWorldBottom(transform, _cap);
        float gap = Vector3.Dot(bottom - g.surfacePoint, g.normal);
        if (gap <= gapIgnoreBelow)
            return;

        float a = Mathf.Min(gap * stickSpring, maxStickAcceleration);
        _rb.AddForce(-g.normal * a, ForceMode.Acceleration);
    }

    void ApplySteerAssist(Vector3 groundNormal, float kmh)
    {
        if (kmh > maxKmhForSteerAssist)
            return;

        float steer = PenguinSteer.ReadSteerInput() * InputSign;
        if (Mathf.Abs(steer) < 0.01f)
            return;

        Vector3 desiredForward = Vector3.ProjectOnPlane(transform.forward, groundNormal);
        if (desiredForward.sqrMagnitude < 1e-5f)
            return;
        desiredForward.Normalize();

        Vector3 vPlane = Vector3.ProjectOnPlane(_rb.linearVelocity, groundNormal);
        float speed = vPlane.magnitude;
        if (speed > 0.2f)
        {
            Vector3 desiredVelocity = desiredForward * speed;
            Vector3 correction = desiredVelocity - vPlane;
            Vector3 turnForce = Vector3.ClampMagnitude(correction * steerVelocityTurnAcceleration, steerAssistAcceleration);
            _rb.AddForce(turnForce, ForceMode.Acceleration);
        }

        _rb.AddForce(desiredForward * (steerForwardPullAcceleration * PowerUpAccelerationMultiplier), ForceMode.Acceleration);
    }

    void KillLiftAlongGroundNormal(Vector3 groundNormal)
    {
        groundNormal.Normalize();
        Vector3 v = _rb.linearVelocity;
        float lift = Vector3.Dot(v, groundNormal);
        if (lift <= 0.02f)
            return;
        v -= groundNormal * (lift * outwardVelocityDamp);
        _rb.linearVelocity = v;
    }

    void CapUpwardSlip(float hitDistance)
    {
        if (hitDistance > groundProximityForClamp)
            return;
        Vector3 v = _rb.linearVelocity;
        if (v.y <= maxUpwardVelocityWhenNearGround)
            return;
        v.y = Mathf.Min(Mathf.Lerp(v.y, maxUpwardVelocityWhenNearGround, 0.92f), maxUpwardVelocityWhenNearGround);
        _rb.linearVelocity = v;
    }

    static bool ReadIdleForwardInput()
    {
        var k = Keyboard.current;
        if (k == null)
            return false;
        return k.upArrowKey.isPressed || k.zKey.isPressed || k.wKey.isPressed;
    }
}
