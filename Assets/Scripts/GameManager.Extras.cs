using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

// Extra features: bloom glow, real music, rain showers, daily missions, shop (styles + hoverboards)
public partial class GameManager
{
    // ====================================================================== bloom (URP post-processing)
    void SetupBloom()
    {
        try
        {
            var volT = System.Type.GetType("UnityEngine.Rendering.Volume, Unity.RenderPipelines.Core.Runtime");
            var profT = System.Type.GetType("UnityEngine.Rendering.VolumeProfile, Unity.RenderPipelines.Core.Runtime");
            var bloomT = System.Type.GetType("UnityEngine.Rendering.Universal.Bloom, Unity.RenderPipelines.Universal.Runtime");
            var camT = System.Type.GetType("UnityEngine.Rendering.Universal.UniversalAdditionalCameraData, Unity.RenderPipelines.Universal.Runtime");
            if (volT == null || profT == null || bloomT == null || camT == null)
            {
                Debug.Log("[Subway Surface] Bloom skipped: this project uses the Built-in render pipeline (bloom needs the URP template). The game works fine without it.");
                return;
            }
            cam.allowHDR = true;
            var cd = cam.GetComponent(camT);
            if (cd == null) cd = cam.gameObject.AddComponent(camT);
            camT.GetProperty("renderPostProcessing").SetValue(cd, true);

            var go = new GameObject("BloomVolume");
            var vol = go.AddComponent(volT);
            volT.GetProperty("isGlobal").SetValue(vol, true);
            volT.GetProperty("priority").SetValue(vol, 10f);
            var prof = ScriptableObject.CreateInstance(profT);
            var add = profT.GetMethod("Add", new[] { typeof(System.Type), typeof(bool) });
            var bloom = add.Invoke(prof, new object[] { bloomT, true });
            SetParam(bloom, "intensity", 0.9f);
            SetParam(bloom, "threshold", 1.0f);
            SetParam(bloom, "scatter", 0.7f);
            volT.GetProperty("sharedProfile").SetValue(vol, prof);
        }
        catch (System.Exception e)
        {
            Debug.Log("[Subway Surface] Bloom skipped: " + e.Message);
        }
    }

    static void SetParam(object comp, string field, float v)
    {
        var f = comp.GetType().GetField(field);
        if (f == null) return;
        var prm = f.GetValue(comp);
        prm.GetType().GetProperty("value").SetValue(prm, v);
        prm.GetType().GetProperty("overrideState").SetValue(prm, true);
    }

    // ====================================================================== music
    AudioClip FindMusic()
    {
        AudioClip clip = Resources.Load<AudioClip>("subway-surfers");
        if (clip == null) clip = Resources.Load<AudioClip>("music");
#if UNITY_EDITOR
        if (clip == null)
        {
            // look for any imported audio clip in the project whose name looks like the song
            foreach (var guid in AssetDatabase.FindAssets("t:AudioClip"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.StartsWith("Assets/")) continue;
                string n = System.IO.Path.GetFileNameWithoutExtension(path).ToLowerInvariant();
                if (n.Contains("subway") || n.Contains("music") || n.Contains("theme"))
                {
                    clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                    if (clip != null) break;
                }
            }
        }
#endif
        Debug.Log(clip != null
            ? "[Subway Surface] Music loaded: " + clip.name + " (" + clip.length.ToString("0.0") + "s)"
            : "[Subway Surface] Music file not found - using built-in tune. Put subway-surfers.mp3 in Assets/Resources/.");
        return clip;
    }

    // ====================================================================== sound helpers
    static AudioClip LoadSfx(string name, AudioClip fallback)
    {
        var c = Resources.Load<AudioClip>("Sfx/" + name);
        return c != null ? c : fallback;
    }

    // one-at-a-time jingles (title letters, mystery box, mission reward, unlock) - new one replaces the old
    void PlayJingle(AudioClip c, float vol, float maxSeconds)
    {
        if (c == null || jingleSrc == null) return;
        jingleSrc.Stop();
        jingleSrc.clip = c;
        jingleSrc.volume = vol;
        jingleSrc.Play();
        jingleStop = maxSeconds < c.length ? Time.unscaledTime + maxSeconds : -1f;
    }

    void UpdateSfx()
    {
        if (jingleStop > 0f && Time.unscaledTime >= jingleStop) { jingleSrc.Stop(); jingleStop = -1f; }
        bool wantMagnet = tMagnet > 0f && state == State.Playing && cMagnet != null;
        if (wantMagnet && !magnetSrc.isPlaying) magnetSrc.Play();
        else if (!wantMagnet && magnetSrc.isPlaying) magnetSrc.Stop();
    }

