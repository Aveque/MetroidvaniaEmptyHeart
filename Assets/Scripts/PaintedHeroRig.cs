using UnityEngine;

// Painted cutout animation: joints animate continuously while the existing
// Animator/PlayerMove remain the authority for movement and attack timing.
[DefaultExecutionOrder(600)]
public class PaintedHeroRig : MonoBehaviour
{
    public Sprite[] parts;
    private PlayerMove move;
    private PlayerHealth health;
    private Animator state;
    private SpriteRenderer source;
    private Transform skin,spine,head,scarf,tail;
    private Limb arm,backArm,leftLeg,rightLeg;
    private Transform weapon;
    private SpriteRenderer[] renderers;
    private float cycle,deathStart,hurtUntil;
    private int previousHealth;
    private bool deadBefore;
    private PlayerRest rest;
    private float restBlend;
    private Vector2 hand=new Vector2(.105f,-.015f);
    private float swordAngle=-115;
    public float MotionPhase=>cycle;
    public Transform Weapon=>weapon;
    private class Limb {public Transform upper,lower,end;public Vector2 root;public float a,b;}

    private void Awake()
    {
        move=GetComponent<PlayerMove>();health=GetComponent<PlayerHealth>();state=GetComponent<Animator>();source=GetComponent<SpriteRenderer>();
        previousHealth=health.CurrentHealth;
        rest=GetComponent<PlayerRest>();
        Build();source.enabled=false;
    }
    private Transform Part(string name,Transform parent,int index,float height,Vector2 pivot,int order,Vector2 at)
    {
        Transform t=new GameObject(name).transform;t.SetParent(parent,false);t.localPosition=at;
        SpriteRenderer r=t.gameObject.AddComponent<SpriteRenderer>();r.sprite=parts[index];r.sortingOrder=order;
        r.sharedMaterial=source.sharedMaterial;
        // Sprite pivots are authored in the importer; each part has its own
        // native-resolution cutout and local height independent of its canvas.
        float scale=height/r.sprite.bounds.size.y;t.localScale=Vector3.one*scale;
        return t;
    }
    private Transform Bone(string name,Transform parent,Vector2 at)
    {Transform t=new GameObject(name).transform;t.SetParent(parent,false);t.localPosition=at;return t;}
    private void Build()
    {
        if(parts==null || parts.Length<11)return;
        skin=Bone("Painted Hero — animated skeleton",transform,Vector2.zero);
        spine=Bone("Spine",skin,Vector2.zero);
        tail=Part("Coat tail",spine,10,.19f,new Vector2(.5f,1),1,new Vector2(-.025f,-.025f));
        leftLeg=MakeLimb("Rear leg",skin,new Vector2(-.035f,-.045f),5,6,.105f,.112f,1,true);
        rightLeg=MakeLimb("Front leg",skin,new Vector2(.035f,-.045f),5,6,.105f,.112f,3,true);
        backArm=MakeLimb("Rear arm",spine,new Vector2(-.015f,.16f),3,4,.112f,.112f,1,false);
        Part("Pelvis",spine,2,.08f,new Vector2(.5f,.5f),3,new Vector2(0,-.035f));
        Part("Torso",spine,1,.21f,new Vector2(.5f,0),4,Vector2.zero);
        scarf=Part("Scarf",spine,9,.075f,new Vector2(1,.5f),5,new Vector2(-.015f,.205f));
        head=Part("Ivory mask",spine,0,.13f,new Vector2(.5f,0),5,new Vector2(.007f,.20f));
        arm=MakeLimb("Sword arm",spine,new Vector2(.025f,.17f),3,4,.112f,.112f,6,false);
        weapon=Part("Saber — follows hand",arm.end,8,.37f,new Vector2(.5f,0),7,Vector2.zero);
        renderers=skin.GetComponentsInChildren<SpriteRenderer>();
    }
    private Limb MakeLimb(string name,Transform parent,Vector2 root,int upperIndex,int lowerIndex,float a,float b,int order,bool boot)
    {
        Limb limb=new Limb{root=root,a=a,b=b};
        limb.upper=Bone(name,parent,root);Part("Upper",limb.upper,upperIndex,a*1.18f,Vector2.up,order,Vector2.zero);
        limb.lower=Bone("Joint",limb.upper,new Vector2(0,-a));Part("Lower",limb.lower,lowerIndex,b*1.15f,Vector2.up,order,Vector2.zero);
        limb.end=Bone("End",limb.lower,new Vector2(0,-b));
        if(boot)Part("Boot",limb.end,7,.055f,Vector2.up,order,new Vector2(0,.018f));
        return limb;
    }
    private void Pose(Limb limb,Vector2 target,float bend,bool flatEnd=false)
    {
        Vector2 d=target-limb.root;float distance=Mathf.Clamp(d.magnitude,.015f,limb.a+limb.b-.001f);
        float angle=Mathf.Atan2(d.y,d.x),offset=Mathf.Acos(Mathf.Clamp((limb.a*limb.a+distance*distance-limb.b*limb.b)/(2*limb.a*distance),-1,1));
        float upper=angle+bend*offset;
        Vector2 elbow=limb.root+new Vector2(Mathf.Cos(upper),Mathf.Sin(upper))*limb.a;
        float lower=Mathf.Atan2(target.y-elbow.y,target.x-elbow.x);
        limb.upper.localRotation=Quaternion.Euler(0,0,upper*Mathf.Rad2Deg+90);
        limb.lower.localRotation=Quaternion.Euler(0,0,(lower-upper)*Mathf.Rad2Deg);
        if(flatEnd)limb.end.localRotation=Quaternion.Euler(0,0,-lower*Mathf.Rad2Deg-90);
    }
    private void LateUpdate()
    {
        if(!skin)return;
        source.enabled=false;
        bool dead=health.IsRespawning && health.CurrentHealth<=0;
        if(dead && !deadBefore)deathStart=Time.time;
        if(health.CurrentHealth<previousHealth)hurtUntil=Time.time+.12f;
        previousHealth=health.CurrentHealth;deadBefore=dead;
        float facing=source.flipX?-1:1;
        skin.localScale=new Vector3(facing,1,1);
        float speed=state.GetFloat("Speed"),vy=GetComponent<Rigidbody2D>().linearVelocity.y;
        bool grounded=state.GetBool("IsGrounded"),attack=state.GetBool("IsAttacking");
        if(grounded && speed>.1f)cycle+=Time.deltaTime*13.5f;
        float breathe=Mathf.Sin(Time.time*3),walk=Mathf.Sin(cycle),bob=grounded && speed>.1f?Mathf.Abs(walk)*.012f:breathe*.002f;
        Vector2 left=new Vector2(-.035f,-.235f),right=new Vector2(.035f,-.235f);
        Vector2 desiredHand=new Vector2(.105f,-.015f);float desiredSword=-115,lean=0;
        if(grounded && speed>.1f)
        {
            left+=new Vector2(walk*.075f,Mathf.Max(0,Mathf.Cos(cycle))*.055f);
            right+=new Vector2(-walk*.075f,Mathf.Max(0,-Mathf.Cos(cycle))*.055f);
            desiredHand=new Vector2(.07f-walk*.045f,.01f);desiredSword=120+walk*12;lean=-5;
        }
        if(!grounded)
        {
            bool rising=vy>0;
            left=new Vector2(-.04f,rising?-.12f:-.22f);right=new Vector2(.07f,rising?-.17f:-.19f);
            desiredHand=new Vector2(.115f,.055f);desiredSword=-130;lean=rising?-7:3;
        }
        if(move.IsWallSliding)
        {
            left=new Vector2(.09f,-.14f);right=new Vector2(.13f,-.22f);
            desiredHand=new Vector2(.145f,.24f);desiredSword=-15;lean=5;
        }
        if(move.IsDashing)
        {
            left=new Vector2(-.12f,-.12f);right=new Vector2(.10f,-.15f);
            desiredHand=new Vector2(-.04f,.09f);desiredSword=160;lean=-35;
        }
        if(!attack && move.ComboWindowActive && grounded && speed<.1f)
        {
            SideFollowThrough(move.ComboStep,out Vector2 ready,out float blade);
            desiredHand=Vector2.Lerp(ready,desiredHand,move.ComboRestBlend);
            desiredSword=Mathf.LerpAngle(blade,desiredSword,move.ComboRestBlend);
        }
        if(attack)
        {
            float p=move.IsAttacking ? move.AttackProgress : state.GetCurrentAnimatorStateInfo(0).normalizedTime%1;
            // Contact happens at .12 s of the .35 s attack. The fast cut crosses
            // that point, followed by a visible recovery to the resting stance.
            float cut=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.18f,.38f,p));
            float recover=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.65f,1,p));
            int direction=Mathf.RoundToInt(state.GetFloat("AttackDirection"));
            Vector2 windup,contact;float from,to;
            if(direction==1){windup=new Vector2(.09f,.015f);contact=new Vector2(.065f,.34f);from=110;to=-5;lean=8;}
            else if(direction==2){windup=new Vector2(.04f,.31f);contact=new Vector2(.10f,grounded ? .14f : -.005f);from=-10;to=-180;lean=-8;}
            else
            {
                int step=move.ComboStep;
                windup=step==1?new Vector2(.10f,.015f):step==2?new Vector2(-.075f,.15f):new Vector2(-.10f,.22f);
                contact=new Vector2(step==2?.245f:.23f,step==1?.16f:.14f);
                from=step==1?-150:35;to=-90;
                SideFollowThrough(step,out Vector2 end,out float endAngle);
                if(step==1)endAngle=35;
                float strike=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.12f,.34f,p));
                float follow=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.4f,.76f,p));
                desiredHand=Vector2.Lerp(Vector2.Lerp(windup,contact,strike),end,follow);
                // Use an unwrapped arc so the reverse cut swings upwards rather
                // than taking the short path back across the previous strike.
                desiredSword=Mathf.Lerp(Mathf.Lerp(from,to,strike),endAngle,follow);
                if(step==1)
                {
                    // Turn the wrist over for the rising cut: the single edge
                    // must lead the motion, not the back of the saber.
                    float chamber=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.76f,1,p));
                    desiredSword=Mathf.Lerp(desiredSword,-90,chamber);
                }
                if(step==2)
                {
                    // A thrust translates a level blade. No overhead windup
                    // or cutting arc; extend, briefly hold, then pull straight back.
                    float retract=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.64f,.95f,p));
                    desiredHand=Vector2.Lerp(Vector2.Lerp(windup,new Vector2(.245f,.15f),strike),windup,retract);
                    desiredSword=-90;
                }
                lean=step==1?Mathf.Lerp(5,-3,strike):step==2?-14*strike:-10*strike;
                if(grounded){left.x-=.015f*strike;right.x+=.025f*strike;}
                if(step==2){desiredSword-=lean;if(grounded)right.x+=.035f*strike;}
            }
            if(direction!=0)
            {
                desiredHand=Vector2.Lerp(Vector2.Lerp(windup,contact,cut),new Vector2(.105f,-.015f),recover);
                desiredSword=Mathf.LerpAngle(Mathf.LerpAngle(from,to,cut),-115,recover);
            }
        }
        if(Time.time<hurtUntil && !dead)lean=12;
        float restTarget=0;
        // Settle into a kneeling pose, hold it, then rise during the final quarter.
        if(rest && rest.IsResting && !dead) restTarget=Mathf.SmoothStep(0,1,Mathf.InverseLerp(0,.2f,rest.Progress))*(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.75f,1,rest.Progress)));
        if(PlayerOrgans.IsChanneling && !dead){restTarget=.8f;desiredHand=new Vector2(.14f,.13f);desiredSword=-160;}
        restBlend=Mathf.MoveTowards(restBlend,restTarget,Time.deltaTime*5);
        left=Vector2.Lerp(left,new Vector2(-.085f,-.155f),restBlend);
        right=Vector2.Lerp(right,new Vector2(.10f,-.155f),restBlend);
        desiredHand=Vector2.Lerp(desiredHand,new Vector2(.09f,.10f),restBlend);
        desiredSword=Mathf.LerpAngle(desiredSword,-160,restBlend);
        lean=Mathf.Lerp(lean,-12,restBlend);
        float death=dead?Mathf.Clamp01((Time.time-deathStart)/.65f):0;
        skin.localPosition=new Vector3(0,dead?-.16f*death:-.08f*restBlend,0);
        skin.localRotation=Quaternion.Euler(0,0,dead?-90*death:0);
        spine.localPosition=new Vector3(0,bob,0);spine.localRotation=Quaternion.Euler(0,0,lean*(1-death));
        head.localRotation=Quaternion.Euler(0,0,-lean*.4f+breathe-16*restBlend);
        scarf.localRotation=Quaternion.Euler(0,0,breathe*4+(speed>.1f?12:0)+(move.IsDashing?20:0));
        tail.localRotation=Quaternion.Euler(0,0,walk*(speed>.1f?12:2)+breathe*2);
        hand=Vector2.Lerp(hand,desiredHand,1-Mathf.Exp(-35*Time.deltaTime));
        swordAngle=Mathf.LerpAngle(swordAngle,desiredSword,1-Mathf.Exp(-35*Time.deltaTime));
        Pose(arm,hand,-1);Pose(backArm,Vector2.Lerp(move.IsWallSliding?new Vector2(.14f,.29f):new Vector2(-.06f-walk*speed*.04f,.01f),new Vector2(.06f,.11f),restBlend),1);
        Pose(leftLeg,left,-1,true);Pose(rightLeg,right,-1,true);
        float handAngle=arm.upper.localEulerAngles.z+arm.lower.localEulerAngles.z;
        weapon.localRotation=Quaternion.Euler(0,0,swordAngle-handAngle);
        // Keep the turned wrist through the thrust chamber, then restore the
        // original blade presentation before the next opening slash.
        weapon.GetComponent<SpriteRenderer>().flipX=(attack || move.ComboWindowActive) && move.ComboStep>0
            && Mathf.RoundToInt(state.GetFloat("AttackDirection"))==0;
        Color tint=dead?Color.white:source.color;
        foreach(SpriteRenderer r in renderers)r.color=tint;
    }
    private static void SideFollowThrough(int step,out Vector2 hand,out float angle)
    {
        hand=step==0?new Vector2(.10f,.015f):step==1?new Vector2(-.075f,.15f):new Vector2(.105f,-.015f);
        angle=step==0?-150:step==1?-90:-115;
    }
}
