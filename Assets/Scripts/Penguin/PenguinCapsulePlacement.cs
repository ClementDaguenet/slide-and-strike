using UnityEngine;

public static class PenguinCapsulePlacement
{
    public static Vector3 GetWorldBottom(Transform t, CapsuleCollider cap)
    {
        if (cap == null || t == null)
            return t != null ? t.position : Vector3.zero;

        Vector3 axis = cap.direction == 0 ? Vector3.right : cap.direction == 1 ? Vector3.up : Vector3.forward;
        return t.TransformPoint(cap.center - axis * (cap.height * 0.5f));
    }

    public static Vector3 PivotPositionForBottomAt(Transform t, CapsuleCollider cap, Vector3 hitPoint, Vector3 hitNormal,
        float surfacePadding)
    {
        Vector3 bottom = GetWorldBottom(t, cap);
        return t.position + (hitPoint - bottom) + hitNormal * surfacePadding;
    }
}
