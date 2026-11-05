using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

// Minimal .glb reader (so no importer package is required and mesh data is always readable).
// Output geometry is already converted to Unity coordinates (Y up, +Z forward, left-handed).
public class GlbPrim
{
    public Vector3[] v, n;
    public Vector2[] uv;
    public int[] tri;
    public Material mat;
}

public static class Glb
{
    [Serializable] public class GView { public int byteOffset; public int byteLength; public int byteStride; }
    [Serializable] public class GAcc { public int bufferView = -1; public int byteOffset; public int componentType; public int count; }
    [Serializable] public class GAttr { public int POSITION = -1; public int NORMAL = -1; public int TEXCOORD_0 = -1; }
    [Serializable] public class GPrim { public GAttr attributes = new GAttr(); public int indices = -1; public int material = -1; }
    [Serializable] public class GMesh { public GPrim[] primitives; }
    [Serializable] public class GTexRef { public int index = -1; }
    [Serializable] public class GPbr { public float[] baseColorFactor; public GTexRef baseColorTexture = new GTexRef(); }
    [Serializable] public class GSpec { public float[] diffuseFactor; public GTexRef diffuseTexture = new GTexRef(); }
    [Serializable] public class GExt { public GSpec KHR_materials_pbrSpecularGlossiness = new GSpec(); }
    [Serializable] public class GMat { public GPbr pbrMetallicRoughness = new GPbr(); public GExt extensions = new GExt(); }
    [Serializable] public class GTex { public int source; }
    [Serializable] public class GImg { public int bufferView = -1; }
    [Serializable] public class GNode { public int[] children; public int mesh = -1; public float[] matrix; public float[] translation; public float[] rotation; public float[] scale; }
    [Serializable] public class GScene { public int[] nodes; }
    [Serializable]
    public class GRoot
    {
        public GView[] bufferViews; public GAcc[] accessors; public GMesh[] meshes; public GMat[] materials;
        public GTex[] textures; public GImg[] images; public GNode[] nodes; public GScene[] scenes;
    }

    // ---------------------------------------------------------------- file lookup
    public static byte[] FindBytes(string baseName)
    {
        string b = baseName.ToLowerInvariant();
        try
        {
            foreach (var p in Directory.GetFiles(Application.dataPath, "*.glb", SearchOption.AllDirectories))
            {
                string n = Path.GetFileNameWithoutExtension(p).ToLowerInvariant();
                if (n == b || n.StartsWith(b + "_") || n.StartsWith(b + " ")) return File.ReadAllBytes(p);
            }
        }
        catch (Exception) { }
        try
        {
            string sp = Path.Combine(Application.streamingAssetsPath, baseName + ".glb");
            if (File.Exists(sp)) return File.ReadAllBytes(sp);
        }
        catch (Exception) { }
        var ta = Resources.Load<TextAsset>(baseName);
        return ta != null ? ta.bytes : null;
    }

    // ---------------------------------------------------------------- loader
    static byte[] data;
    static int binStart;
    static GRoot root;
    static Texture2D[] texCache;
    static Material[] matCache;

    public static List<GlbPrim> Load(string baseName)
    {
        var d = FindBytes(baseName);
        if (d == null || d.Length < 28 || BitConverter.ToUInt32(d, 0) != 0x46546C67u)
        {
            Debug.LogWarning("[NeonRunner] " + baseName + ".glb not found - using built-in fallback.");
            return null;
        }
        try
        {
            data = d;
            int jl = (int)BitConverter.ToUInt32(d, 12);
            string json = System.Text.Encoding.UTF8.GetString(d, 20, jl);
            binStart = 20 + jl + 8;
            root = JsonUtility.FromJson<GRoot>(json);
            texCache = new Texture2D[root.images != null ? root.images.Length : 0];
            matCache = new Material[root.materials != null ? root.materials.Length : 0];
            var list = new List<GlbPrim>();
            if (root.scenes != null && root.scenes.Length > 0 && root.scenes[0].nodes != null)
                foreach (int ni in root.scenes[0].nodes) Walk(ni, Matrix4x4.identity, list);
            return list;
        }
        catch (Exception e)
        {
            Debug.LogWarning("[NeonRunner] could not read " + baseName + ".glb : " + e.Message);
            return null;
        }
    }

    static void Walk(int ni, Matrix4x4 parent, List<GlbPrim> list)
    {
        var n = root.nodes[ni];
        Matrix4x4 w = parent * NodeMatrix(n);
        if (n.mesh >= 0) AddMesh(root.meshes[n.mesh], w, list);
        if (n.children != null) foreach (int c in n.children) Walk(c, w, list);
    }

    static Matrix4x4 NodeMatrix(GNode n)
    {
        if (n.matrix != null && n.matrix.Length == 16)
        {
            var m = new Matrix4x4();
            for (int c = 0; c < 4; c++)
                for (int r = 0; r < 4; r++) m[r, c] = n.matrix[c * 4 + r];
            return m;
        }
        Vector3 t = (n.translation != null && n.translation.Length == 3) ? new Vector3(n.translation[0], n.translation[1], n.translation[2]) : Vector3.zero;
        Quaternion q = (n.rotation != null && n.rotation.Length == 4) ? new Quaternion(n.rotation[0], n.rotation[1], n.rotation[2], n.rotation[3]) : Quaternion.identity;
        Vector3 s = (n.scale != null && n.scale.Length == 3) ? new Vector3(n.scale[0], n.scale[1], n.scale[2]) : Vector3.one;
        return Matrix4x4.TRS(t, q, s);
    }

