using UnityEngine;

// Deforms the painted plasma itself every frame, rather than translating a
// rigid quad. Used for the travelling discharge and the broken armour circuit.
public sealed class LivingEnergyRibbon : MonoBehaviour
{
    private const int Segments = 48;
    private Mesh mesh;
    private Material material;
    private readonly Vector3[] vertices = new Vector3[(Segments + 1) * 2];
    private readonly Vector2[] uv = new Vector2[(Segments + 1) * 2];
    private readonly Color[] colors = new Color[(Segments + 1) * 2];
    private Rect textureArea;
    private Vector2 extent;
    private float arcStart, arcEnd, seed;
    private bool curved;
    public float Opacity = 1;
    public float Spread = 1;
    public Color Tint = Color.white;
    public Mesh AnimatedMesh => mesh;
    public bool Curved => curved;
    public void SetExtent(Vector2 value) { extent=value; }

    public void Configure(Sprite sprite, Material template, Vector2 size, int order,
        bool arc = false, float start = 0, float end = 0)
    {
        extent = size; curved = arc; arcStart = start; arcEnd = end;
        seed = transform.GetSiblingIndex() * 1.7f;
        Rect rect = sprite.textureRect;
        textureArea = new Rect(rect.x / sprite.texture.width, rect.y / sprite.texture.height,
            rect.width / sprite.texture.width, rect.height / sprite.texture.height);
        mesh = new Mesh { name = "Flowing painted plasma", hideFlags = HideFlags.DontSave };
        mesh.MarkDynamic();
        gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
        var renderer = gameObject.AddComponent<MeshRenderer>(); renderer.sortingOrder = order;
        material = new Material(template) { name = "Living painted energy", hideFlags = HideFlags.DontSave };
        material.mainTexture = sprite.texture; renderer.sharedMaterial = material;
        int[] triangles = new int[Segments * 6];
        for (int i = 0; i < Segments; i++)
        {
            int v = i * 2, t = i * 6;
            triangles[t] = v; triangles[t + 1] = v + 2; triangles[t + 2] = v + 1;
            triangles[t + 3] = v + 1; triangles[t + 4] = v + 2; triangles[t + 5] = v + 3;
        }
        mesh.vertices = vertices; mesh.triangles = triangles;
        LateUpdate();
    }

    private void LateUpdate()
    {
        if (!mesh) return;
        float age = Time.time;
        for (int i = 0; i <= Segments; i++)
        {
            float t = i / (float)Segments;
            float taper = Mathf.Sin(t * Mathf.PI);
            float wave = Mathf.Sin(t * 24 - age * 13 + seed) * .6f
                + Mathf.Sin(t * 41 + age * 17 + seed) * .4f;
            Vector2 center, normal;
            float width;
            if (curved)
            {
                float angle = Mathf.Lerp(arcStart, arcEnd, t) * Mathf.Deg2Rad;
                normal = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                center = Vector2.Scale(normal, extent * .5f) * Spread + normal * wave * .045f;
                width = .16f * (.85f + .15f * wave);
            }
            else
            {
                center = new Vector2(wave * extent.x * .12f * taper, Mathf.Lerp(-extent.y / 2, extent.y / 2, t));
                normal = Vector2.right;
                width = extent.x * (.78f + .15f * Mathf.Sin(t * 32 - age * 19));
            }
            vertices[i * 2] = center - normal * width * .5f;
            vertices[i * 2 + 1] = center + normal * width * .5f;
            // Travelling compression of the internal painted wisps. The tips
            // stay anchored, avoiding a texture wrap seam at the floor/ceiling.
            float flow = t + taper * Mathf.Sin(t * 17 - age * 10 + seed) * .025f;
            uv[i * 2] = new Vector2(textureArea.xMin, textureArea.yMin + flow * textureArea.height);
            uv[i * 2 + 1] = new Vector2(textureArea.xMax, textureArea.yMin + flow * textureArea.height);
            float pulse = .7f + .3f * Mathf.Pow(.5f + .5f * Mathf.Sin(t * 26 - age * 16 + seed), 3);
            Color color = Tint; color.a *= Opacity * pulse;
            colors[i * 2] = colors[i * 2 + 1] = color;
        }
        mesh.vertices = vertices; mesh.uv = uv; mesh.colors = colors; mesh.RecalculateBounds();
    }
    private void OnDestroy() { if (mesh) Destroy(mesh); if (material) Destroy(material); }
}
