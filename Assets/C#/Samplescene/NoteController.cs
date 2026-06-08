using UnityEngine;

public class NoteController : MonoBehaviour
{
    [Header("長按音符專用")]
    public Transform bodyTransform; 
    public SpriteRenderer headRenderer; // 🌟 記得在 Prefab 綁定頭部
    public SpriteRenderer bodyRenderer; // 🌟 記得在 Prefab 綁定身體

    // 🌟 新增這個變數，讓你可以自由微調 Body 的前後位置
    public float bodyOffset = 0f;

    private Vector3 startPos;
    private Vector3 endPos;
    private float spawnTime;
    private float leadTime;
    
    public float duration;
    public int direction;
    public int lane;
    public float targetTime;  
    public bool isBeingHeld = false; 

    // 長按音符狀態追蹤
    public bool headHit = false;         
    public bool headPerfect = false;     
    public bool wasReleasedEarly = false;
    public bool isGray = false; // 🌟 紀錄是否已經變成灰色幽靈狀態

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
        // 🌟 長按音符視覺邏輯
        // ============================================
        float headProgress = (currentTime - spawnTime) / leadTime;
        float tailProgress = (currentTime - (spawnTime + duration)) / leadTime;

        float visualHeadProgress = Mathf.Clamp01(headProgress);
        float visualTailProgress = Mathf.Max(0, tailProgress);

        // 🌟 情況 2：如果起手徹底漏打 (超過 Miss 判定時間且沒按到頭) -> 直接變灰
        if (!headHit && !isGray && (currentTime - targetTime > HitManager.instance.missWindow))
        {
            TurnGray();
        }

        // 當尾巴完全抵達終點 (1.0f) 時，結算！
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
            // 🌟 在 Y 軸的計算最後，加上 bodyOffset
            bodyTransform.localPosition = new Vector3(0, -currentLength / 2f + bodyOffset, 0);
        }
    }

    // 🌟 新增：讓音符變成灰暗的幽靈狀態
    public void TurnGray()
    {
        if (isGray) return;
        isGray = true;
        
        Color grayColor = new Color(0.4f, 0.4f, 0.4f, 0.6f); // 變成暗灰色且半透明
        if (headRenderer != null) headRenderer.color = grayColor;
        if (bodyRenderer != null) bodyRenderer.color = grayColor;
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