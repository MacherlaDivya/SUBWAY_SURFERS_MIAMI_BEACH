using System.Collections.Generic;
using UnityEngine;

public static class Gfx
{
    static Shader lit;
    static readonly Dictionary<string, Material> cache = new Dictionary<string, Material>();
    static Texture2D[] winTex;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { lit = null; cache.Clear(); winTex = null; }

    static Shader Lit()
    {
        if (lit == null)
        {
            lit = Shader.Find("Universal Render Pipeline/Lit");
            if (lit == null) lit = Shader.Find("Standard");
            if (lit == null) lit = Shader.Find("Sprites/Default");
        }
        return lit;
    }

    static void SetF(Material m, string n, float v) { if (m.HasProperty(n)) m.SetFloat(n, v); }

    public static Material Mat(Color c, float emission = 0f)
    {
        string key = c.ToString() + "e" + emission;
        Material m;
        if (cache.TryGetValue(key, out m) && m != null) return m;
        m = new Material(Lit());
        m.color = c;
        SetF(m, "_Smoothness", 0.3f);
        SetF(m, "_Glossiness", 0.3f);
        if (emission > 0f)
        {
            m.EnableKeyword("_EMISSION");
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            m.SetColor("_EmissionColor", new Color(c.r, c.g, c.b, 1f) * emission);
        }
        cache[key] = m;
        return m;
    }

    public static Material MatGloss(Color c, float emission, float smooth)
    {
        string key = "g" + c.ToString() + "e" + emission + "s" + smooth;
        Material m;
        if (cache.TryGetValue(key, out m) && m != null) return m;
        m = new Material(Lit());
        m.color = c;
        SetF(m, "_Smoothness", smooth);
        SetF(m, "_Glossiness", smooth);
        SetF(m, "_Metallic", 0.3f);
        if (emission > 0f)
        {
            m.EnableKeyword("_EMISSION");
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            m.SetColor("_EmissionColor", new Color(c.r, c.g, c.b, 1f) * emission);
        }
        cache[key] = m;
        return m;
    }

    // see-through glowing material (shield bubbles, glow halos); works in Built-in and URP
    public static Material Glass(Color c, float alpha, float glow, bool cached = true)
    {
        string key = "gl" + c.ToString() + alpha + "_" + glow;
        Material m;
        if (cached && cache.TryGetValue(key, out m) && m != null) return m;
        m = new Material(Lit());
        bool urp = m.shader != null && m.shader.name.Contains("Universal");
        m.color = new Color(c.r, c.g, c.b, alpha);
        if (urp)
        {
            SetF(m, "_Surface", 1f); SetF(m, "_Blend", 0f);
            SetF(m, "_SrcBlend", 5f); SetF(m, "_DstBlend", 10f); SetF(m, "_ZWrite", 0f);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.SetOverrideTag("RenderType", "Transparent");
        }
        else
        {
            SetF(m, "_Mode", 2f);
            SetF(m, "_SrcBlend", 5f); SetF(m, "_DstBlend", 10f); SetF(m, "_ZWrite", 0f);
            m.DisableKeyword("_ALPHATEST_ON");
            m.EnableKeyword("_ALPHABLEND_ON");
            m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        }
        m.renderQueue = 3000;
        SetF(m, "_Smoothness", 0.9f);
        SetF(m, "_Glossiness", 0.9f);
        if (glow > 0f)
        {
            m.EnableKeyword("_EMISSION");
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            m.SetColor("_EmissionColor", new Color(c.r, c.g, c.b, 1f) * glow);
        }
        if (cached) cache[key] = m;
        return m;
    }

    public static Material TexMat(Texture tex, Color c, float emission)
    {
        var m = new Material(Lit());
        m.color = c;
        if (tex != null) m.mainTexture = tex;
        SetF(m, "_Smoothness", 0.1f);
        SetF(m, "_Glossiness", 0.1f);
        SetF(m, "_Metallic", 0f);
        SetF(m, "_Cull", 0f);
        if (emission > 0f)
        {
            m.EnableKeyword("_EMISSION");
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            if (tex != null) { m.SetTexture("_EmissionMap", tex); m.SetColor("_EmissionColor", Color.white * emission); }
            else m.SetColor("_EmissionColor", new Color(c.r, c.g, c.b, 1f) * emission);
        }
        return m;
    }

