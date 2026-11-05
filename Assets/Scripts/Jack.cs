using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

// Finds your own prefabs in Assets/MiamiAssets (optional roadside buildings).
public static class AssetLoader
{
    public static List<GameObject> LoadEnvironment()
    {
        var list = new List<GameObject>();
#if UNITY_EDITOR
        if (AssetDatabase.IsValidFolder("Assets/MiamiAssets"))
        {
            foreach (var guid in AssetDatabase.FindAssets("t:GameObject", new[] { "Assets/MiamiAssets" }))
            {
                var go = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
                if (go != null && go.GetComponentInChildren<Renderer>(true) != null) list.Add(go);
            }
        }
#endif
        if (list.Count == 0)
            foreach (var go in Resources.LoadAll<GameObject>("MiamiAssets"))
                if (go.GetComponentInChildren<Renderer>(true) != null) list.Add(go);
        return list;
    }
}

// Takes the single static Jack mesh (T-pose, no skeleton) and cuts it into
// body / 2 arms / 2 legs so the arms hang down and arms + legs swing while running.
public static class JackModel
{
    // Detected: cap at the back. Your screenshot shows he ran backwards, so turn him around.
    const bool TurnAround = true;

    public static Rig Build(GlbPrim src, Transform parent, float targetHeight)
    {
        var rig = new Rig();
        var root = new GameObject("JackRoot").transform;
        root.SetParent(parent, false);
        rig.root = root;

        Vector3[] v = (Vector3[])src.v.Clone();
        Vector3[] n = src.n != null ? (Vector3[])src.n.Clone() : null;
        Vector2[] uv = src.uv;
        int[] tri = src.tri;
        Material mat = src.mat;
        bool hasN = n != null;

        float minY = float.MaxValue, maxY = float.MinValue;
        for (int i = 0; i < v.Length; i++)
        {
            minY = Mathf.Min(minY, v[i].y);
            maxY = Mathf.Max(maxY, v[i].y);
        }
        float h = Mathf.Max(maxY - minY, 0.01f);
        for (int i = 0; i < v.Length; i++) v[i].y -= minY;

        // Jack wears his cap backwards, so the furthest head point is the BACK of the model.
        // The game runs along +Z, so make sure his front faces +Z.
        float extreme = 0f;
        for (int i = 0; i < v.Length; i++)
            if (v[i].y > 0.75f * h && Mathf.Abs(v[i].z) > Mathf.Abs(extreme)) extreme = v[i].z;
        // If Jack runs backwards, change TurnAround below to !TurnAround.
        if ((extreme > 0f) != TurnAround)
        {
            for (int i = 0; i < v.Length; i++)
            {
                v[i].x = -v[i].x; v[i].z = -v[i].z;
                if (hasN) { n[i].x = -n[i].x; n[i].z = -n[i].z; }
            }
            // x/z both flipped = 180 degree turn, winding is unchanged
        }

        float hipY = 0.44f * h, legTop = 0.40f * h, shY = 0.64f * h, armX = 0.16f * h;
        float armLo = 0.55f * h, armHi = 0.80f * h;
        var body = new List<int>(); var aL = new List<int>(); var aR = new List<int>();
        var lL = new List<int>(); var lR = new List<int>();
        for (int t = 0; t < tri.Length; t += 3)
        {
            Vector3 c = (v[tri[t]] + v[tri[t + 1]] + v[tri[t + 2]]) / 3f;
            List<int> dst;
            if (c.y < legTop) dst = c.x < 0f ? lL : lR;
            else if (Mathf.Abs(c.x) > armX && c.y > armLo && c.y < armHi) dst = c.x < 0f ? aL : aR;
            else dst = body;
            dst.Add(tri[t]); dst.Add(tri[t + 1]); dst.Add(tri[t + 2]);
        }

        Part(root, "body", body, Vector3.zero, v, n, hasN, uv, mat);
        rig.legL = Part(root, "legL", lL, new Vector3(-0.06f * h, hipY, 0), v, n, hasN, uv, mat);
        rig.legR = Part(root, "legR", lR, new Vector3(0.06f * h, hipY, 0), v, n, hasN, uv, mat);
        rig.armL = Part(root, "armL", aL, new Vector3(-armX, shY, 0), v, n, hasN, uv, mat);
        rig.armR = Part(root, "armR", aR, new Vector3(armX, shY, 0), v, n, hasN, uv, mat);

        root.localScale = Vector3.one * (targetHeight / h);
        rig.armRest = 75f;   // T-pose -> arms hang down
        rig.swing = 0.7f;    // smaller leg swing so the cut stays hidden
        return rig;
    }

    static Transform Part(Transform root, string name, List<int> tris, Vector3 pivot,
                          Vector3[] v, Vector3[] n, bool hasN, Vector2[] uv, Material mat)
    {
        var g = new GameObject(name);
        g.transform.SetParent(root, false);
        g.transform.localPosition = pivot;
        var pv = new Vector3[v.Length];
        for (int i = 0; i < v.Length; i++) pv[i] = v[i] - pivot;
        var mesh = new Mesh();
        mesh.name = name;
        mesh.vertices = pv;
        if (hasN) mesh.normals = n;
        if (uv != null && uv.Length == v.Length) mesh.uv = uv;
        mesh.triangles = tris.ToArray();
        mesh.RecalculateBounds();
        g.AddComponent<MeshFilter>().sharedMesh = mesh;
        var mr = g.AddComponent<MeshRenderer>();
        mr.sharedMaterial = mat;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        return g.transform;
    }
}
