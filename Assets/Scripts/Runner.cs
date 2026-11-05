using UnityEngine;

public class Rig
{
    public Transform root, legL, legR, armL, armR, torso, head;
    public float armRest = 0f, swing = 1f;
}

public static class Builder
{
    static Transform Pivot(Transform parent, Vector3 pos)
    {
        var t = new GameObject("pivot").transform;
        t.SetParent(parent, false);
        t.localPosition = pos;
        return t;
    }

    static Transform Limb(Transform root, Vector3 pivotPos, Vector3 size, Material m, Material endM, bool leg)
    {
        var p = Pivot(root, pivotPos);
        Gfx.Prim(PrimitiveType.Cube, p, new Vector3(0, -size.y / 2f, 0), size, m, "limb");
        if (leg) Gfx.Prim(PrimitiveType.Cube, p, new Vector3(0, -size.y - 0.02f, 0.05f), new Vector3(size.x + 0.04f, 0.13f, size.z + 0.14f), endM, "shoe");
        else Gfx.Prim(PrimitiveType.Sphere, p, new Vector3(0, -size.y - 0.02f, 0), Vector3.one * 0.15f, endM, "hand");
        return p;
    }

    public static Rig Humanoid(Transform parent, Color top, Color pants, Color skin, Color cap, Color shoes, float scale)
    {
        var r = new Rig();
        var root = new GameObject("Rig").transform;
        root.SetParent(parent, false);
        root.localScale = Vector3.one * scale;
        r.root = root;
        Material mTop = Gfx.Mat(top, 0.25f), mPants = Gfx.Mat(pants), mSkin = Gfx.Mat(skin),
                 mCap = Gfx.Mat(cap, 0.4f), mShoe = Gfx.Mat(shoes, 0.7f), mEye = Gfx.Mat(new Color(0.03f, 0.03f, 0.05f));

        r.torso = Gfx.Prim(PrimitiveType.Cube, root, new Vector3(0, 1.25f, 0), new Vector3(0.55f, 0.62f, 0.32f), mTop, "torso").transform;
        r.head = Gfx.Prim(PrimitiveType.Sphere, root, new Vector3(0, 1.83f, 0), new Vector3(0.4f, 0.42f, 0.4f), mSkin, "head").transform;
        Gfx.Prim(PrimitiveType.Sphere, root, new Vector3(-0.075f, 1.86f, 0.18f), Vector3.one * 0.06f, mEye, "eye");
        Gfx.Prim(PrimitiveType.Sphere, root, new Vector3(0.075f, 1.86f, 0.18f), Vector3.one * 0.06f, mEye, "eye");
        Gfx.Prim(PrimitiveType.Sphere, root, new Vector3(0, 1.94f, -0.01f), new Vector3(0.44f, 0.26f, 0.44f), mCap, "cap");
        Gfx.Prim(PrimitiveType.Cube, root, new Vector3(0, 1.9f, 0.2f), new Vector3(0.3f, 0.04f, 0.24f), mCap, "brim");
        r.legL = Limb(root, new Vector3(-0.14f, 0.95f, 0), new Vector3(0.2f, 0.86f, 0.22f), mPants, mShoe, true);
        r.legR = Limb(root, new Vector3(0.14f, 0.95f, 0), new Vector3(0.2f, 0.86f, 0.22f), mPants, mShoe, true);
        r.armL = Limb(root, new Vector3(-0.37f, 1.5f, 0), new Vector3(0.15f, 0.5f, 0.15f), mTop, mSkin, false);
        r.armR = Limb(root, new Vector3(0.37f, 1.5f, 0), new Vector3(0.15f, 0.5f, 0.15f), mTop, mSkin, false);
        return r;
    }
}

public class Runner : MonoBehaviour
{
    public const float LW = 2.6f;
    public int lane;
    public float vy;
    public bool grounded = true, dead, jet, hover;
    public float rollT;
    public float groundY;   // height of the surface under the player (ground, ramp or train roof)
    public float Lean { get { return lean; } }
    public float Height { get { return rollT > 0f ? 0.8f : 1.8f; } }

    Rig rig;
    Transform pivot;
    GameObject jetObj, hoverObj;
    Transform[] flames = new Transform[2];
    Renderer[] rends;
    float phase, lean, stumbleT, deadT, sq, rollBlend, stepT, rollDur = 0.7f;
    bool flicker, wasGrounded = true;

