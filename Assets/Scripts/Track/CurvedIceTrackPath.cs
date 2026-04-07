using System.Collections.Generic;
using UnityEngine;

public static class CurvedIceTrackPath
{
    public static void AppendStraight(ref Vector3 pos, ref Vector3 fwd, float length, int nSeg, List<Vector3> mids, List<Vector3> rights, float totalVerticalDelta = 0f)
    {
        nSeg = Mathf.Max(1, nSeg);
        float step = length / nSeg;
        float yStep = totalVerticalDelta / nSeg;
        Vector3 f = new Vector3(fwd.x, 0f, fwd.z).normalized;
        if (f.sqrMagnitude < 1e-6f)
            f = Vector3.right;
        fwd = f;

        for (int i = 0; i <= nSeg; i++)
        {
            mids.Add(pos);
            rights.Add(Vector3.Cross(Vector3.up, fwd).normalized);
            if (i < nSeg)
            {
                pos += fwd * step;
                pos.y += yStep;
            }
        }
    }

    public static void AppendArc(ref Vector3 pos, ref Vector3 fwd, float R, float sweepRad, float dropTotal, int turnSign, int nSeg, List<Vector3> mids, List<Vector3> rights)
    {
        nSeg = Mathf.Max(6, nSeg);
        Vector3 f = new Vector3(fwd.x, 0f, fwd.z).normalized;
        if (f.sqrMagnitude < 1e-6f)
            f = Vector3.right;

        Vector3 fwd0 = f;
        Vector3 rW0 = Vector3.Cross(Vector3.up, fwd0).normalized;
        Vector3 turnDir0 = (-rW0) * Mathf.Sign(turnSign);
        float dropPerRad = sweepRad > 1e-4f ? dropTotal / sweepRad : 0f;

        Vector3 junction = pos;

        for (int j = 0; j <= nSeg; j++)
        {
            float ang = j / (float)nSeg * sweepRad;
            float sin = Mathf.Sin(ang);
            float cos = Mathf.Cos(ang);
            Vector3 p = junction + fwd0 * (R * sin) + turnDir0 * (R * (1f - cos)) + Vector3.up * (-dropPerRad * ang);

            if (j == 0 && mids.Count > 0 && (p - mids[mids.Count - 1]).sqrMagnitude < 0.0004f)
                continue;

            mids.Add(p);

            Vector3 tanH = fwd0 * (R * cos) + turnDir0 * (R * sin);
            tanH = new Vector3(tanH.x, 0f, tanH.z);
            if (tanH.sqrMagnitude < 1e-8f)
                tanH = fwd0;
            tanH.Normalize();
            rights.Add(Vector3.Cross(Vector3.up, tanH).normalized);
        }

        float cs = Mathf.Cos(sweepRad);
        float sn = Mathf.Sin(sweepRad);
        Vector3 tanEnd = fwd0 * (R * cs) + turnDir0 * (R * sn);
        tanEnd = new Vector3(tanEnd.x, 0f, tanEnd.z);
        fwd = tanEnd.sqrMagnitude > 1e-8f ? tanEnd.normalized : fwd0;

        pos = junction + fwd0 * (R * sn) + turnDir0 * (R * (1f - cs)) + Vector3.up * (-dropPerRad * sweepRad);
    }
}
