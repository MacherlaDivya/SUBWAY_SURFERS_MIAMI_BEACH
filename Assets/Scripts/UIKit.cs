using System.Collections.Generic;
using UnityEngine;

// Tiny UI system built on Unity's built-in IMGUI - NO extra packages needed.
public class UINode
{
    public UINode parent;
    public List<UINode> kids = new List<UINode>();
    public Vector2 anchor, pos, size;
    public Color col;
    public bool stretch, root, active = true;

    public void SetActive(bool b) { active = b; }
    public bool activeSelf { get { return active; } }
    public bool ActiveInHierarchy { get { return active && (parent == null || parent.ActiveInHierarchy); } }

    // Rect in canvas units, origin bottom-left (y up).
    public Rect GetRect(float W, float H)
    {
        if (root) return new Rect(0, 0, W, H);
        Rect p = parent.GetRect(W, H);
        if (stretch) return p;
        float l = p.x + anchor.x * p.width + pos.x - anchor.x * size.x;
        float b = p.y + anchor.y * p.height + pos.y - anchor.y * size.y;
        return new Rect(l, b, size.x, size.y);
    }
}

public class Text : UINode
{
    public string text = "";
    public int fontSize = 30;
    public TextAnchor alignment = TextAnchor.MiddleCenter;
    public Color color = Color.white;
    public float outline = 2f;
}

public class UIButton
{
    public UINode rt;
    public System.Action click;
    public Text label;
}

public static class UIKit
{
    static GUIStyle style;

    // 1 canvas unit = Scale pixels. Based on the SHORT side so the layout is clean on phones and in a wide editor window.
    public static float Scale { get { return Mathf.Max(0.2f, Mathf.Min(Screen.width, Screen.height) / 1080f); } }

    public static UINode Root()
    {
        return new UINode { root = true };
    }

    public static UINode Box(UINode parent, string name, Vector2 anchor, Vector2 pos, Vector2 size, Color col)
    {
        var n = new UINode { parent = parent, anchor = anchor, pos = pos, size = size, col = col };
        parent.kids.Add(n);
        return n;
    }

    public static UINode Full(UINode parent, string name, Color col)
    {
        var n = new UINode { parent = parent, stretch = true, col = col };
        parent.kids.Add(n);
        return n;
    }

    public static Text Label(UINode parent, string s, int fs, Vector2 anchor, Vector2 pos, Vector2 size, TextAnchor al, Color col)
    {
        var t = new Text { parent = parent, anchor = anchor, pos = pos, size = size, text = s, fontSize = fs, alignment = al, color = col };
        parent.kids.Add(t);
        return t;
    }

    public static UIButton Button(UINode parent, string text, int fs, Vector2 anchor, Vector2 pos, Vector2 size, Color col, System.Action click)
    {
        var rt = Box(parent, "btn_" + text, anchor, pos, size, col);
        Box(rt, "shade", new Vector2(0.5f, 0f), Vector2.zero, new Vector2(size.x, size.y * 0.12f), new Color(0, 0, 0, 0.25f));
        var l = Label(rt, text, fs, new Vector2(0.5f, 0.5f), Vector2.zero, size, TextAnchor.MiddleCenter, Color.white);
        return new UIButton { rt = rt, click = click, label = l };
    }

    // px = pointer position in pixels, origin bottom-left (Input.mousePosition style)
    public static bool Hit(UINode n, Vector2 px)
    {
        float s = Scale;
        Rect r = n.GetRect(Screen.width / s, Screen.height / s);
        return px.x >= r.x * s && px.x <= (r.x + r.width) * s && px.y >= r.y * s && px.y <= (r.y + r.height) * s;
    }

    public static void Draw(UINode root)
    {
        if (style == null)
        {
            style = new GUIStyle(GUI.skin.label);
            style.fontStyle = FontStyle.Bold;
            style.wordWrap = false;
            style.clipping = TextClipping.Overflow;
            style.normal.textColor = Color.white;
        }
        float s = Scale;
        DrawNode(root, s, Screen.width / s, Screen.height / s);
        GUI.color = Color.white;
    }

    static void DrawNode(UINode n, float s, float W, float H)
    {
        if (!n.active) return;
        Rect r = n.GetRect(W, H);
        Rect g = new Rect(r.x * s, (H - (r.y + r.height)) * s, r.width * s, r.height * s);
        if (n.col.a > 0f)
        {
            GUI.color = n.col;
            GUI.DrawTexture(g, Texture2D.whiteTexture);
        }
        var t = n as Text;
        if (t != null && !string.IsNullOrEmpty(t.text))
        {
            style.fontSize = Mathf.Max(8, (int)(t.fontSize * s));
            style.alignment = t.alignment;
            GUI.color = new Color(0, 0, 0, 0.85f);
            float o = Mathf.Max(1.5f, t.outline * s);
            GUI.Label(new Rect(g.x + o, g.y + o, g.width, g.height), t.text, style);
            GUI.Label(new Rect(g.x - o, g.y + o, g.width, g.height), t.text, style);
            GUI.Label(new Rect(g.x + o, g.y - o, g.width, g.height), t.text, style);
            GUI.Label(new Rect(g.x - o, g.y - o, g.width, g.height), t.text, style);
            GUI.color = t.color;
            GUI.Label(g, t.text, style);
        }
        for (int i = 0; i < n.kids.Count; i++) DrawNode(n.kids[i], s, W, H);
    }
}
