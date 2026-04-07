using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
public class IceSlopeVisual : MonoBehaviour
{
    [SerializeField] Vector2 baseMapTiling = new Vector2(14f, 56f);
    [SerializeField] int textureSize = 512;

    void Start()
    {
        var mr = GetComponent<MeshRenderer>();
        if (mr == null)
            return;

        Texture2D tex = CreateIceTexture(textureSize);
        tex.wrapModeU = TextureWrapMode.Repeat;
        tex.wrapModeV = TextureWrapMode.Repeat;
        tex.filterMode = FilterMode.Trilinear;
        tex.anisoLevel = 4;

        Material m = new Material(mr.sharedMaterial);
        m.name = "IceTrackRuntime";
        m.SetTexture("_BaseMap", tex);
        m.SetColor("_BaseColor", Color.white);
        m.SetFloat("_Smoothness", 0.9f);
        m.SetFloat("_Metallic", 0.04f);
        m.SetTextureScale("_BaseMap", baseMapTiling);
        mr.material = m;
    }

    static Texture2D CreateIceTexture(int size)
    {
        var t = new Texture2D(size, size, TextureFormat.RGBA32, true);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float fx = x / (float)size;
                float fy = y / (float)size;
                float n = Mathf.PerlinNoise(fx * 18f, fy * 18f);
                float streaks = Mathf.PerlinNoise(fx * 6f + 20f, fy * 40f);
                float frost = Mathf.PerlinNoise(fx * 32f + 100f, fy * 32f + 50f);
                float blend = n * 0.42f + streaks * 0.38f + frost * 0.2f;
                Color c = Color.Lerp(new Color(0.68f, 0.86f, 0.98f), new Color(0.94f, 0.98f, 1f), blend);
                c *= 0.92f + 0.08f * Mathf.PerlinNoise(fx * 50f, fy * 50f);
                t.SetPixel(x, y, c);
            }
        }

        t.Apply();
        return t;
    }
}
