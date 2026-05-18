using System;
using System.Collections.Generic;
using UnityEngine;

public static class ImportedPowerUpCollectibleVisual
{
    sealed class TextureSlot
    {
        public Texture2D Albedo;
        public Texture2D Normal;
        public bool Resolved;
    }

    static readonly Dictionary<string, TextureSlot> TextureSlots = new();

    public static bool TrySpawnUnderCollectible(
        Transform pickupRoot,
        string prefabResourcePath,
        string textureResourceFolder,
        string textureBaseName,
        string childObjectName,
        float referenceRadiusWorld,
        float heightMultiplier,
        Vector3 localEulerAngles = default)
    {
        GameObject prefab = Resources.Load<GameObject>(prefabResourcePath);
        if (prefab == null)
            return false;

        MeshFilter mf = pickupRoot.GetComponent<MeshFilter>();
        if (mf != null)
            UnityEngine.Object.Destroy(mf);
        MeshRenderer mr = pickupRoot.GetComponent<MeshRenderer>();
        if (mr != null)
        {
            // Destroy is end-of-frame: hide immediately so we don't briefly draw the sphere + FBX stacked.
            mr.enabled = false;
            UnityEngine.Object.Destroy(mr);
        }

        GameObject viz = UnityEngine.Object.Instantiate(prefab, pickupRoot, false);
        viz.name = childObjectName;
        viz.transform.localPosition = Vector3.zero;
        viz.transform.localRotation = Quaternion.identity;
        viz.transform.localScale = Vector3.one;

        ImportedModelLayout.StripEmbeddedSceneObjects(viz);

        if (localEulerAngles.sqrMagnitude > 1e-8f)
            viz.transform.localRotation = Quaternion.Euler(localEulerAngles);

        float targetHeight = Mathf.Max(0.09f, referenceRadiusWorld * 2.35f * 0.5f) * heightMultiplier;
        ImportedModelLayout.ScaleToWorldHeight(viz, Mathf.Max(0.04f, targetHeight));

        ApplyImportedTextureLitMaterials(viz, textureResourceFolder, textureBaseName);

        ImportedModelLayout.PositionBottomCenterNearWorldAnchor(viz, pickupRoot.position);
        return true;
    }

    static void EnsureTexturesLoaded(TextureSlot slot, string folder, string baseName)
    {
        if (slot.Resolved)
            return;
        slot.Resolved = true;

        if (string.IsNullOrEmpty(folder) || string.IsNullOrEmpty(baseName))
            return;

        foreach (Texture2D t in Resources.LoadAll<Texture2D>(folder))
        {
            if (t == null)
                continue;
            if (t.name == baseName)
                slot.Albedo = t;
            else if (t.name == baseName + "_normal")
                slot.Normal = t;
        }

        if (slot.Albedo != null)
            return;

        foreach (Texture2D t in Resources.LoadAll<Texture2D>(folder))
        {
            if (t == null)
                continue;
            string n = t.name;
            if (!n.StartsWith(baseName, StringComparison.Ordinal))
                continue;
            if (n.EndsWith("_normal", StringComparison.OrdinalIgnoreCase))
            {
                if (slot.Normal == null)
                    slot.Normal = t;
            }
            else if (!n.Contains("_metallic", StringComparison.OrdinalIgnoreCase) &&
                     !n.Contains("_roughness", StringComparison.OrdinalIgnoreCase))
            {
                if (slot.Albedo == null)
                    slot.Albedo = t;
            }
        }
    }

    static TextureSlot GetTextureSlot(string folder, string baseName)
    {
        string key = folder + "::" + baseName;
        if (!TextureSlots.TryGetValue(key, out TextureSlot slot))
        {
            slot = new TextureSlot();
            TextureSlots[key] = slot;
        }

        EnsureTexturesLoaded(slot, folder, baseName);
        return slot;
    }

    static void ApplyImportedTextureLitMaterials(GameObject root, string textureFolder, string textureBaseName)
    {
        TextureSlot slot = GetTextureSlot(textureFolder, textureBaseName);
        if (slot.Albedo == null)
            return;

        Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        if (shader == null)
            return;

        foreach (Renderer r in root.GetComponentsInChildren<Renderer>())
        {
            int n = Mathf.Max(1, r.sharedMaterials.Length);
            var mats = new Material[n];
            for (var i = 0; i < n; i++)
                mats[i] = BuildLitClone(shader, slot.Albedo, slot.Normal);
            r.sharedMaterials = mats;
        }
    }

    static Material BuildLitClone(Shader shader, Texture2D albedo, Texture2D normal)
    {
        var m = new Material(shader);
        if (shader != null && shader.name.IndexOf("Universal", StringComparison.Ordinal) >= 0)
        {
            m.SetTexture("_BaseMap", albedo);
            m.SetColor("_BaseColor", Color.white);
            if (m.HasProperty("_Surface"))
                m.SetFloat("_Surface", 0f);
            if (m.HasProperty("_Smoothness"))
                m.SetFloat("_Smoothness", 0.42f);
            if (m.HasProperty("_Metallic"))
                m.SetFloat("_Metallic", 0f);
            if (normal != null && m.HasProperty("_BumpMap"))
            {
                m.EnableKeyword("_NORMALMAP");
                m.SetTexture("_BumpMap", normal);
                if (m.HasProperty("_BumpScale"))
                    m.SetFloat("_BumpScale", 1f);
            }
        }
        else
        {
            m.mainTexture = albedo;
            m.color = Color.white;
            if (normal != null && m.HasProperty("_BumpMap"))
            {
                m.EnableKeyword("_NORMALMAP");
                m.SetTexture("_BumpMap", normal);
            }
        }

        return m;
    }
}
