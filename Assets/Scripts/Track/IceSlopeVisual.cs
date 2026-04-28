using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
public class IceSlopeVisual : MonoBehaviour
{
    [SerializeField] Vector2 baseMapTiling = new Vector2(1.1f, 2.2f);
    [SerializeField] bool randomizeTiling = true;
    [SerializeField] Vector2 tilingRandomMultiplier = new Vector2(0.65f, 1.45f);
    [SerializeField] bool randomizeTextureOffset = true;
    [SerializeField] string oceanicResourceFolder = "OceanicFloes";
    [SerializeField] bool useAdvancedOceanicMaps = false;
    [SerializeField] int textureSize = 768;

    Vector2 _runtimeTiling;
    Vector2 _runtimeOffset;

    void Start()
    {
        var mr = GetComponent<MeshRenderer>();
        if (mr == null)
            return;

        Texture2D baseMap = LoadOceanicTexture("basecolor");
        if (baseMap == null)
            baseMap = CreateIceTexture(textureSize);

        _runtimeTiling = baseMapTiling;
        if (randomizeTiling)
        {
            float mult = Random.Range(tilingRandomMultiplier.x, tilingRandomMultiplier.y);
            _runtimeTiling *= mult;
        }
        _runtimeOffset = randomizeTextureOffset ? new Vector2(Random.value, Random.value) : Vector2.zero;

        Material m = new Material(mr.sharedMaterial);
        m.name = "IceTrackRuntime";
        SetTexture(m, "_BaseMap", baseMap);
        SetTexture(m, "_MainTex", baseMap);
        if (useAdvancedOceanicMaps)
        {
            SetTexture(m, "_BumpMap", LoadOceanicTexture("normal"));
            SetTexture(m, "_OcclusionMap", LoadOceanicTexture("ambientOcclusion"));
        }
        m.SetColor("_BaseColor", Color.white);
        m.SetFloat("_Smoothness", 0.78f);
        m.SetFloat("_Metallic", 0f);
        if (m.HasProperty("_BumpScale"))
            m.SetFloat("_BumpScale", 0.18f);
        if (m.HasProperty("_OcclusionStrength"))
            m.SetFloat("_OcclusionStrength", 0.45f);
        if (useAdvancedOceanicMaps)
            m.EnableKeyword("_NORMALMAP");
        mr.material = m;
    }

    Texture2D LoadOceanicTexture(string suffix)
    {
        return Resources.Load<Texture2D>(oceanicResourceFolder + "/OceanicFloes_" + suffix);
    }

    void SetTexture(Material m, string propertyName, Texture2D tex)
    {
        if (tex == null || !m.HasProperty(propertyName))
            return;

        tex.wrapModeU = TextureWrapMode.Repeat;
        tex.wrapModeV = TextureWrapMode.Repeat;
        tex.filterMode = FilterMode.Trilinear;
        tex.anisoLevel = 8;
        m.SetTexture(propertyName, tex);
        m.SetTextureScale(propertyName, _runtimeTiling);
        m.SetTextureOffset(propertyName, _runtimeOffset);
    }

    static float Hash(float x, float y)
    {
        return Mathf.Repeat(Mathf.Sin(x * 127.1f + y * 311.7f) * 43758.5453f, 1f);
    }

    static float Fbm(float x, float y, int octaves)
    {
        float sum = 0f;
        float amp = 0.5f;
        float f = 1f;
        for (int o = 0; o < octaves; o++)
        {
            sum += amp * Mathf.PerlinNoise(x * f + o * 19.2f, y * f + o * 13.7f);
            f *= 2.02f;
            amp *= 0.5f;
        }

        return sum;
    }

    static Texture2D CreateIceTexture(int size)
    {
        var t = new Texture2D(size, size, TextureFormat.RGBA32, true);
        var iceDeep = new Color(0.42f, 0.62f, 0.82f);
        var iceBright = new Color(0.88f, 0.95f, 1f);
        var iceCyan = new Color(0.55f, 0.82f, 0.92f);
        var iceShadow = new Color(0.38f, 0.52f, 0.68f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float fx = x / (float)size;
                float fy = y / (float)size;

                float baseN = Fbm(fx * 3.1f, fy * 3.1f, 4);
                float coarse = Fbm(fx * 1.2f + 40f, fy * 1.2f, 3);
                float streak = Mathf.PerlinNoise(fx * 22f + coarse * 6f, fy * 55f + baseN * 4f);
                float frost = Fbm(fx * 48f + 90f, fy * 48f + 20f, 2);
                float cells = Mathf.Abs(Mathf.PerlinNoise(fx * 14f, fy * 14f) - Mathf.PerlinNoise(fx * 14f + 31f, fy * 14f + 17f));
                float crack = Mathf.Pow(Mathf.Clamp01(cells * 2.2f - 0.35f), 1.8f);

                float hx = fx * 73f + Hash(fy * 20f, 1f);
                float hy = fy * 73f + Hash(fx * 20f, 2f);
                float sparkle = Mathf.Pow(Hash(hx, hy), 12f) * 0.35f;

                float blend = baseN * 0.38f + streak * 0.28f + frost * 0.18f + coarse * 0.16f;
                Color c = Color.Lerp(iceDeep, iceBright, blend);
                c = Color.Lerp(c, iceCyan, Mathf.Clamp01(streak * 0.55f + frost * 0.25f));
                c = Color.Lerp(c, iceShadow, crack * 0.45f);
                c = Color.Lerp(c, iceBright, sparkle);

                float micro = 0.94f + 0.06f * Mathf.PerlinNoise(fx * 120f, fy * 120f);
                c *= micro;

                float vignette = Mathf.Lerp(0.92f, 1.04f, Mathf.PerlinNoise(fx * 2f + 5f, fy * 2f + 2f));
                c *= vignette;

                t.SetPixel(x, y, c);
            }
        }

        t.Apply();
        return t;
    }
}
