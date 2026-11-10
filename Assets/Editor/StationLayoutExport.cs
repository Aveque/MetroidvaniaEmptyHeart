#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;
public static class StationLayoutExport
{
    [Serializable]public class Room {public string name;public Vector2 center,size;}
    [Serializable]public class Gate {public string negative,positive;public Vector2 center,size;public bool vertical;}
    [Serializable]public class Layout {public Room[] rooms;public Gate[] gates;}
    [MenuItem("Tools/Empty Heart/Экспорт/Контуры комнат и переходов")]
    public static void Export()
    {
        if(EditorApplication.isPlaying)return;
        var layout=new Layout{rooms=Object.FindObjectsByType<CameraRoom>(FindObjectsSortMode.None).Select(r=>new Room{name=r.name,center=r.WorldBounds.center,size=r.WorldBounds.size}).ToArray(),
        gates=Object.FindObjectsByType<CameraTransitionGate>(FindObjectsSortMode.None).Select(g=>new Gate{negative=g.NegativeRoom.name,positive=g.PositiveRoom.name,center=g.transform.position,size=g.GetComponent<BoxCollider2D>().size,vertical=g.GetComponent<BoxCollider2D>().size.x>g.GetComponent<BoxCollider2D>().size.y}).ToArray()};
        File.WriteAllText("StationContourLayout.json",JsonUtility.ToJson(layout,true));Debug.Log("EXPORTED CARTOGRAPHY LAYOUT: "+layout.rooms.Length+" rooms, "+layout.gates.Length+" passage planes.");
    }
}
#endif
