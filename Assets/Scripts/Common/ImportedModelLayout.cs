using UnityEngine;

public static class ImportedModelLayout
{
    public static void StripEmbeddedSceneObjects(GameObject root)
    {
        foreach (var light in root.GetComponentsInChildren<Light>(true))
            UnityEngine.Object.Destroy(light);
        foreach (var cam in root.GetComponentsInChildren<Camera>(true))
            UnityEngine.Object.Destroy(cam);
        foreach (var listener in root.GetComponentsInChildren<AudioListener>(true))
            UnityEngine.Object.Destroy(listener);
    }

    public static bool TryEncapsulatingRendererBounds(GameObject root, out Bounds worldBounds)
    {
        var renderers = root.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
        {
            worldBounds = default;
            return false;
        }

        worldBounds = renderers[0].bounds;
        for (var i = 1; i < renderers.Length; i++)
            worldBounds.Encapsulate(renderers[i].bounds);
        return true;
    }

    public static void ScaleToWorldHeight(GameObject root, float targetWorldHeight)
    {
        if (targetWorldHeight <= 0f || !TryEncapsulatingRendererBounds(root, out Bounds b) || b.size.y <= 0.001f)
            return;

        float scaleFactor = targetWorldHeight / b.size.y;
        root.transform.localScale *= scaleFactor;
    }

    public static void PositionBottomCenterNearWorldAnchor(GameObject root, Vector3 worldAnchor)
    {
        if (!TryEncapsulatingRendererBounds(root, out Bounds b))
            return;

        Vector3 delta = worldAnchor - new Vector3(b.center.x, b.min.y, b.center.z);
        root.transform.position += delta;
    }
}
