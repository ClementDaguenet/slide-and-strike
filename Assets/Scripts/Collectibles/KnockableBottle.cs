using UnityEngine;

public class KnockableBottle : MonoBehaviour
{
    [SerializeField] float scoreVelocityThreshold = 1.2f;
    [SerializeField] float chainScoreVelocityThreshold = 1.6f;
    [SerializeField] float tippedScoreAngle = 35f;
    [SerializeField] float hitImpulse = 8f;
    [SerializeField] float upwardImpulse = 2.2f;

    Rigidbody _rb;
    bool _scored;
    float _scoreReadyAt;
    bool _chainEnabled;

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        KeepStanding();
        _scoreReadyAt = Time.time + 0.5f;
    }

    void Update()
    {
        if (_scored || !_chainEnabled || Time.time < _scoreReadyAt || _rb == null)
            return;

        float tilt = Vector3.Angle(transform.up, Vector3.up);
        if (_rb.linearVelocity.magnitude >= chainScoreVelocityThreshold || tilt >= tippedScoreAngle)
            Score();
    }

    void OnCollisionEnter(Collision collision)
    {
        if (_scored || Time.time < _scoreReadyAt)
            return;

        float impact = collision.relativeVelocity.magnitude;
        bool hitByPenguin = TryGetPenguin(collision.collider, out _);
        if (!hitByPenguin)
        {
            var otherBottle = collision.collider.GetComponentInParent<KnockableBottle>();
            if (otherBottle != null && otherBottle._scored && impact >= chainScoreVelocityThreshold)
            {
                HitFrom(otherBottle.transform.position, 1f);
            }
            return;
        }

        if (impact < scoreVelocityThreshold)
            return;

        HitFrom(collision.transform.position, 1f);
    }

    void OnTriggerEnter(Collider other)
    {
        if (_scored || Time.time < _scoreReadyAt)
            return;

        if (TryGetPenguin(other, out _))
        {
            var rb = other.GetComponentInParent<Rigidbody>();
            if (rb != null && rb.linearVelocity.magnitude < scoreVelocityThreshold)
                return;

            HitFrom(other.transform.position, 1f);
            IgnoreCollisionsWithSource(other);
            return;
        }

        var otherBottle = other.GetComponentInParent<KnockableBottle>();
        if (otherBottle != null && otherBottle._scored)
            HitFrom(otherBottle.transform.position, 1f);
    }

    public void HitFrom(Vector3 sourcePosition, float multiplier)
    {
        ActivatePhysics();
        Score();
        _chainEnabled = true;
        if (_rb == null)
            return;

        Vector3 dir = transform.position - sourcePosition;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.01f)
            dir = transform.forward;
        dir.Normalize();

        _rb.AddForce((dir * hitImpulse + Vector3.up * upwardImpulse) * multiplier, ForceMode.Impulse);
        _rb.AddTorque(Random.insideUnitSphere * hitImpulse * multiplier, ForceMode.Impulse);
    }

    void KeepStanding()
    {
        if (_rb == null)
            return;

        _rb.useGravity = false;
        _rb.isKinematic = true;
        SetCollidersTrigger(true);
    }

    void ActivatePhysics()
    {
        if (_rb == null)
            return;

        if (!_rb.isKinematic && _rb.useGravity)
            return;

        _rb.isKinematic = false;
        _rb.useGravity = true;
        _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        SetCollidersTrigger(false);
        _rb.WakeUp();
    }

    void SetCollidersTrigger(bool isTrigger)
    {
        foreach (var col in GetComponentsInChildren<Collider>())
            col.isTrigger = isTrigger;
    }

    void IgnoreCollisionsWithSource(Collider source)
    {
        var sourceRoot = source.attachedRigidbody != null ? source.attachedRigidbody.transform : source.transform.root;
        var bottleColliders = GetComponentsInChildren<Collider>();
        var sourceColliders = sourceRoot.GetComponentsInChildren<Collider>();
        foreach (var bottleCollider in bottleColliders)
        {
            if (bottleCollider == null)
                continue;
            foreach (var sourceCollider in sourceColliders)
            {
                if (sourceCollider != null)
                    Physics.IgnoreCollision(bottleCollider, sourceCollider, true);
            }
        }
    }

    void Score()
    {
        if (_scored)
            return;

        _scored = true;
        BottleScore.AddOne();
    }

    static bool TryGetPenguin(Collider col, out PenguinPowerUpController powerUps)
    {
        var mirror = col.GetComponentInParent<PenguinMirrorClone>();
        if (mirror != null)
        {
            powerUps = mirror.SourcePowerUps;
            return true;
        }

        powerUps = col.GetComponentInParent<PenguinPowerUpController>();
        return col.GetComponentInParent<PenguinSlideDrive>() != null;
    }
}
