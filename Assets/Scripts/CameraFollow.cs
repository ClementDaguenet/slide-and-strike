using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] string targetName = "pinguin-black";
    [SerializeField] float followDistance = 6.2f;
    [SerializeField] float heightAboveTarget = 2.1f;
    [SerializeField] float lookAtHeightOnTarget = 0.45f;
    [SerializeField] float positionSmoothTime = 0.18f;
    [SerializeField] float rotationSmoothTime = 0.15f;
    [SerializeField] float cameraYawOffsetDegrees = 0f;

    Transform _target;
    Vector3 _posVelocity;

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

        Quaternion yaw = Quaternion.AngleAxis(cameraYawOffsetDegrees, _target.up);
        Vector3 behind = yaw * (-_target.forward * followDistance);
        Vector3 desiredPos = _target.position + behind + _target.up * heightAboveTarget;
        transform.position = Vector3.SmoothDamp(transform.position, desiredPos, ref _posVelocity, positionSmoothTime);

        Vector3 lookPoint = _target.position + _target.up * lookAtHeightOnTarget;
        Vector3 toTarget = lookPoint - transform.position;
        if (toTarget.sqrMagnitude < 1e-4f)
            return;

        Quaternion desiredRot = Quaternion.LookRotation(toTarget);
        transform.rotation = Quaternion.Slerp(transform.rotation, desiredRot, 1f - Mathf.Exp(-Time.deltaTime / Mathf.Max(0.01f, rotationSmoothTime)));
    }

    void SnapBehind()
    {
        if (_target == null)
            return;
        Quaternion yaw = Quaternion.AngleAxis(cameraYawOffsetDegrees, _target.up);
        Vector3 behind = yaw * (-_target.forward * followDistance);
        transform.position = _target.position + behind + _target.up * heightAboveTarget;
        Vector3 lookPoint = _target.position + _target.up * lookAtHeightOnTarget;
        transform.rotation = Quaternion.LookRotation(lookPoint - transform.position);
        _posVelocity = Vector3.zero;
    }
}
