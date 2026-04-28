using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshCollider))]
[ExecuteAlways]
[DefaultExecutionOrder(-50)]
public class CurvedIceTrack : MonoBehaviour
{
    static CurvedIceTrack _activeTrack;

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
    [SerializeField] bool randomizeSeedEachRun = true;
    [SerializeField] int courseRandomSeed;
    [SerializeField] float legStraightLengthJitter = 34f;
    [SerializeField] float arcSweepDegreesJitter = 38f;
    [SerializeField] float arcRadiusJitter = 30f;
    [SerializeField] float verticalDropPerArcJitter = 11f;
    [SerializeField] float firstStraightLengthJitter = 24f;

    [SerializeField] float trackWidth = 15f;
    [SerializeField] [Min(1)] int widthSegments = 4;
    [SerializeField] bool randomizeTextureUvs = true;
    [SerializeField] float textureUvAlongScale = 0.026f;
    [SerializeField] float textureUvAcrossScale = 1.05f;
    [SerializeField] Vector2 textureUvSizeRandom = new Vector2(0.35f, 2.2f);
    [SerializeField] float textureUvPlacementJitter = 0.9f;

    [SerializeField] float wallHeight = 4.5f;
    [SerializeField] float wallThickness = 2.8f;
    [SerializeField] [Min(0f)] float wallInsetFromTrackEdge = 0f;
    [SerializeField] [Min(0.25f)] float wallMinOutwardDepth = 1.8f;
    [SerializeField] [Min(0f)] float wallExtendBelow = 24f;
    [SerializeField] bool buildEndWallCaps = true;
    [SerializeField] bool buildSafetyNetCollider = true;
    [SerializeField] float safetyNetPaddingXZ = 95f;
    [SerializeField] [Min(4f)] float safetyNetThickness = 38f;
    [SerializeField] bool buildColorCollectibles = true;
    [SerializeField] [Min(0)] int colorCollectibleCount = 32;
    [SerializeField] float colorCollectibleHeight = 1.05f;
    [SerializeField] float colorCollectibleRadius = 0.75f;
    [SerializeField] float colorCollectibleCollectionRadius = 1.25f;
    [SerializeField] float colorCollectibleDuration = 5f;
    [SerializeField] bool buildBottlePins = true;
    [SerializeField] [Min(0)] int bottlePinCount = 18;
    [SerializeField] string bottleResourcePath = "Bottles/BOTTLE_5";
    [SerializeField] float bottleGroundPadding = 0.08f;
    [SerializeField] float bottleWorldHeight = 4.05f;
    [SerializeField] float bottleScale = 1f;
    [SerializeField] float maxBottleSpawnSlopeDegrees = 6f;
    [SerializeField] bool buildEndBowlingRack = true;
    [SerializeField] float endRackDistanceFromWall = 32f;
    [SerializeField] float endRackPinSpacing = 3.2f;
    [SerializeField] float finishTriggerDepth = 2.5f;
    [SerializeField] bool buildFans = true;
    [SerializeField] [Min(0)] int fanCount = 14;
    [SerializeField] string fanResourcePath = "Fans/fan_blade";
    [SerializeField] float fanWindAcceleration = 24f;
    [SerializeField] float fanWindLength = 14f;
    [SerializeField] float fanScale = 1.5f;
    [SerializeField] Vector3 fanVisualEulerOffset = new Vector3(270f, 0f, 0f);

    [SerializeField] bool generateOnAwake = true;

    Mesh _trackMesh;
    Transform _wallLeft;
    Transform _wallRight;
    Transform _wallStart;
    Transform _wallEnd;
    Transform _safetyNet;
    Transform _collectiblesRoot;
    Transform _bottlesRoot;
    Transform _finishTrigger;
    Transform _fansRoot;
    List<Vector3> _lastMids;
    List<Vector3> _lastRights;

    void Awake()
    {
        if (Application.isPlaying && generateOnAwake)
            RebuildMesh();
    }

    void OnEnable()
    {
        if (!Application.isPlaying && generateOnAwake)
            RebuildMesh();
    }

    void OnValidate()
    {
        if (!Application.isPlaying && generateOnAwake && isActiveAndEnabled)
            RebuildMesh();
    }

