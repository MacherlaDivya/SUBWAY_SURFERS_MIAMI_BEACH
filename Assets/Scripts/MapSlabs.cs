using System.Collections.Generic;
using UnityEngine;

// Turns your "Subway Surfers" map model (subway_surfers.glb) into endless scenery:
// the straight tree-lined railway section is cut into slabs, scaled so its 3 tracks match
// the game's 3 lanes, and the slabs are repeated forever.
public class MapSlabs
{
    public class Piece { public Mesh mesh; public Material mat; }

    public List<Piece>[] slabs;
    public int count;
    public float slabLen;

    // Measurements of this particular model (in its original file units).
    const float RAW_MAXX = 413.2557f;   // furthest point of the file along the track
    const float CORE_NEAR = 58f;        // straight tree-lined section: |x| from 58 ...
    const float CORE_FAR = 283f;        // ... to 283
    const float LANE_RAW = 2.15f;       // distance between two track centres
    const float GROUND_RAW = 1.5f;      // "ground level" height
    const float KEEP_HALF_RAW = 3.7f;   // keep only the track: +-3.7 around the centre line
    const float KEEP_MAXH_RAW = 0.22f;  // keep ONLY rails + sleepers + gravel; drops the map's own trains/props that stood just above the ground

    public static MapSlabs Build(List<GlbPrim> prims, float laneWidth)
    {
        if (prims == null || prims.Count == 0) return null;

        float maxAbsX = 0f;
        double sumX = 0; long cnt = 0;
        foreach (var p in prims)
            for (int k = 0; k < p.v.Length; k++)
            {
                maxAbsX = Mathf.Max(maxAbsX, Mathf.Abs(p.v[k].x));
                sumX += p.v[k].x; cnt++;
            }
        if (cnt == 0 || maxAbsX < 0.001f) return null;

        float s = maxAbsX / RAW_MAXX;                 // file-unit -> unity-unit factor
        float sign = sumX > 0 ? 1f : -1f;
        float xmin = sign > 0 ? CORE_NEAR * s : -CORE_FAR * s;
        float xmax = sign > 0 ? CORE_FAR * s : -CORE_NEAR * s;

        // Centre line across the track = middle of the ground-level objects (rails, fences)
        var zs = new List<float>();
        foreach (var p in prims)
            for (int k = 0; k < p.v.Length; k += 3)
                if (p.v[k].x > xmin && p.v[k].x < xmax && p.v[k].y < GROUND_RAW * s) zs.Add(p.v[k].z);
        if (zs.Count < 10) return null;
        zs.Sort();
        float zc = 0.5f * (zs[(int)(zs.Count * 0.01f)] + zs[(int)(zs.Count * 0.99f)]);

        float k2 = laneWidth / (LANE_RAW * s);          // scale so tracks == lanes
        float L = (xmax - xmin) * k2;
        var res = new MapSlabs();
        res.count = Mathf.Max(1, Mathf.RoundToInt(L / 40f));
        res.slabLen = L / res.count;
        res.slabs = new List<Piece>[res.count];
        for (int i = 0; i < res.count; i++) res.slabs[i] = new List<Piece>();

        foreach (var p in prims)
        {
            var v = p.v; var n = p.n; var uv = p.uv;
            bool hasUV = uv != null && uv.Length == v.Length;
            var buckets = new List<int>[res.count];
            for (int t = 0; t < p.tri.Length; t += 3)
            {
                float cx = (v[p.tri[t]].x + v[p.tri[t + 1]].x + v[p.tri[t + 2]].x) / 3f;
                if (cx < xmin || cx >= xmax) continue;
                float cz = (v[p.tri[t]].z + v[p.tri[t + 1]].z + v[p.tri[t + 2]].z) / 3f;
                float cy = (v[p.tri[t]].y + v[p.tri[t + 1]].y + v[p.tri[t + 2]].y) / 3f;
                if (Mathf.Abs(cz - zc) > KEEP_HALF_RAW * s || cy > KEEP_MAXH_RAW * s) continue;
                float along = (cx - xmin) * k2;
                int sl = Mathf.Clamp((int)(along / res.slabLen), 0, res.count - 1);
                if (buckets[sl] == null) buckets[sl] = new List<int>();
                buckets[sl].Add(p.tri[t]); buckets[sl].Add(p.tri[t + 1]); buckets[sl].Add(p.tri[t + 2]);
            }
            for (int sl = 0; sl < res.count; sl++)
            {
                var b = buckets[sl];
                if (b == null) continue;
                var remap = new Dictionary<int, int>();
                var nv = new List<Vector3>(); var nn = new List<Vector3>(); var nu = new List<Vector2>();
                var ni = new int[b.Count];
                for (int q = 0; q < b.Count; q++)
                {
                    int old = b[q], idx;
                    if (!remap.TryGetValue(old, out idx))
                    {
                        idx = nv.Count;
                        remap[old] = idx;
                        Vector3 pt = v[old];
                        // map axes -> game axes (a proper rotation, so triangles keep their facing)
                        nv.Add(new Vector3(-(pt.z - zc) * k2, pt.y * k2, (pt.x - xmin) * k2 - sl * res.slabLen));
                        if (n != null) nn.Add(new Vector3(-n[old].z, n[old].y, n[old].x));
                        if (hasUV) nu.Add(uv[old]);
                    }
                    ni[q] = idx;
                }
                var mesh = new Mesh();
                mesh.name = "slab" + sl;
                if (nv.Count > 65000) mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
                mesh.SetVertices(nv);
                if (n != null) mesh.SetNormals(nn);
                if (hasUV) mesh.SetUVs(0, nu);
                mesh.SetTriangles(ni, 0);
                mesh.RecalculateBounds();
                res.slabs[sl].Add(new Piece { mesh = mesh, mat = p.mat });
            }
        }
        return res;
    }
}
