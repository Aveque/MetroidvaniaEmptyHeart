using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class BossArena : MonoBehaviour
{
    [SerializeField] private BiomechBoss boss;
    [SerializeField] private CameraRoom cameraRoom;
    [SerializeField] private GameObject entranceSeal;
    [SerializeField] private GameObject exitSeal;
    [SerializeField] private GameObject reward;
    private PlayerHealth player;
    private float victoryUntil;
    private float delayedHealth;
    public bool InCombat { get; private set; }
    public bool Defeated { get; private set; }
    public bool DoorsLocked => entranceSeal != null && entranceSeal.activeSelf;
    public BiomechBoss Boss => boss;
    public Bounds CombatBounds => new Bounds(new Vector3(cameraRoom.WorldBounds.center.x, 2f),
        new Vector3(cameraRoom.WorldBounds.size.x, 10f, 10f));

    public void Configure(BiomechBoss guardian, CameraRoom room, GameObject entrance, GameObject exit, GameObject prize)
    {
        boss = guardian; cameraRoom = room; entranceSeal = entrance; exitSeal = exit; reward = prize;
        GetComponent<BoxCollider2D>().isTrigger = true;
    }

    private void Awake()
    {
        foreach (GameObject seal in new[] { entranceSeal, exitSeal })
        {
            var skin = seal.GetComponent<PaintedArenaSeal>();
            if (!skin) skin = seal.AddComponent<PaintedArenaSeal>();
            skin.Configure(boss.AttackEffects);
        }
        if (!boss.GetComponent<BossArmorVisual>()) boss.gameObject.AddComponent<BossArmorVisual>();
        entranceSeal.SetActive(false);
        exitSeal.SetActive(true);
        if (reward != null) reward.SetActive(false);
    }

    private void OnTriggerEnter2D(Collider2D other) => TryBegin(other.GetComponentInParent<PlayerMove>());
    private void OnTriggerStay2D(Collider2D other) => TryBegin(other.GetComponentInParent<PlayerMove>());

    public bool TryBegin(PlayerMove movement)
    {
        if (InCombat || Defeated || movement == null || !movement.isActiveAndEnabled) return false;
        PlayerHealth health = movement.GetComponent<PlayerHealth>();
        if (health == null || health.CurrentHealth <= 0 || health.IsRespawning) return false;
        player = health;
        player.Died += OnPlayerDied;
        InCombat = true;
        StationMusic.SetBossBattle(true);
        delayedHealth = boss.MaxHealth;
        entranceSeal.SetActive(true); exitSeal.SetActive(true);
        Camera.main.GetComponent<CameraFollow>().SetRoom(cameraRoom, true);
        boss.BeginEncounter(this, player);
        return true;
    }

    private void OnPlayerDied()
    {
        if (player != null) player.Died -= OnPlayerDied;
        InCombat = false;
        StationMusic.SetBossBattle(false);
        if (!Defeated) boss.ResetEncounter();
        entranceSeal.SetActive(false);
        exitSeal.SetActive(!Defeated);
    }

    public void CompleteEncounter()
    {
        if (Defeated) return;
        Defeated = true; InCombat = false;
        StationMusic.SetBossBattle(false);
        if (player != null) player.Died -= OnPlayerDied;
        entranceSeal.SetActive(false); exitSeal.SetActive(false);
        if (reward != null) reward.SetActive(true);
        victoryUntil = Time.time + 4f;
    }

    public void RestoreDefeated(bool rewardCollected)
    {
        Defeated=true;InCombat=false;entranceSeal.SetActive(false);exitSeal.SetActive(false);
        boss.gameObject.SetActive(false);if(reward)reward.SetActive(!rewardCollected);
        StationMusic.SetBossBattle(false);
    }

    private void OnDestroy() { if (player != null) player.Died -= OnPlayerDied; }
    private void Update()
    {
        if (InCombat) delayedHealth = Mathf.MoveTowards(delayedHealth, boss.CurrentHealth, Time.deltaTime * boss.MaxHealth * 0.4f);
    }

    private void OnGUI()
    {
        if (InCombat) EmptyHeartUI.BossHUD(boss, delayedHealth);
        if (Defeated && Time.time < victoryUntil)
            EmptyHeartUI.Announcement("БИОКОНТУР ОСТАНОВЛЕН", "СТРАЖ ОТКЛЮЧЁН", "Проход открыт. Забери ядро в хранилище.", 1f);
    }
}
