using UnityEngine;

public class PenguinMirrorClone : MonoBehaviour
{
    Transform _source;
    PenguinPowerUpController _sourcePowerUps;
    Rigidbody _rb;

    public PenguinPowerUpController SourcePowerUps => _sourcePowerUps;

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        if (_rb != null)
        {
            _rb.isKinematic = true;
            _rb.useGravity = false;
        }
    }

    public void Bind(Transform source)
    {
        _source = source;
        _sourcePowerUps = source != null ? source.GetComponent<PenguinPowerUpController>() : null;
    }

    void FixedUpdate()
    {
        if (_source == null)
            return;

        Vector3 pos = _source.position;
        Vector3 fwd = _source.forward;
        if (CurvedIceTrack.TryMirrorPose(_source.position, _source.forward, out Vector3 mirroredPos,
                out Vector3 mirroredForward))
        {
            pos = mirroredPos;
            fwd = mirroredForward;
        }

        Quaternion rot = Quaternion.LookRotation(fwd, _source.up);
        if (_rb != null)
        {
            _rb.MovePosition(pos);
            _rb.MoveRotation(rot);
        }
        else
        {
            transform.SetPositionAndRotation(pos, rot);
        }
    }
}
