using System.Collections.Generic;
using UnityEngine;

public class HitManager : MonoBehaviour
{
    public static HitManager instance;

    [Header("判定區間 (秒)")]
    public float perfectWindow = 0.05f; 
    public float greatWindow = 0.10f;  
    public float missWindow = 0.15f;   

    [Header("軌道發光視覺設定")]
    // 放入場景中代表 4 個方向軌道背景的 SpriteRenderer (0=上, 1=下, 2=左, 3=右)
    public SpriteRenderer[] trackHighlights = new SpriteRenderer[4]; 
    public Color normalColor = new Color(1f, 1f, 1f, 0.1f); // 沒按時的顏色 (極度半透明)
    public Color highlightColor = new Color(1f, 1f, 1f, 0.6f); // 按下時的顏色 (明顯發亮)

    private List<NoteController>[] tracks = new List<NoteController>[8];
    private NoteController activeHoldJ = null;
    private NoteController activeHoldK = null;

    void Awake()
    {
        instance = this;
        for (int i = 0; i < 8; i++) tracks[i] = new List<NoteController>();
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

        // 1. 偵測左手搖桿目前的方向
        int currentDir = GetCurrentJoystickDirection();

        // 🌟 新增：更新軌道發光視覺
        UpdateTrackVisuals(currentDir);

        // 2. 處理右手紅圈軌道 (Lane 0) => 支援鍵盤 J 或 Arduino 的 "J"
        if (Input.GetKeyDown(KeyCode.J) || ArduinoSerialPOC.GetButtonDown("J")) 
            TryHitNote(currentDir, 0, ref activeHoldJ);
        else if ((Input.GetKeyUp(KeyCode.J) || ArduinoSerialPOC.GetButtonUp("J")) && activeHoldJ != null) 
            TryReleaseNote(ref activeHoldJ);

        // 3. 處理右手藍圈軌道 (Lane 1) => 支援鍵盤 K 或 Arduino 的 "K"
        if (Input.GetKeyDown(KeyCode.K) || ArduinoSerialPOC.GetButtonDown("K")) 
            TryHitNote(currentDir, 1, ref activeHoldK);
        else if ((Input.GetKeyUp(KeyCode.K) || ArduinoSerialPOC.GetButtonUp("K")) && activeHoldK != null) 
            TryReleaseNote(ref activeHoldK);
    }

    // 🌟 新增：控制 4 個方向的發光狀態
    void UpdateTrackVisuals(int currentDir)
    {
        for (int i = 0; i < 4; i++)
        {
            if (trackHighlights[i] != null)
            {
                // 如果目前推的方向等於這個軌道的編號，就換成發光色，否則變回暗色
                trackHighlights[i].color = (i == currentDir) ? highlightColor : normalColor;
            }
        }
    }

    int GetCurrentJoystickDirection()
    {
        // 1. 先讀取 Arduino 實體搖桿方向
        string serialDir = ArduinoSerialPOC.JoystickDirection;
        if (serialDir == "UP") return 0;
        if (serialDir == "DOWN") return 1;
        if (serialDir == "LEFT") return 2;
        if (serialDir == "RIGHT") return 3;

        // 2. 如果沒接 Arduino，保留鍵盤 WASD 作為備用測試
        if (Input.GetKey(KeyCode.W)) return 0; // 上
        if (Input.GetKey(KeyCode.S)) return 1; // 下
        if (Input.GetKey(KeyCode.A)) return 2; // 左
        if (Input.GetKey(KeyCode.D)) return 3; // 右
        return -1; 
    }
    void TryHitNote(int dir, int lane, ref NoteController holdRef)
    {
        if (dir == -1) return; 

        int trackIndex = dir * 2 + lane;
        if (tracks[trackIndex].Count == 0) return; 

        NoteController oldestNote = tracks[trackIndex][0];
        
        float currentTime = SongManager.instance.songPosition;
        float timeDiff = Mathf.Abs(currentTime - oldestNote.targetTime); 

        if (timeDiff <= missWindow) 
        {
            if (timeDiff <= perfectWindow) Debug.Log("🌟 Perfect!");
            else if (timeDiff <= greatWindow) Debug.Log("⭐ Great!");
            else Debug.Log("❌ Miss! (太早或太晚)");

            if (oldestNote.duration > 0)
            {
                holdRef = oldestNote;
                oldestNote.isBeingHeld = true; 
            }
            else
            {
                oldestNote.HitAndDestroy(); 
            }
        }
    }

    void TryReleaseNote(ref NoteController holdRef)
    {
        float currentTime = SongManager.instance.songPosition;
        float endTime = holdRef.targetTime + holdRef.duration; 
        float timeDiff = Mathf.Abs(currentTime - endTime);
        
        if (timeDiff <= greatWindow) Debug.Log("🌟 長按完美放開！");
        else Debug.Log("❌ 長按中斷 (Miss！)");
        
        holdRef.HitAndDestroy();
        holdRef = null; 
    }
}