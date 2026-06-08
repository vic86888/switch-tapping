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
    public float targetTime;  
    public bool isBeingHeld = false; 

    // 🌟 新增：專門用來記錄長按音符狀態的變數
    public bool headHit = false;         // 頭部有沒有按到？
    public bool headPerfect = false;     // 頭部是不是 Perfect？
    public bool wasReleasedEarly = false;// 中途是不是有提早放手？

    public void Initialize(Vector3 start, Vector3 end, float spawnTime, float leadTime, float duration, int direction, int lane)
    {
        this.startPos = start;
        this.endPos = end;
        this.spawnTime = spawnTime;
        this.leadTime = leadTime;
        this.duration = duration;
        
        this.direction = direction;
        this.lane = lane;
        this.targetTime = spawnTime + leadTime; 

        Vector2 moveDirection = (end - start).normalized;
        if (moveDirection != Vector2.zero) 
        {
            float angle = Mathf.Atan2(moveDirection.y, moveDirection.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle - 90f);
        }

        if (HitManager.instance != null)
        {
            HitManager.instance.RegisterNote(this, this.direction, this.lane);
        }

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

        if (duration <= 0) // 單擊音符 (維持原樣)
        {
            float progress = (currentTime - spawnTime) / leadTime;
            transform.position = Vector3.LerpUnclamped(startPos, endPos, progress);

            if (progress > 1.2f) 
            {
                HitManager.instance.TriggerMiss(direction, lane);
                HitAndDestroy();
            }
            return; 
        }

        // ============================================
        // 🌟 長按音符視覺與「尾端結算」邏輯
        // ============================================
        float headProgress = (currentTime - spawnTime) / leadTime;
        float tailProgress = (currentTime - (spawnTime + duration)) / leadTime;

        float visualHeadProgress = Mathf.Clamp01(headProgress);
        float visualTailProgress = Mathf.Max(0, tailProgress);

        // 當尾巴完全抵達終點 (1.0f) 時，呼叫 HitManager 進行一氣呵成的結算！
        if (visualTailProgress >= 1.0f)
        {
            if (HitManager.instance != null)
            {
                HitManager.instance.ResolveHoldNote(this);
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

    public void HitAndDestroy()
    {
        if (HitManager.instance != null)
        {
            HitManager.instance.RemoveNote(this, direction, lane);
        }
        Destroy(gameObject);
    }
}