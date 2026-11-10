using System.Collections.Generic;
using UnityEngine;

// The solid door collider is retained; only its old flat rectangle is hidden.
public sealed class PaintedArenaSeal : MonoBehaviour
{
    private readonly List<SpriteRenderer> roots = new List<SpriteRenderer>();
    private readonly List<Material> materials = new List<Material>();
    private Transform skin;
    private float born;
    public bool Ready => skin && roots.Count == 6;
    public void Configure(Sprite[] art)
    {
        if (Ready || art == null || art.Length != 4) return;
        var source = GetComponent<SpriteRenderer>(); source.enabled = false;
        skin = new GameObject("Interlocking chitin portcullis").transform;
        skin.SetParent(transform, false);
        skin.localScale = new Vector3(1 / transform.lossyScale.x, 1 / transform.lossyScale.y, 1);
        for (int i = 0; i < 6; i++)
        {
            var part = new GameObject(i < 4 ? "Closing thorn tendons" : "Root socket"); part.transform.SetParent(skin, false);
            var r = part.AddComponent<SpriteRenderer>(); r.sprite = art[i < 4 ? 1 : 2]; r.sortingOrder = 11;
            var material = new Material(source.sharedMaterial) { name = "Painted arena roots", hideFlags = HideFlags.DontSave };
            material.mainTexture = r.sprite.texture; r.sharedMaterial = material; materials.Add(material); roots.Add(r);
        }
        born = Time.time; LateUpdate();
    }
    private void OnEnable() { born = Time.time; }
    private void LateUpdate()
    {
        if (!Ready) return;
        float grow = Mathf.SmoothStep(.65f, 1, Mathf.Clamp01((Time.time - born) / .45f));
        for (int i = 0; i < roots.Count; i++)
        {
            var r = roots[i]; float pulse = Mathf.Sin(Time.time * 3 + i * 1.8f);
            bool top = i % 2 == 1; float height = i < 4 ? 1.95f * grow : .38f;
            float width = i < 4 ? .62f : 1.05f;
            r.transform.localPosition = new Vector3(i < 4 ? (i < 2 ? -.13f : .13f) : 0,
                top ? 1.5f - height / 2 : -1.5f + height / 2);
            r.transform.localRotation = Quaternion.Euler(0, 0, (top ? 180 : 0) + (i < 4 ? pulse * 2 : 0));
            Vector2 content = r.sprite.bounds.size - Vector3.one * (48f / r.sprite.pixelsPerUnit);
            r.transform.localScale = new Vector3(width / content.x, height / content.y, 1);
            r.color = new Color(.82f + pulse * .04f, .88f, .86f, 1);
        }
    }
    private void OnDestroy() { foreach (var material in materials) if (material) Destroy(material); }
}
