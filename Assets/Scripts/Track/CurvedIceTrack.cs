using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshCollider))]
[DefaultExecutionOrder(-50)]
public class CurvedIceTrack : MonoBehaviour
{
    [SerializeField] float firstStraightLength = 78f;
    [SerializeField] [Min(4)] int firstStraightSegments = 36;
    [SerializeField] [Min(0f)] float firstStraightVerticalDrop = 14f;

    [SerializeField] float trackYawDegrees = 270f;

    [SerializeField] [Min(1)] int courseLegs = 24;
    [SerializeField] float legStraightLength = 72f;
    [SerializeField] [Min(4)] int legStraightSegments = 34;
    [SerializeField] float arcRadius = 68f;
    [SerializeField] [Range(25f, 130f)] float arcSweepDegrees = 68f;
    [SerializeField] [Min(6)] int arcSegments = 26;
    [SerializeField] float verticalDropPerArc = 16f;
    [SerializeField] bool alternateLeftRightTurns = true;
    [SerializeField] bool randomizeCourse = true;
    [SerializeField] int courseRandomSeed;
    [SerializeField] float legStraightLengthJitter = 16f;
    [SerializeField] float arcSweepDegreesJitter = 22f;
    [SerializeField] float arcRadiusJitter = 18f;
    [SerializeField] float verticalDropPerArcJitter = 6f;
    [SerializeField] float firstStraightLengthJitter = 10f;

    [SerializeField] float trackWidth = 15f;
    [SerializeField] [Min(1)] int widthSegments = 4;

    [SerializeField] float wallHeight = 11f;
    [SerializeField] float wallThickness = 1.65f;
    [SerializeField] [Min(0f)] float wallInsetFromTrackEdge = 1.1f;
    [SerializeField] [Min(0.25f)] float wallMinOutwardDepth = 0.52f;
    [SerializeField] [Min(0f)] float wallExtendBelow = 24f;
    [SerializeField] bool buildEndWallCaps = true;
    [SerializeField] bool buildSafetyNetCollider = true;
    [SerializeField] float safetyNetPaddingXZ = 95f;
    [SerializeField] [Min(4f)] float safetyNetThickness = 38f;

    [SerializeField] bool generateOnAwake = true;

    Mesh _trackMesh;
    Transform _wallLeft;
    Transform _wallRight;
    Transform _wallStart;
    Transform _wallEnd;
    Transform _safetyNet;

    void Awake()
    {
        if (generateOnAwake)
            RebuildMesh();
    }

    void OnDestroy()
    {
        if (_trackMesh != null)
            Destroy(_trackMesh);
    }

