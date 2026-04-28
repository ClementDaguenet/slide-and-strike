using System.Text.RegularExpressions;
using UnityEngine;

public static class PenguinMaterialAlign
{
    public static Material[] AlignToReference(Material[] reference, Material[] variant)
    {
        if (variant == null || variant.Length == 0)
            return variant;
        if (reference == null || reference.Length == 0)
            return variant;

        var result = new Material[reference.Length];
        for (var i = 0; i < reference.Length; i++)
        {
            var key = KeyFromMaterialName(reference[i] != null ? reference[i].name : "");
            Material found = null;
            if (!string.IsNullOrEmpty(key))
            {
                foreach (var m in variant)
                {
                    if (m == null)
                        continue;
                    if (KeyFromMaterialName(m.name) == key)
                    {
                        found = m;
                        break;
                    }
                }
            }

            result[i] = found != null ? found : (i < variant.Length ? variant[i] : reference[i]);
        }

        return result;
    }

    public static Material[] MergeColoredPartsOnly(Material[] reference, Material[] variant)
    {
        if (reference == null || reference.Length == 0)
            return variant;
        if (variant == null || variant.Length == 0)
            return reference;

        var result = new Material[reference.Length];
        for (var i = 0; i < reference.Length; i++)
            result[i] = reference[i];

        for (var i = 0; i < reference.Length; i++)
        {
            if (reference[i] == null)
                continue;
            if (!IsSwappableColoredPart(reference[i].name))
                continue;

            var key = KeyFromMaterialName(reference[i].name);
            if (string.IsNullOrEmpty(key))
                continue;

            var found = FindMaterialByKey(variant, key);
            if (found != null)
                result[i] = found;
        }

        return result;
    }

    public static Material FindMatchingForReferenceSlot(Material referenceSlot, Material[] variantMaterials)
    {
        if (referenceSlot == null || variantMaterials == null || variantMaterials.Length == 0)
            return null;
        var key = KeyFromMaterialName(referenceSlot.name);
        if (string.IsNullOrEmpty(key))
            return null;
        return FindMaterialByKey(variantMaterials, key);
    }

    static Material FindMaterialByKey(Material[] materials, string key)
    {
        foreach (var m in materials)
        {
            if (m == null)
                continue;
            if (KeyFromMaterialName(m.name) == key)
                return m;
        }

        return null;
    }

    public static bool IsFixedPartFromBlackSkin(string rawName)
    {
        if (string.IsNullOrEmpty(rawName))
            return false;
        var s = rawName.ToLowerInvariant();
        if (s.Contains("eye") || s.Contains("yeux") || s.Contains("beak") || s.Contains("bec") ||
            s.Contains("foot") || s.Contains("feet") || s.Contains("pied") || s.Contains("patte") ||
            s.Contains("nose") || s.Contains("nez") || s.Contains("flipper") || s.Contains("aileron") ||
            s.Contains("mouth") || s.Contains("bouche"))
            return true;
        return false;
    }

    public static bool IsRobeMaterialName(string rawName)
    {
        if (string.IsNullOrEmpty(rawName))
            return false;
        if (IsFixedPartFromBlackSkin(rawName))
            return false;

        var k = KeyFromMaterialName(rawName);
        if (string.IsNullOrEmpty(k))
            return false;
        k = k.ToLowerInvariant();
        return k.Contains("body") || k.Contains("robe") || k.Contains("cloth") || k.Contains("suit") ||
               k.Contains("tux") || k.Contains("veste") || k.Contains("maillot") || k.Contains("costume") ||
               k.Contains("shirt") || k.Contains("torso") || k.Contains("dress");
    }

    static bool IsSwappableColoredPart(string rawName)
    {
        if (string.IsNullOrEmpty(rawName))
            return false;
        if (IsFixedPartFromBlackSkin(rawName))
            return false;

        return IsRobeMaterialName(rawName);
    }

    static string KeyFromMaterialName(string raw)
    {
        if (string.IsNullOrEmpty(raw))
            return "";
        var s = raw.ToLowerInvariant();
        s = Regex.Replace(s, @"\s+", "");
        s = Regex.Replace(s, @"_?(penguin|pinguin|mat|material)[-_]?", "");
        s = Regex.Replace(s,
            @"_?(black|blue|red|pink|green|noir|bleu|rouge|rose|vert|white|blanc|jaune|yellow|orange|violet|purple)\b",
            "");
        s = Regex.Replace(s, @"[0-9]+$", "");
        s = s.Trim('_', '-', '.');
        return s;
    }
}
