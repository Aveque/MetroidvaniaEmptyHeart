using System;
using System.Collections.Generic;
using UnityEngine;

// A shared, resolution-independent skin for the existing lightweight HUD.
public static class EmptyHeartUI
{
    public static readonly Color Ink = new Color(0.025f, 0.045f, 0.05f, 0.94f);
    public static readonly Color Copper = new Color(0.56f, 0.39f, 0.25f);
    public static readonly Color Jade = new Color(0.37f, 0.9f, 0.76f);
    public static readonly Color Paper = new Color(0.87f, 0.9f, 0.8f);
    public static readonly Color Muted = new Color(0.48f, 0.6f, 0.57f);
    public static readonly Color Danger = new Color(0.97f, 0.43f, 0.31f);
    private static Font headingFont, bodyFont;
    private static Texture2D bevel;
    private static readonly Dictionary<string, GUIStyle> styles = new Dictionary<string, GUIStyle>();
    public static float Scale => Mathf.Max(0.3f, Mathf.Min(Screen.height / 900f, Screen.width / 1280f));
    public static float Width => Screen.width / Scale;
    public static float Height => Screen.height / Scale;
    public static bool FontsLoaded => HeadingFont != null && BodyFont != null;
    public static Font HeadingFont => headingFont != null ? headingFont : headingFont = Resources.Load<Font>("UI/Fonts/RussoOne-Regular");
    public static Font BodyFont => bodyFont != null ? bodyFont : bodyFont = Resources.Load<Font>("UI/Fonts/PTSans-Regular");
    public static Scope Begin() => new Scope();

