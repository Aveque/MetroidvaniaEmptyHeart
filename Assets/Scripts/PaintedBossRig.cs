using UnityEngine;

// The combat controller owns time, facing, damage and collision. This child-only
// puppet evaluates continuous joint curves every rendered frame, including the
// transitions between attacks. No fading between complete character drawings.
[DefaultExecutionOrder(650)]
public sealed class PaintedBossRig : MonoBehaviour
{
    public Sprite[] parts;
    private BiomechBoss boss;
    private SpriteRenderer source;
    private Transform skin, torso, head, neck, abdomen, lens;
    private Limb frontArm, rearArm;
    private Limb[] legs;
    private SpriteRenderer[] visuals;
    private readonly Material[] partMaterials = new Material[12];
    private Vector2 frontTip = new Vector2(-1.45f, -.8f), rearTip = new Vector2(-.9f, -.5f);
    private float bodyY = .55f, lean, headAngle, cycle;
    private static readonly float[] FootX = {1.35f, -.8f, 1.75f, -1.45f};
    public static Vector2 JointOrigin(int i)
    {
        if (i == 4) return new Vector2(.254f, .887f);
        if (i == 5) return new Vector2(.835f, .876f);
        if (i == 6) return new Vector2(.15f, .875f);
        if (i == 7) return new Vector2(.181f, .877f);
        if (i == 8) return new Vector2(.8f, .856f);
        if (i == 9) return new Vector2(.185f, .86f);
        if (i == 10) return new Vector2(.19f, .85f);
        if (i == 1) return new Vector2(.5f, .08f);
        if (i == 2) return new Vector2(.7f, .08f);
        return new Vector2(.5f, .5f);
    }
    private static Vector2 JointEnd(int i)
    {
        if (i == 4) return new Vector2(.829f, .134f);
        if (i == 5) return new Vector2(.058f, .037f);
        if (i == 6) return new Vector2(.793f, .128f);
        if (i == 7) return new Vector2(.858f, .123f);
        if (i == 8) return new Vector2(.03f, .067f);
        if (i == 9) return new Vector2(.844f, .125f);
        return new Vector2(.86f, .128f);
    }
    public Transform FrontBlade => frontArm?.lower;
    public Transform RearBlade => rearArm?.lower;
    public Transform Body => torso;
    public bool Ready => skin && visuals != null && visuals.Length >= 20;
    private SpriteRenderer sweep;
    private Material sweepMaterial;
    private sealed class Limb { public Transform upper, lower, end; public Vector2 root; public float a, b; }

