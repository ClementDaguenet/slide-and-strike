using UnityEngine;

public class PenguinHeavyShieldPowerUp : MonoBehaviour
{
    [SerializeField] float scaleMultiplier = 1.28f;
    [SerializeField] float massMultiplier = 2.4f;

    Rigidbody _rb;
    Vector3 _baseScale;
    float _baseMass;
    bool _hasBaseValues;

    public bool IsActive { get; private set; }

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
    }

    public void Apply()
    {
        if (!_hasBaseValues)
        {
            _baseScale = transform.localScale;
            _baseMass = _rb != null ? _rb.mass : 1f;
            _hasBaseValues = true;
        }

        transform.localScale = _baseScale * scaleMultiplier;
        if (_rb != null)
            _rb.mass = _baseMass * massMultiplier;
        IsActive = true;
    }

    public void Clear()
    {
        if (_hasBaseValues)
            transform.localScale = _baseScale;
        if (_rb != null && _hasBaseValues)
            _rb.mass = _baseMass;
        IsActive = false;
    }
}
