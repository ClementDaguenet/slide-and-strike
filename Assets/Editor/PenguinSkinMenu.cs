#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class PenguinSkinMenu
{
    const string PlayerName = "pinguin-black";
    const string ColorCycleScriptPath = "Assets/Scripts/Penguin/PenguinColorCycle.cs";

    static readonly string[] FbxPathsInOrder =
    {
        "Assets/Models/pinguin-black.fbx",
        "Assets/Models/penguin-blue.fbx",
        "Assets/Models/penguin-red.fbx",
        "Assets/Models/penguin-pink.fbx",
        "Assets/Models/penguin-green.fbx"
    };

    [MenuItem("Slide and Strike/Skins pingouin (FBX)")]
    public static void AssignSkinsFromFbx()
    {
        var go = GameObject.Find(PlayerName);
        if (go == null)
        {
            EditorUtility.DisplayDialog("Skins", "Pas d’objet « " + PlayerName + " » dans cette scène.", "OK");
            return;
        }

        var ms = AssetDatabase.LoadAssetAtPath<MonoScript>(ColorCycleScriptPath);
        if (ms == null || ms.GetClass() == null)
        {
            EditorUtility.DisplayDialog("Skins", "Script manquant : " + ColorCycleScriptPath, "OK");
            return;
        }

        var type = ms.GetClass();
        var cycle = go.GetComponent(type);
        if (cycle == null)
            cycle = go.AddComponent(type);

        var built = new List<(Mesh mesh, Material[] materials)>();
        foreach (string path in FbxPathsInOrder)
        {
            string abs = System.IO.Path.Combine(Application.dataPath, path.Substring(7));
            if (!System.IO.File.Exists(abs))
            {
                Debug.LogWarning("FBX absent : " + path);
                built.Add((null, System.Array.Empty<Material>()));
                continue;
            }

            var root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (root == null)
            {
                built.Add((null, System.Array.Empty<Material>()));
                continue;
            }

            if (!TryGetMeshAndMaterialsInRendererOrder(root, out Mesh mesh, out Material[] mats))
            {
                Debug.LogWarning("Pas de mesh sur : " + path);
                built.Add((null, System.Array.Empty<Material>()));
                continue;
            }

            foreach (var m in mats)
                EnsureUrpLitWithTextures(m);
            built.Add((mesh, mats));
        }

        if (built.Count > 0 && built[0].materials != null && built[0].materials.Length > 0)
        {
            var reference = built[0].materials;
            for (var i = 1; i < built.Count; i++)
            {
                var tuple = built[i];
                if (tuple.materials == null || tuple.materials.Length == 0)
                    continue;
                built[i] = (tuple.mesh, PenguinMaterialAlign.AlignToReference(reference, tuple.materials));
            }
        }

        var so = new SerializedObject(cycle);
        var prop = so.FindProperty("skinsInOrder");
        prop.ClearArray();
        for (var i = 0; i < built.Count; i++)
        {
            prop.InsertArrayElementAtIndex(i);
            var el = prop.GetArrayElementAtIndex(i);
            el.FindPropertyRelative("mesh").objectReferenceValue = built[i].mesh;
            var matsProp = el.FindPropertyRelative("materials");
            var materials = built[i].materials;
            matsProp.arraySize = materials != null ? materials.Length : 0;
            if (materials != null)
            {
                for (var j = 0; j < materials.Length; j++)
                    matsProp.GetArrayElementAtIndex(j).objectReferenceValue = materials[j];
            }
        }

        so.FindProperty("startSkinIndex").intValue = 0;
        var robeSlots = so.FindProperty("robeMaterialSlots");
        robeSlots.arraySize = 1;
        robeSlots.GetArrayElementAtIndex(0).intValue = 2;
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(cycle);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog("Skins", "C’est bon. Sauve la scène si tu veux garder ça.", "OK");
    }

    static bool TryGetMeshAndMaterialsInRendererOrder(GameObject root, out Mesh mesh, out Material[] materials)
    {
        mesh = null;
        materials = null;

        MeshFilter bestMf = null;
        float bestSize = -1f;
        foreach (var mf in root.GetComponentsInChildren<MeshFilter>(true))
        {
            if (mf.sharedMesh == null)
                continue;
            float s = mf.sharedMesh.bounds.size.sqrMagnitude;
            if (s > bestSize)
            {
                bestSize = s;
                bestMf = mf;
            }
        }

        if (bestMf != null)
        {
            var mr = bestMf.GetComponent<MeshRenderer>();
            if (mr != null && mr.sharedMaterials != null && mr.sharedMaterials.Length > 0)
            {
                mesh = bestMf.sharedMesh;
                materials = mr.sharedMaterials;
                return true;
            }
        }

        SkinnedMeshRenderer bestSmr = null;
        bestSize = -1f;
        foreach (var smr in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            if (smr.sharedMesh == null)
                continue;
            float s = smr.sharedMesh.bounds.size.sqrMagnitude;
            if (s > bestSize)
            {
                bestSize = s;
                bestSmr = smr;
            }
        }

        if (bestSmr != null && bestSmr.sharedMaterials != null && bestSmr.sharedMaterials.Length > 0)
        {
            mesh = bestSmr.sharedMesh;
            materials = bestSmr.sharedMaterials;
            return true;
        }

        return false;
    }

    static void EnsureUrpLitWithTextures(Material m)
    {
        if (m == null)
            return;

        var lit = Shader.Find("Universal Render Pipeline/Lit");
        if (lit == null)
            return;

        Texture albedo = null;
        if (m.HasProperty("_BaseMap"))
            albedo = m.GetTexture("_BaseMap");
        if (albedo == null && m.HasProperty("_MainTex"))
            albedo = m.GetTexture("_MainTex");

        Color baseColor = Color.white;
        if (m.HasProperty("_BaseColor"))
            baseColor = m.GetColor("_BaseColor");
        else if (m.HasProperty("_Color"))
            baseColor = m.GetColor("_Color");

        float smooth = 0.5f;
        if (m.HasProperty("_Smoothness"))
            smooth = m.GetFloat("_Smoothness");
        else if (m.HasProperty("_Glossiness"))
            smooth = m.GetFloat("_Glossiness");

        float metallic = 0f;
        if (m.HasProperty("_Metallic"))
            metallic = m.GetFloat("_Metallic");

        m.shader = lit;
        if (albedo != null)
        {
            m.SetTexture("_BaseMap", albedo);
            m.SetTextureScale("_BaseMap", m.HasProperty("_MainTex") ? m.GetTextureScale("_MainTex") : Vector2.one);
            m.SetTextureOffset("_BaseMap", m.HasProperty("_MainTex") ? m.GetTextureOffset("_MainTex") : Vector2.zero);
        }

        m.SetColor("_BaseColor", albedo != null ? Color.white : baseColor);

        if (m.HasProperty("_Smoothness"))
            m.SetFloat("_Smoothness", smooth);
        if (m.HasProperty("_Metallic"))
            m.SetFloat("_Metallic", metallic);

        if (m.HasProperty("_BumpMap") && m.GetTexture("_BumpMap") != null)
        {
            m.EnableKeyword("_NORMALMAP");
            m.SetFloat("_BumpScale", m.HasProperty("_BumpScale") ? m.GetFloat("_BumpScale") : 1f);
        }

        EditorUtility.SetDirty(m);
    }
}
#endif