    // ====================================================================== power-up shield + glow
    void UpdateShield()
    {
        if (shieldGo == null) return;
        float t = Time.time;
        bool any = tMagnet > 0f || tMult > 0f || tSpeed > 0f || tBoost > 0f || tHover > 0f || tJet > 0f;
        if (!any) { shieldGo.SetActive(false); ResetGlow(); return; }
        if (GetT(lastPower) <= 0f)
            foreach (var k in powerOrder) if (GetT(k) > 0f) { lastPower = k; break; }
        Color c = PowerColor(lastPower);
        float left = GetT(lastPower);
        bool blinkOff = left < 2f && ((int)(t * 8f) % 2 == 0);        // blinks when about to run out
        shieldGo.SetActive(!blinkOff);
        float pulse = 1f + 0.05f * Mathf.Sin(t * 6f);
        shieldGo.transform.localScale = new Vector3(1.9f * pulse, 2.35f * pulse, 1.9f * pulse);
        shieldMat.color = new Color(c.r, c.g, c.b, 0.2f + 0.08f * Mathf.Sin(t * 4f));
        if (shieldMat.HasProperty("_EmissionColor"))
            shieldMat.SetColor("_EmissionColor", new Color(c.r, c.g, c.b, 1f) * (0.8f + 0.4f * Mathf.Sin(t * 5f)));
        if (auraLight != null) { auraLight.color = c; auraLight.intensity = 4f + 1.5f * Mathf.Sin(t * 6f); }
        if (Fx.I != null && Random.value < 0.7f)
            Fx.I.Spawn(runner.transform.position + Vector3.up * 0.95f + Random.onUnitSphere * 1.2f, Vector3.up * 0.6f, c, 0.07f, 0.4f, 0f, 3f);
    }

    void ResetGlow()
    {
        if (auraLight == null) return;
        auraLight.color = new Color(1f, 0.8f, 0.55f);
        auraLight.intensity = 1.2f;
    }

    // ====================================================================== rain
    bool rainOn = false;   // rain is off by default (press R in play mode to turn it on)

    void RainFx()
    {
        if (!rainOn || Fx.I == null) return;
        Vector3 p = runner.transform.position;
        for (int i = 0; i < 6; i++)
            Fx.I.Rain(new Vector3(p.x + Random.Range(-10f, 10f), p.y + Random.Range(7f, 14f), p.z + Random.Range(-6f, 34f)));
    }

    // ====================================================================== daily missions
    class Mission { public int type, target, reward, progress; public bool done; }
    readonly Mission[] missions = new Mission[3];
    static readonly string[] mText = { "Collect {0} coins", "Run {0} meters", "Jump {0} times", "Roll {0} times", "Collect {0} power-ups", "Open {0} mystery boxes" };
    static readonly int[][] mTarget = {
        new[] { 100, 200, 300 }, new[] { 1000, 2000, 3500 }, new[] { 25, 50, 80 },
        new[] { 15, 30, 50 }, new[] { 4, 8, 12 }, new[] { 1, 3, 5 } };
    float mDist;
    UINode missionPanel;
    readonly Text[] tmText = new Text[3], tmProg = new Text[3], tmReward = new Text[3], tmHud = new Text[3];
    readonly UINode[] tmFill = new UINode[3];

    void InitMissions()
    {
        string today = System.DateTime.Now.ToString("yyyyMMdd");
        bool fresh = PlayerPrefs.GetString("nr_mdate", "") != today;
        var rng = new System.Random(int.Parse(today));
        var types = new List<int> { 0, 1, 2, 3, 4, 5 };
        for (int i = types.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            int tmp = types[i]; types[i] = types[j]; types[j] = tmp;
        }
        for (int i = 0; i < 3; i++)
        {
            int ty = types[i], lv = rng.Next(0, 3);
            var m = new Mission { type = ty, target = mTarget[ty][lv], reward = 60 + 40 * lv };
            if (!fresh)
            {
                m.progress = PlayerPrefs.GetInt("nr_m" + i + "p", 0);
                m.done = PlayerPrefs.GetInt("nr_m" + i + "d", 0) == 1;
            }
            missions[i] = m;
        }
        if (fresh) { PlayerPrefs.SetString("nr_mdate", today); SaveMissions(); }
    }

    void SaveMissions()
    {
        for (int i = 0; i < 3; i++)
        {
            if (missions[i] == null) continue;
            PlayerPrefs.SetInt("nr_m" + i + "p", missions[i].progress);
            PlayerPrefs.SetInt("nr_m" + i + "d", missions[i].done ? 1 : 0);
        }
    }

