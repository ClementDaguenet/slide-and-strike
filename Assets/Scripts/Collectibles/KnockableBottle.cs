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
        bool hitByPenguin = TryGetPenguin(collision.collider, out PenguinPowerUpController powerUps);
        if (!hitByPenguin)
        {
            var otherBottle = collision.collider.GetComponentInParent<KnockableBottle>();
            if (otherBottle != null && otherBottle._scored && impact >= chainScoreVelocityThreshold)
            {
                _chainEnabled = true;
                Score();
            }
            return;
        }

        if (impact < scoreVelocityThreshold)
            return;

        Score();
        _chainEnabled = true;

        HitFrom(collision.transform.position, 1f);
    }

    public void HitFrom(Vector3 sourcePosition, float multiplier)
    {
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
