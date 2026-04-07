using System;
using UnityEngine;
using UnityEngine.InputSystem;

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
    [SerializeField] int[] robeMaterialSlots;

    int _index;
    MeshFilter _mf;
    MeshRenderer _mr;
    PenguinBodyCollider _body;

    void Awake()
    {
        _mf = GetComponent<MeshFilter>();
        _mr = GetComponent<MeshRenderer>();
        _body = GetComponent<PenguinBodyCollider>();

        if (skinsInOrder != null && skinsInOrder.Length > 0)
        {
            _index = Mathf.Clamp(startSkinIndex, 0, skinsInOrder.Length - 1);
            Apply(_index);
        }
    }

    void Update()
    {
        if (skinsInOrder == null || skinsInOrder.Length == 0)
            return;

        if (!SkinHotkey())
            return;

        _index = (_index + 1) % skinsInOrder.Length;
        Apply(_index);
    }

    static bool SkinHotkey()
    {
        var kb = Keyboard.current;
        if (kb != null)
        {
            if (kb.oKey.wasPressedThisFrame || kb.cKey.wasPressedThisFrame)
                return true;
        }

#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKeyDown(KeyCode.O) || Input.GetKeyDown(KeyCode.C))
            return true;
#endif
        return false;
    }

    void Apply(int index)
    {
        if (skinsInOrder == null || index < 0 || index >= skinsInOrder.Length)
            return;

        var black = skinsInOrder[0];
        if (black.mesh != null)
            _mf.sharedMesh = black.mesh;

        if (black.materials == null || black.materials.Length == 0)
            return;

        Material[] next;
        if (index == 0)
        {
            next = black.materials;
        }
        else
        {
            var variant = skinsInOrder[index].materials;
            if (variant == null || variant.Length == 0)
                next = black.materials;
            else
                next = MergeSkins(black.materials, variant);
        }

        _mr.sharedMaterials = next;

        if (_body != null)
            _body.RebuildCapsuleFromMesh();
    }

    Material[] MergeSkins(Material[] black, Material[] variant)
    {
        var aligned = PenguinMaterialAlign.AlignToReference(black, variant);
        var next = (Material[])black.Clone();

        for (int i = 0; i < next.Length; i++)
        {
            var bm = black[i];
            var name = bm != null ? bm.name : "";

            if (PenguinMaterialAlign.IsFixedPartFromBlackSkin(name))
            {
                next[i] = bm;
                continue;
            }

            if (robeMaterialSlots != null && robeMaterialSlots.Length > 0)
            {
                if (Array.IndexOf(robeMaterialSlots, i) >= 0)
                {
                    if (i < variant.Length && variant[i] != null)
                        next[i] = variant[i];
                    else if (i < aligned.Length && aligned[i] != null)
                        next[i] = aligned[i];
                    else
                        next[i] = bm;
                }
                else
                    next[i] = bm;
            }
            else
            {
                if (i < aligned.Length && aligned[i] != null)
                    next[i] = aligned[i];
                else
                    next[i] = bm;
            }
        }

        return next;
    }
}
