using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class PenguinSlideDrive : MonoBehaviour
{
    [SerializeField] float alongSlopeAcceleration = 6.5f;
    [SerializeField] float groundedRayLength = 6f;
    [SerializeField] LayerMask groundMask = ~0;
    [SerializeField] float maxKmhForIdleNudge = 0.85f;
    [SerializeField] float idleNudgeAcceleration = 5.5f;

    Rigidbody _rb;

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
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

        float lift = 0.12f;
        var cap = GetComponent<CapsuleCollider>();
        if (cap != null)
            lift = Mathf.Max(cap.radius, cap.height * 0.5f * Mathf.Abs(transform.lossyScale.y)) + 0.08f;

        Vector3 pos = hit.point + hit.normal * lift;
        transform.position = pos;
        _rb.position = pos;
        _rb.linearVelocity = Vector3.zero;
        _rb.angularVelocity = Vector3.zero;
    }

    void FixedUpdate()
    {
        Vector3 origin = transform.position + Vector3.up * 0.35f;
        if (!Physics.Raycast(origin, Vector3.down, out RaycastHit hit, groundedRayLength, groundMask,
                QueryTriggerInteraction.Ignore))
            return;

        Vector3 normal = hit.normal;
        Vector3 g = Physics.gravity.sqrMagnitude > 1e-6f ? Physics.gravity : new Vector3(0f, -9.81f, 0f);
        Vector3 gPlane = Vector3.ProjectOnPlane(g, normal);

        Vector3 alongSlide;
        if (gPlane.sqrMagnitude > 1e-6f)
        {
            Vector3 vPlane = Vector3.ProjectOnPlane(_rb.linearVelocity, normal);
            if (vPlane.sqrMagnitude > 1.5f)
                alongSlide = (gPlane.normalized * 0.78f + vPlane.normalized * 0.22f).normalized;
            else
                alongSlide = gPlane.normalized;
            _rb.AddForce(alongSlide * alongSlopeAcceleration, ForceMode.Acceleration);
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
    }

    static bool ReadIdleForwardInput()
    {
        var k = Keyboard.current;
        if (k == null)
            return false;
        return k.upArrowKey.isPressed || k.zKey.isPressed || k.wKey.isPressed;
    }
}
