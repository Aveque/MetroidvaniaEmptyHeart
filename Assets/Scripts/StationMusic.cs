using UnityEngine;

// The two looping cues share a tonal centre, so an equal-power crossfade stays musical.
public class StationMusic : MonoBehaviour
{
    public static StationMusic Instance { get; private set; }
    [SerializeField] private AudioClip explorationTheme;
    [SerializeField] private AudioClip bossTheme;
    [SerializeField, Range(0f, 1f)] private float musicVolume = 0.32f;
    [SerializeField, Min(0.1f)] private float crossfadeSeconds = 1.8f;
    private AudioSource exploration, battle;
    private bool combat;
    private float blend;
    public bool InBossMode => combat;
    public float Blend => blend;
    public AudioSource ExplorationSource => exploration;
    public AudioSource BossSource => battle;
    public AudioClip ExplorationTheme => explorationTheme;
    public AudioClip BossTheme => bossTheme;

    public void Configure(AudioClip location, AudioClip boss)
    {
        explorationTheme = location; bossTheme = boss;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) { enabled = false; return; }
        Instance = this;
        exploration = MakeSource("Exploration Music", explorationTheme, 160);
        battle = MakeSource("Boss Music", bossTheme, 159);
    }

    private AudioSource MakeSource(string label, AudioClip clip, int priority)
    {
        GameObject channel = new GameObject(label); channel.transform.SetParent(transform, false);
        AudioSource source = channel.AddComponent<AudioSource>();
        source.clip = clip; source.loop = true; source.playOnAwake = false;
        source.spatialBlend = 0f; source.volume = 0f; source.priority = priority;
        source.dopplerLevel = 0f;
        return source;
    }

    private void Start()
    {
        if (exploration.clip != null) { exploration.volume = musicVolume; exploration.Play(); }
    }

    public static void SetBossBattle(bool active)
    {
        if (Instance != null) Instance.SetCombat(active);
    }

    private void SetCombat(bool active)
    {
        if (combat == active) return;
        combat = active;
        if (active && battle.clip != null && !battle.isPlaying)
        {
            battle.time = 0f; battle.Play();
        }
    }

    private void Update()
    {
        blend = Mathf.MoveTowards(blend, combat ? 1f : 0f, Time.unscaledDeltaTime / crossfadeSeconds);
        exploration.volume = musicVolume * Mathf.Cos(blend * Mathf.PI * 0.5f);
        battle.volume = musicVolume * Mathf.Sin(blend * Mathf.PI * 0.5f);
        if (!combat && blend <= 0f && battle.isPlaying) battle.Stop();
    }

    private void OnDestroy() { if (Instance == this) Instance = null; }
}
