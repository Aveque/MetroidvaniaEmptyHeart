using UnityEngine;

// Short contact flash and ballistic, tapered steel sparks. World-space so the
// effect stays on the contact point while the player/target continue moving.
public sealed class SwordImpactBurst : MonoBehaviour
{
    private const int Sparks = 16, FlashRays = 8;
    private readonly Vector3[] vertices = new Vector3[(Sparks + FlashRays) * 3];
    private readonly Color[] colors = new Color[(Sparks + FlashRays) * 3];
    private readonly Vector2[] velocities = new Vector2[Sparks];
    private Mesh mesh;
    private Material material;
    private float born;
    private bool armor;
    public void Configure(Vector2 direction, bool armored)
    {
        born = Time.time; armor = armored;
        mesh = new Mesh { name = "Steel impact sparks", hideFlags = HideFlags.DontSave }; mesh.MarkDynamic();
        gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
        var renderer = gameObject.AddComponent<MeshRenderer>(); renderer.sortingOrder = 24;
        material = new Material(Shader.Find("Sprites/Default")) { name = "Sword impact light", hideFlags = HideFlags.DontSave };
        material.mainTexture = Texture2D.whiteTexture; renderer.sharedMaterial = material;
        int[] indices = new int[vertices.Length]; for (int i = 0; i < indices.Length; i++) indices[i] = i;
        mesh.vertices = vertices; mesh.triangles = indices;
        float angle = Mathf.Atan2(direction.y, direction.x);
        for (int i = 0; i < Sparks; i++)
        {
            float spread = Mathf.Lerp(-1.5f, 1.5f, i / (float)(Sparks - 1));
            // A few reflected sparks travel back along the blade.
            float a = angle + spread + (i % 5 == 0 ? Mathf.PI : 0);
            velocities[i] = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * (1.2f + (i * 7 % 11) * .26f);
        }
        LateUpdate();
    }
    private void LateUpdate()
    {
        if (!mesh) return;
        float age = Time.time - born;
        if (age > .3f) { Destroy(gameObject); return; }
        for (int i = 0; i < Sparks; i++)
        {
            Vector2 velocity = velocities[i];
            Vector2 position = velocity * age + Vector2.down * age * age * 2.8f;
            Vector2 heading = (velocity + Vector2.down * age * 5.6f).normalized;
            Vector2 side = new Vector2(-heading.y, heading.x);
            float alpha = Mathf.Clamp01(1 - age / (.16f + (i % 4) * .035f));
            float length = (.07f + velocity.magnitude * .032f) * alpha;
            int v = i * 3;
            vertices[v] = position + side * .014f * alpha;
            vertices[v + 1] = position - side * .014f * alpha;
            vertices[v + 2] = position - heading * length;
            Color hot = armor && i % 3 == 0 ? new Color(.45f, 1, .92f, alpha) : new Color(1, .87f, .5f, alpha);
            colors[v] = colors[v + 1] = hot; colors[v + 2] = new Color(1, .32f, .07f, 0);
        }
        float flash = Mathf.Clamp01(1 - age / .085f);
        for (int i = 0; i < FlashRays; i++)
        {
            float angle = i * Mathf.PI / 4;
            Vector2 axis = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            Vector2 side = new Vector2(-axis.y, axis.x);
            int v = (Sparks + i) * 3;
            vertices[v] = side * .035f * flash; vertices[v + 1] = -side * .035f * flash;
            vertices[v + 2] = axis * (i % 2 == 0 ? .25f : .11f) * flash;
            colors[v] = colors[v + 1] = new Color(1, 1, .87f, flash);
            colors[v + 2] = new Color(1, .7f, .3f, 0);
        }
        mesh.vertices = vertices; mesh.colors = colors; mesh.RecalculateBounds();
    }
    private void OnDestroy() { if (mesh) Destroy(mesh); if (material) Destroy(material); }
}
