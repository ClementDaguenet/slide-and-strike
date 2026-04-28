using UnityEngine;

public class KnockableBottle : MonoBehaviour
{
    [SerializeField] float scoreVelocityThreshold = 1.2f;
    [SerializeField] float hitImpulse = 8f;
    [SerializeField] float upwardImpulse = 2.2f;

    Rigidbody _rb;
    bool _scored;

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
    }

    void OnCollisionEnter(Collision collision)
    {
        if (_scored || !IsPenguin(collision.collider))
            return;

        float impact = collision.relativeVelocity.magnitude;
        if (impact < scoreVelocityThreshold)
            return;

        _scored = true;
        BottleScore.AddOne();

        if (_rb == null)
            return;

        Vector3 dir = transform.position - collision.transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.01f && collision.contactCount > 0)
            dir = collision.GetContact(0).normal;
        if (dir.sqrMagnitude < 0.01f)
            dir = transform.forward;
        dir.Normalize();

        _rb.AddForce(dir * hitImpulse + Vector3.up * upwardImpulse, ForceMode.Impulse);
        _rb.AddTorque(Random.insideUnitSphere * hitImpulse, ForceMode.Impulse);
    }

    static bool IsPenguin(Collider col)
    {
        return col.GetComponentInParent<PenguinSlideDrive>() != null;
    }
}