    public void Build()
    {
        pivot = new GameObject("pivot").transform;
        pivot.SetParent(transform, false);
        pivot.localPosition = new Vector3(0, 0.9f, 0);
        var jackPrims = Glb.Load("Jack");
        if (jackPrims != null && jackPrims.Count > 0) rig = JackModel.Build(jackPrims[0], pivot, 1.82f);
        if (rig == null)
        {
            rig = Builder.Humanoid(pivot, new Color(1f, 0.45f, 0.1f), new Color(0.1f, 0.15f, 0.45f),
                new Color(0.95f, 0.75f, 0.6f), new Color(0.1f, 0.85f, 0.95f), new Color(0.9f, 1f, 0.2f), 0.9f);
            Gfx.Prim(PrimitiveType.Cube, rig.root, new Vector3(0, 1.27f, -0.26f), new Vector3(0.42f, 0.5f, 0.2f),
                Gfx.Mat(new Color(0.2f, 1f, 0.5f), 0.7f), "bag");
        }
        rig.root.localPosition = new Vector3(0, -0.9f, 0);

        jetObj = new GameObject("jet");
        jetObj.transform.SetParent(pivot, false);
        var mG = Gfx.Mat(new Color(0.6f, 0.65f, 0.7f), 0.1f);
        var mF = Gfx.Mat(new Color(1f, 0.6f, 0.1f), 3f);
        for (int s = -1; s <= 1; s += 2)
        {
            Gfx.Prim(PrimitiveType.Cylinder, jetObj.transform, new Vector3(s * 0.13f, 0.27f, -0.36f), new Vector3(0.15f, 0.26f, 0.15f), mG, "tank");
            flames[(s + 1) / 2] = Gfx.Prim(PrimitiveType.Sphere, jetObj.transform, new Vector3(s * 0.13f, -0.05f, -0.36f), new Vector3(0.13f, 0.36f, 0.13f), mF, "flame").transform;
        }
        jetObj.SetActive(false);

        hoverObj = new GameObject("hoverboard");
        hoverObj.transform.SetParent(transform, false);
        hoverRends = new Renderer[3];
        hoverRends[0] = Gfx.Prim(PrimitiveType.Cube, hoverObj.transform, new Vector3(0, 0.04f, 0.05f), new Vector3(0.6f, 0.07f, 1.3f), Gfx.Mat(new Color(1f, 0.2f, 0.65f), 1.5f), "board").GetComponent<Renderer>();
        var mC = Gfx.Mat(new Color(0.2f, 0.95f, 1f), 3f);
        hoverRends[1] = Gfx.Prim(PrimitiveType.Cube, hoverObj.transform, new Vector3(0, 0.04f, 0.7f), new Vector3(0.64f, 0.05f, 0.1f), mC, "edge").GetComponent<Renderer>();
        hoverRends[2] = Gfx.Prim(PrimitiveType.Cube, hoverObj.transform, new Vector3(0, 0.04f, -0.6f), new Vector3(0.64f, 0.05f, 0.1f), mC, "edge").GetComponent<Renderer>();
        hoverObj.SetActive(false);

        rends = GetComponentsInChildren<Renderer>(true);
    }

    public void ResetRunner()
    {
        groundY = 0f; lane = 0; vy = 0; grounded = true; rollT = 0; dead = false; stumbleT = 0; deadT = 0; lean = 0;
        transform.position = Vector3.zero;
        pivot.localRotation = Quaternion.identity;
        pivot.localScale = Vector3.one;
        sq = 0; rollBlend = 0; wasGrounded = true;
        SetJet(false); SetHover(false); SetFlicker(false);
    }

    Renderer[] hoverRends;

