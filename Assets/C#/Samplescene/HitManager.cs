using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // 🌟 必須加入這行才能控制 UI
using UnityEngine.SceneManagement;

public class HitManager : MonoBehaviour
{
    public static HitManager instance;

    [Header("判定區間 (秒)")]
    public float perfectWindow = 0.05f; 
    public float greatWindow = 0.10f;  
    public float missWindow = 0.15f;   

    [Header("軌道發光視覺設定")]
    public SpriteRenderer[] trackHighlights = new SpriteRenderer[4]; 
    public Color normalColor = new Color(1f, 1f, 1f, 0.1f); 
    public Color highlightColor = new Color(1f, 1f, 1f, 0.6f); 

    [Header("打擊點視覺設定 (色塊)")]
    public SpriteRenderer[] hitPointRenderers = new SpriteRenderer[8]; // 🌟 綁定 8 個打擊點色塊
    public float defaultAlpha = 100f / 255f; // 沒按下的預設透明度 (大約 0.39)
    public float activeAlpha = 1f;           // 按下時的全不透明 (1.0)

    [Header("UI 視覺設定")]
    public Text comboText; // 🌟 中間的 Combo 文字

    [Header("Combo 視覺設定")]
    public Color comboNormalColor = Color.white;                   // 50 連擊以下的顏色
    public int comboNormalSize = 80;                               // 50 連擊以下的大小    
    public Color combo50Color = new Color(0f, 0.8f, 1f, 1f);       // 50 連擊以上的顏色 (預設亮藍)
    public int combo50Size = 100;                                  // 50 連擊以上的大小    
    public Color combo100Color = new Color(1f, 0.8f, 0f, 1f);      // 100 連擊以上的顏色 (預設金色)
    public int combo100Size = 120;                                 // 100 連擊以上的大小
    
    public Text[] judgementTexts = new Text[8]; // 🌟 8 個軌道的判定文字 (0~7)
    public Color perfectColor = Color.yellow;
    public Color greatColor = Color.green;
    public Color missColor = Color.red;

    private List<NoteController>[] tracks = new List<NoteController>[8];
    private NoteController activeHoldJ = null;
    private NoteController activeHoldK = null;

    private int currentCombo = 0; // 🌟 紀錄目前的 Combo 數

    [Header("遊戲結束設定")]
    public float songEndTime = 120f; // 🌟 音樂結束時間 (秒)，你可以自由調整
    public string resultSceneName = "ResultScene"; // 🌟 結算場景名稱
    private bool isGameEnded = false;

    // 🌟 靜態變數：跨場景儲存成績
    public static int totalPerfect = 0;
    public static int totalGreat = 0;
    public static int totalMiss = 0;

    void Awake()
    {
        instance = this;
        for (int i = 0; i < 8; i++) tracks[i] = new List<NoteController>();
    }

    void Start()
    {
        ResetCombo();
        for (int i = 0; i < 8; i++) 
        {
            if (judgementTexts[i] != null) judgementTexts[i].text = "";
        }

        // 🌟 遊戲開始時，重置所有成績
        totalPerfect = 0;
        totalGreat = 0;
        totalMiss = 0;
        isGameEnded = false;
    }

    public void RegisterNote(NoteController note, int direction, int lane)
    {
        tracks[direction * 2 + lane].Add(note);
    }

    public void RemoveNote(NoteController note, int direction, int lane)
    {
        if (tracks[direction * 2 + lane].Contains(note))
            tracks[direction * 2 + lane].Remove(note);
    }