    [ContextMenu("Rebuild mesh")]
    public void RebuildMesh()
    {
        EnsureWallParents();

        if (_trackMesh == null)
        {
            _trackMesh = new Mesh { name = "CurvedIceTrack" };
            _trackMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        }
        else
            _trackMesh.Clear();

        var mids = new List<Vector3>(512);
        var rights = new List<Vector3>(512);

        int seed = courseRandomSeed != 0
            ? courseRandomSeed
            : unchecked((int)(DateTime.UtcNow.Ticks & 0x7FFFFFFF) ^ GetInstanceID());
        var rng = randomizeCourse ? new System.Random(seed) : null;

        float fsLen = firstStraightLength + (rng != null ? (float)(rng.NextDouble() * 2.0 - 1.0) * firstStraightLengthJitter : 0f);
        fsLen = Mathf.Max(18f, fsLen);
        int fsSeg = Mathf.Clamp(Mathf.RoundToInt(firstStraightSegments * (fsLen / Mathf.Max(1f, firstStraightLength))), 8, 48);

        Vector3 pos = new Vector3(-fsLen, 0f, 0f);
        Vector3 fwd = Vector3.right;

        CurvedIceTrackPath.AppendStraight(ref pos, ref fwd, fsLen, fsSeg, mids, rights, -firstStraightVerticalDrop);

        for (int leg = 0; leg < courseLegs; leg++)
        {
            int sign;
            if (rng != null)
                sign = rng.Next(0, 2) == 0 ? 1 : -1;
            else
                sign = alternateLeftRightTurns ? (leg % 2 == 0 ? 1 : -1) : 1;

            float sweepDeg = arcSweepDegrees + (rng != null ? (float)(rng.NextDouble() * 2.0 - 1.0) * arcSweepDegreesJitter : 0f);
            sweepDeg = Mathf.Clamp(sweepDeg, 28f, 125f);
            float sweepRad = sweepDeg * Mathf.Deg2Rad;

            float radius = Mathf.Max(28f, arcRadius + (rng != null ? (float)(rng.NextDouble() * 2.0 - 1.0) * arcRadiusJitter : 0f));
            float drop = Mathf.Max(4f, verticalDropPerArc + (rng != null ? (float)(rng.NextDouble() * 2.0 - 1.0) * verticalDropPerArcJitter : 0f));

            int arcSeg = Mathf.Clamp(arcSegments + (rng != null ? rng.Next(-4, 5) : 0), 8, 48);

            float legLen = legStraightLength + (rng != null ? (float)(rng.NextDouble() * 2.0 - 1.0) * legStraightLengthJitter : 0f);
            legLen = Mathf.Max(22f, legLen);
            int legSeg = Mathf.Clamp(Mathf.RoundToInt(legStraightSegments * (legLen / Mathf.Max(1f, legStraightLength))), 6, 44);

            CurvedIceTrackPath.AppendArc(ref pos, ref fwd, radius, sweepRad, drop, sign, arcSeg, mids, rights);
            CurvedIceTrackPath.AppendStraight(ref pos, ref fwd, legLen, legSeg, mids, rights);
        }

        int rows = mids.Count;
        if (rows < 2)
            return;

        int vx = widthSegments + 1;
        var verts = new Vector3[vx * rows];
        var norms = new Vector3[vx * rows];
        var uvs = new Vector2[vx * rows];

        for (int i = 0; i < rows; i++)
        {
            Vector3 rW = rights[i];
            for (int w = 0; w <= widthSegments; w++)
            {
                float u = (w / (float)widthSegments - 0.5f) * trackWidth;
                int idx = i * vx + w;
                verts[idx] = mids[i] + rW * u;
                uvs[idx] = new Vector2(i * 0.08f, (w / (float)widthSegments) * 3f);
            }
        }

        for (int i = 0; i < rows; i++)
        {
            Vector3 tPrev = i > 0 ? (mids[i] - mids[i - 1]) : (mids[i + 1] - mids[i]);
            Vector3 tNext = i < rows - 1 ? (mids[i + 1] - mids[i]) : (mids[i] - mids[i - 1]);
            Vector3 along = (tPrev + tNext) * 0.5f;
            if (along.sqrMagnitude < 1e-8f)
                along = fwd;
            along.Normalize();

            Vector3 r = rights[i];
            Vector3 n = Vector3.Cross(along, r).normalized;
            if (n.y < 0.12f)
                n = Vector3.Cross(r, along).normalized;

            for (int w = 0; w <= widthSegments; w++)
                norms[i * vx + w] = n;
        }

        int quadCount = (rows - 1) * widthSegments;
        var tris = new int[quadCount * 6];
        int ti = 0;
        for (int i = 0; i < rows - 1; i++)
        {
            for (int w = 0; w < widthSegments; w++)
            {
                int a = i * vx + w;
                int b = a + 1;
                int c = a + vx;
                int d = c + 1;
                tris[ti++] = a;
                tris[ti++] = c;
                tris[ti++] = b;
                tris[ti++] = b;
                tris[ti++] = c;
                tris[ti++] = d;
            }
        }

        _trackMesh.vertices = verts;
        _trackMesh.triangles = tris;
        _trackMesh.normals = norms;
        _trackMesh.uv = uvs;
        _trackMesh.RecalculateBounds();

        var mf = GetComponent<MeshFilter>();
        mf.sharedMesh = _trackMesh;

        var mc = GetComponent<MeshCollider>();
        mc.sharedMesh = null;
        mc.sharedMesh = _trackMesh;

        BuildEdgeWalls(mids, rights);
        if (buildSafetyNetCollider)
            BuildSafetyNetCollider();

        transform.localRotation = Quaternion.Euler(0f, trackYawDegrees, 0f);
    }

