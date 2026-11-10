using UnityEngine;

// Saved level landmarks also let editor tools and traversal checks use the current layout.
public class StationMapInfo : MonoBehaviour
{
    public string version = "Expanded station 2";
    public Vector2 spawn = new Vector2(-47f, -2.48f);
    public Vector2 dashPickup = new Vector2(52.5f, 26.85f);
    public RectInt solidBounds = new RectInt(-64, -28, 165, 77);
}