    void Update()
    {
        if (SongManager.instance == null) return;

        // 🌟 新增這行：只要進入暫停狀態，直接退出，無視底下所有的搖桿與按鍵判斷！
        if (PauseManager.isPaused) return;

        // 🌟 判斷是否抵達結束時間
        if (!isGameEnded && SongManager.instance.songPosition >= songEndTime)
        {
            isGameEnded = true;
            SceneManager.LoadScene(resultSceneName); // 跳轉到結算場景
            return; // 停止執行後續邏輯
        }

        int currentDir = GetCurrentJoystickDirection();
        UpdateTrackVisuals(currentDir);
        UpdateHitPointVisuals(currentDir); // 🌟 新增這行：隨時更新 8 個色塊的透明度
        FadeOutJudgementTexts(); 

        // ============================================
        // 🌟 新增：長按防呆機制 (監聽搖桿是否鬆開或換向)
        // ============================================
        // 如果目前有正在長按的 J 音符，但搖桿方向跑掉了 (沒推或是推到別邊)
        if (activeHoldJ != null && activeHoldJ.direction != currentDir)
        {
            // 視同提早放手：變灰、斷開連結
            TryReleaseNote(ref activeHoldJ); 
        }
        
        // 如果目前有正在長按的 K 音符，但搖桿方向跑掉了
        if (activeHoldK != null && activeHoldK.direction != currentDir)
        {
            TryReleaseNote(ref activeHoldK);
        }

        // ============================================
        // 原本的按鍵監聽邏輯
        // ============================================
        if (Input.GetKeyDown(KeyCode.J) || ArduinoSerialPOC.GetButtonDown("J")) 
            TryHitNote(currentDir, 0, ref activeHoldJ);
        else if ((Input.GetKeyUp(KeyCode.J) || ArduinoSerialPOC.GetButtonUp("J")) && activeHoldJ != null) 
            TryReleaseNote(ref activeHoldJ);

        if (Input.GetKeyDown(KeyCode.K) || ArduinoSerialPOC.GetButtonDown("K")) 
            TryHitNote(currentDir, 1, ref activeHoldK);
        else if ((Input.GetKeyUp(KeyCode.K) || ArduinoSerialPOC.GetButtonUp("K")) && activeHoldK != null) 
            TryReleaseNote(ref activeHoldK);
    }

    // 🌟 新增的方法：處理打擊點色塊的透明度變化
    void UpdateHitPointVisuals(int currentDir)
    {
        // 讀取目前玩家是否「正在按住」按鍵 (包含鍵盤與實體按鈕)
        bool isJPressed = Input.GetKey(KeyCode.J) || ArduinoSerialPOC.GetButton("J");
        bool isKPressed = Input.GetKey(KeyCode.K) || ArduinoSerialPOC.GetButton("K");

        for (int i = 0; i < 8; i++)
        {
            if (hitPointRenderers[i] == null) continue;

            Color c = hitPointRenderers[i].color;
            
            // 透過索引反推方向與顏色 (巧妙的數學：0~7 對應 dir 與 lane)
            int dir = i / 2;  // 0:上, 1:下, 2:左, 3:右
            int lane = i % 2; // 0:紅(J), 1:藍(K)

            // 如果搖桿方向對了，而且對應的按鍵正在被按住，就亮起！
            if (dir == currentDir)
            {
                if (lane == 0 && isJPressed) c.a = activeAlpha;
                else if (lane == 1 && isKPressed) c.a = activeAlpha;
                else c.a = defaultAlpha;
            }
            else
            {
                // 方向不對，或者沒按按鍵，就維持半透明
                c.a = defaultAlpha;
            }

            hitPointRenderers[i].color = c;
        }
    }

    // 🌟 處理文字淡出效果
    void FadeOutJudgementTexts()
    {
        for (int i = 0; i < 8; i++)
        {
            if (judgementTexts[i] != null && judgementTexts[i].color.a > 0)
            {
                Color c = judgementTexts[i].color;
                c.a -= Time.deltaTime * 2f; // 每秒扣 0.5 透明度 (約 0.5 秒消失)
                judgementTexts[i].color = c;
            }
        }
    }

    // 🌟 顯示判定文字
    void ShowJudgement(int trackIndex, string result, Color color)
    {
        if (judgementTexts[trackIndex] != null)
        {
            judgementTexts[trackIndex].text = result;
            color.a = 1f; // 確保文字是完全不透明的
            judgementTexts[trackIndex].color = color;
        }
    }

    // 🌟 增加 Combo
    // 🌟 升級版：會根據 Combo 數量變色的 AddCombo
    // 🌟 升級版：使用 Inspector 變數來控制 Combo 外觀
    void AddCombo()
    {
        currentCombo++;
        
        if (comboText != null) 
        {
            comboText.text = $"Combo {currentCombo}";

            // 根據連擊數套用對應的顏色與大小
            if (currentCombo >= 100)
            {
                comboText.color = combo100Color; 
                comboText.fontSize = combo100Size; 
            }
            else if (currentCombo >= 50)
            {
                comboText.color = combo50Color; 
                comboText.fontSize = combo50Size;
            }
            else
            {
                comboText.color = comboNormalColor; 
                comboText.fontSize = comboNormalSize; 
            }
        }
    }

