using UnityEngine;
using UnityEngine.Tilemaps;

// Palette variants retain terrain silhouettes and have no collision of their own.
[CreateAssetMenu(menuName="Empty Heart/Station Material Tile")]
public sealed class StationMaterialTile : PaintedStationTile
{
    public Sprite Interior;
    public Color Tint=Color.white;
    public bool GratingDeck;
    public override void GetTileData(Vector3Int position,ITilemap map,ref TileData data)
    {
        base.GetTileData(position,map,ref data);
        bool inside=map.GetTile(position+Vector3Int.up) && map.GetTile(position+Vector3Int.down)
            && map.GetTile(position+Vector3Int.left) && map.GetTile(position+Vector3Int.right);
        if(Interior && (inside || GratingDeck))
        {data.sprite=Interior;Vector2 size=Interior.bounds.size;data.transform=Matrix4x4.Scale(new Vector3(1/size.x,1/size.y,1));}
        data.color=Tint;
    }
}
