using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class ResultManager : MonoBehaviour
{
    [Header("UI 綁定")]
    public Text perfectText;
    public Text greatText;
    public Text missText;

    [Header("場景名稱設定")]
    public string gameSceneName = "SampleScene"; // ⚠️ 請改成你的遊戲場景名稱
    public string mainMenuSceneName = "MainMenu";

    void Start()
    {
        // 抓取 HitManager 裡面跨場景留存的成績，並顯示在畫面上
        if (perfectText != null) perfectText.text = "Perfect : " + HitManager.totalPerfect;
        if (greatText != null) greatText.text = "Great : " + HitManager.totalGreat;
        if (missText != null) missText.text = "Miss : " + HitManager.totalMiss;
    }

    void Update()
    {
        // 🌟 快捷鍵 R：重新開始
        if (Input.GetKeyDown(KeyCode.R))
        {
            RestartGame();
        }
    }

    public void RestartGame()
    {
        // 確保時間流動與暫停狀態恢復正常
        Time.timeScale = 1f;
        PauseManager.isPaused = false;
        SceneManager.LoadScene(gameSceneName);
    }

    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        PauseManager.isPaused = false;
        SceneManager.LoadScene(mainMenuSceneName);
    }
}