    private void Awake()
    {
        boss = GetComponent<BiomechBoss>(); source = GetComponent<SpriteRenderer>();
        if (parts == null || parts.Length != 12) return;
        skin = Bone("Gardener — continuous painted puppet", transform, Vector2.zero);
        torso = Bone("Body motion", skin, new Vector2(0, bodyY));
        legs = new Limb[4];
        legs[0] = MakeLimb("Far rear leg", torso, new Vector2(.65f, -.2f), 9, 10, .8f, .88f, 0, true);
        legs[1] = MakeLimb("Far front leg", torso, new Vector2(-.35f, -.3f), 6, 7, .8f, .88f, 0, true);
        rearArm = MakeLimb("Far sickle", torso, new Vector2(-.02f, .65f), 4, 5, .78f, 1.35f, 1, false);
        abdomen = Part("Abdominal armour", torso, 3, .85f, 1, new Vector2(.65f, -.25f));
        Part("Carapace and reactor", torso, 0, 1.9f, 2, new Vector2(.2f, .15f));
        neck = Part("Flexible tendon neck", torso, 1, .85f, 3, new Vector2(-.45f, .4f));
        head = Part("Horned head", torso, 2, .7f, 4, new Vector2(-.82f, .98f));
        lens = Part("Pulsing reactor lens", torso, 11, .29f, 4, new Vector2(-.12f, .38f));
        legs[2] = MakeLimb("Near rear leg", torso, new Vector2(.6f, -.32f), 9, 10, .85f, .9f, 4, true);
        legs[3] = MakeLimb("Near front leg", torso, new Vector2(-.35f, -.4f), 6, 7, .85f, .9f, 5, true);
        frontArm = MakeLimb("Near sickle", torso, new Vector2(-.55f, .55f), 4, 5, .85f, 1.45f, 6, false);
        visuals = skin.GetComponentsInChildren<SpriteRenderer>();
        if(boss.AttackEffects!=null && boss.AttackEffects.Length==4)
        {
            Transform trail=Bone("Painted sickle sweep",skin,Vector2.zero);
            sweep=trail.gameObject.AddComponent<SpriteRenderer>();sweep.sprite=boss.AttackEffects[3];sweep.sortingOrder=8;
            sweepMaterial=new Material(source.sharedMaterial){hideFlags=HideFlags.DontSave,name="Sickle brush trail"};
            sweepMaterial.mainTexture=sweep.sprite.texture;sweep.sharedMaterial=sweepMaterial;
        }
        source.enabled = false;
        LateUpdate();
    }
    private static Transform Bone(string label, Transform parent, Vector2 at)
    {
        Transform t = new GameObject(label).transform; t.SetParent(parent, false); t.localPosition = at; return t;
    }
    private Transform Part(string label, Transform parent, int index, float height, int order, Vector2 at)
    {
        Transform t = Bone(label, parent, at);
        SpriteRenderer r = t.gameObject.AddComponent<SpriteRenderer>(); r.sprite = parts[index];
        // Keep one material per cutout texture. Sharing the global sprite material
        // between many independent textures produced incorrect batched draws in
        // this project's DX12 renderer, including other characters on screen.
        if (!partMaterials[index])
        {
            partMaterials[index] = new Material(source.sharedMaterial) { name = "Gardener cutout " + index, hideFlags = HideFlags.DontSave };
            partMaterials[index].mainTexture = parts[index].texture;
        }
        r.sharedMaterial = partMaterials[index]; r.sortingLayerID = source.sortingLayerID; r.sortingOrder = order;
        float contentHeight = r.sprite.bounds.size.y - 48f / r.sprite.pixelsPerUnit;
        t.localScale = Vector3.one * (height / contentHeight); return t;
    }
    private Transform Segment(string label, Transform parent, int index, float length, int order)
    {
        Transform t = Part(label, parent, index, length, order, Vector2.zero);
        Vector2 dimensions = parts[index].bounds.size - Vector3.one * (48f / parts[index].pixelsPerUnit);
        Vector2 delta = Vector2.Scale(JointEnd(index) - JointOrigin(index), dimensions);
        t.localRotation = Quaternion.Euler(0, 0, -90f - Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
        t.localScale = Vector3.one * (length / delta.magnitude); return t;
    }
    private Limb MakeLimb(string label, Transform parent, Vector2 joint, int upper, int lower, float a, float b, int order, bool claw)
    {
        Limb limb = new Limb { root = joint, a = a, b = b };
        limb.upper = Bone(label, parent, joint);
        Segment("Upper segment", limb.upper, upper, a, order);
        limb.lower = Bone("Joint", limb.upper, new Vector2(0, -a));
        Segment("Lower segment", limb.lower, lower, b, order);
        limb.end = Bone("Tip", limb.lower, new Vector2(0, -b));
        if (claw) Segment("Claw", limb.end, 8, .18f, order);
        return limb;
    }
    private static void Pose(Limb limb, Vector2 target, float bend, bool planted)
    {
        Vector2 d = target - limb.root;
        float distance = Mathf.Clamp(d.magnitude, Mathf.Abs(limb.a - limb.b) + .025f, limb.a + limb.b - .025f);
        float angle = Mathf.Atan2(d.y, d.x);
        float offset = Mathf.Acos(Mathf.Clamp((limb.a * limb.a + distance * distance - limb.b * limb.b) / (2 * limb.a * distance), -1, 1));
        float upper = angle + bend * offset;
        Vector2 elbow = limb.root + new Vector2(Mathf.Cos(upper), Mathf.Sin(upper)) * limb.a;
        float lower = Mathf.Atan2(target.y - elbow.y, target.x - elbow.x);
        limb.upper.localRotation = Quaternion.Euler(0, 0, upper * Mathf.Rad2Deg + 90);
        limb.lower.localRotation = Quaternion.Euler(0, 0, (lower - upper) * Mathf.Rad2Deg);
        if (planted) limb.end.localRotation = Quaternion.Euler(0, 0, -lower * Mathf.Rad2Deg - 90);
    }
    private static float Ease(float p) => Mathf.SmoothStep(0, 1, Mathf.Clamp01(p));
    private void LateUpdate()
    {
        if (!skin) return;
        source.enabled = false;
        skin.localScale = new Vector3(source.flipX ? -1 : 1, 1, 1);
        float p = boss.StateProgress, age = boss.StateAge;
        var state = boss.CurrentState; var attack = boss.ActiveAttack;
        float breathe = Mathf.Sin(Time.time * 2.6f);
        float wantedY = 1.45f + breathe * .025f, wantedLean = breathe * .5f;
        float wantedHead = -breathe * 1.2f;
        Vector2 idleFront = new Vector2(-1.45f, -.8f), idleRear = new Vector2(-.9f, -.5f);
        Vector2 front = idleFront, rear = idleRear;
        float stride = 0, corePower = .5f;
        if (state == BiomechBoss.State.Dormant || state == BiomechBoss.State.Intro)
        {
            float rise = state == BiomechBoss.State.Dormant ? 0 : Ease(p);
            wantedY = Mathf.Lerp(.55f, 1.45f, rise); wantedLean = Mathf.Lerp(14, 0, rise);
            wantedHead = Mathf.Lerp(-22, 0, rise);
            front = Vector2.Lerp(new Vector2(-1.7f, -.3f), idleFront, rise);
            rear = Vector2.Lerp(new Vector2(-1.2f, -.2f), idleRear, rise); corePower = rise;
        }
        if (state == BiomechBoss.State.Telegraph)
        {
            float wind = Ease(p / .8f);
            if (attack == BiomechBoss.Attack.Energy)
            {
                front = Vector2.Lerp(idleFront, new Vector2(-.45f, .25f), wind);
                rear = Vector2.Lerp(idleRear, new Vector2(-.1f, .55f), wind);
                wantedY += wind * .1f; wantedLean = -5 * wind; wantedHead = 5 * wind; corePower = .5f + p * 2;
            }
            else if (attack == BiomechBoss.Attack.Sickle)
            {
                front = Vector2.Lerp(idleFront, new Vector2(.55f, .65f), wind);
                rear = Vector2.Lerp(idleRear, new Vector2(.7f, .8f), wind);
                wantedY -= .25f * wind; wantedLean = -10 * wind; wantedHead = 10 * wind;
            }
            else
            {
                float strike = Ease(Mathf.InverseLerp(.82f, 1f, p));
                front = Vector2.Lerp(Vector2.Lerp(idleFront, new Vector2(-.75f, 2.25f), wind), new Vector2(-1.15f, -1.25f), strike);
                rear = Vector2.Lerp(Vector2.Lerp(idleRear, new Vector2(.25f, 2.05f), wind), new Vector2(-.35f, -1.15f), strike);
                wantedY += .12f * wind - .3f * strike; wantedLean = -7 * wind + 18 * strike;
                wantedHead = 10 * wind - 18 * strike;
            }
        }
        if (state == BiomechBoss.State.Pulse)
        {
            float cast = Ease(age / .14f), recover = Ease(Mathf.InverseLerp(.65f, 1.35f, age));
            front = Vector2.Lerp(Vector2.Lerp(new Vector2(-.45f, .25f), new Vector2(-2.45f, .35f), cast), idleFront, recover);
            rear = Vector2.Lerp(Vector2.Lerp(new Vector2(-.1f, .55f), new Vector2(-1.7f, .7f), cast), idleRear, recover);
            wantedLean = Mathf.Lerp(7, 0, recover); corePower = 1.5f + Mathf.Sin(age * 18) * .3f;
        }
        if (state == BiomechBoss.State.Charge)
        {
            float sweep = Ease(p / .23f), brake = Ease(Mathf.InverseLerp(.7f, 1f, p));
            front = Vector2.Lerp(Vector2.Lerp(new Vector2(.55f, .65f), new Vector2(-2.65f, .35f), sweep), idleFront, brake);
            rear = Vector2.Lerp(Vector2.Lerp(new Vector2(.7f, .8f), new Vector2(-1.9f, .8f), sweep), idleRear, brake);
            wantedY -= .2f * (1 - brake); wantedLean = Mathf.Lerp(12, 0, brake); wantedHead = -8 * (1 - brake);
            stride = Mathf.Sin(Mathf.PI * p); cycle += Time.deltaTime * 22;
        }
        if (state == BiomechBoss.State.Slam)
        {
            float release = Ease(p);
            front = Vector2.Lerp(new Vector2(-1.15f, -1.25f), idleFront, release);
            rear = Vector2.Lerp(new Vector2(-.35f, -1.15f), idleRear, release);
            wantedY -= .25f * (1 - release); wantedLean = 11 * (1 - release); wantedHead = -10 * (1 - release);
        }
        bool dying = state == BiomechBoss.State.Dying || state == BiomechBoss.State.Dead;
        if (dying)
        {
            float collapse = state == BiomechBoss.State.Dead ? 1 : Ease(p);
            wantedY = Mathf.Lerp(1.45f, .35f, collapse); wantedLean = 22 * collapse; wantedHead = -30 * collapse;
            front = Vector2.Lerp(idleFront, new Vector2(-1.7f, -.2f), collapse);
            rear = Vector2.Lerp(idleRear, new Vector2(-1.1f, -.15f), collapse); corePower = (1 - collapse) * .5f;
        }
        // Exponential smoothing is frame-rate independent and also bridges state
        // boundaries, including the enraged double slam and interrupted death.
        float follow = 1 - Mathf.Exp(-28 * Time.deltaTime);
        frontTip = Vector2.Lerp(frontTip, front, follow); rearTip = Vector2.Lerp(rearTip, rear, follow);
        bodyY = Mathf.Lerp(bodyY, wantedY, follow); lean = Mathf.LerpAngle(lean, wantedLean, follow);
        headAngle = Mathf.LerpAngle(headAngle, wantedHead, follow);
        torso.localPosition = new Vector3(0, bodyY); torso.localRotation = Quaternion.Euler(0, 0, lean);
        head.localRotation = Quaternion.Euler(0, 0, headAngle); neck.localRotation = Quaternion.Euler(0, 0, headAngle * .4f);
        abdomen.localRotation = Quaternion.Euler(0, 0, -lean * .3f + breathe);
        Pose(frontArm, frontTip, -1, false); Pose(rearArm, rearTip, 1, false);
        for (int i = 0; i < legs.Length; i++)
        {
            float phase = cycle + (i % 2 == 0 ? 0 : Mathf.PI);
            Vector2 planted = new Vector2(FootX[i] + Mathf.Sin(phase) * stride * .35f,
                .16f + Mathf.Max(0, Mathf.Cos(phase)) * stride * .3f);
            Vector3 worldFoot = skin.TransformPoint(planted);
            Pose(legs[i], torso.InverseTransformPoint(worldFoot), i % 2 == 0 ? 1 : -1, true);
        }
        Color tint = source.color;
        foreach (SpriteRenderer r in visuals) r.color = tint;
        float glow = .85f + .1f * Mathf.Sin(Time.time * 5);
        Color reactor=boss.CanTakeDamage ? new Color(1,.91f,.64f,1) : new Color(.3f,.67f,.61f,.65f);
        if(boss.ArmorHitFlash)reactor=new Color(1,.6f,.25f,1);
        lens.GetComponent<SpriteRenderer>().color = dying ? new Color(.25f, .4f, .4f, Mathf.Clamp01(corePower * 2)) : reactor*new Color(glow,1,1,1);
        lens.localScale = Vector3.one * (.29f / (parts[11].bounds.size.y - 48f / parts[11].pixelsPerUnit)) * (1 + Mathf.Sin(Time.time * 8) * .035f * corePower)*(boss.CanTakeDamage?1.35f:1);
        if(sweep)
        {
            float cut=Mathf.InverseLerp(.02f,.48f,p);
            sweep.enabled=state==BiomechBoss.State.Charge && cut>0 && cut<1;
            sweep.transform.localPosition=new Vector3(-1.45f,1.25f);
            sweep.transform.localRotation=Quaternion.Euler(0,0,Mathf.Lerp(-35,30,cut));
            sweep.transform.localScale=new Vector3(3.8f/sweep.sprite.bounds.size.x,2.3f/sweep.sprite.bounds.size.y,1);
            sweep.color=new Color(1,1,1,Mathf.Sin(cut*Mathf.PI)*.85f);
        }
    }
    private void OnDisable() { if (source) source.enabled = true; if (skin) skin.gameObject.SetActive(false); }
    private void OnEnable() { if (skin) skin.gameObject.SetActive(true); }
    private void OnDestroy() { foreach (Material material in partMaterials) if (material) Destroy(material); if(sweepMaterial)Destroy(sweepMaterial); }
}
