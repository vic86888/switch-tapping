using UnityEngine;

public class NoteController : MonoBehaviour
{
    [Header("長按音符專用")]
    public Transform bodyTransform; 

    private Vector3 startPos;
    private Vector3 endPos;
    private float spawnTime;
    private float leadTime;
    
    // 開放給 HitManager 讀取的公開變數
    public float duration;
    public int direction;
    public int lane;
    public float targetTime;  // 預計精準到達判定點的時間
    public bool isBeingHeld = false; // 是否正被玩家死死按著？

    // 🌟 注意這裡多加了 int lane 參數
    public void Initialize(Vector3 start, Vector3 end, float spawnTime, float leadTime, float duration, int direction, int lane)
    {
        this.startPos = start;
        this.endPos = end;
        this.spawnTime = spawnTime;
        this.leadTime = leadTime;
        this.duration = duration;
        
        // 🌟 補上變數儲存與計算目標時間
        this.direction = direction;
        this.lane = lane;
        this.targetTime = spawnTime + leadTime; // 算出玩家應該擊中的精準時間

        // 🌟 終極解法：刪除原本寫死的 direction 判斷，改用數學讓方塊「自動看向終點」
        Vector2 moveDirection = (end - start).normalized;
        if (moveDirection != Vector2.zero) 
        {
            float angle = Mathf.Atan2(moveDirection.y, moveDirection.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle - 90f);
        }

        // 🌟 最關鍵的一步：向 HitManager 報到，加入排隊清單！
        if (HitManager.instance != null)
        {
            HitManager.instance.RegisterNote(this, this.direction, this.lane);
        }

        // 立刻更新一次視覺，防止第一幀閃爍
        UpdateNoteVisual();
    }

    void Update()
    {
        if (leadTime == 0) return;
        UpdateNoteVisual();
    }

    void UpdateNoteVisual()
    {
        float currentTime = SongManager.instance.songPosition;

        if (duration <= 0) // 單擊音符
        {
            float progress = (currentTime - spawnTime) / leadTime;
            transform.position = Vector3.LerpUnclamped(startPos, endPos, progress);

            if (progress > 1.2f) 
            {
                // 🌟 改成呼叫 HitManager 斷 Combo，並顯示 Miss
                HitManager.instance.TriggerMiss(direction, lane);
                HitAndDestroy();
            }
            return; 
        }

        // 長按音符
        float headProgress = (currentTime - spawnTime) / leadTime;
        float tailProgress = (currentTime - (spawnTime + duration)) / leadTime;

        float visualHeadProgress = Mathf.Clamp01(headProgress);
        float visualTailProgress = Mathf.Max(0, tailProgress);

        if (visualTailProgress >= 1.0f)
        {
            if (!isBeingHeld) 
            {
                // 🌟 長按中途漏掉，斷 Combo
                HitManager.instance.TriggerMiss(direction, lane);
            }
            
            HitAndDestroy();
            return;
        }

        Vector3 currentHeadPos = Vector3.Lerp(startPos, endPos, visualHeadProgress);
        Vector3 currentTailPos = Vector3.Lerp(startPos, endPos, visualTailProgress);

        transform.position = currentHeadPos;

        if (bodyTransform != null)
        {
            float currentLength = Vector3.Distance(currentHeadPos, currentTailPos);
            bodyTransform.localScale = new Vector3(bodyTransform.localScale.x, currentLength, 1);
            bodyTransform.localPosition = new Vector3(0, -currentLength / 2f, 0);
        }
    }

    // 被 HitManager 打中或飛過頭銷毀時，呼叫此方法
    public void HitAndDestroy()
    {
        if (HitManager.instance != null)
        {
            HitManager.instance.RemoveNote(this, direction, lane);
        }
        Destroy(gameObject);
    }
}