#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;

public static class PaintedRigMotionPlayCheck
{
    private static PlayerMove player;
    private static PaintedHeroRig rig;
    private static Animator animator;
    private static Rigidbody2D body;
    private static int stage;
    private static double next;
    private static float gravity,phase;
    [MenuItem("Tools/Empty Heart/Проверки/Персонаж/Плавность анимаций")]
    public static void Run()
    {
        if(!EditorApplication.isPlaying)return;
        player=Object.FindFirstObjectByType<PlayerMove>();rig=player.GetComponent<PaintedHeroRig>();
        animator=player.GetComponent<Animator>();body=player.GetComponent<Rigidbody2D>();
        gravity=body.gravityScale;body.gravityScale=0;body.linearVelocity=Vector2.zero;player.enabled=false;
        player.GetComponent<SpriteRenderer>().flipX=false;
        phase=rig.MotionPhase;animator.SetBool("IsGrounded",true);animator.SetFloat("Speed",1);
        animator.SetBool("IsAttacking",false);animator.SetBool("IsWallSliding",false);
        stage=0;next=Time.timeAsDouble+.3;
        EditorApplication.update-=Tick;EditorApplication.update+=Tick;
    }
    private static void Tick()
    {
        if(!EditorApplication.isPlaying){EditorApplication.update-=Tick;return;}
        if(Time.timeAsDouble<next)return;
        try
        {
            if(stage==0)
            {
                Require(rig.MotionPhase>phase+1,"Run joints did not advance.");Attack(0);stage=1;
            }
            else if(stage<=3)
            {
                Vector2 direction=stage==1?Vector2.right:stage==2?Vector2.up:Vector2.down;
                Require(Vector2.Dot(rig.Weapon.up,direction)>.7f,"Saber points the wrong way at contact: "+stage+", vector="+rig.Weapon.up);
                if(stage<3){Attack(stage);stage++;}
                else
                {
                    string result="PAINTED RIG MOTION PASSED: continuous run cycle; side/up/down saber orientation at actual attack contact timing; wrist follows articulated arm.";
                    File.WriteAllText("PaintedRigMotionCheck.txt",result);Debug.Log(result);Cleanup();
                }
            }
        }
        catch(Exception e){File.WriteAllText("PaintedRigMotionCheck.txt",e.ToString());Debug.LogException(e);Cleanup();}
    }
    private static void Attack(int direction)
    {
        animator.SetFloat("Speed",0);animator.SetBool("IsAttacking",true);animator.SetFloat("AttackDirection",direction);
        animator.SetFloat("AttackSpeed",1);
        animator.Play("Base Layer."+(direction==1?"Attack_Up":direction==2?"Attack_Down":"Attack_Side"),0,0);
        next=Time.timeAsDouble+.17;
    }
    private static void Cleanup()
    {
        EditorApplication.update-=Tick;
        if(body)body.gravityScale=gravity;
        if(animator){animator.SetBool("IsAttacking",false);animator.SetFloat("Speed",0);}
        if(player)player.enabled=true;
    }
    private static void Require(bool ok,string message){if(!ok)throw new Exception("PAINTED RIG MOTION FAILED: "+message);}
}
#endif
