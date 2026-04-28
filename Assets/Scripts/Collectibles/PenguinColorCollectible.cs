using UnityEngine;

public class PenguinColorCollectible : MonoBehaviour
{
    [SerializeField] PenguinPowerUpType powerUpType = PenguinPowerUpType.Blue;
    [SerializeField] float spinSpeed = 90f;
    bool _collected;

    public void Configure(int nextSkinIndex, float effectDuration)
    {
        powerUpType = (PenguinPowerUpType)nextSkinIndex;
    }

    void Update()
    {
        transform.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.World);
    }

    void OnTriggerEnter(Collider other)
    {
        var mirror = other.GetComponentInParent<PenguinMirrorClone>();
        if (mirror != null)
        {
            TryCollect(mirror.SourcePowerUps);
            return;
        }

        var controller = other.GetComponentInParent<PenguinPowerUpController>();
        if (controller == null)
        {
            var colorCycle = other.GetComponentInParent<PenguinColorCycle>();
            if (colorCycle != null)
                controller = colorCycle.gameObject.AddComponent<PenguinPowerUpController>();
        }

        if (controller == null)
            return;

        TryCollect(controller);
    }

    public bool TryCollect(PenguinPowerUpController controller)
    {
        if (_collected || controller == null)
            return false;
        if (!controller.TryStore(powerUpType))
            return false;

        _collected = true;
        gameObject.SetActive(false);
        return true;
    }
}
