using UnityEngine;

public class TrackFinishTrigger : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        if (other.GetComponentInParent<PenguinSlideDrive>() == null)
            return;

        BottleScore.Finish();
    }
}
