using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer), typeof(BoxCollider2D))]
public class BiomechBoss : MonoBehaviour, IDamageable
{
    public enum State { Dormant, Intro, Idle, Telegraph, Charge, Slam, Pulse, Stunned, Dying, Dead }
    public enum Attack { None, Energy, Sickle, Roots }
    [SerializeField] private Sprite[] frames;
    [SerializeField] private Sprite[] energyFrames, sickleFrames, rootFrames;
    public Attack ActiveAttack { get; private set; }
    public bool HasDistinctAttacks => energyFrames?.Length == 4 && sickleFrames?.Length == 4 && rootFrames?.Length == 4;
    [SerializeField] private Sprite hazardSprite;
    [SerializeField] private Sprite[] attackEffects;
    public Sprite[] AttackEffects => attackEffects;
    public bool ArmorHitFlash => Time.time < armorFlashUntil;
    public void ConfigureEffects(Sprite[] effects) { attackEffects=effects; }
    [SerializeField] private int maxHealth = 30;
    private SpriteRenderer renderer2D;
    private BoxCollider2D hitbox;
    private BossArena arena;
    private PlayerHealth player;
    private Vector3 spawn;
    private float stateSince;
    private float stateDuration = 1f;
    private float flashUntil;
    private float nextContact;
    private bool phaseTransitionPlayed;
    private float coreExposedUntil;
    private float armorFlashUntil;
    private readonly List<BossHazard> hazards = new List<BossHazard>();
    private AudioSource audioSource;
    private AudioClip lowTone, highTone, impactTone;
    public State CurrentState { get; private set; }
    public float StateProgress => Mathf.Clamp01((Time.time - stateSince) / stateDuration);
    public float StateAge => Mathf.Max(0f, Time.time - stateSince);
    public int CurrentHealth { get; private set; }
    public int MaxHealth => maxHealth;
    public bool Enraged => CurrentHealth > 0 && CurrentHealth <= maxHealth / 2;
    public bool ActiveBody => CurrentHealth > 0 && CurrentState != State.Dormant && CurrentState != State.Intro
        && CurrentState != State.Dying && CurrentState != State.Dead;
    public bool CanTakeDamage => ActiveBody && Time.time < coreExposedUntil;
    public float CoreWindowRemaining => Mathf.Max(0, coreExposedUntil - Time.time);
    public string AttackHint { get; private set; }

    public void Configure(Sprite[] animationFrames, Sprite energySprite)
    {
        frames = animationFrames; hazardSprite = energySprite;
        GetComponent<SpriteRenderer>().sprite = frames[0];
    }
    public void ConfigureAttacks(Sprite[] energy, Sprite[] sickle, Sprite[] roots)
    { energyFrames = energy; sickleFrames = sickle; rootFrames = roots; }

    private void Awake()
    {
        renderer2D = GetComponent<SpriteRenderer>(); hitbox = GetComponent<BoxCollider2D>();
        hitbox.isTrigger = true; hitbox.enabled = false;
        spawn = transform.position; CurrentHealth = maxHealth;
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false; audioSource.spatialBlend = 0; audioSource.volume = 0.16f;
        lowTone = CreateTone("Reactor Warning", 125f, 0.3f);
        highTone = CreateTone("Impulse Warning", 520f, 0.18f);
        impactTone = CreateTone("Mechanical Impact", 68f, 0.35f);
        ChangeState(State.Dormant, 1);
    }

    private static AudioClip CreateTone(string label, float frequency, float seconds)
    {
        const int rate = 22050;
        float[] samples = new float[(int)(rate * seconds)];
        for (int i = 0; i < samples.Length; i++)
        {
            float t = i / (float)rate;
            samples[i] = (Mathf.Sin(2 * Mathf.PI * frequency * t) * 0.65f
                + Mathf.Sin(2 * Mathf.PI * frequency * 0.5f * t) * 0.25f) * Mathf.Sin(Mathf.PI * i / samples.Length);
        }
        AudioClip clip = AudioClip.Create(label, samples.Length, 1, rate, false);
        clip.SetData(samples, 0); return clip;
    }

