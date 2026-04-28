using UnityEngine;

public class PenguinSpeedPowerUp : MonoBehaviour
{
    [SerializeField] float accelerationMultiplier = 2.7f;
    [SerializeField] float launchImpulse = 12f;
    [SerializeField] float particleRadius = 0.75f;

    PenguinSlideDrive _drive;
    Rigidbody _rb;
    ParticleSystem _particles;

    void Awake()
    {
        _drive = GetComponent<PenguinSlideDrive>();
        _rb = GetComponent<Rigidbody>();
    }

    public void Apply()
    {
        if (_drive != null)
            _drive.PowerUpAccelerationMultiplier = accelerationMultiplier;
        if (_rb != null)
            _rb.AddForce(transform.forward * launchImpulse, ForceMode.VelocityChange);
        EnsureParticles();
        _particles.Play();
    }

    public void Clear()
    {
        if (_drive != null)
            _drive.PowerUpAccelerationMultiplier = 1f;
        if (_particles != null)
            _particles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
    }

    void EnsureParticles()
    {
        if (_particles != null)
            return;

        var go = new GameObject("RedSpeedWind");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = Vector3.up * 0.7f;
        _particles = go.AddComponent<ParticleSystem>();

        var main = _particles.main;
        main.startColor = new Color(1f, 0.08f, 0.02f, 0.7f);
        main.startLifetime = 0.35f;
        main.startSpeed = 5.5f;
        main.startSize = 0.24f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = _particles.emission;
        emission.rateOverTime = 90f;

        var shape = _particles.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = particleRadius;

        _particles.Stop();
    }
}