    void MissionAdd(int type, int amount)
    {
        for (int i = 0; i < 3; i++)
        {
            var m = missions[i];
            if (m == null || m.done || m.type != type) continue;
            m.progress = Mathf.Min(m.target, m.progress + amount);
            if (m.progress >= m.target)
            {
                m.done = true;
                bank += m.reward;
                PlayerPrefs.SetInt("nr_coins", bank);
                Toast("MISSION COMPLETE!  +" + m.reward + " COINS");
                PlayJingle(cMission, 0.9f, 3f);
                if (Fx.I != null) Fx.I.Burst(runner.transform.position + Vector3.up * 1.5f, gold, 26, 6f, 0.14f, 0.7f, 3f, 3f);
                SaveMissions();
            }
        }
    }

    void BuildMissionHud(UINode h)
    {
        for (int i = 0; i < 3; i++)
            tmHud[i] = UIKit.Label(h, "", 24, Vector2.zero, new Vector2(24, 24 + i * 32), new Vector2(760, 30), TextAnchor.MiddleLeft, new Color(1, 1, 1, 0.8f));
    }

    void BuildMissions()
    {
        var mp = UIKit.Full(ui, "Missions", new Color(0.0f, 0.12f, 0.22f, 0.95f));
        missionPanel = mp;
        var t = UIKit.Label(mp, "DAILY MISSIONS", 78, new Vector2(0.5f, 0.92f), Vector2.zero, new Vector2(1000, 100), TextAnchor.MiddleCenter, new Color(1f, 0.85f, 0.15f));
        t.outline = 4f;
        UIKit.Label(mp, "New missions every day. Rewards are added to your coins.", 26, new Vector2(0.5f, 0.85f), Vector2.zero, new Vector2(1000, 40), TextAnchor.MiddleCenter, new Color(1, 1, 1, 0.7f));
        for (int i = 0; i < 3; i++)
        {
            float y = 0.7f - i * 0.2f;
            tmText[i] = UIKit.Label(mp, "", 38, new Vector2(0f, y + 0.035f), new Vector2(40, 0), new Vector2(760, 50), TextAnchor.MiddleLeft, Color.white);
            tmReward[i] = UIKit.Label(mp, "", 40, new Vector2(1f, y + 0.035f), new Vector2(-40, 0), new Vector2(240, 50), TextAnchor.MiddleRight, new Color(1f, 0.85f, 0.2f));
            UIKit.Box(mp, "barbg", new Vector2(0f, y - 0.02f), new Vector2(40, 0), new Vector2(760, 26), new Color(0.2f, 0.2f, 0.32f));
            tmFill[i] = UIKit.Box(mp, "fill", new Vector2(0f, y - 0.02f), new Vector2(40, 0), new Vector2(0, 26), new Color(0.3f, 0.85f, 0.4f));
            tmProg[i] = UIKit.Label(mp, "", 30, new Vector2(0f, y - 0.02f), new Vector2(820, 0), new Vector2(240, 36), TextAnchor.MiddleLeft, Color.white);
        }
        buttons.Add(UIKit.Button(mp, "BACK", 54, new Vector2(0.5f, 0.05f), Vector2.zero, new Vector2(360, 80), new Color(0.2f, 0.75f, 0.3f), CloseMissions));
        mp.SetActive(false);
    }

    void OpenMissions() { homePanel.SetActive(false); missionPanel.SetActive(true); }
    void CloseMissions() { missionPanel.SetActive(false); homePanel.SetActive(true); }

    void RefreshMissionUI()
    {
        for (int i = 0; i < 3; i++)
        {
            var m = missions[i];
            if (m == null) continue;
            string txt = string.Format(mText[m.type], m.target);
            if (hud.active)
            {
                tmHud[i].text = txt + "   " + (m.done ? "DONE" : m.progress + "/" + m.target);
                tmHud[i].color = m.done ? new Color(0.4f, 1f, 0.5f) : new Color(1, 1, 1, 0.8f);
            }
            if (missionPanel.active)
            {
                tmText[i].text = txt;
                tmReward[i].text = "+" + m.reward;
                tmFill[i].size = new Vector2(760f * m.progress / m.target, 26f);
                tmProg[i].text = m.done ? "DONE" : m.progress + "/" + m.target;
                tmProg[i].color = m.done ? new Color(0.4f, 1f, 0.5f) : Color.white;
            }
        }
    }

