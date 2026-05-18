using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] string targetName = "pinguin-black";
    [SerializeField] float followDistance = 6.2f;
    [SerializeField] float heightAboveTarget = 2.8f;
    [SerializeField] float lookAtHeightOnTarget = 0.65f;
    [SerializeField] float positionSmoothTime = 0.18f;
    [SerializeField] float rotationSmoothTime = 0.15f;
    [SerializeField] float cameraYawOffsetDegrees = 0f;

    [Header("Wall Avoidance")]
    [SerializeField] float collisionRadius = 0.3f;
    [SerializeField] float minDistanceFromTarget = 1.0f;
    [SerializeField] LayerMask collisionLayers = ~0;

    Transform _target;
    Vector3 _posVelocity;

    static Vector3 HorizontalForward(Transform t)
    {
        Vector3 f = t.forward;
        f.y = 0f;
        if (f.sqrMagnitude < 1e-6f)
        {
            f = t.rotation * Vector3.forward;
            f.y = 0f;
        }

        if (f.sqrMagnitude < 1e-6f)
            f = Vector3.forward;
        return f.normalized;
    }

    void Start()
    {
        var go = GameObject.Find(targetName);
        if (go != null)
        {
            _target = go.transform;
            SnapBehind();
        }
    }

    void LateUpdate()
    {
        if (_target == null)
        {
            var go = GameObject.Find(targetName);
            if (go == null)
                return;
            _target = go.transform;
            SnapBehind();
        }

        Vector3 hf = HorizontalForward(_target);
        Quaternion yaw = Quaternion.AngleAxis(cameraYawOffsetDegrees, Vector3.up);
        Vector3 behind = yaw * (-hf * followDistance);
        Vector3 desiredPos = _target.position + behind + Vector3.up * heightAboveTarget;

        Vector3 lookPoint = _target.position + Vector3.up * lookAtHeightOnTarget;
        desiredPos = ResolveWallCollision(lookPoint, desiredPos);

        transform.position = Vector3.SmoothDamp(transform.position, desiredPos, ref _posVelocity, positionSmoothTime);

        transform.position = ResolveWallCollision(lookPoint, transform.position);

        Vector3 toTarget = lookPoint - transform.position;
        if (toTarget.sqrMagnitude < 1e-4f)
            return;

        Quaternion desiredRot = Quaternion.LookRotation(toTarget);
        transform.rotation = Quaternion.Slerp(transform.rotation, desiredRot, 1f - Mathf.Exp(-Time.deltaTime / Mathf.Max(0.01f, rotationSmoothTime)));
    }

    Vector3 ResolveWallCollision(Vector3 origin, Vector3 cameraPos)
    {
        Vector3 dir = cameraPos - origin;
        float dist = dir.magnitude;
        if (dist < 1e-4f)
            return cameraPos;

        dir /= dist;

        float castDist = dist - collisionRadius;
        if (castDist <= 0f)
            return cameraPos;

        if (Physics.SphereCast(new Ray(origin, dir), collisionRadius, out RaycastHit hit,
                castDist, collisionLayers, QueryTriggerInteraction.Ignore))
        {
            float safeDist = Mathf.Max(hit.distance, minDistanceFromTarget);
            return origin + dir * safeDist;
        }

        return cameraPos;
    }

    void SnapBehind()
    {
        if (_target == null)
            return;
        Vector3 hf = HorizontalForward(_target);
        Quaternion yaw = Quaternion.AngleAxis(cameraYawOffsetDegrees, Vector3.up);
        Vector3 behind = yaw * (-hf * followDistance);
        Vector3 lookPoint = _target.position + Vector3.up * lookAtHeightOnTarget;

        Vector3 desiredPos = _target.position + behind + Vector3.up * heightAboveTarget;
        desiredPos = ResolveWallCollision(lookPoint, desiredPos);

        transform.position = desiredPos;
        transform.rotation = Quaternion.LookRotation(lookPoint - transform.position);
        _posVelocity = Vector3.zero;
    }
}
