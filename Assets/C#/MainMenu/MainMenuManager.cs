using UnityEngine;
using UnityEngine.SceneManagement; // 🌟 負責切換場景
using UnityEngine.UI;              // 🌟 負責控制 UI

public class MainMenuManager : MonoBehaviour
{
    [Header("場景設定")]
    // ⚠️ 這裡請填入你「遊戲場景」的確切名稱 (例如 "SampleScene" 或 "GameScene")
    public string gameSceneName = "SampleScene"; 

    [Header("UI 綁定")]
    public Slider volumeSlider;

    void Start()
    {
        // 1. 強制將遊戲的初始總音量設為 0.1 (10%)
        AudioListener.volume = 0.1f;

        // 2. 讓畫面上的音量滑桿也同步顯示在 0.1 的位置
        if (volumeSlider != null)
        {
            volumeSlider.value = 0.1f;
        }
    }

    // ==========================================
    // 給「開始遊戲」按鈕呼叫的方法
    // ==========================================
    public void StartGame()
    {
        Debug.Log("載入遊戲場景...");
        // 恢復時間流逝 (避免玩家在暫停時退回主畫面，導致新開一局卡住)
        Time.timeScale = 1f; 
        SceneManager.LoadScene(gameSceneName);
    }

    // ==========================================
    // 給「音量滑桿」呼叫的方法 (必須要有 float 參數)
    // ==========================================
    public void SetVolume(float volume)
    {
        // AudioListener.volume 會直接控制整個遊戲的總音量 (0.0 ~ 1.0)
        AudioListener.volume = volume;
    }

    // ==========================================
    // 給「退出遊戲」按鈕呼叫的方法
    // ==========================================
    public void ExitGame()
    {
        Debug.Log("遊戲退出！");
        Application.Quit(); // 打包成執行檔後，這行才會真正關閉視窗
    }
}