#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class PenguinImportSetup
{
    const string ModelsFolder = "Assets/Models";
    const string PreferredModelPath = "Assets/Models/pinguin-black.fbx";
    const string ScenePath = "Assets/Scenes/SampleScene.unity";

    public const string PlayerRootName = "pinguin-black";

    static string PreferredAbsolutePath => Path.Combine(Application.dataPath, "Models", "pinguin-black.fbx");

    public static string AbsolutePathFromAsset(string assetPath)
    {
        if (string.IsNullOrEmpty(assetPath) || !assetPath.StartsWith("Assets/", StringComparison.Ordinal))
            return null;
        return Path.Combine(Application.dataPath, assetPath.Substring(7));
    }

    public static string ResolveModelInModels()
    {
        if (File.Exists(PreferredAbsolutePath))
            return PreferredModelPath;
        return FindFirstFbxInModels();
    }

    public static string FindFirstFbxInModels()
    {
        if (!AssetDatabase.IsValidFolder(ModelsFolder))
            return null;
        string[] guids = AssetDatabase.FindAssets("", new[] { ModelsFolder });
        return guids
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(p => p.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase))
            .OrderBy(p => p, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();
    }

    [MenuItem("Slide and Strike/Import pingouin (Téléchargements) et appliquer à la scène")]
    static void ImportFromDownloadsAndApply()
    {
        if (!TryCopyFromDownloads())
        {
            if (!File.Exists(PreferredAbsolutePath) && FindFirstFbxInModels() == null)
            {
                EditorUtility.DisplayDialog(
                    "FBX introuvable",
                    "Aucun .fbx dans Téléchargements ni dans " + ModelsFolder + ".",
                    "OK");
                return;
            }
        }

        AssetDatabase.Refresh();
        string path = ResolveModelInModels();
        if (path != null)
            ApplyModelToScene(path, showDialog: true);
    }

    [MenuItem("Slide and Strike/Appliquer le FBX dans Models (pingouin)")]
    static void ApplyFromModelsMenu()
    {
        AssetDatabase.Refresh();
        string path = ResolveModelInModels();
        if (path == null)
        {
            EditorUtility.DisplayDialog("Manquant", "Aucun fichier .fbx dans " + ModelsFolder + ".", "OK");
            return;
        }

        ApplyModelToScene(path, showDialog: true);
    }

    static bool TryCopyFromDownloads()
    {
        string downloads = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
        if (!Directory.Exists(downloads))
            return File.Exists(PreferredAbsolutePath) || FindFirstFbxInModels() != null;

        string[] fbxs = Directory.GetFiles(downloads, "*.fbx", SearchOption.AllDirectories);
        string pick = fbxs.FirstOrDefault(f =>
            Path.GetFileNameWithoutExtension(f).ToLowerInvariant().Contains("pingouin"))
            ?? fbxs.FirstOrDefault(f =>
                Path.GetFileNameWithoutExtension(f).ToLowerInvariant().Contains("penguin"));
        if (pick == null && fbxs.Length > 0)
            pick = fbxs.OrderByDescending(f => File.GetLastWriteTimeUtc(f)).First();

        if (pick == null)
            return File.Exists(PreferredAbsolutePath) || FindFirstFbxInModels() != null;

        string dir = Path.GetDirectoryName(PreferredAbsolutePath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        File.Copy(pick, PreferredAbsolutePath, true);
        return true;
    }

    public static bool ApplyModelToScene(string modelAssetPath, bool showDialog)
    {
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(modelAssetPath);
        if (model == null)
        {
            if (showDialog)
                EditorUtility.DisplayDialog("Erreur", "Impossible de charger : " + modelAssetPath, "OK");
            return false;
        }

        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        GameObject root = GameObject.Find(PlayerRootName) ?? GameObject.Find("Penguin") ?? GameObject.Find("Blue Sphere");
        if (root == null)
        {
            if (showDialog)
                EditorUtility.DisplayDialog("Erreur", "Objet « " + PlayerRootName + " », « Penguin » ou « Blue Sphere » introuvable.", "OK");
            return false;
        }

        var meshRenderer = model.GetComponentInChildren<MeshRenderer>();
        var skinned = model.GetComponentInChildren<SkinnedMeshRenderer>();
        Mesh mesh = null;
        Material[] materials = null;

        if (meshRenderer != null)
        {
            var mfModel = meshRenderer.GetComponent<MeshFilter>();
            if (mfModel != null)
                mesh = mfModel.sharedMesh;
            materials = meshRenderer.sharedMaterials;
        }

        if (mesh == null && skinned != null)
        {
            mesh = skinned.sharedMesh;
            materials = skinned.sharedMaterials;
        }

        if (mesh == null)
        {
            mesh = AssetDatabase.LoadAllAssetsAtPath(modelAssetPath)
                .OfType<Mesh>()
                .OrderByDescending(m => m.vertexCount)
                .FirstOrDefault();
        }

        if (mesh == null)
        {
            if (showDialog)
                EditorUtility.DisplayDialog("Erreur", "Aucun mesh utilisable dans le FBX.", "OK");
            return false;
        }

        if (materials == null || materials.Length == 0)
            materials = AssetDatabase.LoadAllAssetsAtPath(modelAssetPath).OfType<Material>().ToArray();

        var mf = root.GetComponent<MeshFilter>();
        var mr = root.GetComponent<MeshRenderer>();
        if (mf != null)
            mf.sharedMesh = mesh;
        if (mr != null && materials != null && materials.Length > 0)
            mr.sharedMaterials = materials;

        root.name = PlayerRootName;

        float h = mesh.bounds.size.y;
        if (h > 0.001f)
            root.transform.localScale = Vector3.one * (1f / h);

        Bounds b = mesh.bounds;
        foreach (var old in root.GetComponents<SphereCollider>())
            UnityEngine.Object.DestroyImmediate(old);
        foreach (var old in root.GetComponents<CapsuleCollider>())
            UnityEngine.Object.DestroyImmediate(old);

        var cap = root.gameObject.AddComponent<CapsuleCollider>();
        cap.direction = 1;
        cap.center = b.center;
        cap.height = Mathf.Max(b.size.y, 0.0001f);
        cap.radius = Mathf.Max(b.extents.x, b.extents.z, cap.height * 0.12f, 0.0001f);
        var slide = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>("Assets/Physics/SlopeSlide.physicMaterial");
        if (slide != null)
            cap.sharedMaterial = slide;

        var body = root.GetComponent<PenguinBodyCollider>();
        if (body == null)
            root.gameObject.AddComponent<PenguinBodyCollider>();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        if (showDialog)
            EditorUtility.DisplayDialog("OK", "Pingouin appliqué. Lance Play pour tester.", "OK");
        return true;
    }
}
#endif