    public void SetBoard(Color main, Color edge)
    {
        if (hoverRends == null) return;
        for (int i = 0; i < hoverRends.Length; i++)
        {
            var m = hoverRends[i].material;
            Color c = i == 0 ? main : edge;
            m.color = c;
            if (m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", new Color(c.r, c.g, c.b, 1f) * (i == 0 ? 1.6f : 3f));
        }
    }

    Renderer[] skinR;
    Color[] skinBase, skinEmi;

    // Outfit style: tints every body part (works for Jack and for the built-in character).
    public void SetSkin(Color tint)
    {
        if (skinR == null)
        {
            var all = rig.root.GetComponentsInChildren<Renderer>(true);
            skinR = all;
            skinBase = new Color[all.Length];
            skinEmi = new Color[all.Length];
            for (int i = 0; i < all.Length; i++)
            {
                var m = all[i].material;      // own copy
                skinBase[i] = m.color;
                skinEmi[i] = m.HasProperty("_EmissionColor") ? m.GetColor("_EmissionColor") : Color.black;
            }
        }
        for (int i = 0; i < skinR.Length; i++)
        {
            var m = skinR[i].material;
            Color b = skinBase[i];
            m.color = new Color(b.r * tint.r, b.g * tint.g, b.b * tint.b, b.a);
            if (m.HasProperty("_EmissionColor"))
            {
                Color e = skinEmi[i];
                m.SetColor("_EmissionColor", new Color(e.r * tint.r, e.g * tint.g, e.b * tint.b, 1f));
            }
        }
    }

    public void SetJet(bool b) { jet = b; jetObj.SetActive(b); }
    public void SetHover(bool b) { hover = b; hoverObj.SetActive(b); }
    public void SetFlicker(bool b)
    {
        flicker = b;
        if (!b) for (int i = 0; i < rends.Length; i++) rends[i].enabled = true;
    }

    public void Move(int d) { if (!dead) lane = Mathf.Clamp(lane + d, -1, 1); }
    public bool Jump()
    {
        if (dead || jet || !grounded) return false;
        vy = 11.5f; grounded = false; rollT = 0; sq = 1f;
        if (Fx.I != null) Fx.I.Dust(transform.position, 6, 2.2f);
        return true;
    }
    public void Roll()
    {
        if (dead || jet) return;
        if (grounded && rollT > 0.15f) return;
        if (!grounded) vy = -24f;
        rollT = rollDur;
        if (Fx.I != null) { Fx.I.Dust(transform.position, 8, 2.8f); Fx.I.Burst(transform.position + Vector3.up * 0.4f, new Color(0.4f, 0.9f, 1f), 6, 3f, 0.08f, 0.35f, 0f, 3f); }
    }
    public void Stumble() { stumbleT = 0.5f; }
    public void Die() { dead = true; deadT = 0; rollT = 0; }
    public void Revive() { dead = false; pivot.localRotation = Quaternion.identity; vy = 0; }

    public void Tick(float dt, float speed, bool running)
    {
        Vector3 p = transform.position;
        float dist = Mathf.Abs(lane * LW - p.x);
        float nx = Mathf.MoveTowards(p.x, lane * LW, Mathf.Max(7f, dist * 15f) * dt);   // ease-out lane change
        float vxl = (nx - p.x) / Mathf.Max(dt, 0.0001f);
        lean = Mathf.Lerp(lean, Mathf.Clamp(vxl / 22f, -1f, 1f), 12f * dt);
        p.x = nx;
        if (running && !dead) p.z += speed * dt;

        if (jet && !dead)
        {
            p.y = Mathf.Lerp(p.y, 5.5f, 4f * dt);
            grounded = false; vy = 0;
        }
        else
        {
            vy -= 28f * dt;
            p.y += vy * dt;
            if (p.y <= groundY + 0.001f && vy <= 0f) { p.y = groundY; vy = 0f; grounded = true; }
            else grounded = false;
        }
        transform.position = p;
        if (grounded && !wasGrounded && !dead)
        {
            sq = -1f;
            if (Fx.I != null) Fx.I.Dust(p, 10, 3f);
        }
        wasGrounded = grounded;
        if (rollT > 0f) rollT -= dt;
        if (running && grounded && !dead && !jet)
        {
            stepT -= dt;
            if (stepT <= 0f) { stepT = Mathf.Lerp(0.2f, 0.11f, speed / 30f); if (Fx.I != null) Fx.I.Dust(p, 1, 0.8f); }
        }
        Animate(dt, speed, running);
    }

    static Quaternion X(float a) { return Quaternion.Euler(a, 0, 0); }
    void AL(float a) { rig.armL.localRotation = Quaternion.Euler(a, 0, rig.armRest); }
    void AR(float a) { rig.armR.localRotation = Quaternion.Euler(a, 0, -rig.armRest); }

    void Animate(float dt, float speed, bool running)
    {
        if (flicker)
        {
            bool on = ((int)(Time.time * 14f)) % 2 == 0;
            for (int i = 0; i < rends.Length; i++) rends[i].enabled = on;
        }

        if (dead)
        {
            deadT += dt;
            pivot.localRotation = Quaternion.Euler(Mathf.Lerp(0f, -85f, Mathf.Clamp01(deadT * 3f)), 0, 0);
            pivot.localScale = Vector3.one;
            rig.legL.localRotation = X(-20); rig.legR.localRotation = X(25);
            AL(-110); AR(-60);
            return;
        }

        phase += dt * (running ? Mathf.Lerp(11f, 17f, speed / 30f) : 2f);
        float s = Mathf.Sin(phase);
        float leanX = 8f, rollRot = 0f, bob = 0f;

        if (jet)
        {
            leanX = 70f;
            rig.legL.localRotation = X(15); rig.legR.localRotation = X(35);
            AL(-160); AR(-160);
            float f = 1f + 0.4f * Mathf.Sin(Time.time * 40f);
            for (int i = 0; i < 2; i++) flames[i].localScale = new Vector3(0.13f, 0.36f * f, 0.13f);
            if (Fx.I != null) Fx.I.Spawn(transform.position + new Vector3(Random.Range(-0.2f, 0.2f), 0.8f, -0.5f), new Vector3(0, -3f, -2f), new Color(1f, Random.value < 0.5f ? 0.45f : 0.75f, 0.1f), 0.2f, 0.4f, 0f, 3f);
        }
        else if (rollT > 0f)
        {
            float rt = 1f - rollT / rollDur;
            rollRot = Mathf.SmoothStep(0f, 1f, rt) * 360f;
            rig.legL.localRotation = X(-110); rig.legR.localRotation = X(-110);
            AL(-110); AR(-110);
        }
        else if (!grounded)
        {
            rig.legL.localRotation = X(-40); rig.legR.localRotation = X(25);
            AL(-150); AR(-60);
        }
        else if (running)
        {
            rig.legL.localRotation = X(s * 65f * rig.swing); rig.legR.localRotation = X(-s * 65f * rig.swing);
            AL(-s * 65f * rig.swing); AR(s * 65f * rig.swing);
            bob = Mathf.Abs(s) * 0.1f;
        }
        else
        {
            leanX = 0f;
            rig.legL.localRotation = X(0); rig.legR.localRotation = X(0);
            AL(s * 6f); AR(-140f + Mathf.Sin(phase * 4f) * 22f);   // friendly wave on the title screen
            bob = (s * 0.5f + 0.5f) * 0.03f;
        }

        if (stumbleT > 0f)
        {
            stumbleT -= dt;
            leanX = 10f + 35f * Mathf.Sin(Mathf.Clamp01(stumbleT / 0.5f) * Mathf.PI);
        }

        rollBlend = Mathf.MoveTowards(rollBlend, rollT > 0f ? 1f : 0f, 10f * dt);
        sq = Mathf.MoveTowards(sq, 0f, 6f * dt);
        pivot.localRotation = Quaternion.Euler(leanX + rollRot, lean * 22f, -lean * 18f);
        pivot.localPosition = new Vector3(0, 0.9f + bob + (hover ? 0.07f : 0f) - 0.33f * rollBlend, 0);
        float sy = (1f + sq * 0.22f) * Mathf.Lerp(1f, 0.68f, rollBlend);
        float sxz = 1f - sq * 0.12f;
        pivot.localScale = new Vector3(sxz, sy, sxz);
    }
}

public class Chaser : MonoBehaviour
{
    Rig insp;
    Transform dogRoot;
    Transform[] dl = new Transform[4];
    float offset = -18f, phase;

