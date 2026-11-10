#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Object=UnityEngine.Object;
public static class TerminalPlayCheck
{
    static StationLoreNode node;static PlayerMove player;static Keyboard keyboard;static KeyboardState keys;static Mouse mouse;static MouseState buttons;static int step;static double next;static Vector2 position;static InputSettings.EditorInputBehaviorInPlayMode oldInput;
    [MenuItem("Tools/Empty Heart/Проверки/Персонаж/Окна терминалов")]
    static void Run()
    {
        if(!EditorApplication.isPlaying)return;
        node=Object.FindObjectsByType<StationLoreNode>(FindObjectsSortMode.None).First(n=>n.Title=="ТИШИНА НАД САДАМИ");player=Object.FindFirstObjectByType<PlayerMove>();
        var body=player.GetComponent<Rigidbody2D>();position=(Vector2)node.transform.position+new Vector2(-.8f,.5f);body.position=position;player.transform.position=position;body.linearVelocity=Vector2.zero;Physics2D.SyncTransforms();Camera.main.GetComponent<CameraFollow>().SnapToPlayerRoom();player.GetComponent<PlayerHealth>().GrantContactGrace(30);
        oldInput=InputSystem.settings.editorInputBehaviorInPlayMode;InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        keyboard=InputSystem.AddDevice<Keyboard>();mouse=InputSystem.AddDevice<Mouse>();keys=new KeyboardState(Key.E);buttons=new MouseState();InputSystem.onBeforeUpdate+=Feed;step=0;next=EditorApplication.timeSinceStartup+.3;EditorApplication.update+=Tick;
    }
    static void Feed(){InputSystem.QueueStateEvent(keyboard,keys);InputSystem.QueueStateEvent(mouse,buttons);}
    static void Need(bool b,string text){if(!b)throw new Exception(text);}
    static void Tick()
    {
        if(!EditorApplication.isPlaying){Finish(new Exception("Play stopped"));return;}if(EditorApplication.timeSinceStartup<next)return;
        try{
            if(step==0){Need(node.IsOpen && Time.timeScale==0,"E did not open and pause terminal");Need(node.ArchiveText.Length>500,"Expanded lore missing");keys=new KeyboardState();step=1;next=EditorApplication.timeSinceStartup+.15;}
            else if(step==1){keys=new KeyboardState(Key.E,Key.M,Key.Space,Key.LeftShift,Key.D);buttons=new MouseState().WithButton(MouseButton.Left);step=2;next=EditorApplication.timeSinceStartup+6;}
            else if(step==2){Need(node.IsOpen && Time.timeScale==0,"Terminal closed by timer/E/M");Need(!player.IsAttacking && !player.IsDashing && !Object.FindFirstObjectByType<StationAtlas>().IsOpen,"Gameplay leaked through modal");Need(Vector2.Distance(player.transform.position,position)<.1f,"Player moved while reading");keys=new KeyboardState(Key.Escape);buttons=new MouseState();step=3;next=EditorApplication.timeSinceStartup+.2;}
            else if(step==3){Need(!StationLoreNode.IsReading && Time.timeScale==1,"Esc did not close/resume");keys=new KeyboardState();step=4;next=EditorApplication.timeSinceStartup+.2;}
            else if(step==4){keys=new KeyboardState(Key.E);step=5;next=EditorApplication.timeSinceStartup+.2;}
            else if(step==5){Need(node.IsOpen,"Terminal cannot reopen");keys=new KeyboardState(Key.Escape);step=6;next=EditorApplication.timeSinceStartup+.2;}
            else{Need(!node.IsOpen && Time.timeScale==1,"Second close failed");Finish(null);}
        }catch(Exception e){Finish(e);}
    }
    static void Finish(Exception error)
    {
        EditorApplication.update-=Tick;InputSystem.onBeforeUpdate-=Feed;if(keyboard!=null)InputSystem.RemoveDevice(keyboard);if(mouse!=null)InputSystem.RemoveDevice(mouse);InputSystem.settings.editorInputBehaviorInPlayMode=oldInput;
        string text=error==null?"TERMINAL PASSED: real E opens; expanded lore; six seconds without timeout; E/M/jump/Dash/movement/LMB do not close window or trigger gameplay; Esc closes and restores time; reopening and second Esc work.":"TERMINAL FAILED: "+error;File.WriteAllText("TerminalCheck.txt",text);if(error==null)Debug.Log(text);else Debug.LogError(text);
    }
}
#endif
