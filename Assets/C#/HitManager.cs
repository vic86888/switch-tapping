using System.Collections.Generic;
using UnityEngine;

public class HitManager : MonoBehaviour
{
    public static HitManager instance;

    [Header("判定區間 (秒)")]
    public float perfectWindow = 0.05f; // 誤差小於 0.05 秒算 Perfect
    public float greatWindow = 0.10f;  // 誤差小於 0.10 秒算 Great
    public float missWindow = 0.15f;   // 誤差小於 0.15 秒算 Miss，超過則不理會

    // 8 條軌道的音符清單 (演算法：direction * 2 + lane)
    // 這樣系統就能把上下左右、紅藍圈的音符分開排隊
    private List<NoteController>[] tracks = new List<NoteController>[8];

    // 紀錄右手目前 J 和 K 鍵「正在按住」的長按音符
    private NoteController activeHoldJ = null;
    private NoteController activeHoldK = null;

    void Awake()
    {
        instance = this;
        // 初始化 8 條軌道的排隊清單
        for (int i = 0; i < 8; i++) tracks[i] = new List<NoteController>();
    }

    // 當音符生成時，把它加入對應的軌道排隊
    public void RegisterNote(NoteController note, int direction, int lane)
    {
        tracks[direction * 2 + lane].Add(note);
    }

    // 當音符飛走(Miss)或被打掉時，從排隊清單中移除
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

        // 2. 處理右手 J 鍵 (紅圈軌道, Lane 0)
        if (Input.GetKeyDown(KeyCode.J)) 
            TryHitNote(currentDir, 0, ref activeHoldJ);
        else if (Input.GetKeyUp(KeyCode.J) && activeHoldJ != null) 
            TryReleaseNote(ref activeHoldJ);

        // 3. 處理右手 K 鍵 (藍圈軌道, Lane 1)
        if (Input.GetKeyDown(KeyCode.K)) 
            TryHitNote(currentDir, 1, ref activeHoldK);
        else if (Input.GetKeyUp(KeyCode.K) && activeHoldK != null) 
            TryReleaseNote(ref activeHoldK);
    }

    // 讀取左手 WASD 的輸入 (模擬實體搖桿)
    int GetCurrentJoystickDirection()
    {
        if (Input.GetKey(KeyCode.W)) return 0; // 上
        if (Input.GetKey(KeyCode.S)) return 1; // 下
        if (Input.GetKey(KeyCode.A)) return 2; // 左
        if (Input.GetKey(KeyCode.D)) return 3; // 右
        return -1; // 搖桿置中 (沒推)
    }

    // 嘗試打擊頭部或單擊音符
    void TryHitNote(int dir, int lane, ref NoteController holdRef)
    {
        string keyName = (lane == 0) ? "J" : "K";

        if (dir == -1) 
        {
            Debug.Log($"⚠️ 按下了 {keyName} 鍵，但左手沒有按住方向 (WASD)！");
            return; 
        }

        int trackIndex = dir * 2 + lane;
        if (tracks[trackIndex].Count == 0) 
        {
            Debug.Log($"💨 揮空！你按了方向 {dir} + {keyName} 鍵，但該軌道目前沒有音符。");
            return; 
        }

        // 抓出該軌道最靠近玩家的第一顆音符
        NoteController oldestNote = tracks[trackIndex][0];
        
        float currentTime = SongManager.instance.songPosition;
        float timeDiff = Mathf.Abs(currentTime - oldestNote.targetTime); 

        // 如果音符已經進入可打擊的有效判定區
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
        else
        {
            // 如果誤差大於 missWindow (例如提早了 0.5 秒按)
            Debug.Log($"太早或太晚按了！誤差: {timeDiff:F3} 秒 (必須小於 {missWindow} 秒)");
        }
    }

    // 嘗試鬆開長按音符的尾巴
    void TryReleaseNote(ref NoteController holdRef)
    {
        float currentTime = SongManager.instance.songPosition;
        float endTime = holdRef.targetTime + holdRef.duration; // 尾巴抵達的時間
        
        float timeDiff = Mathf.Abs(currentTime - endTime);
        
        if (timeDiff <= greatWindow) {
            Debug.Log("🌟 長按完美放開！");
        } else {
            Debug.Log("❌ 長按中斷 (Miss！)");
        }
        
        holdRef.HitAndDestroy();
        holdRef = null; // 清空佔用，準備接下一顆
    }
}