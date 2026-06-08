using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // 🌟 必須加入這行才能控制 UI

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

    [Header("UI 視覺設定")]
    public Text comboText; // 🌟 中間的 Combo 文字
    public Text[] judgementTexts = new Text[8]; // 🌟 8 個軌道的判定文字 (0~7)
    public Color perfectColor = Color.yellow;
    public Color greatColor = Color.green;
    public Color missColor = Color.red;

    private List<NoteController>[] tracks = new List<NoteController>[8];
    private NoteController activeHoldJ = null;
    private NoteController activeHoldK = null;

    private int currentCombo = 0; // 🌟 紀錄目前的 Combo 數

    void Awake()
    {
        instance = this;
        for (int i = 0; i < 8; i++) tracks[i] = new List<NoteController>();
    }

    void Start()
    {
        // 遊戲開始時清空所有文字
        ResetCombo();
        for (int i = 0; i < 8; i++) 
        {
            if (judgementTexts[i] != null) judgementTexts[i].text = "";
        }
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

        int currentDir = GetCurrentJoystickDirection();
        UpdateTrackVisuals(currentDir);
        FadeOutJudgementTexts(); // 🌟 每幀讓判定文字慢慢變透明消失

        if (Input.GetKeyDown(KeyCode.J) || ArduinoSerialPOC.GetButtonDown("J")) 
            TryHitNote(currentDir, 0, ref activeHoldJ);
        else if ((Input.GetKeyUp(KeyCode.J) || ArduinoSerialPOC.GetButtonUp("J")) && activeHoldJ != null) 
            TryReleaseNote(ref activeHoldJ);

        if (Input.GetKeyDown(KeyCode.K) || ArduinoSerialPOC.GetButtonDown("K")) 
            TryHitNote(currentDir, 1, ref activeHoldK);
        else if ((Input.GetKeyUp(KeyCode.K) || ArduinoSerialPOC.GetButtonUp("K")) && activeHoldK != null) 
            TryReleaseNote(ref activeHoldK);
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
    void AddCombo()
    {
        currentCombo++;
        
        if (comboText != null) 
        {
            comboText.text = "Combo " + currentCombo;

            // === 讓 Combo 數字根據連擊數改變顏色 ===
            if (currentCombo >= 100)
            {
                // 100 連擊以上：變成傳說的金色
                comboText.color = new Color(1f, 0.8f, 0f, 1f); 
            }
            else if (currentCombo >= 50)
            {
                // 50 連擊以上：變成亮藍色或粉色
                comboText.color = new Color(0f, 0.8f, 1f, 1f); 
            }
            else
            {
                // 50 連擊以下：維持純白色
                comboText.color = Color.white; 
            }
        }
    }

    // 🌟 別忘了在斷 Combo 的時候把它變回預設狀態
    void ResetCombo()
    {
        currentCombo = 0;
        if (comboText != null) 
        {
            comboText.text = ""; 
            comboText.color = Color.white; // 恢復成純白
        }
    }

    // 🌟 斷 Combo (開放給外部呼叫)
    public void TriggerMiss(int direction, int lane)
    {
        int trackIndex = direction * 2 + lane;
        ShowJudgement(trackIndex, "Miss", missColor);
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

        // 🌟 防呆尋找：找出這條軌道上「第一個還可以被打」的音符
        NoteController targetNote = null;
        float currentTime = SongManager.instance.songPosition;

        foreach (var note in tracks[trackIndex])
        {
            // 如果是長按音符且頭部已經打過了，不能重複打
            if (note.duration > 0 && note.headHit) continue; 
            
            // 如果這顆音符已經徹底飛過去錯過了，略過它找下一顆
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
                // 🌟 長按音符邏輯：只在背景紀錄，【不】顯示 UI 也【不】加 Combo
                targetNote.headHit = true;
                targetNote.headPerfect = (timeDiff <= perfectWindow);
                
                holdRef = targetNote;
                targetNote.isBeingHeld = true; 
            }
            else
            {
                // 🌟 單擊音符邏輯：照舊直接結算
                if (timeDiff <= perfectWindow) 
                {
                    ShowJudgement(trackIndex, "Perfect", perfectColor);
                    AddCombo();
                }
                else if (timeDiff <= greatWindow) 
                {
                    ShowJudgement(trackIndex, "Great", greatColor);
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
        // 🌟 玩家中途鬆開按鍵 (不馬上結算，只標記狀態)
        if (holdRef != null)
        {
            holdRef.isBeingHeld = false;
            holdRef.wasReleasedEarly = true; // 記下這筆「提早鬆手」的帳
            holdRef = null; // 清空佔用，讓玩家的手可以去按別的音符
        }
    }

    // 🌟 新增：專屬於長按音符的終極結算中心 (在尾巴走完時被 NoteController 呼叫)
    public void ResolveHoldNote(NoteController note)
    {
        int trackIndex = note.direction * 2 + note.lane;

        // 情況 1：頭部根本沒摸到
        if (!note.headHit)
        {
            TriggerMiss(note.direction, note.lane);
        }
        // 情況 2：頭部有摸到
        else
        {
            // 完美條件：起手 Perfect + 中間沒偷放開 + 到最後一刻都乖乖按著
            if (note.headPerfect && !note.wasReleasedEarly && note.isBeingHeld)
            {
                ShowJudgement(trackIndex, "Perfect", perfectColor);
                AddCombo();
            }
            else
            {
                // 寬容模式 (選項A)：只要有摸到頭，不管是起手沒抓準、還是中途放開，保底都有 Great！
                ShowJudgement(trackIndex, "Great", greatColor);
                AddCombo();
            }
        }

        // 清理 HitManager 裡的追蹤器 (如果玩家乖乖按到最後一刻，reference 會殘留在這裡，需手動清除)
        if (activeHoldJ == note) activeHoldJ = null;
        if (activeHoldK == note) activeHoldK = null;
    }
}