using UnityEngine;

[ExecuteAlways]
[DefaultExecutionOrder(-100)]
public class PenguinColorCycle : MonoBehaviour
{
    [System.Serializable]
    public class SkinEntry
    {
        public Mesh mesh;
        public Material[] materials;
    }

    [SerializeField] SkinEntry[] skinsInOrder;
    [SerializeField] int startSkinIndex;
    [SerializeField] int[] robeMaterialSlots = { 2 };

    int _index;
    MeshFilter _mf;
    MeshRenderer _mr;
    PenguinBodyCollider _body;
    int[] _autoRobeSlots;
    MaterialPropertyBlock _robeBlock;
    float _revertAt = -1f;

    void Awake()
    {
        CacheComponents();
        ApplyStartSkin();
    }

    void OnEnable()
    {
        CacheComponents();
        ApplyStartSkin();
    }

    void OnValidate()
    {
        if (Application.isPlaying)
            return;
        CacheComponents();
        ApplyStartSkin();
    }

    void CacheComponents()
    {
        _mf = GetComponent<MeshFilter>();
        _mr = GetComponent<MeshRenderer>();
        _body = GetComponent<PenguinBodyCollider>();
        if (_robeBlock == null)
            _robeBlock = new MaterialPropertyBlock();
    }

    void ApplyStartSkin()
    {
        if (skinsInOrder != null && skinsInOrder.Length > 0)
        {
            _index = Mathf.Clamp(startSkinIndex, 0, skinsInOrder.Length - 1);
            Apply(_index);
        }
    }

    void Update()
    {
        if (!Application.isPlaying || _revertAt < 0f)
            return;

        if (Time.time < _revertAt)
            return;

        _revertAt = -1f;
        Apply(0);
    }

    public void ActivateTemporarySkin(int index, float duration)
    {
        if (skinsInOrder == null || skinsInOrder.Length == 0)
            return;
        _index = Mathf.Clamp(index, 0, skinsInOrder.Length - 1);
        Apply(_index);
        _revertAt = Time.time + Mathf.Max(0.1f, duration);
    }

    void Apply(int index)
    {
        if (skinsInOrder == null || index < 0 || index >= skinsInOrder.Length)
            return;
        if (_mf == null || _mr == null)
            return;

        var black = skinsInOrder[0];
        if (black.mesh != null)
            _mf.sharedMesh = black.mesh;

        if (black.materials == null || black.materials.Length == 0)
            return;

        int[] robeSlots = GetRobeSlots(black);
        Material[] next = MaterialsWithFbxParts(index, black, robeSlots);

        _mr.sharedMaterials = next;
        ApplyRobeTint(index, robeSlots, next.Length);

        if (_body != null && Application.isPlaying)
            _body.RebuildCapsuleFromMesh();
    }

    Material[] MergeSkins(Material[] black, Material[] variant, int[] robeSlots)
    {
        var aligned = PenguinMaterialAlign.AlignToReference(black, variant);
        var next = (Material[])black.Clone();

        foreach (int i in robeSlots)
        {
            if (i < 0 || i >= next.Length)
                continue;
            if (i < variant.Length && variant[i] != null)
                next[i] = variant[i];
            else if (i < aligned.Length && aligned[i] != null)
                next[i] = aligned[i];
        }

        return next;
    }

    Material[] MaterialsWithFbxParts(int index, SkinEntry black, int[] robeSlots)
    {
        var next = (Material[])black.materials.Clone();

        if (index <= 0)
            return next;

        int donorIndex = index;
        if (donorIndex <= 0)
            return next;

        var variant = skinsInOrder[donorIndex].materials;
        if (variant == null || variant.Length == 0)
            return next;

        var aligned = PenguinMaterialAlign.AlignToReference(black.materials, variant);
        for (int i = 0; i < next.Length; i++)
        {
            if (IsRobeSlot(i, robeSlots))
                continue;
            if (i < aligned.Length && aligned[i] != null)
                next[i] = aligned[i];
            else if (i < variant.Length && variant[i] != null)
                next[i] = variant[i];
        }

        return next;
    }

    static bool IsRobeSlot(int index, int[] robeSlots)
    {
        foreach (int slot in robeSlots)
        {
            if (slot == index)
                return true;
        }
        return false;
    }

    int[] GetRobeSlots(SkinEntry black)
    {
        if (robeMaterialSlots != null && robeMaterialSlots.Length > 0)
            return robeMaterialSlots;
        if (_autoRobeSlots != null)
            return _autoRobeSlots;

        int count = black.materials.Length;
        for (int i = 0; i < count; i++)
        {
            if (PenguinMaterialAlign.IsRobeMaterialName(black.materials[i] != null ? black.materials[i].name : ""))
            {
                _autoRobeSlots = new[] { i };
                return _autoRobeSlots;
            }
        }

        int best = 0;
        float bestScore = -1f;
        for (int i = 0; i < count; i++)
        {
            float score = 0f;
            Color baseColor = ReadMaterialColor(black.materials[i]);
            for (int s = 1; s < skinsInOrder.Length; s++)
            {
                var mats = skinsInOrder[s].materials;
                if (mats == null || i >= mats.Length || mats[i] == null)
                    continue;
                score += ColorDistance(baseColor, ReadMaterialColor(mats[i]));
            }

            if (LooksLikeOrangePart(baseColor))
                score *= 0.15f;
            if (score > bestScore)
            {
                bestScore = score;
                best = i;
            }
        }

        _autoRobeSlots = new[] { best };
        return _autoRobeSlots;
    }

    void ApplyRobeTint(int index, int[] robeSlots, int materialCount)
    {
        for (int i = 0; i < materialCount; i++)
            _mr.SetPropertyBlock(null, i);

        Color c = SkinColor(index);
        _robeBlock.Clear();
        _robeBlock.SetColor("_BaseColor", c);
        _robeBlock.SetColor("_Color", c);
        foreach (int slot in robeSlots)
        {
            if (slot >= 0 && slot < materialCount)
                _mr.SetPropertyBlock(_robeBlock, slot);
        }
    }

    static Color SkinColor(int index)
    {
        switch (index)
        {
            case 1: return new Color(0.05f, 0.28f, 0.95f);
            case 2: return new Color(0.9f, 0.04f, 0.03f);
            case 3: return new Color(1f, 0.22f, 0.72f);
            case 4: return new Color(0.05f, 0.55f, 0.14f);
            default: return new Color(0.015f, 0.015f, 0.018f);
        }
    }

    static Color ReadMaterialColor(Material m)
    {
        if (m == null)
            return Color.white;
        if (m.HasProperty("_BaseColor"))
            return m.GetColor("_BaseColor");
        if (m.HasProperty("_Color"))
            return m.GetColor("_Color");
        return Color.white;
    }

    static float ColorDistance(Color a, Color b)
    {
        return Mathf.Abs(a.r - b.r) + Mathf.Abs(a.g - b.g) + Mathf.Abs(a.b - b.b);
    }

    static bool LooksLikeOrangePart(Color c)
    {
        return c.r > 0.55f && c.g > 0.25f && c.g < 0.75f && c.b < 0.25f;
    }
}
