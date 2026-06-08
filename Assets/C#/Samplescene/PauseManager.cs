using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseManager : MonoBehaviour
{
    [Header("UI 綁定")]
    public GameObject pauseMenuPanel; // 暫停選單的透明背景與按鈕群

    public static bool isPaused = false; // 讓 HitManager 知道現在是否暫停
    private AudioSource musicSource;

    void Start()
    {
        // 遊戲開始時，確保時間正常，選單隱藏
        Time.timeScale = 1f;
        isPaused = false;
        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(false);
        }

        // 嘗試自動抓取場景中的背景音樂
        if (SongManager.instance != null)
        {
            musicSource = SongManager.instance.GetComponent<AudioSource>();
        }
        else
        {
            // 如果沒掛在 SongManager 上，就在場景中隨便找一個 AudioSource
            musicSource = FindObjectOfType<AudioSource>();
        }
    }

    void Update()
    {
        // 快捷鍵 P：切換暫停 / 繼續
        if (Input.GetKeyDown(KeyCode.P))
        {
            if (isPaused)
            {
                ResumeGame();
            }
            else
            {
                PauseGame();
            }
        }

        // 快捷鍵 R：一鍵重來
        if (Input.GetKeyDown(KeyCode.R))
        {
            RestartGame();
        }
    }

    public void PauseGame()
    {
        isPaused = true;
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(true);
        Time.timeScale = 0f; 
        
        // 🌟 換成這行：呼叫我們剛寫好的神仙方法
        if (SongManager.instance != null) SongManager.instance.PauseMusic(); 
    }

    public void ResumeGame()
    {
        isPaused = false;
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
        Time.timeScale = 1f; 
        
        // 🌟 換成這行：呼叫我們剛寫好的神仙方法
        if (SongManager.instance != null) SongManager.instance.ResumeMusic(); 
    }

    public void RestartGame()
    {
        // 確保時間恢復正常，然後重新載入「當前」場景
        Time.timeScale = 1f;
        isPaused = false;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void ExitToMainMenu()
    {
        // 確保時間恢復正常，載入主畫面
        Time.timeScale = 1f;
        isPaused = false;
        SceneManager.LoadScene("MainMenu"); 
    }
}