    public void Build()
    {
        insp = Builder.Humanoid(transform, new Color(0.1f, 0.2f, 0.7f), new Color(0.08f, 0.1f, 0.3f),
            new Color(0.9f, 0.7f, 0.55f), new Color(0.05f, 0.1f, 0.4f), new Color(0.05f, 0.05f, 0.05f), 1.05f);
        insp.root.localPosition = new Vector3(-0.55f, 0, 0);
        Gfx.Prim(PrimitiveType.Cube, insp.root, new Vector3(0.12f, 1.38f, 0.17f), new Vector3(0.1f, 0.1f, 0.02f), Gfx.Mat(new Color(1f, 0.8f, 0.2f), 1f), "badge");

        dogRoot = new GameObject("dog").transform;
        dogRoot.SetParent(transform, false);
        dogRoot.localPosition = new Vector3(0.9f, 0, 1.0f);
        var mD = Gfx.Mat(new Color(0.7f, 0.45f, 0.2f));
        var mDark = Gfx.Mat(new Color(0.3f, 0.18f, 0.08f));
        Gfx.Prim(PrimitiveType.Cube, dogRoot, new Vector3(0, 0.6f, 0), new Vector3(0.35f, 0.38f, 0.9f), mD, "body");
        Gfx.Prim(PrimitiveType.Cube, dogRoot, new Vector3(0, 0.82f, 0.6f), new Vector3(0.3f, 0.3f, 0.34f), mD, "head");
        Gfx.Prim(PrimitiveType.Cube, dogRoot, new Vector3(0, 0.76f, 0.82f), new Vector3(0.18f, 0.14f, 0.2f), mDark, "snout");
        Gfx.Prim(PrimitiveType.Cube, dogRoot, new Vector3(-0.12f, 1.0f, 0.55f), new Vector3(0.08f, 0.16f, 0.1f), mDark, "ear");
        Gfx.Prim(PrimitiveType.Cube, dogRoot, new Vector3(0.12f, 1.0f, 0.55f), new Vector3(0.08f, 0.16f, 0.1f), mDark, "ear");
        var tail = Gfx.Prim(PrimitiveType.Cube, dogRoot, new Vector3(0, 0.75f, -0.55f), new Vector3(0.08f, 0.08f, 0.35f), mDark, "tail");
        tail.transform.localRotation = Quaternion.Euler(-35, 0, 0);
        Vector3[] lp = { new Vector3(-0.12f, 0.42f, 0.3f), new Vector3(0.12f, 0.42f, 0.3f), new Vector3(-0.12f, 0.42f, -0.3f), new Vector3(0.12f, 0.42f, -0.3f) };
        for (int i = 0; i < 4; i++)
        {
            var p = new GameObject("dleg").transform;
            p.SetParent(dogRoot, false);
            p.localPosition = lp[i];
            Gfx.Prim(PrimitiveType.Cube, p, new Vector3(0, -0.21f, 0), new Vector3(0.1f, 0.42f, 0.1f), mD, "l");
            dl[i] = p;
        }
        gameObject.SetActive(false);
    }