    static readonly Color[] houseTints = {
        new Color(1f, 0.72f, 0.8f), new Color(0.7f, 0.96f, 0.86f), new Color(0.68f, 0.9f, 1f),
        new Color(1f, 0.93f, 0.68f), new Color(1f, 0.8f, 0.62f) };   // Miami pastels: pink, mint, aqua, butter, peach

    public static Material BuildingTint(int idx, float h, int tint)
    {
        string key = "h" + idx + "_" + h + "_" + tint;
        Material m;
        if (cache.TryGetValue(key, out m) && m != null) return m;
        var tex = WinTex(idx);
        m = new Material(Lit());
        m.color = houseTints[tint % houseTints.Length];
        m.mainTexture = tex;
        m.mainTextureScale = new Vector2(10f / 6f, h / 6f);
        SetF(m, "_Smoothness", 0.1f);
        SetF(m, "_Glossiness", 0.1f);
        m.EnableKeyword("_EMISSION");
        m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
        m.SetTexture("_EmissionMap", tex);
        m.SetColor("_EmissionColor", Color.white * 0.4f);
        cache[key] = m;
        return m;
    }

    public static Material BuildingMat(int idx, float h)
    {
        string key = "b" + idx + "_" + h;
        Material m;
        if (cache.TryGetValue(key, out m) && m != null) return m;
        var tex = WinTex(idx);
        m = new Material(Lit());
        m.color = new Color(0.8f, 0.9f, 1f);
        m.mainTexture = tex;
        m.mainTextureScale = new Vector2(10f / 6f, h / 6f);
        SetF(m, "_Smoothness", 0.1f);
        SetF(m, "_Glossiness", 0.1f);
        m.EnableKeyword("_EMISSION");
        m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
        m.SetTexture("_EmissionMap", tex);
        m.SetColor("_EmissionColor", Color.white * 0.35f);
        cache[key] = m;
        return m;
    }

    static Texture2D WinTex(int idx)
    {
        if (winTex == null) winTex = new Texture2D[3];
        if (winTex[idx] != null) return winTex[idx];
        int w = 32, h = 60;
        var t = new Texture2D(w, h, TextureFormat.RGBA32, true);
        t.wrapMode = TextureWrapMode.Repeat;
        t.filterMode = FilterMode.Trilinear;
        t.anisoLevel = 8;
        var rng = new System.Random(1000 + idx * 77);
        Color32[] pal = {
            new Color32(60, 110, 140, 255), new Color32(80, 140, 170, 255),
            new Color32(120, 185, 210, 255), new Color32(40, 85, 115, 255) };
        int cw = w / 4, ch = h / 6;
        var lit = new Color32[cw * ch];
        for (int i = 0; i < lit.Length; i++)
            lit[i] = pal[rng.Next(pal.Length)];
        var px = new Color32[w * h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                Color32 c = new Color32(240, 240, 240, 255);                 // light wall (the pastel tint colours it)
                int mx = x % 4, my = y % 6;
                if (my == 0) c = new Color32(215, 215, 215, 255);           // Art Deco ledge line
                if ((mx == 1 || mx == 2) && (my >= 2 && my <= 4))
                    c = lit[(y / 6) * cw + (x / 4)];                         // blue glass window
                px[y * w + x] = c;
            }
        t.SetPixels32(px);
        t.Apply(true);
        winTex[idx] = t;
        return t;
    }

    public static GameObject Prim(PrimitiveType type, Transform parent, Vector3 pos, Vector3 scale, Material m, string name = "p")
    {
        var g = GameObject.CreatePrimitive(type);
        g.name = name;
        var col = g.GetComponent<Collider>();
        if (col != null) Object.Destroy(col);
        g.transform.SetParent(parent, false);
        g.transform.localPosition = pos;
        g.transform.localScale = scale;
        var r = g.GetComponent<Renderer>();
        r.sharedMaterial = m;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows = false;
        return g;
    }
}
