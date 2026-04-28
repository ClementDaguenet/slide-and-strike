using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(120)]
public class PenguinPowerUpController : MonoBehaviour
{
    public static bool SuppressAutoHud { get; set; }

    public event Action Changed;

    [SerializeField] int maxStoredPowerUps = 3;
    [SerializeField] float effectDuration = 5f;
    [SerializeField] float redEffectDuration = 3f;

    readonly Queue<PenguinPowerUpType> _stored = new Queue<PenguinPowerUpType>();
    PenguinColorCycle _colorCycle;
    PenguinSpeedPowerUp _speed;
    PenguinGiantPowerUp _giant;
    PenguinClonePowerUp _clone;
    PenguinMagnetPowerUp _magnet;
    PenguinHeavyShieldPowerUp _heavyShield;
    PenguinPowerUpHud _hud;
    PenguinPowerUpType _active = PenguinPowerUpType.Normal;
    float _activeUntil = -1f;
    bool _shiftWasDown;

    public PenguinPowerUpType ActivePowerUp => _active;
    public bool HasActivePowerUp => _active != PenguinPowerUpType.Normal && Time.time < _activeUntil;
    public float CooldownRemaining => HasActivePowerUp ? Mathf.Max(0f, _activeUntil - Time.time) : 0f;
    public float Cooldown01 => HasActivePowerUp ? Mathf.Clamp01(CooldownRemaining / Mathf.Max(0.01f, DurationFor(_active))) : 0f;
    public int StoredCount => _stored.Count;
    public int MaxStoredPowerUps => maxStoredPowerUps;

    void Awake()
    {
        CacheComponents();
        if (!SuppressAutoHud)
            EnsureHud();
    }

    void OnEnable()
    {
        Changed?.Invoke();
    }

    void Update()
    {
        if (HasActivePowerUp)
        {
            Changed?.Invoke();
            return;
        }

        if (_active != PenguinPowerUpType.Normal)
            ClearActivePowerUp();

        bool shiftDown = IsShiftDown();
        if (shiftDown && !_shiftWasDown)
            TryActivateNext();
        _shiftWasDown = shiftDown;
    }

    public bool TryStore(PenguinPowerUpType type)
    {
        if (type == PenguinPowerUpType.Normal || _stored.Count >= maxStoredPowerUps)
            return false;

        _stored.Enqueue(type);
        Changed?.Invoke();
        return true;
    }

    public PenguinPowerUpType[] StoredPowerUps()
    {
        return _stored.ToArray();
    }

    void TryActivateNext()
    {
        if (HasActivePowerUp || _stored.Count == 0)
            return;

        ApplyPowerUp(_stored.Dequeue());
    }

    void ApplyPowerUp(PenguinPowerUpType type)
    {
        _active = type;
        _activeUntil = Time.time + DurationFor(type);
        if (_colorCycle != null)
            _colorCycle.ApplySkin((int)type);

        if (type == PenguinPowerUpType.Red)
            _speed.Apply();
        else if (type == PenguinPowerUpType.Blue)
            _giant.Apply();
        else if (type == PenguinPowerUpType.Green)
            _clone.Apply();
        else if (type == PenguinPowerUpType.Gray)
            _magnet.Apply();
        else if (type == PenguinPowerUpType.Pink)
            _heavyShield.Apply();

        Changed?.Invoke();
    }

    void ClearActivePowerUp()
    {
        _speed.Clear();
        _giant.Clear();
        _clone.Clear();
        _magnet.Clear();
        _heavyShield.Clear();
        _active = PenguinPowerUpType.Normal;
        _activeUntil = -1f;
        if (_colorCycle != null)
            _colorCycle.ApplySkin(0);
        Changed?.Invoke();
    }

    void CacheComponents()
    {
        _colorCycle = GetComponent<PenguinColorCycle>();
        _speed = GetOrAdd<PenguinSpeedPowerUp>();
        _giant = GetOrAdd<PenguinGiantPowerUp>();
        _clone = GetOrAdd<PenguinClonePowerUp>();
        _magnet = GetOrAdd<PenguinMagnetPowerUp>();
        _heavyShield = GetOrAdd<PenguinHeavyShieldPowerUp>();
    }

    void EnsureHud()
    {
        _hud = FindFirstObjectByType<PenguinPowerUpHud>();
        if (_hud == null)
            _hud = new GameObject("PowerUpHUD").AddComponent<PenguinPowerUpHud>();
        _hud.Bind(this);
    }

    T GetOrAdd<T>() where T : Component
    {
        var c = GetComponent<T>();
        return c != null ? c : gameObject.AddComponent<T>();
    }

    static bool IsShiftDown()
    {
        var k = Keyboard.current;
        return k != null && (k.leftShiftKey.isPressed || k.rightShiftKey.isPressed);
    }

    float DurationFor(PenguinPowerUpType type)
    {
        return type == PenguinPowerUpType.Red ? redEffectDuration : effectDuration;
    }
}
