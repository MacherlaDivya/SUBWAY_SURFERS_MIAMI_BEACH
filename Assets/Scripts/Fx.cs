using UnityEngine;

// Lightweight particle effects (dust, sparkles, speed lines, magnet aura) made from pooled cubes,
// so no particle shaders / packages are needed.
public class Fx : MonoBehaviour
{
    public static Fx I;

    class P
    {
        public Transform t;
        public Renderer r;
        public Vector3 vel, size;
        public float life, max, grav;
        public bool shrink = true;
        public bool active;
    }

    P[] pool;
    int next;
    readonly System.Collections.Generic.Dictionary<int, Material> mats = new System.Collections.Generic.Dictionary<int, Material>();

    Material M(Color c, float glow)
    {
        int key = (((int)(c.r * 31f)) << 20) | (((int)(c.g * 31f)) << 15) | (((int)(c.b * 31f)) << 10) | (int)(glow * 10f);
        Material m;
        if (!mats.TryGetValue(key, out m) || m == null) { m = Gfx.Mat(c, glow); mats[key] = m; }
        return m;
    }

    public static Fx Create(int n)
    {
        var g = new GameObject("Fx");
        var f = g.AddComponent<Fx>();
        f.pool = new P[n];
        for (int i = 0; i < n; i++)
        {
            var c = Gfx.Prim(PrimitiveType.Cube, g.transform, Vector3.zero, Vector3.one * 0.1f, Gfx.Mat(Color.white, 1f), "fx");
            c.SetActive(false);
            f.pool[i] = new P { t = c.transform, r = c.GetComponent<Renderer>() };
        }
        I = f;
        return f;
    }

    P Take()
    {
        for (int k = 0; k < pool.Length; k++)
        {
            next = (next + 1) % pool.Length;
            if (!pool[next].active) return pool[next];
        }
        next = (next + 1) % pool.Length;
        return pool[next];
    }

    public void Spawn(Vector3 pos, Vector3 vel, Color col, float size, float life, float grav = 0f, float glow = 1.5f)
    {
        var p = Take();
        p.active = true;
        p.t.gameObject.SetActive(true);
        p.t.position = pos;
        p.t.rotation = Random.rotation;
        p.vel = vel; p.life = p.max = life; p.grav = grav;
        p.size = Vector3.one * size;
        p.r.sharedMaterial = M(col, glow);
        p.shrink = true;
    }

    public void Streak(Vector3 pos, Color col, float len, float life)
    {
        var p = Take();
        p.active = true;
        p.t.gameObject.SetActive(true);
        p.t.position = pos;
        p.t.rotation = Quaternion.identity;
        p.vel = Vector3.zero; p.life = p.max = life; p.grav = 0f;
        p.size = new Vector3(0.035f, 0.035f, len);
        p.r.sharedMaterial = M(col, 2f);
        p.shrink = true;
    }

    public void Rain(Vector3 pos)
    {
        var p = Take();
        p.active = true;
        p.t.gameObject.SetActive(true);
        p.t.position = pos;
        p.t.rotation = Quaternion.identity;
        p.vel = new Vector3(-1.5f, -34f, 0f);
        p.life = p.max = 0.55f; p.grav = 0f;
        p.size = new Vector3(0.025f, 0.8f, 0.025f);
        p.r.sharedMaterial = M(new Color(0.65f, 0.8f, 1f), 1.4f);
        p.shrink = false;
    }

    public void Burst(Vector3 pos, Color col, int count, float speed, float size, float life, float grav = 6f, float glow = 1.5f)
    {
        for (int i = 0; i < count; i++)
            Spawn(pos, Random.onUnitSphere * speed * Random.Range(0.4f, 1f) + Vector3.up * speed * 0.3f, col, size * Random.Range(0.6f, 1.2f), life * Random.Range(0.7f, 1.1f), grav, glow);
    }

    public void Dust(Vector3 pos, int count, float spread)
    {
        for (int i = 0; i < count; i++)
        {
            var v = new Vector3(Random.Range(-spread, spread), Random.Range(0.3f, 1.4f), Random.Range(-spread, spread) * 0.6f);
            Spawn(pos + new Vector3(Random.Range(-0.2f, 0.2f), 0.05f, 0), v, new Color(0.7f, 0.68f, 0.8f), Random.Range(0.12f, 0.24f), Random.Range(0.35f, 0.6f), -0.5f, 0.35f);
        }
    }

    void Update()
    {
        float dt = Time.deltaTime;
        for (int i = 0; i < pool.Length; i++)
        {
            var p = pool[i];
            if (!p.active) continue;
            p.life -= dt;
            if (p.life <= 0f) { p.active = false; p.t.gameObject.SetActive(false); continue; }
            p.vel.y -= p.grav * dt;
            p.t.position += p.vel * dt;
            float f = p.shrink ? p.life / p.max : 1f;
            p.t.localScale = new Vector3(p.size.x * f, p.size.y * f, p.size.z * (p.size.z > 0.5f ? 1f : f));
        }
    }

    public void Clear()
    {
        for (int i = 0; i < pool.Length; i++) { pool[i].active = false; pool[i].t.gameObject.SetActive(false); }
    }
}