    public void BeginEncounter(BossArena owner, PlayerHealth target)
    {
        arena = owner; player = target;
        StopAllCoroutines(); ClearHazards();
        CurrentHealth = maxHealth; phaseTransitionPlayed = false;
        coreExposedUntil = 0;
        transform.position = spawn; renderer2D.enabled = true; renderer2D.flipX = false;
        hitbox.enabled = false;
        StartCoroutine(FightRoutine());
    }

    public void ResetEncounter()
    {
        StopAllCoroutines(); ClearHazards();
        CurrentHealth = maxHealth; phaseTransitionPlayed = false;
        coreExposedUntil = 0;
        transform.position = spawn; transform.localScale = Vector3.one;
        renderer2D.color = Color.white; renderer2D.flipX = false;
        hitbox.enabled = false; AttackHint = "";
        ChangeState(State.Dormant, 1);
    }

    private void ChangeState(State state, float duration)
    {
        CurrentState = state; stateSince = Time.time; stateDuration = Mathf.Max(0.01f, duration);
        if (state == State.Idle || state == State.Dormant || state == State.Intro || state == State.Dying || state == State.Dead)
            ActiveAttack = Attack.None;
    }

    private IEnumerator FightRoutine()
    {
        AttackHint = "САДОВНИК-07 ПРОБУЖДАЕТСЯ";
        ChangeState(State.Intro, 1.8f); audioSource.PlayOneShot(lowTone);
        yield return new WaitForSeconds(1.8f);
        hitbox.enabled = true; AttackHint = "";
        int turn = 0;
        while (CurrentHealth > 0)
        {
            if (Enraged && !phaseTransitionPlayed)
            {
                phaseTransitionPlayed = true;
                AttackHint = "ПЕРЕГРУЗКА: РИТМ АТАК УСКОРЯЕТСЯ";
                ChangeState(State.Stunned, 1.3f); audioSource.PlayOneShot(lowTone);
                yield return new WaitForSeconds(1.3f);
            }
            ChangeState(State.Idle, 0.85f); AttackHint = "";
            yield return new WaitForSeconds(Enraged ? 0.65f : 0.95f);
            switch (turn++ % 3)
            {
                case 0: yield return PulseWall(); break;
                case 1: yield return SickleCharge(); break;
                default: yield return RootSlam(); break;
            }
            AttackHint = CanTakeDamage ? "ЯДРО ОТКРЫТО — АТАКУЙ" : "БИОБРОНЯ АКТИВНА — РАЗОРВИ ИМПУЛЬСНЫЙ КОНТУР РЫВКОМ";
            ChangeState(State.Stunned, 1f);
            yield return new WaitForSeconds(Enraged ? 0.8f : 1.1f);
        }
    }

    private IEnumerator PulseWall()
    {
        ActiveAttack = Attack.Energy;
        float direction = transform.position.x > 74f ? -1f : 1f;
        renderer2D.flipX = direction > 0;
        Bounds bounds = arena.CombatBounds;
        var leftWall=Physics2D.Raycast(new Vector2(bounds.center.x,-1.5f),Vector2.left,30,1<<LayerMask.NameToLayer("Wall"));
        var rightWall=Physics2D.Raycast(new Vector2(bounds.center.x,-1.5f),Vector2.right,30,1<<LayerMask.NameToLayer("Wall"));
        if(leftWall.collider && rightWall.collider)bounds.SetMinMax(new Vector3(leftWall.point.x+.05f,bounds.min.y,-5),new Vector3(rightWall.point.x-.05f,bounds.max.y,5));
        float x = direction < 0 ? bounds.max.x - 1f : bounds.min.x + 1f;
        float endX = direction < 0 ? bounds.min.x + .32f : bounds.max.x - .32f;
        float travelTime = Mathf.Abs(endX - x) / 8.5f;
        float warning = Enraged ? 0.75f : 1.05f;
        AttackHint = "ИМПУЛЬСНАЯ СТЕНА — РЫВОК [SHIFT] СКВОЗЬ БИРЮЗОВЫЙ ЛУЧ";
        ChangeState(State.Telegraph, warning); audioSource.PlayOneShot(highTone);
        var preview=SpawnHazard(new Vector2(x, 2f), new Vector2(0.16f, 10f), Vector2.zero, warning, false, true, new Color(0.2f, 1f, 0.84f, 0.3f));preview.ConfineTo(bounds);
        yield return new WaitForSeconds(warning);
        ChangeState(State.Pulse, travelTime);
        SpawnHazard(new Vector2(x, bounds.center.y), new Vector2(0.55f, bounds.size.y), new Vector2(direction * 8.5f, 0), travelTime, true, true, new Color(0.38f, 1f, 0.85f, 0.92f));
        audioSource.PlayOneShot(impactTone);
        yield return new WaitForSeconds(travelTime);
    }

