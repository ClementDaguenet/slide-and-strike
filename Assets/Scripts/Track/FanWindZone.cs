using UnityEngine;

public class FanWindZone : MonoBehaviour
{
    [SerializeField] Vector3 windDirection = Vector3.right;
    [SerializeField] float windAcceleration = 24f;
    [SerializeField] float particleRadius = 3f;

    ParticleSystem _particles;

    public void Configure(Vector3 direction, float acceleration, float radius)
    {
        windDirection = direction.sqrMagnitude > 0.001f ? direction.normalized : Vector3.right;
        windAcceleration = acceleration;
        particleRadius = radius;
        EnsureParticles();
        var shape = _particles.shape;
        shape.radius = particleRadius;
    }

    void Awake()
    {
        EnsureParticles();
    }

    void OnTriggerStay(Collider other)
    {
        var drive = other.GetComponentInParent<PenguinSlideDrive>();
        if (drive == null)
            return;

        var shield = drive.GetComponent<PenguinHeavyShieldPowerUp>();
        if (shield != null && shield.IsActive)
            return;

        var rb = drive.GetComponent<Rigidbody>();
        if (rb != null)
            rb.AddForce(windDirection.normalized * windAcceleration, ForceMode.Acceleration);
    }

    void EnsureParticles()
    {
        if (_particles != null)
            return;

        var go = new GameObject("WhiteWindTrails");
        go.transform.SetParent(transform, false);
        _particles = go.AddComponent<ParticleSystem>();

        var main = _particles.main;
        main.startColor = new Color(1f, 1f, 1f, 0.62f);
        main.startLifetime = 0.75f;
        main.startSpeed = 4.8f;
        main.startSize = 0.16f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = _particles.emission;
        emission.rateOverTime = 85f;

        var shape = _particles.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = particleRadius;

        _particles.Play();
    }
}