    void OnDestroy()
    {
        if (_trackMesh != null)
        {
            if (Application.isPlaying)
                Destroy(_trackMesh);
            else
                DestroyImmediate(_trackMesh);
        }
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

        int seed = CourseSeed();
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
            {
                if (alternateLeftRightTurns && rng.NextDouble() < 0.62f)
                    sign = leg % 2 == 0 ? 1 : -1;
                else
                    sign = rng.Next(0, 2) == 0 ? 1 : -1;
            }
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
        var uvAlong = new float[rows];
        var uvSideOffset = new float[rows];
        var uvSideScale = new float[rows];
        BuildTrackUvs(mids, seed, uvAlong, uvSideOffset, uvSideScale);

        for (int i = 0; i < rows; i++)
        {
            Vector3 rW = rights[i];
            for (int w = 0; w <= widthSegments; w++)
            {
                float u = (w / (float)widthSegments - 0.5f) * trackWidth;
                float width01 = w / (float)widthSegments;
                int idx = i * vx + w;
                verts[idx] = mids[i] + rW * u;
                uvs[idx] = new Vector2(uvAlong[i], (width01 - 0.5f) * uvSideScale[i] + uvSideOffset[i]);
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
        if (Application.isPlaying)
            BottleScore.Reset();
        if (Application.isPlaying && buildColorCollectibles)
            BuildColorCollectibles(mids, rights, seed);
        if (Application.isPlaying && buildBottlePins)
            BuildBottlePins(mids, rights, seed);
        if (Application.isPlaying && buildFans)
            BuildFans(mids, rights, seed);
        if (Application.isPlaying && buildEndBowlingRack)
            BuildEndBowlingRack(mids, rights);
        transform.localRotation = Quaternion.Euler(0f, trackYawDegrees, 0f);
        _activeTrack = this;
        _lastMids = mids;
        _lastRights = rights;
    }

    public static bool TryMirrorPose(Vector3 worldPosition, Vector3 worldForward, out Vector3 mirroredPosition,
        out Vector3 mirroredForward)
    {
        mirroredPosition = worldPosition;
        mirroredForward = worldForward;
        if (_activeTrack == null || _activeTrack._lastMids == null || _activeTrack._lastRights == null)
            return false;
        return _activeTrack.TryMirrorPoseInternal(worldPosition, worldForward, out mirroredPosition, out mirroredForward);
    }

    public static bool IsNearEnd(Vector3 worldPosition, float minProgress)
    {
        if (_activeTrack == null || _activeTrack._lastMids == null || _activeTrack._lastMids.Count < 2)
            return false;

        int nearest = _activeTrack.NearestTrackRow(worldPosition);
        float progress = nearest / (float)(_activeTrack._lastMids.Count - 1);
        return progress >= minProgress;
    }

    bool TryMirrorPoseInternal(Vector3 worldPosition, Vector3 worldForward, out Vector3 mirroredPosition,
        out Vector3 mirroredForward)
    {
        mirroredPosition = worldPosition;
        mirroredForward = worldForward;
        if (_lastMids.Count == 0)
            return false;

        int best = NearestTrackRow(worldPosition);

        Vector3 center = transform.TransformPoint(_lastMids[best]);
        Vector3 right = transform.TransformDirection(_lastRights[best]).normalized;
        float lateral = Vector3.Dot(worldPosition - center, right);
        mirroredPosition = worldPosition - right * (2f * lateral);
        mirroredForward = Vector3.Reflect(worldForward, right);
        mirroredForward.y = worldForward.y;
        if (mirroredForward.sqrMagnitude < 0.001f)
            mirroredForward = -worldForward;
        mirroredForward.Normalize();
        return true;
    }

    int NearestTrackRow(Vector3 worldPosition)
    {
        int best = 0;
        float bestDist = float.PositiveInfinity;
        for (int i = 0; i < _lastMids.Count; i++)
        {
            Vector3 mid = transform.TransformPoint(_lastMids[i]);
            float dist = (worldPosition - mid).sqrMagnitude;
            if (dist < bestDist)
            {
                bestDist = dist;
                best = i;
            }
        }
        return best;
    }

    int DefaultSeed()
    {
        if (!Application.isPlaying)
            return 12345;
        return unchecked((int)(DateTime.UtcNow.Ticks & 0x7FFFFFFF) ^ GetInstanceID());
    }

    int CourseSeed()
    {
        if (Application.isPlaying && randomizeSeedEachRun)
            return DefaultSeed();
        return courseRandomSeed != 0 ? courseRandomSeed : DefaultSeed();
    }

    void BuildTrackUvs(List<Vector3> mids, int seed, float[] uvAlong, float[] uvSideOffset, float[] uvSideScale)
    {
        if (!randomizeTextureUvs)
        {
            for (int i = 0; i < mids.Count; i++)
            {
                uvAlong[i] = i * 0.08f;
                uvSideOffset[i] = textureUvAcrossScale * 0.5f;
                uvSideScale[i] = textureUvAcrossScale;
            }
            return;
        }

        var rng = new System.Random(seed ^ 0x4f1bbcdc);
        float along = NextRange(rng, 0f, 8f);
        float alongScale = NextRange(rng, textureUvSizeRandom.x, textureUvSizeRandom.y);
        float sideScale = textureUvAcrossScale * NextRange(rng, textureUvSizeRandom.x, textureUvSizeRandom.y);
        float sideOffset = textureUvAcrossScale * 0.5f + NextRange(rng, -textureUvPlacementJitter, textureUvPlacementJitter);
        float targetAlongScale = alongScale;
        float targetSideScale = sideScale;
        float targetSideOffset = sideOffset;

        for (int i = 0; i < mids.Count; i++)
        {
            if (i % 6 == 0)
            {
                targetAlongScale = NextRange(rng, textureUvSizeRandom.x, textureUvSizeRandom.y);
                targetSideScale = textureUvAcrossScale * NextRange(rng, textureUvSizeRandom.x, textureUvSizeRandom.y);
                targetSideOffset = textureUvAcrossScale * 0.5f + NextRange(rng, -textureUvPlacementJitter, textureUvPlacementJitter);
            }

            alongScale = Mathf.Lerp(alongScale, targetAlongScale, 0.32f);
            sideScale = Mathf.Lerp(sideScale, targetSideScale, 0.26f);
            sideOffset = Mathf.Lerp(sideOffset, targetSideOffset, 0.26f);
            if (i > 0)
                along += Vector3.Distance(mids[i], mids[i - 1]) * textureUvAlongScale * alongScale * NextRange(rng, 0.72f, 1.28f);

            uvAlong[i] = along;
            uvSideOffset[i] = sideOffset;
            uvSideScale[i] = sideScale;
        }
    }

    static float NextRange(System.Random rng, float min, float max)
    {
        if (max < min)
            (min, max) = (max, min);
        return min + (float)rng.NextDouble() * (max - min);
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

        if (_collectiblesRoot == null)
        {
            var t = transform.Find("ColorCollectibles");
            if (t != null)
                _collectiblesRoot = t;
        }

        if (_bottlesRoot == null)
        {
            var t = transform.Find("BottlePins");
            if (t != null)
                _bottlesRoot = t;
        }

        if (_finishTrigger == null)
        {
            var t = transform.Find("FinishTrigger");
            if (t != null)
                _finishTrigger = t;
        }

        if (_fansRoot == null)
        {
            var t = transform.Find("Fans");
            if (t != null)
                _fansRoot = t;
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
        EnsureEndWallFinish();
    }

    void EnsureEndWallFinish()
    {
        if (_wallEnd == null)
            return;

        if (_wallEnd.GetComponent<TrackFinishTrigger>() == null)
            _wallEnd.gameObject.AddComponent<TrackFinishTrigger>();
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

    void BuildColorCollectibles(List<Vector3> mids, List<Vector3> rights, int seed)
    {
        EnsureCollectibleRoot();
        ClearChildren(_collectiblesRoot);
        if (colorCollectibleCount <= 0 || mids.Count < 20)
            return;

        var rng = new System.Random(seed ^ 0x35b7c91);
        int min = Mathf.Min(8, mids.Count - 1);
        int max = Mathf.Max(min + 1, mids.Count - 8);
        var colors = CollectibleColors();

        for (int i = 0; i < colorCollectibleCount; i++)
        {
            int row = rng.Next(min, max);
            int skin = 1 + rng.Next(0, 5);
            float lateral = ((float)rng.NextDouble() * 2f - 1f) * trackWidth * 0.24f;
            Vector3 p = mids[row] + rights[row] * lateral + Vector3.up * colorCollectibleHeight;

            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "ColorCollectible_" + skin;
            go.transform.SetParent(_collectiblesRoot, false);
            go.transform.localPosition = p;
            go.transform.localScale = Vector3.one * (colorCollectibleRadius * 2f);

            var col = go.GetComponent<SphereCollider>();
            col.isTrigger = true;
            col.radius = colorCollectibleCollectionRadius / Mathf.Max(colorCollectibleRadius * 2f, 0.01f);

            var item = go.AddComponent<PenguinColorCollectible>();
            item.Configure(skin, colorCollectibleDuration);

            var renderer = go.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = BuildCollectibleMaterial(colors[skin - 1]);
        }
    }

    void BuildBottlePins(List<Vector3> mids, List<Vector3> rights, int seed)
    {
        EnsureBottleRoot();
        ClearChildren(_bottlesRoot);
        if (bottlePinCount <= 0 || mids.Count < 24)
            return;

        var prefab = Resources.Load<GameObject>(bottleResourcePath);
        if (prefab == null)
            return;

        var rng = new System.Random(seed ^ 0x75533b1);
        int min = Mathf.Min(12, mids.Count - 1);
        int max = Mathf.Max(min + 1, mids.Count - 10);
        var flatRows = BottleSpawnRows(mids, min, max);
        if (flatRows.Count == 0)
            return;

        for (int i = 0; i < bottlePinCount; i++)
        {
            float t = (i + 0.5f) / bottlePinCount;
            int rowIndex = Mathf.RoundToInt(Mathf.Lerp(0, flatRows.Count - 1, t));
            rowIndex = Mathf.Clamp(rowIndex + rng.Next(-2, 3), 0, flatRows.Count - 1);
            int row = flatRows[rowIndex];
            float lateral = ((float)rng.NextDouble() * 2f - 1f) * trackWidth * 0.34f;
            Vector3 p = mids[row] + rights[row] * lateral;
            float yaw = (float)rng.NextDouble() * 360f;
            CreateBottlePin(prefab, p, Quaternion.Euler(0f, yaw, 0f));
        }
    }

    void BuildEndBowlingRack(List<Vector3> mids, List<Vector3> rights)
    {
        EnsureBottleRoot();
        if (mids.Count < 4)
            return;

        var prefab = Resources.Load<GameObject>(bottleResourcePath);
        if (prefab == null)
            return;

        int last = mids.Count - 1;
        Vector3 forward = mids[last] - mids[last - 1];
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.001f)
            forward = Vector3.forward;
        forward.Normalize();
        Vector3 right = rights[last];
        right.y = 0f;
        if (right.sqrMagnitude < 0.001f)
            right = Vector3.Cross(Vector3.up, forward);
        right.Normalize();

        Vector3 frontPin = mids[last] - forward * endRackDistanceFromWall;
        int created = 0;
        for (int row = 0; row < 4; row++)
        {
            int count = row + 1;
            for (int col = 0; col < count; col++)
            {
                float lateral = (col - row * 0.5f) * endRackPinSpacing;
                Vector3 p = frontPin + forward * (row * endRackPinSpacing) + right * lateral;
                CreateBottlePin(prefab, p, Quaternion.LookRotation(forward, Vector3.up));
                created++;
                if (created >= 10)
                    return;
            }
        }
    }

    void BuildFans(List<Vector3> mids, List<Vector3> rights, int seed)
    {
        EnsureFansRoot();
        ClearChildren(_fansRoot);
        if (fanCount <= 0 || mids.Count < 32)
            return;

        var prefab = LoadFanPrefab();
        var rng = new System.Random(seed ^ 0x216b31);
        int min = Mathf.Min(18, mids.Count - 1);
        int max = Mathf.Max(min + 1, mids.Count - 20);

        for (int i = 0; i < fanCount; i++)
        {
            float t = (i + 0.5f) / fanCount;
            int row = Mathf.RoundToInt(Mathf.Lerp(min, max - 1, t));
            row = Mathf.Clamp(row + rng.Next(-8, 9), min, max - 1);

            int side = rng.Next(0, 2) == 0 ? -1 : 1;
            Vector3 right = rights[row].normalized;
            Vector3 forward = RowForward(mids, row);
            Vector3 p = mids[row] + right * (side * trackWidth * 0.58f) + Vector3.up * 0.5f;
            Vector3 windDir = -right * side;

            var go = new GameObject("FanWind");
            go.transform.SetParent(_fansRoot, false);
            go.transform.localPosition = p;
            go.transform.localRotation = Quaternion.LookRotation(forward, Vector3.up);

            if (prefab != null)
            {
                var visual = Instantiate(prefab, go.transform);
                visual.name = "FanVisual";
                visual.transform.localPosition = Vector3.zero;
                visual.transform.rotation = Quaternion.LookRotation(transform.TransformDirection(windDir), Vector3.up) *
                                            Quaternion.Euler(fanVisualEulerOffset);
                visual.transform.localScale = Vector3.one * fanScale;
                StripImportedSceneComponents(visual);
                NormalizeVisualHeight(visual, 2.8f * fanScale);
                CenterVisualOnRoot(visual, go.transform.position);
            }
            else
            {
                CreateFallbackFanVisual(go.transform, side);
            }

            var box = go.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.center = new Vector3(-side * trackWidth * 0.42f, 2.2f, 0f);
            box.size = new Vector3(trackWidth * 0.84f, 5f, fanWindLength);

            var wind = go.AddComponent<FanWindZone>();
            wind.Configure(transform.TransformDirection(windDir), fanWindAcceleration, Mathf.Max(2f, fanWindLength * 0.22f));
        }
    }

    GameObject LoadFanPrefab()
    {
        var prefab = Resources.Load<GameObject>(fanResourcePath);
        if (prefab != null)
            return prefab;

        var allFans = Resources.LoadAll<GameObject>("Fans");
        if (allFans == null || allFans.Length == 0)
            return null;
        foreach (var fan in allFans)
        {
            if (fan != null && fan.name.ToLowerInvariant().Contains("fan_blade"))
                return fan;
        }
        return allFans[0];
    }

    static Vector3 RowForward(List<Vector3> mids, int row)
    {
        Vector3 prev = mids[Mathf.Max(0, row - 1)];
        Vector3 next = mids[Mathf.Min(mids.Count - 1, row + 1)];
        Vector3 fwd = next - prev;
        fwd.y = 0f;
        if (fwd.sqrMagnitude < 0.001f)
            return Vector3.forward;
        return fwd.normalized;
    }

    static void CreateFallbackFanVisual(Transform parent, int side)
    {
        var stand = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        stand.name = "FanFallbackStand";
        stand.transform.SetParent(parent, false);
        stand.transform.localPosition = Vector3.up * 0.9f;
        stand.transform.localScale = new Vector3(0.18f, 0.9f, 0.18f);

        var head = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        head.name = "FanFallbackHead";
        head.transform.SetParent(parent, false);
        head.transform.localPosition = Vector3.up * 1.9f;
        head.transform.localRotation = Quaternion.Euler(0f, side > 0 ? 90f : -90f, 90f);
        head.transform.localScale = new Vector3(0.75f, 0.18f, 0.75f);
    }

    void BuildFinishTrigger(List<Vector3> mids, List<Vector3> rights)
    {
        if (mids.Count < 2)
            return;

        EnsureFinishTrigger();
        int last = mids.Count - 1;
        Vector3 forward = mids[last] - mids[last - 1];
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.001f)
            forward = Vector3.forward;
        forward.Normalize();

        _finishTrigger.localPosition = mids[last] - forward * (finishTriggerDepth * 0.5f);
        _finishTrigger.localRotation = Quaternion.LookRotation(forward, Vector3.up);
        var box = _finishTrigger.GetComponent<BoxCollider>();
        box.isTrigger = true;
        box.center = new Vector3(0f, 3f, 0f);
        box.size = new Vector3(trackWidth + 5f, 8f, finishTriggerDepth);
    }

    void CreateBottlePin(GameObject prefab, Vector3 localPosition, Quaternion localRotation)
    {
        var go = new GameObject("BottlePin");
        go.transform.SetParent(_bottlesRoot, false);
        go.transform.localPosition = localPosition + Vector3.up * bottleGroundPadding;
        go.transform.localRotation = localRotation;

        GameObject visual = Instantiate(prefab, go.transform);
        visual.name = "BOTTLE_5_Visual";
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.identity;
        visual.transform.localScale = Vector3.one * bottleScale;
        StripImportedSceneComponents(visual);
        NormalizeBottleVisual(visual, bottleWorldHeight);
        CenterBottleVisualOnRoot(visual, go.transform.position);
        ConfigureBottlePhysics(go);
        IgnoreBottleWallCollisions(go);
    }

    List<int> BottleSpawnRows(List<Vector3> mids, int min, int max)
    {
        var rows = new List<int>(Mathf.Max(0, max - min));
        for (int i = min; i < max; i++)
        {
            Vector3 prev = mids[Mathf.Max(0, i - 1)];
            Vector3 next = mids[Mathf.Min(mids.Count - 1, i + 1)];
            Vector3 delta = next - prev;
            float horizontal = new Vector2(delta.x, delta.z).magnitude;
            if (horizontal < 0.001f)
                continue;

            float slopeDeg = Mathf.Atan2(Mathf.Abs(delta.y), horizontal) * Mathf.Rad2Deg;
            if (slopeDeg <= maxBottleSpawnSlopeDegrees)
                rows.Add(i);
        }
        return rows;
    }

    void EnsureBottleRoot()
    {
        if (_bottlesRoot != null)
            return;

        var t = transform.Find("BottlePins");
        if (t != null)
        {
            _bottlesRoot = t;
            return;
        }

        var go = new GameObject("BottlePins");
        go.transform.SetParent(transform, false);
        _bottlesRoot = go.transform;
    }

    void EnsureFinishTrigger()
    {
        if (_finishTrigger == null)
        {
            var t = transform.Find("FinishTrigger");
            if (t != null)
                _finishTrigger = t;
        }

        if (_finishTrigger == null)
        {
            var go = new GameObject("FinishTrigger");
            go.transform.SetParent(transform, false);
            _finishTrigger = go.transform;
        }

        if (_finishTrigger.GetComponent<BoxCollider>() == null)
            _finishTrigger.gameObject.AddComponent<BoxCollider>();
        if (_finishTrigger.GetComponent<TrackFinishTrigger>() == null)
            _finishTrigger.gameObject.AddComponent<TrackFinishTrigger>();
    }

    void EnsureFansRoot()
    {
        if (_fansRoot != null)
            return;

        var t = transform.Find("Fans");
        if (t != null)
        {
            _fansRoot = t;
            return;
        }

        var go = new GameObject("Fans");
        go.transform.SetParent(transform, false);
        _fansRoot = go.transform;
    }

    void EnsureCollectibleRoot()
    {
        if (_collectiblesRoot != null)
            return;

        var t = transform.Find("ColorCollectibles");
        if (t != null)
        {
            _collectiblesRoot = t;
            return;
        }

        var go = new GameObject("ColorCollectibles");
        go.transform.SetParent(transform, false);
        _collectiblesRoot = go.transform;
    }

    void IgnoreBottleWallCollisions(GameObject bottle)
    {
        var bottleColliders = bottle.GetComponentsInChildren<Collider>();
        IgnoreCollisionsWith(_wallLeft, bottleColliders);
        IgnoreCollisionsWith(_wallRight, bottleColliders);
    }

    static void IgnoreCollisionsWith(Transform wall, Collider[] bottleColliders)
    {
        if (wall == null)
            return;

        var wallColliders = wall.GetComponentsInChildren<Collider>();
        foreach (var bottleCollider in bottleColliders)
        {
            if (bottleCollider == null)
                continue;
            foreach (var wallCollider in wallColliders)
            {
                if (wallCollider != null)
                    Physics.IgnoreCollision(bottleCollider, wallCollider, true);
            }
        }
    }

    static void StripImportedSceneComponents(GameObject go)
    {
        foreach (var light in go.GetComponentsInChildren<Light>(true))
            Destroy(light);
        foreach (var cam in go.GetComponentsInChildren<Camera>(true))
            Destroy(cam);
        foreach (var listener in go.GetComponentsInChildren<AudioListener>(true))
            Destroy(listener);
    }

    static void NormalizeBottleVisual(GameObject go, float targetHeight)
    {
        NormalizeVisualHeight(go, targetHeight);
    }

    static void CenterBottleVisualOnRoot(GameObject visual, Vector3 rootPosition)
    {
        CenterVisualOnRoot(visual, rootPosition);
    }

    static void NormalizeVisualHeight(GameObject go, float targetHeight)
    {
        if (targetHeight <= 0f || !TryGetRendererBounds(go, out Bounds b) || b.size.y <= 0.001f)
            return;

        float scale = targetHeight / b.size.y;
        go.transform.localScale *= scale;
    }

    static void CenterVisualOnRoot(GameObject visual, Vector3 rootPosition)
    {
        if (!TryGetRendererBounds(visual, out Bounds b))
            return;

        Vector3 delta = rootPosition - new Vector3(b.center.x, b.min.y, b.center.z);
        visual.transform.position += delta;
    }

    static bool TryGetRendererBounds(GameObject go, out Bounds bounds)
    {
        var renderers = go.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
        {
            bounds = default;
            return false;
        }

        bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);
        return true;
    }

    static void ConfigureBottlePhysics(GameObject go)
    {
        var rb = go.GetComponent<Rigidbody>();
        if (rb == null)
            rb = go.AddComponent<Rigidbody>();
        rb.mass = 0.7f;
        rb.linearDamping = 0.08f;
        rb.angularDamping = 0.05f;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        if (go.GetComponent<Collider>() == null)
            AddBoundsCollider(go);
        if (go.GetComponent<KnockableBottle>() == null)
            go.AddComponent<KnockableBottle>();
    }

    static void AddBoundsCollider(GameObject go)
    {
        if (!TryGetRendererBounds(go, out Bounds b))
        {
            var fallback = go.AddComponent<CapsuleCollider>();
            fallback.height = 1.8f;
            fallback.radius = 0.32f;
            return;
        }

        var col = go.AddComponent<BoxCollider>();
        col.center = go.transform.InverseTransformPoint(b.center);
        Vector3 size = go.transform.InverseTransformVector(b.size);
        col.size = new Vector3(Mathf.Abs(size.x), Mathf.Abs(size.y), Mathf.Abs(size.z));
    }

    static void ClearChildren(Transform root)
    {
        for (int i = root.childCount - 1; i >= 0; i--)
        {
            var child = root.GetChild(i).gameObject;
            if (Application.isPlaying)
                Destroy(child);
            else
                DestroyImmediate(child);
        }
    }

    static Material BuildCollectibleMaterial(Color c)
    {
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Standard");
        var m = new Material(shader);
        m.color = c;
        if (m.HasProperty("_BaseColor"))
            m.SetColor("_BaseColor", c);
        if (m.HasProperty("_EmissionColor"))
            m.SetColor("_EmissionColor", c * 0.6f);
        return m;
    }

    static Color[] CollectibleColors()
    {
        return new[]
        {
            new Color(0.05f, 0.28f, 0.95f),
            new Color(0.9f, 0.04f, 0.03f),
            new Color(1f, 0.22f, 0.72f),
            new Color(0.05f, 0.55f, 0.14f),
            new Color(0.48f, 0.48f, 0.5f)
        };
    }

    void BuildWallRibbon(Transform parent, Vector3[] edge, Vector3[] rgt, int rows, bool isLeft)
    {
        float rawOutward = wallThickness - wallInsetFromTrackEdge;
        float outwardDepth = Mathf.Clamp(rawOutward, wallMinOutwardDepth, 8f);
        int cols = 6;
        var v = new Vector3[rows * cols];

        for (int i = 0; i < rows; i++)
        {
            float sign = isLeft ? -1f : 1f;
            Vector3 outward = rgt[i] * sign;
            for (int c = 0; c < cols; c++)
            {
                float a = c / (float)(cols - 1);
                float h = wallHeight * Mathf.Pow(a, 0.55f);
                v[i * cols + c] = edge[i] + outward * (outwardDepth * a) + Vector3.up * h;
            }
        }

        int quadCount = (rows - 1) * (cols - 1);
        var t = new int[quadCount * 12];
        int ti = 0;
        for (int i = 0; i < rows - 1; i++)
        {
            for (int c = 0; c < cols - 1; c++)
            {
                int a = i * cols + c;
                int b = a + 1;
                int d = a + cols;
                int e = d + 1;
                t[ti++] = a;
                t[ti++] = d;
                t[ti++] = b;
                t[ti++] = b;
                t[ti++] = d;
                t[ti++] = e;
                t[ti++] = a;
                t[ti++] = b;
                t[ti++] = d;
                t[ti++] = b;
                t[ti++] = e;
                t[ti++] = d;
            }
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

        var mc = parent.GetComponent<MeshCollider>();
        var trackMc = GetComponent<MeshCollider>();
        mc.sharedMesh = null;
        mc.convex = false;
        mc.sharedMesh = mesh;
        if (trackMc != null && trackMc.sharedMaterial != null)
            mc.sharedMaterial = trackMc.sharedMaterial;
    }
}