    private IEnumerator SickleCharge()
    {
        ActiveAttack = Attack.Sickle;
        float targetX = transform.position.x > 74 ? 67f : 80.5f;
        renderer2D.flipX = targetX > transform.position.x;
        float warning = Enraged ? 0.65f : 0.95f;
        AttackHint = "БРОСОК СЕРПОВ — ПРЫГНИ ИЛИ ПРОЙДИ РЫВКОМ";
        ChangeState(State.Telegraph, warning); audioSource.PlayOneShot(lowTone);
        SpawnHazard(new Vector2(74, -2.65f), new Vector2(18, 0.12f), Vector2.zero, warning, false, false, new Color(1f, 0.35f, 0.2f, 0.5f));
        yield return new WaitForSeconds(warning);
        ChangeState(State.Charge, 0.75f); audioSource.PlayOneShot(impactTone);
        Vector3 from = transform.position, to = new Vector3(targetX, spawn.y, 0);
        float elapsed = 0;
        while (elapsed < 0.75f)
        {
            elapsed += Time.deltaTime;
            transform.position = Vector3.Lerp(from, to, Mathf.Clamp01(elapsed / 0.75f));
            yield return null;
        }
    }

    private IEnumerator RootSlam()
    {
        ActiveAttack = Attack.Roots;
        int count = Enraged ? 2 : 1;
        for (int i = 0; i < count; i++)
        {
            renderer2D.flipX = player.transform.position.x > transform.position.x;
            float x = Mathf.Clamp(player.transform.position.x, 65.5f, 82.5f);
            float warning = Enraged ? 0.65f : 0.95f;
            AttackHint = "КОРНЕВОЙ ПРОБОЙ — ПОКИНЬ КРАСНУЮ ЗОНУ";
            ChangeState(State.Telegraph, warning); audioSource.PlayOneShot(lowTone);
            SpawnHazard(new Vector2(x, -2.8f), new Vector2(2.6f, 0.22f), Vector2.zero, warning, false, false, new Color(1f, 0.35f, 0.25f, 0.65f));
            yield return new WaitForSeconds(warning);
            ChangeState(State.Slam, 0.35f); audioSource.PlayOneShot(impactTone);
            SpawnHazard(new Vector2(x, 1.7f), new Vector2(2.6f, 9.4f), Vector2.zero, 0.28f, true, true, new Color(1f, 0.25f, 0.22f, 0.8f));
            yield return new WaitForSeconds(0.4f);
        }
    }

    public BossHazard SpawnHazard(Vector2 position, Vector2 size, Vector2 speed, float duration, bool damaging, bool canDashThrough, Color color)
    {
        GameObject obj = new GameObject(damaging ? "Boss Attack" : "Attack Warning");
        obj.transform.SetParent(arena.transform, false); obj.transform.position = position;
        SpriteRenderer r = obj.AddComponent<SpriteRenderer>(); r.sprite = hazardSprite;
        r.sortingOrder = damaging ? 8 : 7; r.color = color;
        r.sharedMaterial = renderer2D.sharedMaterial;
        obj.transform.localScale = new Vector3(size.x, size.y, 1f);
        BossHazard hazard = obj.AddComponent<BossHazard>();
        hazard.Configure(player, size, speed, duration, damaging, canDashThrough);
        if (Mathf.Abs(speed.x) > 0 && size.y >= 10f) hazard.ConfineTo(arena.CombatBounds);
        obj.AddComponent<BossAttackIndicator>().Configure(hazard.HitSize, duration, damaging, canDashThrough,
            transform.position.x > 74f ? -1f : 1f, attackEffects);
        if (damaging && canDashThrough && Mathf.Abs(speed.x) > 0f && size.y >= 10f) hazard.CoreCircuitOwner = this;
        hazards.RemoveAll(h => h == null); hazards.Add(hazard); return hazard;
    }

