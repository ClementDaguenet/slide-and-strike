using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[DefaultExecutionOrder(80)]
public class PenguinFallRecover : MonoBehaviour
{
    [SerializeField] Transform trackRoot;
    [SerializeField] float marginBelowTrackMin = 5f;
    [SerializeField] LayerMask groundMask = ~0;
    [SerializeField] int physicsFramesCooldown = 4;
    [SerializeField] float groundRayLength = 28f;

    Rigidbody _rb;
    MeshCollider _trackCollider;
    int _cooldown;
    Vector3 _lastOnTrackPos;
    bool _hasLastOnTrack;

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        if (trackRoot == null)
        {
            var go = GameObject.Find("Slope");
            if (go != null)
                trackRoot = go.transform;
        }

        if (trackRoot != null)
            _trackCollider = trackRoot.GetComponent<MeshCollider>();
    }

    void FixedUpdate()
    {
        if (_cooldown > 0)
        {
            _cooldown--;
            return;
        }

        if (_trackCollider == null || _rb == null)
            return;

        TryRecordLastOnIce();

        Bounds wb = _trackCollider.bounds;
        float fallLine = wb.min.y - marginBelowTrackMin;
        if (_rb.position.y >= fallLine)
            return;

        Vector3 target = ResolveRecoverPosition(wb);
        ApplyTeleport(target);
        _cooldown = physicsFramesCooldown;
    }

    void TryRecordLastOnIce()
    {
        Vector3 origin = _rb.position + Vector3.up * 0.45f;
        if (!Physics.Raycast(origin, Vector3.down, out RaycastHit hit, groundRayLength, groundMask,
                QueryTriggerInteraction.Ignore))
            return;
        if (hit.collider != _trackCollider)
            return;

        var cap = GetComponent<CapsuleCollider>();
        _lastOnTrackPos = cap != null
            ? PenguinCapsulePlacement.PivotPositionForBottomAt(transform, cap, hit.point, hit.normal, -0.12f)
            : hit.point + hit.normal * FallbackLift();
        _hasLastOnTrack = true;
    }

    Vector3 ResolveRecoverPosition(Bounds wb)
    {
        var cap = GetComponent<CapsuleCollider>();
        float liftLegacy = FallbackLift();
        Vector3 candidate;

        if (_hasLastOnTrack)
            candidate = _lastOnTrackPos;
        else
        {
            candidate = _trackCollider.ClosestPoint(_rb.position);
            Vector3 rayFrom = new Vector3(candidate.x, wb.max.y + 220f, candidate.z);
            if (Physics.Raycast(rayFrom, Vector3.down, out RaycastHit hit, 500f, groundMask,
                    QueryTriggerInteraction.Ignore))
                candidate = cap != null
                    ? PenguinCapsulePlacement.PivotPositionForBottomAt(transform, cap, hit.point, hit.normal, -0.12f)
                    : hit.point + hit.normal * liftLegacy;
            else
                candidate.y = wb.center.y + liftLegacy;
        }

        Vector3 refineFrom = new Vector3(candidate.x, candidate.y + 18f, candidate.z);
        if (Physics.Raycast(refineFrom, Vector3.down, out RaycastHit h2, 60f, groundMask,
                QueryTriggerInteraction.Ignore))
        {
            if (h2.collider == _trackCollider)
                candidate = cap != null
                    ? PenguinCapsulePlacement.PivotPositionForBottomAt(transform, cap, h2.point, h2.normal, -0.12f)
                    : h2.point + h2.normal * liftLegacy;
        }

        return candidate;
    }

    void ApplyTeleport(Vector3 target)
    {
        _rb.MovePosition(target);
        _rb.position = target;
        transform.position = target;
        _rb.linearVelocity = Vector3.zero;
        _rb.angularVelocity = Vector3.zero;
    }

    float FallbackLift()
    {
        var cap = GetComponent<CapsuleCollider>();
        if (cap == null)
            return 0.35f;
        float h = Mathf.Abs(transform.lossyScale.y);
        return Mathf.Max(cap.radius, cap.height * 0.5f * h) + 0.1f;
    }
}