    // ====================================================================== shop: character styles + hoverboards
    UINode shopPanel;
    bool shopOpen;
    int charSel, boardSel, boardEq, charMask = 1, boardMask = 1;
    Text tShopCoins, tCharName, tBoardName, tShopMsg;
    UIButton charBtn, boardBtn;
    static readonly int[] charPrice = { 0, 300, 300, 500, 500, 800 };
    static readonly string[] boardNames = { "FLAMINGO", "OCEAN WAVE", "SUNSHINE", "PALM LEAF" };
    static readonly int[] boardPrice = { 0, 150, 300, 450 };
    static readonly Color[] boardMain = {
        new Color(1f, 0.2f, 0.65f), new Color(0.2f, 0.85f, 1f), new Color(1f, 0.8f, 0.2f), new Color(0.4f, 1f, 0.3f) };
    static readonly Color[] boardEdge = {
        new Color(0.2f, 0.95f, 1f), new Color(1f, 0.3f, 0.7f), new Color(1f, 0.55f, 0.15f), new Color(1f, 0.85f, 0.2f) };

    void LoadShopPrefs()
    {
        charMask = PlayerPrefs.GetInt("nr_chars", 1) | 1;
        boardMask = PlayerPrefs.GetInt("nr_boards", 1) | 1;
        boardEq = Mathf.Clamp(PlayerPrefs.GetInt("nr_board", 0), 0, boardNames.Length - 1);
        if (((charMask >> skin) & 1) == 0) skin = 0;
        if (((boardMask >> boardEq) & 1) == 0) boardEq = 0;
        runner.SetSkin(skinTint[skin]);
        runner.SetBoard(boardMain[boardEq], boardEdge[boardEq]);
    }

    void BuildShop()
    {
        var sp = UIKit.Full(ui, "Shop", new Color(0, 0, 0, 0));
        shopPanel = sp;
        Vector2 B = new Vector2(0.5f, 0f);
        Color purple = new Color(0.55f, 0.3f, 0.9f);
        UIKit.Box(sp, "sheet", B, Vector2.zero, new Vector2(6000, 600), new Color(0.04f, 0.02f, 0.12f, 0.92f));
        tShopCoins = UIKit.Label(sp, "", 44, B, new Vector2(0, 538), new Vector2(900, 56), TextAnchor.MiddleCenter, new Color(1f, 0.85f, 0.2f));
        UIKit.Label(sp, "CHARACTER STYLE", 24, B, new Vector2(0, 498), new Vector2(600, 30), TextAnchor.MiddleCenter, new Color(1, 1, 1, 0.7f));
        buttons.Add(UIKit.Button(sp, "<", 56, B, new Vector2(-300, 400), new Vector2(88, 88), purple, () => ShopChar(-1)));
        buttons.Add(UIKit.Button(sp, ">", 56, B, new Vector2(300, 400), new Vector2(88, 88), purple, () => ShopChar(1)));
        tCharName = UIKit.Label(sp, "", 40, B, new Vector2(0, 414), new Vector2(520, 60), TextAnchor.MiddleCenter, Color.white);
        charBtn = UIKit.Button(sp, "", 34, B, new Vector2(0, 330), new Vector2(380, 60), new Color(0.2f, 0.7f, 0.3f), CharAction);
        buttons.Add(charBtn);

        UIKit.Label(sp, "HOVERBOARD", 24, B, new Vector2(0, 292), new Vector2(600, 30), TextAnchor.MiddleCenter, new Color(1, 1, 1, 0.7f));
        buttons.Add(UIKit.Button(sp, "<", 56, B, new Vector2(-300, 194), new Vector2(88, 88), purple, () => ShopBoard(-1)));
        buttons.Add(UIKit.Button(sp, ">", 56, B, new Vector2(300, 194), new Vector2(88, 88), purple, () => ShopBoard(1)));
        tBoardName = UIKit.Label(sp, "", 40, B, new Vector2(0, 208), new Vector2(520, 60), TextAnchor.MiddleCenter, Color.white);
        boardBtn = UIKit.Button(sp, "", 34, B, new Vector2(0, 124), new Vector2(380, 60), new Color(0.2f, 0.7f, 0.3f), BoardAction);
        buttons.Add(boardBtn);

        tShopMsg = UIKit.Label(sp, "", 26, B, new Vector2(0, 88), new Vector2(800, 30), TextAnchor.MiddleCenter, new Color(1f, 0.6f, 0.5f));
        buttons.Add(UIKit.Button(sp, "BACK", 40, B, new Vector2(0, 16), new Vector2(300, 64), new Color(0.25f, 0.5f, 0.85f), CloseShop));
        sp.SetActive(false);
    }

