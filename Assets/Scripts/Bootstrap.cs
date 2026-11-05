using UnityEngine;

// Creates the whole game automatically when you press Play (in any scene).
public static class Bootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Init()
    {
        if (GameObject.Find("NeonRunnerGame") != null) return;
        var g = new GameObject("NeonRunnerGame");
        g.AddComponent<GameManager>();
    }
}

public enum Kind { Coin, Magnet, Mult, Jetpack, Speed, ScoreBoost, Hover, Box, Ramp, Train, Barrier, Overhead, Cargo }

public class Item : MonoBehaviour
{
    public Kind kind;
    public float halfZ = 0.5f, halfX = 0.9f, bottom, top, baseY;
    public bool done;
    public float vz;      // trains that drive towards you
    public Transform spin;
    public Transform halo;   // sunburst rays + glow around pickups
    public bool IsObstacle { get { return kind >= Kind.Train; } }
    public bool IsPower { get { return kind >= Kind.Magnet && kind <= Kind.Hover; } }
}

// Works with the old Input Manager and the new Input System package.
#if ENABLE_INPUT_SYSTEM
public static class Inp
{
    static bool K(UnityEngine.InputSystem.Key k)
    {
        var kb = UnityEngine.InputSystem.Keyboard.current;
        return kb != null && kb[k].wasPressedThisFrame;
    }
    public static bool Left { get { return K(UnityEngine.InputSystem.Key.LeftArrow) || K(UnityEngine.InputSystem.Key.A); } }
    public static bool Right { get { return K(UnityEngine.InputSystem.Key.RightArrow) || K(UnityEngine.InputSystem.Key.D); } }
    public static bool Up { get { return K(UnityEngine.InputSystem.Key.UpArrow) || K(UnityEngine.InputSystem.Key.W) || K(UnityEngine.InputSystem.Key.Space); } }
    public static bool Down { get { return K(UnityEngine.InputSystem.Key.DownArrow) || K(UnityEngine.InputSystem.Key.S); } }
    public static bool Pause { get { return K(UnityEngine.InputSystem.Key.Escape) || K(UnityEngine.InputSystem.Key.P); } }
    public static bool Confirm { get { return K(UnityEngine.InputSystem.Key.Enter) || K(UnityEngine.InputSystem.Key.Space); } }
    public static bool Mute { get { return K(UnityEngine.InputSystem.Key.M); } }
    public static bool Rain { get { return K(UnityEngine.InputSystem.Key.R); } }

    public static bool PointerDown(out Vector2 p)
    {
        p = Vector2.zero;
        var ts = UnityEngine.InputSystem.Touchscreen.current;
        if (ts != null && ts.primaryTouch.press.wasPressedThisFrame) { p = ts.primaryTouch.position.ReadValue(); return true; }
        var m = UnityEngine.InputSystem.Mouse.current;
        if (m != null && m.leftButton.wasPressedThisFrame) { p = m.position.ReadValue(); return true; }
        return false;
    }
    public static bool PointerHeld(out Vector2 p)
    {
        p = Vector2.zero;
        var ts = UnityEngine.InputSystem.Touchscreen.current;
        if (ts != null && ts.primaryTouch.press.isPressed) { p = ts.primaryTouch.position.ReadValue(); return true; }
        var m = UnityEngine.InputSystem.Mouse.current;
        if (m != null && m.leftButton.isPressed) { p = m.position.ReadValue(); return true; }
        return false;
    }
}
#else
public static class Inp
{
    public static bool Left { get { return Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A); } }
    public static bool Right { get { return Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D); } }
    public static bool Up { get { return Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.Space); } }
    public static bool Down { get { return Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S); } }
    public static bool Pause { get { return Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P); } }
    public static bool Confirm { get { return Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space); } }
    public static bool Mute { get { return Input.GetKeyDown(KeyCode.M); } }
    public static bool Rain { get { return Input.GetKeyDown(KeyCode.R); } }

    public static bool PointerDown(out Vector2 p)
    {
        p = Vector2.zero;
        if (Input.touchCount > 0)
        {
            var t = Input.GetTouch(0);
            if (t.phase == TouchPhase.Began) { p = t.position; return true; }
            return false;
        }
        if (Input.GetMouseButtonDown(0)) { p = Input.mousePosition; return true; }
        return false;
    }
    public static bool PointerHeld(out Vector2 p)
    {
        p = Vector2.zero;
        if (Input.touchCount > 0) { p = Input.GetTouch(0).position; return true; }
        if (Input.GetMouseButton(0)) { p = Input.mousePosition; return true; }
        return false;
    }
}
#endif
