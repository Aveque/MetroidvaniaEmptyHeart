using System;
using UnityEngine;

// All planes are behind the solid tilemap and have no colliders. Camera movement,
// including room snaps/respawns, is sampled after CameraFollow.LateUpdate.
[DefaultExecutionOrder(1000)]
public class StationParallax : MonoBehaviour
{
    [Serializable]
    public class Plane
    {
        public string name;
        public Sprite sprite;
        public Vector2 tileSize;
        public Vector2 parallax;
        public Color tint = Color.white;
        public int sortingOrder;
        [Range(0f,.3f)] public float seamOverlap;
        [NonSerialized] public Material material;
        [Range(0f, 1f)] public float quietRoomOpacity = 1f;
        [NonSerialized] public Transform root;
        [NonSerialized] public SpriteRenderer[,] tiles;
    }

    [SerializeField] private Plane[] planes;
    [SerializeField] private CameraRoom quietRoom;
    [SerializeField] private Material featherTemplate;
    private Camera view;
    private CameraFollow follow;
    private Material unlit;
    public Plane[] Planes => planes;
    public void SetSeamBlend(Material material,float overlap){featherTemplate=material;foreach(var layer in planes)layer.seamOverlap=overlap;}

    public void Configure(Plane[] layers, CameraRoom calmRoom = null) { planes = layers; quietRoom = calmRoom; }

    private void Start()
    {
        view = Camera.main;
        follow = view != null ? view.GetComponent<CameraFollow>() : null;
        unlit = new Material(Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default"));
        foreach (Plane layer in planes)
        {
            if (layer.sprite == null) continue;
            layer.material=unlit;if(layer.seamOverlap>0 && featherTemplate){layer.material=new Material(featherTemplate);layer.material.SetFloat("_EdgeFade",layer.seamOverlap);layer.material.SetFloat("_DstBlend",layer.sortingOrder<-60?1:10);}
            GameObject root = new GameObject(layer.name); root.transform.SetParent(transform, false);
            layer.root = root.transform;
            layer.tiles = new SpriteRenderer[5, 5];
            for (int x = 0; x < 5; x++) for (int y = 0; y < 5; y++)
            {
                GameObject tile = new GameObject("Repeat " + x + "," + y);
                tile.transform.SetParent(root.transform, false);
                SpriteRenderer renderer = tile.AddComponent<SpriteRenderer>();
                renderer.sprite = layer.sprite; renderer.color = layer.tint;
                renderer.sharedMaterial = layer.material; renderer.sortingOrder = layer.sortingOrder;
                Vector2 source = layer.sprite.bounds.size;
                tile.transform.localScale = new Vector3(layer.tileSize.x / source.x, layer.tileSize.y / source.y, 1);
                layer.tiles[x, y] = renderer;
            }
        }
        UpdatePlanes();
    }

    private void LateUpdate() => UpdatePlanes();

    public void UpdatePlanes()
    {
        if (view == null || planes == null) return;
        Vector3 cameraPosition = view.transform.position;
        foreach (Plane layer in planes)
        {
            if (layer.root == null) continue;
            Vector2 origin = new Vector2(cameraPosition.x * (1f - layer.parallax.x), cameraPosition.y * (1f - layer.parallax.y));
            layer.root.position = new Vector3(origin.x, origin.y, 0f);
            Vector2 stride=layer.tileSize*(1-layer.seamOverlap);
            int centreX = Mathf.RoundToInt((cameraPosition.x - origin.x) / stride.x);
            int centreY = Mathf.RoundToInt((cameraPosition.y - origin.y) / stride.y);
            for (int x = 0; x < 5; x++) for (int y = 0; y < 5; y++)
            {
                SpriteRenderer tile = layer.tiles[x, y];
                Color tint = layer.tint;
                if (quietRoom != null && follow != null && follow.ActiveRoom == quietRoom)
                    tint.a *= layer.quietRoomOpacity;
                tile.color = tint;
                tile.transform.localPosition = new Vector3((centreX + x - 2) * stride.x,
                    (centreY + y - 2) * stride.y, 0);
                // Cull whole tiles, but leave a generous edge to avoid seams at room cuts.
                Vector3 p = tile.transform.position;
                tile.enabled = Mathf.Abs(p.x - cameraPosition.x) < view.orthographicSize * view.aspect + layer.tileSize.x * .51f
                    && Mathf.Abs(p.y - cameraPosition.y) < view.orthographicSize + layer.tileSize.y * .51f;
            }
        }
    }

    private void OnDestroy() { if(planes!=null)foreach(var layer in planes)if(layer.material && layer.material!=unlit)Destroy(layer.material);if (unlit != null) Destroy(unlit); }
}

