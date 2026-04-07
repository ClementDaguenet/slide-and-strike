using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class TrackWallSlowdown : MonoBehaviour
{
    [SerializeField] [Range(0.04f, 0.45f)] float speedLossPerSecond = 0.14f;
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

        float damp = Mathf.Clamp01(1f - speedLossPerSecond * Time.fixedDeltaTime);
        v *= damp;
        _rb.linearVelocity = v;

        Vector3 av = _rb.angularVelocity;
        _rb.angularVelocity = new Vector3(0f, av.y * yawAngularRetentionOnWall, 0f);
    }

    static bool IsTrackWall(Collision c)
    {
        return c.transform.name.IndexOf("TrackWall", System.StringComparison.Ordinal) >= 0;
    }
}