    void OpenShop()
    {
        shopOpen = true;
        homePanel.SetActive(false);
        shopPanel.SetActive(true);
        charSel = skin; boardSel = boardEq;
        runner.SetSkin(skinTint[charSel]);
        runner.SetHover(true);
        runner.SetBoard(boardMain[boardSel], boardEdge[boardSel]);
        tShopMsg.text = "";
        RefreshShop();
    }

    void CloseShop()
    {
        shopOpen = false;
        shopPanel.SetActive(false);
        homePanel.SetActive(true);
        runner.SetSkin(skinTint[skin]);
        runner.SetHover(false);
        runner.SetBoard(boardMain[boardEq], boardEdge[boardEq]);
        tHomeBest.text = "BEST  " + best.ToString("D5") + "      COINS  " + bank;
    }

    void ShopChar(int d)
    {
        charSel = (charSel + d + skinNames.Length) % skinNames.Length;
        runner.SetSkin(skinTint[charSel]);
        Play(cPower, 0.4f);
        if (Fx.I != null) Fx.I.Burst(runner.transform.position + Vector3.up, SkinGlowOf(charSel), 18, 4f, 0.1f, 0.6f, 2f, 3f);
        tShopMsg.text = "";
        RefreshShop();
    }

    void ShopBoard(int d)
    {
        boardSel = (boardSel + d + boardNames.Length) % boardNames.Length;
        runner.SetBoard(boardMain[boardSel], boardEdge[boardSel]);
        Play(cPower, 0.4f);
        tShopMsg.text = "";
        RefreshShop();
    }

    Color SkinGlowOf(int i)
    {
        Color t = skinTint[i];
        float m = Mathf.Max(t.r, Mathf.Max(t.g, t.b));
        return new Color(Mathf.Clamp01(t.r / m), Mathf.Clamp01(t.g / m), Mathf.Clamp01(t.b / m));
    }

    void CharAction()
    {
        bool owned = ((charMask >> charSel) & 1) == 1;
        if (!owned)
        {
            if (bank < charPrice[charSel]) { tShopMsg.text = "NOT ENOUGH COINS"; return; }
            bank -= charPrice[charSel];
            charMask |= 1 << charSel;
            PlayerPrefs.SetInt("nr_chars", charMask);
            PlayerPrefs.SetInt("nr_coins", bank);
            if (Fx.I != null) Fx.I.Burst(runner.transform.position + Vector3.up, gold, 30, 6f, 0.12f, 0.8f, 3f, 3f);
            PlayJingle(cUnlock, 0.9f, 3f);
            tShopMsg.text = "UNLOCKED!";
        }
        else tShopMsg.text = "EQUIPPED";
        skin = charSel;
        PlayerPrefs.SetInt("nr_skin", skin);
        RefreshShop();
    }

    void BoardAction()
    {
        bool owned = ((boardMask >> boardSel) & 1) == 1;
        if (!owned)
        {
            if (bank < boardPrice[boardSel]) { tShopMsg.text = "NOT ENOUGH COINS"; return; }
            bank -= boardPrice[boardSel];
            boardMask |= 1 << boardSel;
            PlayerPrefs.SetInt("nr_boards", boardMask);
            PlayerPrefs.SetInt("nr_coins", bank);
            if (Fx.I != null) Fx.I.Burst(runner.transform.position + Vector3.up * 0.3f, gold, 30, 6f, 0.12f, 0.8f, 3f, 3f);
            PlayJingle(cUnlock, 0.9f, 3f);
            tShopMsg.text = "UNLOCKED!";
        }
        else tShopMsg.text = "EQUIPPED";
        boardEq = boardSel;
        PlayerPrefs.SetInt("nr_board", boardEq);
        RefreshShop();
    }

    void RefreshShop()
    {
        tShopCoins.text = "SHOP   -   COINS  " + bank;
        bool co = ((charMask >> charSel) & 1) == 1;
        tCharName.text = skinNames[charSel] + (co ? "" : "  (locked)");
        charBtn.label.text = co ? (skin == charSel ? "EQUIPPED" : "EQUIP") : "BUY  " + charPrice[charSel];
        bool bo = ((boardMask >> boardSel) & 1) == 1;
        tBoardName.text = boardNames[boardSel] + (bo ? "" : "  (locked)");
        boardBtn.label.text = bo ? (boardEq == boardSel ? "EQUIPPED" : "EQUIP") : "BUY  " + boardPrice[boardSel];
    }
}