    public sealed class Scope : IDisposable
    {
        private readonly Matrix4x4 matrix;
        private readonly Color color;
        private readonly int depth;
        public Scope()
        {
            matrix = GUI.matrix; color = GUI.color; depth = GUI.depth;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one * Scale);
            GUI.color = Color.white; GUI.depth = -50;
        }
        public void Dispose() { GUI.matrix = matrix; GUI.color = color; GUI.depth = depth; }
    }

    public static void Interaction(string key,string title,string detail="",float bottom=145)
    {
        float w=Mathf.Min(640,Width-40),h=string.IsNullOrEmpty(detail)?58:78;
        Rect r=new Rect((Width-w)/2,Height-bottom,w,h);GUI.depth=-85;
        Panel(r,Copper);Fill(new Rect(r.x+1,r.y+1,4,h-2),Jade);
        Keycap(new Rect(r.x+18,r.y+16,40,32),key,Copper);
        Text(new Rect(r.x+76,r.y+10,w-94,32),title,17,Paper,true);
        if(!string.IsNullOrEmpty(detail))Text(new Rect(r.x+76,r.y+40,w-94,28),detail,14,Muted);
    }
    public static void Fill(Rect rect, Color color)
    {
        Color old = GUI.color; GUI.color = color; GUI.DrawTexture(rect, Texture2D.whiteTexture); GUI.color = old;
    }
    private static Texture2D Bevel
    {
        get
        {
            if (bevel != null) return bevel;
            bevel = new Texture2D(32, 32, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, hideFlags = HideFlags.HideAndDontSave };
            for (int y = 0; y < 32; y++) for (int x = 0; x < 32; x++)
            {
                int dx = Mathf.Min(x, 31 - x), dy = Mathf.Min(y, 31 - y);
                bevel.SetPixel(x, y, dx + dy >= 6 ? Color.white : Color.clear);
            }
            bevel.Apply(); return bevel;
        }
    }
    public static void Hex(Rect rect, Color color)
    {
        Color old = GUI.color; GUI.color = color; GUI.DrawTexture(rect, Bevel); GUI.color = old;
    }
    public static void Panel(Rect r, Color accent)
    {
        Fill(new Rect(r.x + 3, r.y + 4, r.width, r.height), new Color(0, 0, 0, 0.35f));
        Fill(r, new Color(accent.r, accent.g, accent.b, 0.65f));
        Fill(new Rect(r.x + 1, r.y + 1, r.width - 2, r.height - 2), Ink);
        Fill(new Rect(r.x + 10, r.y, 28, 2), accent);
        Fill(new Rect(r.xMax - 38, r.yMax - 2, 28, 2), accent);
        Hex(new Rect(r.x - 3, r.y + 8, 6, 6), accent);
        Hex(new Rect(r.xMax - 3, r.yMax - 14, 6, 6), accent);
    }
    public static void Text(Rect rect, string text, int size, Color color, bool heading = false,
        TextAnchor align = TextAnchor.MiddleLeft)
    {
        string key = size + ":" + heading + ":" + (int)align;
        if (!styles.TryGetValue(key, out GUIStyle style))
        {
            style = new GUIStyle(GUI.skin.label) { font = heading ? HeadingFont : BodyFont, fontSize = size,
                alignment = align, wordWrap = true, padding = new RectOffset(0, 0, 0, 0), richText = false };
            styles.Add(key, style);
        }
        Color old = GUI.color;
        GUI.color = new Color(0, 0, 0, color.a * 0.75f);
        GUI.Label(new Rect(rect.x + 1, rect.y + 1, rect.width, rect.height), text, style);
        GUI.color = color; GUI.Label(rect, text, style); GUI.color = old;
    }
    public static void Keycap(Rect rect, string label, Color accent)
    {
        Hex(rect, accent); Hex(new Rect(rect.x + 1, rect.y + 1, rect.width - 2, rect.height - 3), Ink);
        Text(rect, label, 14, Paper, false, TextAnchor.MiddleCenter);
    }
    public static void Ornament(float center, float y, float width, Color tint)
    {
        Fill(new Rect(center - width / 2, y, width / 2 - 12, 1), tint);
        Fill(new Rect(center + 12, y, width / 2 - 12, 1), tint);
        Hex(new Rect(center - 5, y - 4, 10, 9), tint);
        Hex(new Rect(center - 2, y - 1, 4, 3), Ink);
    }
    public static void PlayerHUD(int health, int max, bool dash, float dashCooldown)
    {
        using (Begin())
        {
            float width = Mathf.Max(306, 84 + max * 36);
            Rect panel = new Rect(24, 24, width, 132);
            Panel(panel, Copper);
            Text(new Rect(42, 32, width - 90, 22), "ОБОЛОЧКА", 15, Paper, true);
            Text(new Rect(panel.xMax - 70, 32, 50, 22), health + "/" + max, 16, Jade, false, TextAnchor.MiddleRight);
            Fill(new Rect(42, 59, width - 36, 1), new Color(0.2f, 0.28f, 0.27f));
            for (int i = 0; i < max; i++)
            {
                Rect cell = new Rect(42 + i * 36, 69, 27, 37);
                Hex(cell, i < health ? Copper : new Color(0.23f, 0.27f, 0.26f));
                Hex(new Rect(cell.x + 2, cell.y + 2, 23, 33), Ink);
                Color fill = i < health ? (health == 1 ? Danger : Jade) : new Color(0.12f, 0.2f, 0.19f);
                Hex(new Rect(cell.x + 6, cell.y + 6, 15, 25), fill);
                Fill(new Rect(cell.x + 9, cell.y + 8, 3, 17), i < health ? new Color(0.8f, 1f, 0.91f, 0.7f) : Color.clear);
                Fill(new Rect(cell.x + 3, cell.y + 17, 21, 2), Ink);
            }
            if (dash)
            {
                Keycap(new Rect(42, 114, 53, 25), "SHIFT", Copper);
                Text(new Rect(108, 113, width - 140, 22), dashCooldown > 0 ? "ПРИВОД ЗАРЯЖАЕТСЯ" : "ИМПУЛЬС ГОТОВ", 13, dashCooldown > 0 ? Muted : Jade);
                Fill(new Rect(108, 138, width - 140, 2), new Color(0.13f, 0.25f, 0.23f));
                Fill(new Rect(108, 138, (width - 140) * (1 - Mathf.Clamp01(dashCooldown)), 2), Jade);
            }
            else Text(new Rect(42, 113, width - 36, 25), "НЕЙРОПРИВОД НЕ ОБНАРУЖЕН", 13, Muted);
        }
    }
    public static void BossHUD(BiomechBoss boss, float delayedHealth)
    {
        using (Begin())
        {
            float width = Mathf.Min(660, Width - 110), x = (Width - width) / 2, y = Height - 84;
            Panel(new Rect(x - 20, y - 7, width + 40, 77), Copper);
            Text(new Rect(x + 14, y + 1, width - 28, 29), "САДОВНИК-07", 24, Paper, true, TextAnchor.MiddleCenter);
            Text(new Rect(x, y + 29, width, 18), "ХРАНИТЕЛЬ НИЖНИХ САДОВ", 12, Muted, false, TextAnchor.MiddleCenter);
            Color accent = boss.Enraged ? Danger : Jade;
            Fill(new Rect(x, y + 53, width, 12), new Color(0.12f, 0.19f, 0.18f));
            Fill(new Rect(x + 2, y + 55, (width - 4) * Mathf.Clamp01(delayedHealth / boss.MaxHealth), 8), Copper);
            Fill(new Rect(x + 2, y + 55, (width - 4) * boss.CurrentHealth / boss.MaxHealth, 8), accent);
            for (int i = 1; i < 10; i++) Fill(new Rect(x + width * i / 10, y + 53, 1, 12), Ink);
            Hex(new Rect(x - 10, y + 52, 10, 14), Copper);
            Hex(new Rect(x + width, y + 52, 10, 14), Copper);
            // A shield-shaped emblem, rather than combat instructions. Its
            // dark open centre and dim colour indicate a broken circuit.
            bool shielded = boss.ActiveBody && !boss.CanTakeDamage;
            Color shield = shielded ? (boss.ArmorHitFlash ? Copper : Jade) : Muted * .45f;
            Hex(new Rect(x - 52, y + 27, 25, 31), shield);
            Hex(new Rect(x - 48, y + 31, 17, 21), Ink);
            if (shielded) Hex(new Rect(x - 43, y + 36, 7, 11), shield);
        }
    }
    public static void Announcement(string overline, string title, string detail, float alpha, float y = 0.29f)
    {
        using (Begin())
        {
            float width = Mathf.Min(600, Width - 64), center = Width / 2, top = Height * y;
            Color paper = Paper; paper.a = alpha;
            Color copper = Copper; copper.a = alpha;
            Color muted = Muted; muted.a = alpha;
            Fill(new Rect(center - width / 2, top - 12, width, 133), new Color(0.02f, 0.04f, 0.045f, 0.88f * alpha));
            Text(new Rect(center - width / 2, top, width, 19), overline, 12, muted, false, TextAnchor.MiddleCenter);
            Text(new Rect(center - width / 2 + 12, top + 25, width - 24, 44), title, title.Length > 28 ? 22 : 27, paper, true, TextAnchor.MiddleCenter);
            Ornament(center, top + 80, width - 80, copper);
            Text(new Rect(center - width / 2 + 14, top + 91, width - 28, 25), detail, 16, paper, false, TextAnchor.MiddleCenter);
        }
    }
}