    static float[] ReadFloats(int accIdx, int comps)
    {
        var a = root.accessors[accIdx];
        var v = root.bufferViews[a.bufferView];
        int stride = v.byteStride > 0 ? v.byteStride : comps * 4;
        int off = binStart + v.byteOffset + a.byteOffset;
        var r = new float[a.count * comps];
        for (int i = 0; i < a.count; i++)
            for (int c = 0; c < comps; c++)
                r[i * comps + c] = BitConverter.ToSingle(data, off + i * stride + c * 4);
        return r;
    }

    static int[] ReadIndices(int accIdx)
    {
        var a = root.accessors[accIdx];
        var v = root.bufferViews[a.bufferView];
        int off = binStart + v.byteOffset + a.byteOffset;
        var r = new int[a.count];
        for (int i = 0; i < a.count; i++)
        {
            if (a.componentType == 5125) r[i] = (int)BitConverter.ToUInt32(data, off + i * 4);
            else if (a.componentType == 5123) r[i] = BitConverter.ToUInt16(data, off + i * 2);
            else r[i] = data[off + i];
        }
        return r;
    }

    static void AddMesh(GMesh mesh, Matrix4x4 w, List<GlbPrim> list)
    {
        foreach (var p in mesh.primitives)
        {
            if (p.attributes.POSITION < 0) continue;
            var pos = ReadFloats(p.attributes.POSITION, 3);
            int vc = pos.Length / 3;
            float[] nor = p.attributes.NORMAL >= 0 ? ReadFloats(p.attributes.NORMAL, 3) : null;
            float[] uvs = p.attributes.TEXCOORD_0 >= 0 ? ReadFloats(p.attributes.TEXCOORD_0, 2) : null;
            int[] idx;
            if (p.indices >= 0) idx = ReadIndices(p.indices);
            else { idx = new int[vc]; for (int i = 0; i < vc; i++) idx[i] = i; }

            var g = new GlbPrim();
            g.v = new Vector3[vc];
            g.n = nor != null ? new Vector3[vc] : null;
            g.uv = uvs != null ? new Vector2[vc] : null;
            for (int i = 0; i < vc; i++)
            {
                Vector3 q = w.MultiplyPoint3x4(new Vector3(pos[i * 3], pos[i * 3 + 1], pos[i * 3 + 2]));
                g.v[i] = new Vector3(q.x, q.y, -q.z);                         // right-handed -> left-handed
                if (nor != null)
                {
                    Vector3 nn = w.MultiplyVector(new Vector3(nor[i * 3], nor[i * 3 + 1], nor[i * 3 + 2])).normalized;
                    g.n[i] = new Vector3(nn.x, nn.y, -nn.z);
                }
                if (uvs != null) g.uv[i] = new Vector2(uvs[i * 2], 1f - uvs[i * 2 + 1]);
            }
            g.tri = new int[idx.Length - idx.Length % 3];
            for (int t = 0; t < g.tri.Length; t += 3) { g.tri[t] = idx[t]; g.tri[t + 1] = idx[t + 2]; g.tri[t + 2] = idx[t + 1]; }
            g.mat = GetMaterial(p.material);
            list.Add(g);
        }
    }

    static Texture2D GetTex(int imgIdx)
    {
        if (imgIdx < 0 || imgIdx >= texCache.Length) return null;
        if (texCache[imgIdx] != null) return texCache[imgIdx];
        var img = root.images[imgIdx];
        if (img.bufferView < 0) return null;
        var v = root.bufferViews[img.bufferView];
        var bytes = new byte[v.byteLength];
        Buffer.BlockCopy(data, binStart + v.byteOffset, bytes, 0, v.byteLength);
        var t = new Texture2D(2, 2, TextureFormat.RGBA32, true);
        t.LoadImage(bytes);
        t.wrapMode = TextureWrapMode.Repeat;
        t.filterMode = FilterMode.Trilinear;
        t.anisoLevel = 8;
        texCache[imgIdx] = t;
        return t;
    }

    static Material GetMaterial(int mi)
    {
        if (mi < 0 || mi >= matCache.Length) return Gfx.Mat(new Color(0.6f, 0.6f, 0.6f));
        if (matCache[mi] != null) return matCache[mi];
        var m = root.materials[mi];
        Color c = Color.white;
        int ti = -1;
        var sg = m.extensions != null ? m.extensions.KHR_materials_pbrSpecularGlossiness : null;
        if (sg != null && ((sg.diffuseFactor != null && sg.diffuseFactor.Length >= 3) || sg.diffuseTexture.index >= 0))
        {
            if (sg.diffuseFactor != null && sg.diffuseFactor.Length >= 3) c = new Color(sg.diffuseFactor[0], sg.diffuseFactor[1], sg.diffuseFactor[2], 1f);
            ti = sg.diffuseTexture.index;
        }
        else if (m.pbrMetallicRoughness != null)
        {
            var f = m.pbrMetallicRoughness.baseColorFactor;
            if (f != null && f.Length >= 3) c = new Color(f[0], f[1], f[2], 1f);
            ti = m.pbrMetallicRoughness.baseColorTexture.index;
        }
        Texture2D tex = null;
        if (ti >= 0 && root.textures != null && ti < root.textures.Length) tex = GetTex(root.textures[ti].source);
        matCache[mi] = Gfx.TexMat(tex, c, 0.35f);
        return matCache[mi];
    }
}
