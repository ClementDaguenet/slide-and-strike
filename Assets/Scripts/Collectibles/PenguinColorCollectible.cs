using UnityEngine;

public class PenguinColorCollectible : MonoBehaviour
{
    [SerializeField] int skinIndex = 1;
    [SerializeField] float duration = 5f;
    [SerializeField] float spinSpeed = 90f;

    public void Configure(int nextSkinIndex, float effectDuration)
    {
        skinIndex = nextSkinIndex;
        duration = effectDuration;
    }

    void Update()
    {
        transform.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.World);
    }

    void OnTriggerEnter(Collider other)
    {
        var colorCycle = other.GetComponentInParent<PenguinColorCycle>();
        if (colorCycle == null)
            return;

        colorCycle.ActivateTemporarySkin(skinIndex, duration);
        gameObject.SetActive(false);
    }
}