    private void Update()
    {
        if (frames == null || frames.Length < 16) return;
        float progress = Mathf.Clamp01((Time.time - stateSince) / stateDuration);
        int frame = 0;
        switch (CurrentState)
        {
            case State.Intro: frame = Mathf.Min(3, (int)(progress * 4)); break;
            case State.Idle: case State.Stunned: frame = 4 + (int)(Time.time * 6) % 4; break;
            case State.Telegraph: frame = 8 + (progress > 0.5f ? 1 : 0); break;
            case State.Charge: frame = 10 + (int)(Time.time * 10) % 2; break;
            case State.Slam: frame = progress < 0.4f ? 9 : 11; break;
            case State.Pulse: frame = 8 + (int)(Time.time * 8) % 4; break;
            case State.Dying: frame = 12 + Mathf.Min(3, (int)(progress * 4)); break;
            case State.Dead: frame = 15; break;
        }
        renderer2D.sprite = frames[frame];
        Sprite[] action = ActiveAttack == Attack.Energy ? energyFrames : ActiveAttack == Attack.Sickle ? sickleFrames : rootFrames;
        if (ActiveAttack != Attack.None && action != null && action.Length == 4)
        {
            if (CurrentState == State.Telegraph) renderer2D.sprite = action[progress < .45f ? 0 : 1];
            else if (CurrentState == State.Charge || CurrentState == State.Slam)
                renderer2D.sprite = action[progress < .72f ? 2 : 3];
            else if (CurrentState == State.Pulse) renderer2D.sprite = action[progress < .22f ? 2 : 3];
            else if (CurrentState == State.Stunned && progress < .4f) renderer2D.sprite = action[3];
        }
        renderer2D.color = Time.time < flashUntil ? new Color(1f, 0.4f, 0.35f) : Enraged && CanTakeDamage ? new Color(1f, 0.72f, 0.65f) : Color.white;
        if (ActiveBody && player != null && player.CurrentHealth > 0 && Time.time >= nextContact &&
            hitbox.bounds.Intersects(player.GetComponent<Collider2D>().bounds) && !player.GetComponent<PlayerMove>().IsDashing)
        {
            player.TakeContactDamage(1, transform.position); nextContact = Time.time + 0.45f;
        }
    }

    public void TakeDamage(int damage)
    {
        if (ActiveBody && !CanTakeDamage && damage > 0) armorFlashUntil = Time.time + 0.18f;
        if (!CanTakeDamage || damage <= 0) return;
        CurrentHealth = Mathf.Max(0, CurrentHealth - damage); flashUntil = Time.time + 0.12f;
        if (CurrentHealth == 0)
        {
            StopAllCoroutines(); ClearHazards(); hitbox.enabled = false;
            StartCoroutine(DeathRoutine());
        }
    }

    public void BreakCoreCircuit()
    {
        if (!ActiveBody) return;
        coreExposedUntil = Time.time + 5f;
        AttackHint = "БИОКОНТУР РАЗОРВАН — ЯДРО ОТКРЫТО";
        audioSource.PlayOneShot(highTone);
    }

    private IEnumerator DeathRoutine()
    {
        AttackHint = "СБОЙ БИОКОНТУРА";
        ChangeState(State.Dying, 1.7f); audioSource.PlayOneShot(impactTone);
        yield return new WaitForSeconds(1.7f);
        ChangeState(State.Dead, 1f); AttackHint = "";
        arena.CompleteEncounter();
    }

    private void ClearHazards()
    {
        foreach (BossHazard hazard in hazards) if (hazard != null) { hazard.enabled = false; Destroy(hazard.gameObject); }
        hazards.Clear();
    }
    private void OnDestroy()
    {
        if (lowTone != null) Destroy(lowTone);
        if (highTone != null) Destroy(highTone);
        if (impactTone != null) Destroy(impactTone);
    }
}
