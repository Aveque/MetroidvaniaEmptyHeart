#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;

public static class StationRefinementPlayCheck
{
    static PlayerMove player;static Rigidbody2D body;static StationLift lift;static Vector3 cabinStart;static double began;static int stage;
    static void Require(bool value,string message){if(!value)throw new Exception(message);}
    [MenuItem("Tools/Empty Heart/Проверки/Мир/Карта, оборудование и лифты")]
    public static void Run()
    {
        if(!EditorApplication.isPlaying)return;
        try
        {
            player=Object.FindFirstObjectByType<PlayerMove>();body=player.GetComponent<Rigidbody2D>();
            var rooms=Object.FindObjectsByType<CameraRoom>(FindObjectsSortMode.None);Require(rooms.Length==15 && rooms.All(r=>r.GetComponent<StationRoomShape>() && r.GetComponent<StationRoomShape>().Fill && r.GetComponent<StationRoomShape>().Outline),"Baked physical contours missing.");
            foreach(var r in Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None))
            {
                if(r.name=="Observatory grounded receiver")Require(Mathf.Abs(r.bounds.min.y-43)<.08f && r.bounds.size.y<2.5f,"Observatory receiver not grounded or oversized.");
                if(r.name=="Sinew circulation pump")Require(Mathf.Abs(r.bounds.min.y+36)<.08f && AssetDatabase.GetAssetPath(r.sprite).Contains("StationRefinement/pump"),"Pump artwork or ground contact incorrect.");
            }
            Require(!Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Any(t=>new[]{"Pressure piston","Lift cradle","Lift fixed drive","Observatory tracking receiver","Synaptic chrysalis"}.Contains(t.name)),"Obsolete decoration remains.");
            foreach(var sentry in Object.FindObjectsByType<SynapticSentry>(FindObjectsSortMode.None))
            {
                Require(AssetDatabase.GetAssetPath(sentry.Head.GetComponent<SpriteRenderer>().sprite).Contains("StationRefinement/sentry"),"Sentry still uses boss anatomy.");
                Vector2 back=sentry.FacesLeft?Vector2.right:Vector2.left;var hit=Physics2D.Raycast(sentry.transform.position,back,.5f,1<<LayerMask.NameToLayer("Wall"));Require(hit.collider,"Sentry is floating away from wall: "+sentry.name+" at "+sentry.transform.position);
            }
            var walls=Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).Where(m=>m.name.EndsWith("opaque distant wall")).ToArray();Require(walls.Length==6,"Opaque rear rooms missing.");
            foreach(var wall in walls){var colors=wall.GetComponent<MeshFilter>().sharedMesh.colors;Require(colors.All(c=>c.a>.99f) && !wall.GetComponent<Collider2D>(),"Rear wall is transparent or has solid collision.");}
            Require(Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).Count(m=>m.name.Contains("passage") && m.name.EndsWith("architectural aperture frame"))>=10,"Architectural passage frames missing.");
            foreach(var fragment in Object.FindObjectsByType<ShellFragment>(FindObjectsSortMode.None))Require(AssetDatabase.GetAssetPath(fragment.GetComponent<SpriteRenderer>().sprite).Contains("shell-fragment"),"Collectible still uses borrowed boss art.");
            var relay=rooms.First(r=>r.name.StartsWith("02 ")).GetComponent<StationRoomShape>();var gallery=rooms.First(r=>r.name.StartsWith("03 ")).GetComponent<StationRoomShape>();var shaft=rooms.First(r=>r.name.StartsWith("08 ")).GetComponent<StationRoomShape>();
            Require(relay.WorldBounds.max.y>13 && gallery.WorldBounds.max.y>37 && shaft.WorldBounds.max.y>28,"Atlas still clips vaults to camera/transition rectangles.");
            CheckMounts();
            var galleryLift=Object.FindObjectsByType<StationLift>(FindObjectsSortMode.None).First(l=>l.Id=="observatory-gallery" && !l.UnlockEnd);
            Require(Mathf.Abs(galleryLift.transform.position.x-40.5f)<.1f,"Gallery lift not relocated.");
            Require(!Physics2D.OverlapBoxAll((Vector2)galleryLift.transform.position+Vector2.up*.95f,new Vector2(1.5f,2.7f),0,1<<LayerMask.NameToLayer("Wall")).Any(),"Gallery passenger cabin intersects terrain.");
            lift=Object.FindObjectsByType<StationLift>(FindObjectsSortMode.None).First(l=>l.Id=="pump-relay" && l.UnlockEnd);
            Require(lift.Cabin && lift.Other.Cabin,"Lift passenger cabins missing.");body.position=lift.transform.position;player.transform.position=body.position;body.linearVelocity=Vector2.zero;Physics2D.SyncTransforms();Camera.main.GetComponent<CameraFollow>().SnapToPlayerRoom();
            cabinStart=lift.Cabin.position;Require(lift.TryUse(player),"Lift refused use.");stage=0;began=EditorApplication.timeSinceStartup;EditorApplication.update-=Tick;EditorApplication.update+=Tick;
        }catch(Exception e){Finish(e);}
    }
    static void Tick()
    {
        if(!EditorApplication.isPlaying){Finish(new Exception("Play stopped"));return;}
        try
        {
            double age=EditorApplication.timeSinceStartup-began;
            if(stage==0 && age>.55){Require(lift.IsRiding && Vector3.Distance(lift.Cabin.position,cabinStart)>.15f && !player.enabled && !body.simulated,"Cabin/player do not move together during departure.");stage=1;}
            if(stage==1 && age>1.7)
            {
                Require(!lift.IsRiding && body.simulated && player.enabled && Vector2.Distance(body.position,lift.Other.transform.position)<.5f,"Lift arrival did not restore physics/control.");
                Require(Vector3.Distance(lift.Cabin.position,cabinStart)<.01f,"Departure cabin did not return to its landing.");CheckMounts();Finish(null);
            }
        }catch(Exception e){Finish(e);}
    }
    static void Finish(Exception e)
    {
        EditorApplication.update-=Tick;string result=e==null?"VISUAL REFINEMENT PASSED: complete Relay/Gallery vaults and Service shaft map contours; dedicated shell fragment art; all active decor alpha contacts on physical floors/ceilings before and after pulse animation; six opaque non-solid rear walls with framed passage apertures; moving lift cabin and restored player control.":"VISUAL REFINEMENT FAILED: "+e;
        File.WriteAllText("StationRefinementCheck.txt",result);if(e==null)Debug.Log(result);else Debug.LogError(result);
    }
    static void Observatory()=>Place(new Vector2(38,43.5f));
    static void Pumpworks()=>Place(new Vector2(16,-35.5f));
    static void Edge()=>Place(new Vector2(-54.5f,20.5f));
    [MenuItem("Tools/Empty Heart/Просмотр/Отсек с фрагментом оболочки")]
    static void Fragment()=>Place(new Vector2(-102,22.5f));
    static void CheckMounts()
    {
        int count=0;Physics2D.SyncTransforms();
        foreach(var mount in Object.FindObjectsByType<StationDecorMount>(FindObjectsSortMode.None))
        {
            var sr=mount.GetComponent<SpriteRenderer>();if(!sr || !sr.enabled)continue;
            Vector2 dir=mount.Ceiling?Vector2.up:Vector2.down;Vector2 contact=mount.Contact;
            var hit=Physics2D.Raycast(contact-dir*.12f,dir,.26f,1<<LayerMask.NameToLayer("Wall"));
            Require(hit.collider && Mathf.Abs(hit.point.y-contact.y)<.04f,"Decoration not supported by actual collision: "+mount.name+" at "+contact);count++;
        }
        Require(count>=20,"Decor mount coverage missing: "+count);
    }
    static void Place(Vector2 p)
    {if(!EditorApplication.isPlaying)return;var player=Object.FindFirstObjectByType<PlayerMove>();var body=player.GetComponent<Rigidbody2D>();body.position=p;player.transform.position=p;body.linearVelocity=Vector2.zero;player.GetComponent<PlayerHealth>().GrantContactGrace(30);Physics2D.SyncTransforms();Camera.main.GetComponent<CameraFollow>().SnapToPlayerRoom();}
}
#endif