    public void Hide() { offset = -18f; gameObject.SetActive(false); }

    public void Tick(Vector3 p, bool active, float dt)
    {
        offset = Mathf.MoveTowards(offset, active ? -3.8f : -18f, 16f * dt);
        bool vis = offset > -17f;
        if (vis && !gameObject.activeSelf)
        {
            gameObject.SetActive(true);
            transform.position = new Vector3(p.x, 0, p.z + offset);
        }
        if (!vis) { if (gameObject.activeSelf) gameObject.SetActive(false); return; }
        phase += dt * 14f;
        float s = Mathf.Sin(phase);
        float x = Mathf.Lerp(transform.position.x, p.x, 1f - Mathf.Exp(-8f * dt));
        transform.position = new Vector3(x, Mathf.Abs(s) * 0.05f, p.z + offset);
        insp.legL.localRotation = Quaternion.Euler(s * 55f, 0, 0);
        insp.legR.localRotation = Quaternion.Euler(-s * 55f, 0, 0);
        insp.armL.localRotation = Quaternion.Euler(-s * 50f, 0, 0);
        insp.armR.localRotation = Quaternion.Euler(s * 50f, 0, 0);
        dl[0].localRotation = Quaternion.Euler(s * 50f, 0, 0);
        dl[1].localRotation = Quaternion.Euler(-s * 50f, 0, 0);
        dl[2].localRotation = Quaternion.Euler(-s * 50f, 0, 0);
        dl[3].localRotation = Quaternion.Euler(s * 50f, 0, 0);
    }
}
