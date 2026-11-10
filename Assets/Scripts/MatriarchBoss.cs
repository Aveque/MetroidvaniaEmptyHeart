using System.Collections;
using UnityEngine;
[RequireComponent(typeof(BoxCollider2D))]public sealed class MatriarchBoss:MonoBehaviour,IDamageable
{
 public static MatriarchBoss Instance;public static bool AnyCombat=>Instance&&Instance.InCombat;
 public RelayMechanicalGate[] Gates;public CameraRoom Room;public GameObject Reward;public Sprite Plant,Vine,Seed,Petal;public AudioClip AttackSound;
 public int MaxHealth=48;public int Health{get;private set;}public bool InCombat{get;private set;}public bool Defeated{get;private set;}public string Pose{get;private set;}="Dormant";public const float VisualHeight=9.4f;public int Phase=>secondPhase?2:1;public float Mutation{get;private set;}public Bounds ContactBounds=>new Bounds(transform.position+Vector3.up*2.5f,new Vector3(1.1f,5,2));public bool CanTakeDamage=>InCombat&&Pose!="Arrival"&&Pose!="Death"&&Pose!="Molting";
 public MatriarchAttackFX Effects{get;private set;}
 public float Motion{get;private set;}public float Crown{get;private set;}public int SweepSide{get;private set;}=1;
 Sprite generatedSeed;bool secondPhase;PlayerHealth player;Collider2D playerBody;BoxCollider2D hurtbox;MatriarchRig rig;AudioSource audioSource;Coroutine routine;float hitUntil,delayedHp,readyAt,victory;
 void Awake(){var preview=transform.Find("Рисунок босса — предпросмотр");if(preview)preview.gameObject.SetActive(false);Instance=this;Health=MaxHealth;hurtbox=GetComponent<BoxCollider2D>();hurtbox.isTrigger=true;hurtbox.size=new Vector2(4.6f,5.6f);hurtbox.offset=new Vector2(0,2.8f);Effects=gameObject.AddComponent<MatriarchAttackFX>();Effects.Initialize(this);rig=gameObject.AddComponent<MatriarchRig>();rig.Owner=this;audioSource=gameObject.AddComponent<AudioSource>();audioSource.volume=.45f;}
 void Start(){var seedRect=Plant.rect;seedRect.x+=seedRect.width*.22f;seedRect.y+=seedRect.height*.63f;seedRect.width*=.56f;seedRect.height*=.34f;generatedSeed=Seed=Sprite.Create(Plant.texture,seedRect,new Vector2(.5f,.5f),Plant.pixelsPerUnit,0,SpriteMeshType.FullRect);if(Reward)Reward.SetActive(false);if(StationJourney.Opened.Contains("matriarch-defeated")){Defeated=true;Pose="Defeated";Crown=1;hurtbox.enabled=false;if(Reward)Reward.SetActive(true);}}
 public bool TryBegin(PlayerMove p){if(InCombat||Defeated||GardenCircuit.Count!=2||!p||!p.isActiveAndEnabled||!VascularResonance.InputAllowed||(p.GetComponent<PlayerRest>()?.IsResting??false)||p.GetComponent<PlayerHealth>().IsRespawning||Time.time<readyAt||p.transform.position.x<99||p.transform.position.x>131||!Room||!Room.Contains(p.transform.position))return false;player=p.GetComponent<PlayerHealth>();playerBody=p.GetComponent<Collider2D>();player.Died+=ResetEncounter;InCombat=true;Health=MaxHealth;delayedHp=Health;foreach(var g in Gates)g.BattleLocked=true;Camera.main.GetComponent<CameraFollow>().SetRoom(Room,true);StationMusic.SetBossBattle(true);routine=StartCoroutine(Fight());return true;}
 void Update(){if(!InCombat&&!Defeated){var p=FindFirstObjectByType<PlayerMove>();if(p)TryBegin(p);}delayedHp=Mathf.MoveTowards(delayedHp,Health,Time.deltaTime*25);}
 IEnumerator Animate(string pose,float duration,float crown){Pose=pose;float from=Crown;for(float t=0;t<duration;t+=Time.deltaTime){Motion=t/duration;if(pose=="Molting")Mutation=Mathf.SmoothStep(0,1,Motion);Crown=Mathf.Lerp(from,crown,Mathf.SmoothStep(0,1,Motion));yield return null;}Motion=1;Crown=crown;}
 IEnumerator Fight(){yield return Animate("Arrival",2.2f,.65f);yield return Attacks();}
 IEnumerator TransformPhase()
 {
  ClearHazards();secondPhase=true;Sound();
  yield return Animate("Molting",2.1f,1);
  Mutation=1;yield return Animate("EnragedRoar",.8f,1);yield return Attacks();
 }
 IEnumerator Attacks()
 {
  int attack=0;
  while(InCombat)
  {
   yield return Animate("Recover",Phase==2?.95f:1.35f,Phase==2?.65f:.2f);
   int choice=attack++%(Phase==2?7:5);
   if(choice==0||choice==5)
   {
    SweepSide=choice==5?0:player.transform.position.x<transform.position.x?-1:1;
    yield return Animate(choice==5?"PincerPrepare":"VinePrepare",Phase==2?.8f:1.05f,.35f);Sound();
    yield return Animate(choice==5?"PincerSweep":"VineSweep",.75f,.7f);
    if(choice==5){yield return Animate("PincerRecoil",.35f,.5f);Sound();yield return Animate("PincerSweep",.7f,1);}
   }
   else if(choice==1)
   {
    Sound();for(int j=-1;j<=1;j++)SpawnRoot(Mathf.Clamp(player.transform.position.x+j*5,100,130),Phase==2?.85f:1.05f);
    yield return Animate("RootPrepare",1.7f,.8f);
   }
   else if(choice==2)
   {
    yield return Animate("SeedPrepare",.95f,1);Sound();int shots=Phase==2?7:5;
    Vector2 origin=(Vector2)transform.position+Vector2.up*7.1f;
    Vector2 aim=((Vector2)player.transform.position-origin).normalized;
    Pose="SeedVolley";for(int i=0;i<shots;i++){Vector2 dir=Quaternion.Euler(0,0,(i-(shots-1)/2f)*12)*aim;MatriarchHazard.Projectile(this,Seed,origin,dir*(Phase==2?10:8));yield return new WaitForSeconds(.1f);}
    yield return Animate("SeedRecover",.7f,.4f);
   }
   else if(choice==3)
   {
    yield return Animate("RainPrepare",1.15f,1);Sound();
    // Alternating columns leave broad ground lanes between the falling seeds.
    for(int i=0;i<7;i++)MatriarchHazard.Projectile(this,Seed,new Vector2(101+i*4.6f,6.8f),Vector2.down*2.5f);
    yield return Animate("SeedRain",1.35f,.6f);
   }
   else if(choice==4)
   {
    SweepSide=player.transform.position.x<transform.position.x?-1:1;Sound();
    for(int i=0;i<5;i++)SpawnRoot(Mathf.Clamp(transform.position.x+SweepSide*(3+i*2.8f),100,130),1+i*.28f);
    yield return Animate("RootMarch",2.5f,.9f);
   }
   else
   {
    yield return Animate("SpiralPrepare",1.15f,1);Sound();
    Vector2 origin=(Vector2)transform.position+Vector2.up*6.7f;
    for(int ring=0;ring<3;ring++){for(int i=0;i<7;i++){float a=(205+i*21+ring*8)*Mathf.Deg2Rad;MatriarchHazard.Projectile(this,Seed,origin,new Vector2(Mathf.Cos(a),Mathf.Sin(a))*7.5f);}yield return Animate("SpiralBurst",.45f,1);}
   }
  }
 }
 void Sound(){if(AttackSound)audioSource.PlayOneShot(AttackSound);}
 public void SpawnRoot(float x,float warning){MatriarchHazard.Root(this,Vine,new Vector2(x,-3),warning);}
 public void HurtPlayer(Vector2 point){if(!InCombat||!player||player.IsRespawning)return;var p=player.GetComponent<PlayerMove>();if(p.IsDashing)return;player.TakeContactDamage(1,point);}
 void OnTriggerStay2D(Collider2D c){if(CanTakeDamage&&c.GetComponent<PlayerMove>()&&ContactBounds.Intersects(c.bounds))HurtPlayer(transform.position);}
 public void TakeDamage(int amount){if(!CanTakeDamage||amount<=0||Time.time<hitUntil)return;hitUntil=Time.time+.09f;Health=Mathf.Max(0,Health-amount);rig.Flash();if(Health==0){if(routine!=null)StopCoroutine(routine);routine=StartCoroutine(Die());}else if(!secondPhase&&Health<=MaxHealth/2){if(routine!=null)StopCoroutine(routine);routine=StartCoroutine(TransformPhase());}}
 IEnumerator Die(){Pose="Death";hurtbox.enabled=false;ClearHazards();yield return Animate("Death",2.4f,1);Defeated=true;InCombat=false;Pose="Defeated";StationJourney.Opened.Add("matriarch-defeated");foreach(var g in Gates)g.BattleLocked=false;if(player)player.Died-=ResetEncounter;StationMusic.SetBossBattle(false);if(Reward)Reward.SetActive(true);victory=Time.time+4;}
 public void ResetEncounter(){if(routine!=null)StopCoroutine(routine);ClearHazards();if(player)player.Died-=ResetEncounter;InCombat=false;Health=MaxHealth;secondPhase=false;Mutation=0;Pose="Dormant";Crown=Motion=0;hurtbox.enabled=true;foreach(var g in Gates)g.BattleLocked=false;StationMusic.SetBossBattle(false);readyAt=Time.time+2;}
 void ClearHazards(){if(Effects)Effects.Clear();foreach(var h in FindObjectsByType<MatriarchHazard>(FindObjectsSortMode.None))if(h.Owner==this)Destroy(h.gameObject);}
 void OnDestroy(){if(generatedSeed)Destroy(generatedSeed);if(player)player.Died-=ResetEncounter;if(Instance==this)Instance=null;if(InCombat)StationMusic.SetBossBattle(false);}
 void OnGUI(){if(InCombat){using(EmptyHeartUI.Begin()){float w=Mathf.Min(760,EmptyHeartUI.Width-120),x=(EmptyHeartUI.Width-w)/2,y=EmptyHeartUI.Height-46;EmptyHeartUI.Panel(new Rect(x-16,y-30,w+32,72),EmptyHeartUI.Copper);EmptyHeartUI.Text(new Rect(x,y-27,w,24),"МАТЕРИНСКИЙ ЦВЕТОК / "+(Phase==2?"ПРОБУЖДЁННАЯ СЕРДЦЕВИНА":"ХРАНИТЕЛЬНИЦА ПРИВИВОК"),16,EmptyHeartUI.Paper,true,TextAnchor.MiddleCenter);EmptyHeartUI.Fill(new Rect(x,y+8,w,12),EmptyHeartUI.Ink);EmptyHeartUI.Fill(new Rect(x,y+8,w*delayedHp/MaxHealth,12),EmptyHeartUI.Copper);EmptyHeartUI.Fill(new Rect(x,y+8,w*Health/MaxHealth,12),EmptyHeartUI.Jade);}}if(Time.time<victory)EmptyHeartUI.Announcement("СОСУДИСТЫЙ КОНТУР ОСВОБОЖДЁН","МАТЕРИНСКИЙ ЦВЕТОК УСНУЛ","Затворы открыты. В сердцевине осталось живое семя.",1);}
}
