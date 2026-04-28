using UnityEngine;

[DisallowMultipleComponent]
public class PenguinBodyCollider : MonoBehaviour
{
    [SerializeField] PhysicsMaterial bouncyMaterial;

    void Awake()
    {
        ConfigureRigidbody();
        RebuildCapsuleFromMesh();
    }

    void ConfigureRigidbody()
    {
        var rb = GetComponent<Rigidbody>();
        if (rb == null)
            return;
        rb.linearDamping = 0.035f;
        rb.angularDamping = 0.28f;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.constraints = RigidbodyConstraints.None;
        rb.angularVelocity = Vector3.zero;
    }

    public void RebuildCapsuleFromMesh()
    {
        var mf = GetComponent<MeshFilter>();
        if (mf == null || mf.sharedMesh == null)
            return;

        PhysicsMaterial pm = null;
        var sphere = GetComponent<SphereCollider>();
        if (sphere != null)
        {
            pm = sphere.sharedMaterial;
            Destroy(sphere);
        }

        Mesh mesh = mf.sharedMesh;
        Bounds b = mesh.bounds;

        var cap = GetComponent<CapsuleCollider>();
        if (cap == null)
            cap = gameObject.AddComponent<CapsuleCollider>();

        cap.direction = 1;
        float sink = Mathf.Min(b.size.y * 0.01f, 0.006f);
        cap.center = new Vector3(b.center.x, b.center.y - sink, b.center.z);
        cap.height = Mathf.Max(b.size.y, 0.0001f);
        cap.radius = Mathf.Max(b.extents.x, b.extents.z, cap.height * 0.12f, 0.0001f);

        if (pm != null)
            cap.sharedMaterial = pm;
        else if (bouncyMaterial != null)
            cap.sharedMaterial = bouncyMaterial;
    }
}
