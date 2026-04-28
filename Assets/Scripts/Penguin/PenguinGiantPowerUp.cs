using UnityEngine;

public class PenguinGiantPowerUp : MonoBehaviour
{
    [SerializeField] float visualScaleMultiplier = 1.75f;

    readonly System.Collections.Generic.List<Transform> _visualRoots = new System.Collections.Generic.List<Transform>();
    readonly System.Collections.Generic.List<Vector3> _baseVisualScales = new System.Collections.Generic.List<Vector3>();
    bool _hasBaseScale;

    public bool IsActive { get; private set; }

    public void Apply()
    {
        if (!_hasBaseScale)
        {
            CacheVisualScales();
            _hasBaseScale = true;
        }

        for (int i = 0; i < _visualRoots.Count; i++)
            _visualRoots[i].localScale = _baseVisualScales[i] * visualScaleMultiplier;
        IsActive = true;
    }

    public void Clear()
    {
        if (_hasBaseScale)
        {
            for (int i = 0; i < _visualRoots.Count; i++)
            {
                if (_visualRoots[i] != null)
                    _visualRoots[i].localScale = _baseVisualScales[i];
            }
        }
        IsActive = false;
    }

    void CacheVisualScales()
    {
        _visualRoots.Clear();
        _baseVisualScales.Clear();
        foreach (var renderer in GetComponentsInChildren<Renderer>())
        {
            if (renderer.GetComponentInParent<ParticleSystem>() != null)
                continue;
            Transform t = renderer.transform;
            if (_visualRoots.Contains(t))
                continue;
            _visualRoots.Add(t);
            _baseVisualScales.Add(t.localScale);
        }
    }
}
