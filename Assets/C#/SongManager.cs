using UnityEngine;

public class SongManager : MonoBehaviour
{
    public static SongManager instance;

    [Header("音樂設定")]
    public AudioSource audioSource;
    public AudioClip songClip;

    [Header("遊戲延遲設定")]
    public float startDelay = 30.0f; // 🌟 遊戲開始前的準備時間 (等待三秒)

    [Header("即時資訊 (供外部讀取)")]
    public float songPosition; 

    private float dspSongTime; // 記錄音樂真正開始播放的系統精準時間

    void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        if (audioSource == null) audioSource = GetComponent<AudioSource>();

        if (songClip != null)
        {
            audioSource.clip = songClip;
            
            // 1. 取得現在 Unity 音效引擎的絕對時間，並加上 3 秒的延遲
            dspSongTime = (float)AudioSettings.dspTime + startDelay;

            // 2. 告訴音效引擎：「請準時在 dspSongTime 這個時間點，自動把音樂播出來」
            audioSource.PlayScheduled(dspSongTime);
        }
    }

    void Update()
    {
        // 3. 計算目前的遊戲時間
        // 🌟 魔法在這裡：前三秒的時候，(現在時間 - 未來播放時間) 會是「負數」！
        // 時間會這樣跑：-3.0 -> -2.0 -> -1.0 -> 0 (音樂響起) -> 1.0 ...
        songPosition = (float)(AudioSettings.dspTime - dspSongTime);
    }
}