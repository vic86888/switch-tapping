using UnityEngine;

public class SongManager : MonoBehaviour
{
    public static SongManager instance;

    [Header("音樂設定")]
    public AudioSource audioSource;
    public AudioClip songClip;

    [Header("遊戲延遲設定")]
    public float startDelay = 3.0f; // 遊戲開始前的準備時間

    [Header("即時資訊 (供外部讀取)")]
    public float songPosition; 

    private double dspSongTime;       // 記錄音樂真正開始播放的精準時間 (改為 double)
    private double pauseStartDSPTime; // 🌟 記錄按下暫停瞬間的系統時間

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
            
            // 取得精準時間並加上延遲
            dspSongTime = AudioSettings.dspTime + startDelay;
            audioSource.PlayScheduled(dspSongTime);
        }
    }

    void Update()
    {
        // 🌟 進入暫停狀態時，停止更新遊戲時間！
        if (PauseManager.isPaused) return;

        songPosition = (float)(AudioSettings.dspTime - dspSongTime);
    }

    // ==========================================
    // 🌟 專門給暫停系統呼叫的方法
    // ==========================================
    public void PauseMusic()
    {
        pauseStartDSPTime = AudioSettings.dspTime; // 記下按下暫停的那一刻

        if (songPosition < 0)
        {
            audioSource.Stop(); // 如果還在倒數 3 秒內，直接取消預定播放
        }
        else
        {
            audioSource.Pause(); // 如果已經開始了，就凍結音樂
        }
    }

    public void ResumeMusic()
    {
        // 核心魔法：計算出我們到底暫停了多久？
        double pauseDuration = AudioSettings.dspTime - pauseStartDSPTime;

        // 把音樂的「起始基準點」往未來推遲，把被吃掉的時間補回來
        dspSongTime += pauseDuration;

        if (songPosition < 0)
        {
            // 如果原本還在倒數，就用新的時間重新預定播放
            audioSource.PlayScheduled(dspSongTime); 
        }
        else
        {
            audioSource.UnPause(); // 恢復播放
        }
    }
}