    void ResetCombo()
    {
        currentCombo = 0;
        if (comboText != null) 
        {
            comboText.text = ""; // 斷 Combo 時隱藏文字
            comboText.color = comboNormalColor; // 恢復成預設顏色
            comboText.fontSize = comboNormalSize; // 恢復預設大小
        }
    }

    // 🌟 斷 Combo (開放給外部呼叫)
    public void TriggerMiss(int direction, int lane)
    {
        int trackIndex = direction * 2 + lane;
        ShowJudgement(trackIndex, "Miss", missColor);
        totalMiss++; // 🌟 紀錄
        ResetCombo();
    }

    void UpdateTrackVisuals(int currentDir)
    {
        for (int i = 0; i < 4; i++)
        {
            if (trackHighlights[i] != null)
                trackHighlights[i].color = (i == currentDir) ? highlightColor : normalColor;
        }
    }

    int GetCurrentJoystickDirection()
    {
        string serialDir = ArduinoSerialPOC.JoystickDirection;
        if (serialDir == "UP") return 0;
        if (serialDir == "DOWN") return 1;
        if (serialDir == "LEFT") return 2;
        if (serialDir == "RIGHT") return 3;

        if (Input.GetKey(KeyCode.W)) return 0; 
        if (Input.GetKey(KeyCode.S)) return 1; 
        if (Input.GetKey(KeyCode.A)) return 2; 
        if (Input.GetKey(KeyCode.D)) return 3; 
        return -1; 
    }

    void TryHitNote(int dir, int lane, ref NoteController holdRef)
    {
        if (dir == -1) return; 

        int trackIndex = dir * 2 + lane;
        if (tracks[trackIndex].Count == 0) return; 

        NoteController targetNote = null;
        float currentTime = SongManager.instance.songPosition;

        foreach (var note in tracks[trackIndex])
        {
            // 🌟 阻擋：已經變灰的音符不可觸碰！
            if (note.isGray) continue; 
            
            if (note.duration > 0 && note.headHit) continue; 
            if (currentTime - note.targetTime > missWindow) continue; 

            targetNote = note;
            break;
        }

        if (targetNote == null) return; 

        float timeDiff = Mathf.Abs(currentTime - targetNote.targetTime); 

        if (timeDiff <= missWindow) 
        {
            if (targetNote.duration > 0)
            {
                // 長按音符起手：只紀錄狀態，不跳字
                targetNote.headHit = true;
                targetNote.headPerfect = (timeDiff <= perfectWindow);
                
                holdRef = targetNote;
                targetNote.isBeingHeld = true; 
            }
            else
            {
                // 單擊音符：照舊結算
                if (timeDiff <= perfectWindow) 
                {
                    ShowJudgement(trackIndex, "Perfect", perfectColor);
                    totalPerfect++; // 🌟 紀錄
                    AddCombo();
                }
                else if (timeDiff <= greatWindow) 
                {
                    ShowJudgement(trackIndex, "Great", greatColor);
                    totalGreat++; // 🌟 紀錄
                    AddCombo();
                }
                else 
                {
                    TriggerMiss(dir, lane);
                }
                targetNote.HitAndDestroy(); 
            }
        }
    }

    void TryReleaseNote(ref NoteController holdRef)
    {
        if (holdRef != null)
        {
            holdRef.isBeingHeld = false;
            holdRef.wasReleasedEarly = true; 
            holdRef.TurnGray(); // 🌟 情況 1：中途提早放開，音符瞬間變灰！
            holdRef = null; 
        }
    }

    public void ResolveHoldNote(NoteController note)
    {
        int trackIndex = note.direction * 2 + note.lane;

        // 情況 2：頭部根本沒摸到
        if (!note.headHit)
        {
            TriggerMiss(note.direction, note.lane);
        }
        // 情況 1 & 3：頭部有摸到
        else
        {
            // 完美條件：起手 Perfect + 中途沒放開 + 到最後一刻都按著
            if (note.headPerfect && !note.wasReleasedEarly && note.isBeingHeld)
            {
                ShowJudgement(trackIndex, "Perfect", perfectColor);
                totalPerfect++; // 🌟 紀錄
                AddCombo();
            }
            else
            {
                // 寬容模式：有摸到頭但中途放開(變灰了)，或起手沒抓準，最後保底 Great
                ShowJudgement(trackIndex, "Great", greatColor);
                totalGreat++; // 🌟 紀錄
                AddCombo();
            }
        }

        if (activeHoldJ == note) activeHoldJ = null;
        if (activeHoldK == note) activeHoldK = null;
    }
}