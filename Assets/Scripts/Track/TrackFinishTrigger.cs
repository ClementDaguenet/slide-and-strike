using UnityEngine;

public class TrackFinishTrigger : MonoBehaviour
{
    [SerializeField] float minTrackProgress = 0.985f;

    void OnTriggerEnter(Collider other)
    {
        TryFinish(other);
    }

    void OnCollisionEnter(Collision collision)
    {
        TryFinish(collision.collider);
    }

    void TryFinish(Collider other)
    {
        if (other.GetComponentInParent<PenguinMirrorClone>() != null)
            return;
        var drive = other.GetComponentInParent<PenguinSlideDrive>();
        if (drive == null)
            return;
        if (!CurvedIceTrack.IsNearEnd(drive.transform.position, minTrackProgress))
            return;

        BottleScore.Finish();
    }
}
