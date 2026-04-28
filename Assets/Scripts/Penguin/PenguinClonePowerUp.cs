using UnityEngine;

public class PenguinClonePowerUp : MonoBehaviour
{
    GameObject _clone;

    public void Apply()
    {
        Clear();

        PenguinPowerUpController.SuppressAutoHud = true;
        _clone = Instantiate(gameObject, transform.position, transform.rotation);
        PenguinPowerUpController.SuppressAutoHud = false;
        _clone.name = "MirrorPenguinClone";
        var mirror = _clone.GetComponent<PenguinMirrorClone>();
        if (mirror == null)
            mirror = _clone.AddComponent<PenguinMirrorClone>();
        mirror.Bind(transform);

        foreach (var controller in _clone.GetComponentsInChildren<PenguinPowerUpController>())
            Destroy(controller);
        foreach (var effect in _clone.GetComponentsInChildren<PenguinClonePowerUp>())
            Destroy(effect);
        foreach (var hud in _clone.GetComponentsInChildren<PenguinPowerUpHud>())
            Destroy(hud.gameObject);

        var drive = _clone.GetComponent<PenguinSlideDrive>();
        if (drive != null)
            Destroy(drive);
        var steer = _clone.GetComponent<PenguinSteer>();
        if (steer != null)
            Destroy(steer);

        var colors = _clone.GetComponent<PenguinColorCycle>();
        if (colors != null)
            colors.ApplySkin((int)PenguinPowerUpType.Green);

        MakeGhost(_clone);
        IgnoreMainCollisions(_clone);
    }

    public void Clear()
    {
        if (_clone != null)
            Destroy(_clone);
        _clone = null;
    }

    void IgnoreMainCollisions(GameObject clone)
    {
        var mainCols = GetComponentsInChildren<Collider>();
        var cloneCols = clone.GetComponentsInChildren<Collider>();
        foreach (var a in mainCols)
        {
            foreach (var b in cloneCols)
            {
                if (a != null && b != null)
                    Physics.IgnoreCollision(a, b, true);
            }
        }
    }

    static void MakeGhost(GameObject clone)
    {
        foreach (var renderer in clone.GetComponentsInChildren<Renderer>())
        {
            var mats = renderer.materials;
            for (int i = 0; i < mats.Length; i++)
            {
                Color c = mats[i].HasProperty("_BaseColor") ? mats[i].GetColor("_BaseColor") : mats[i].color;
                c.a = 0.38f;
                if (mats[i].HasProperty("_BaseColor"))
                    mats[i].SetColor("_BaseColor", c);
                mats[i].color = c;
                if (mats[i].HasProperty("_Surface"))
                    mats[i].SetFloat("_Surface", 1f);
                if (mats[i].HasProperty("_Blend"))
                    mats[i].SetFloat("_Blend", 0f);
                if (mats[i].HasProperty("_SrcBlend"))
                    mats[i].SetFloat("_SrcBlend", 5f);
                if (mats[i].HasProperty("_DstBlend"))
                    mats[i].SetFloat("_DstBlend", 10f);
                if (mats[i].HasProperty("_ZWrite"))
                    mats[i].SetFloat("_ZWrite", 0f);
                mats[i].EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                mats[i].renderQueue = 3000;
            }
            renderer.materials = mats;
        }
    }
}