    void EnsureWallParents()
    {
        if (_wallLeft == null)
        {
            var t = transform.Find("TrackWall_Left");
            if (t != null)
                _wallLeft = t;
            else
            {
                var go = new GameObject("TrackWall_Left");
                go.transform.SetParent(transform, false);
                go.layer = gameObject.layer;
                _wallLeft = go.transform;
                _wallLeft.gameObject.AddComponent<MeshCollider>();
            }
        }

        if (_wallRight == null)
        {
            var t = transform.Find("TrackWall_Right");
            if (t != null)
                _wallRight = t;
            else
            {
                var go = new GameObject("TrackWall_Right");
                go.transform.SetParent(transform, false);
                go.layer = gameObject.layer;
                _wallRight = go.transform;
                _wallRight.gameObject.AddComponent<MeshCollider>();
            }
        }

        if (_wallStart == null)
        {
            var t = transform.Find("TrackWall_Start");
            if (t != null)
                _wallStart = t;
        }

        if (_wallEnd == null)
        {
            var t = transform.Find("TrackWall_End");
            if (t != null)
                _wallEnd = t;
        }

        if (_safetyNet == null)
        {
            var t = transform.Find("TrackSafetyNet");
            if (t != null)
                _safetyNet = t;
        }
    }

    void BuildEdgeWalls(List<Vector3> mids, List<Vector3> rights)
    {
        int rows = mids.Count;
        if (rows < 2)
            return;

        float half = trackWidth * 0.5f;
        var leftPts = new Vector3[rows];
        var rightPts = new Vector3[rows];
        var rArr = new Vector3[rows];
        for (int i = 0; i < rows; i++)
        {
            rArr[i] = rights[i];
            leftPts[i] = mids[i] + rights[i] * (-half);
            rightPts[i] = mids[i] + rights[i] * half;
        }

        BuildWallRibbon(_wallLeft, leftPts, rArr, rows, true);
        BuildWallRibbon(_wallRight, rightPts, rArr, rows, false);

        if (buildEndWallCaps)
            BuildTransverseEndCaps(mids, rights, half);
    }

    void BuildTransverseEndCaps(List<Vector3> mids, List<Vector3> rights, float half)
    {
        EnsureCapWall(ref _wallStart, "TrackWall_Start");
        EnsureCapWall(ref _wallEnd, "TrackWall_End");

        int last = mids.Count - 1;
        float capInset = Mathf.Min(wallInsetFromTrackEdge, Mathf.Max(0f, half - 1.2f));
        Vector3 r0 = rights[0];
        Vector3 r1 = rights[last];
        Vector3 left0 = mids[0] + r0 * (-half + capInset);
        Vector3 right0 = mids[0] + r0 * (half - capInset);

        Vector3 left1 = mids[last] + r1 * (-half + capInset);
        Vector3 right1 = mids[last] + r1 * (half - capInset);

        BuildCapMesh(_wallStart, left0, right0);
        BuildCapMesh(_wallEnd, left1, right1);
    }

    void EnsureCapWall(ref Transform t, string name)
    {
        if (t == null)
        {
            var tr = transform.Find(name);
            if (tr != null)
                t = tr;
        }

        if (t != null)
            return;

        var go = new GameObject(name);
        go.transform.SetParent(transform, false);
        go.layer = gameObject.layer;
        go.AddComponent<MeshFilter>();
        go.AddComponent<MeshCollider>();
        t = go.transform;
    }

