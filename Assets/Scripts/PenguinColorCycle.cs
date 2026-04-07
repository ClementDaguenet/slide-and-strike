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
        var k = Keyboard.current;
        if (k == null || skinsInOrder == null || skinsInOrder.Length == 0)
            return;
        if (!k.oKey.wasPressedThisFrame)
            return;

        _index = (_index + 1) % skinsInOrder.Length;
        Apply(_index);
    }

    void Apply(int index)
    {
        if (skinsInOrder == null || index < 0 || index >= skinsInOrder.Length)
            return;

        var s = skinsInOrder[index];
        if (s.mesh != null)
            _mf.sharedMesh = s.mesh;

        if (s.materials != null && s.materials.Length > 0)
            _mr.sharedMaterials = s.materials;

        if (_body != null)
            _body.RebuildCapsuleFromMesh();
    }
}
