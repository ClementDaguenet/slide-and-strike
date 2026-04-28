using UnityEngine;

public class PenguinMagnetPowerUp : MonoBehaviour
{
    [SerializeField] float radius = 12f;

    PenguinPowerUpController _controller;
    ParticleSystem _particles;
    bool _active;

    void Awake()
    {
        _controller = GetComponent<PenguinPowerUpController>();
    }

    void FixedUpdate()
    {
        if (!_active || _controller == null)
            return;

        var hits = Physics.OverlapSphere(transform.position, radius, ~0, QueryTriggerInteraction.Collide);
        foreach (var hit in hits)
        {
            var collectible = hit.GetComponentInParent<PenguinColorCollectible>();
            if (collectible == null)
                continue;

            collectible.TryCollect(_controller);
        }
    }

    public void Apply()
    {
        _active = true;
        EnsureParticles();
        _particles.Play();
    }

    public void Clear()
    {
        _active = false;
        if (_particles != null)
            _particles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
    }

    void EnsureParticles()
    {
        if (_particles != null)
            return;

        var go = new GameObject("GrayMagnetParticles");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = Vector3.up * 0.7f;
        _particles = go.AddComponent<ParticleSystem>();

        var main = _particles.main;
        main.startColor = new Color(0.55f, 0.55f, 0.58f, 0.7f);
        main.startLifetime = 0.55f;
        main.startSpeed = 1.8f;
        main.startSize = 0.2f;

        var emission = _particles.emission;
        emission.rateOverTime = 70f;

        var shape = _particles.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 2.1f;

        _particles.Stop();
    }
}