    void BuildCapMesh(Transform parent, Vector3 left, Vector3 right)
    {
        Vector3 lowL = left - Vector3.up * wallExtendBelow;
        Vector3 lowR = right - Vector3.up * wallExtendBelow;
        Vector3 hiL = left + Vector3.up * wallHeight;
        Vector3 hiR = right + Vector3.up * wallHeight;

        var v = new[] { lowL, lowR, hiR, hiL };
        var tris = new[]
        {
            0, 1, 2, 0, 2, 3,
            0, 2, 1, 0, 3, 2
        };

        var mesh = new Mesh { name = parent.name };
        mesh.vertices = v;
        mesh.triangles = tris;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        var mf = parent.GetComponent<MeshFilter>();
        if (mf == null)
            mf = parent.gameObject.AddComponent<MeshFilter>();
        mf.sharedMesh = mesh;

        var mc = parent.GetComponent<MeshCollider>();
        if (mc == null)
            mc = parent.gameObject.AddComponent<MeshCollider>();
        mc.sharedMesh = null;
        mc.convex = false;
        mc.sharedMesh = mesh;
        var trackPhys = GetComponent<MeshCollider>();
        if (trackPhys != null && trackPhys.sharedMaterial != null)
            mc.sharedMaterial = trackPhys.sharedMaterial;
    }

    void BuildSafetyNetCollider()
    {
        if (_trackMesh == null)
            return;

        _trackMesh.RecalculateBounds();
        Bounds b = _trackMesh.bounds;

        if (_safetyNet == null)
        {
            var tr = transform.Find("TrackSafetyNet");
            if (tr != null)
                _safetyNet = tr;
        }

        if (_safetyNet == null)
        {
            var go = new GameObject("TrackSafetyNet");
            go.layer = gameObject.layer;
            go.transform.SetParent(transform, false);
            go.AddComponent<BoxCollider>();
            _safetyNet = go.transform;
        }

        var box = _safetyNet.GetComponent<BoxCollider>();
        if (box == null)
            box = _safetyNet.gameObject.AddComponent<BoxCollider>();

        float halfH = safetyNetThickness * 0.5f;
        float cy = b.min.y - halfH - 3f;
        box.center = new Vector3(b.center.x, cy, b.center.z);
        box.size = new Vector3(
            Mathf.Max(b.size.x + safetyNetPaddingXZ, 48f),
            safetyNetThickness,
            Mathf.Max(b.size.z + safetyNetPaddingXZ, 48f));
        box.isTrigger = false;
    }

    void BuildWallRibbon(Transform parent, Vector3[] edge, Vector3[] rgt, int rows, bool isLeft)
    {
        var mc = parent.GetComponent<MeshCollider>();
        var trackMc = GetComponent<MeshCollider>();
        int segCount = rows - 1;
        var v = new Vector3[segCount * 4];
        var t = new int[segCount * 12];
        int vi = 0;
        int ti = 0;

        float rawOutward = wallThickness - wallInsetFromTrackEdge;
        float outwardDepth = Mathf.Clamp(rawOutward, wallMinOutwardDepth, 8f);

        for (int i = 0; i < segCount; i++)
        {
            float sign = isLeft ? -1f : 1f;
            Vector3 o = rgt[i] * (sign * outwardDepth);
            Vector3 base0 = edge[i] + o;
            Vector3 base1 = edge[i + 1] + o;
            Vector3 low0 = base0 - Vector3.up * wallExtendBelow;
            Vector3 low1 = base1 - Vector3.up * wallExtendBelow;
            Vector3 hi0 = base0 + Vector3.up * wallHeight;
            Vector3 hi1 = base1 + Vector3.up * wallHeight;

            v[vi++] = low0;
            v[vi++] = low1;
            v[vi++] = hi1;
            v[vi++] = hi0;

            int b = vi - 4;
            t[ti++] = b;
            t[ti++] = b + 1;
            t[ti++] = b + 2;
            t[ti++] = b;
            t[ti++] = b + 2;
            t[ti++] = b + 3;
            t[ti++] = b;
            t[ti++] = b + 2;
            t[ti++] = b + 1;
            t[ti++] = b;
            t[ti++] = b + 3;
            t[ti++] = b + 2;
        }

        var mesh = new Mesh { name = parent.name };
        mesh.vertices = v;
        mesh.triangles = t;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        var mf = parent.GetComponent<MeshFilter>();
        if (mf == null)
            mf = parent.gameObject.AddComponent<MeshFilter>();
        mf.sharedMesh = mesh;

        mc.sharedMesh = null;
        mc.convex = false;
        mc.sharedMesh = mesh;
        if (trackMc != null && trackMc.sharedMaterial != null)
            mc.sharedMaterial = trackMc.sharedMaterial;
    }
}
