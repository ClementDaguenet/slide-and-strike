using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class TrackWallSlowdown : MonoBehaviour
{
    [SerializeField] [Range(0f, 0.2f)] float speedLossPerSecond = 0.02f;
    [SerializeField] [Range(0.2f, 0.95f)] float yawAngularRetentionOnWall = 0.55f;

    Rigidbody _rb;

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
    }

    void OnCollisionStay(Collision collision)
    {
        if (!IsTrackWall(collision))
            return;

        if (collision.contactCount == 0)
            return;

        Vector3 n = Vector3.zero;
        for (int i = 0; i < collision.contactCount; i++)
            n += collision.GetContact(i).normal;
        if (n.sqrMagnitude < 1e-8f)
            return;
        n.Normalize();

        Vector3 v = _rb.linearVelocity;
        float into = Vector3.Dot(v, n);
        if (into < 0f)
            v -= into * n;

        if (n.y > 0.18f)
        {
            _rb.linearVelocity = v;
            return;
        }

        float damp = Mathf.Clamp01(1f - speedLossPerSecond * Time.fixedDeltaTime);
        v *= damp;
        _rb.linearVelocity = v;

        float angDamp = Mathf.Lerp(damp, 1f, yawAngularRetentionOnWall);
        _rb.angularVelocity *= angDamp;
    }

    static bool IsTrackWall(Collision c)
    {
        return c.transform.name.IndexOf("TrackWall", System.StringComparison.Ordinal) >= 0;
    }
}
