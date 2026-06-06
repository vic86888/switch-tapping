using UnityEngine;

public class SongManager : MonoBehaviour
{
    public static SongManager instance;

    [Header("音樂設定")]
    public AudioSource audioSource;
    // 新增這行：讓我們可以直接在面板放入 AudioClip
    public AudioClip songClip; 

    [Header("即時資訊 (供外部讀取)")]
    public float songPosition; 

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        // 核心修改：在遊戲開始時，強制使用 AudioClip 模式並播放，避開新版面板的坑
        if (songClip != null)
        {
            audioSource.clip = songClip;
            audioSource.Play();
        }
    }

    void Update()
    {
        if (audioSource.isPlaying)
        {
            songPosition = audioSource.time;
        }
    }
}