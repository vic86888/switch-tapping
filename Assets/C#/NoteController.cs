using UnityEngine;

public class NoteController : MonoBehaviour
{
    [Header("長按音符專用 (單擊音符請留空)")]
    public Transform bodyTransform; 

    private Vector3 startPos;
    private Vector3 endPos;
    private float spawnTime;
    private float leadTime;
    private float duration;

    public void Initialize(Vector3 start, Vector3 end, float spawnTime, float leadTime, float duration, int direction)
    {
        this.startPos = start;
        this.endPos = end;
        this.spawnTime = spawnTime;
        this.leadTime = leadTime;
        this.duration = duration;

        // 轉向邏輯維持不變
        if (direction == 0) transform.rotation = Quaternion.Euler(0, 0, 0);   
        if (direction == 1) transform.rotation = Quaternion.Euler(0, 0, 180); 
        if (direction == 2) transform.rotation = Quaternion.Euler(0, 0, 90);  
        if (direction == 3) transform.rotation = Quaternion.Euler(0, 0, -90); 

        // 🌟 核心修正：在初始化的當下「立刻」計算一次正確的位置與大小
        // 這樣在 Unity 渲染出畫面的前一刻，它的外觀就已經是正確的縮小狀態，不會露出 Prefab 的大尺寸
        UpdateNoteVisual();
    }

    void Update()
    {
        if (leadTime == 0) return;

        // 每一幀持續更新視覺
        UpdateNoteVisual();
    }

    // 🌟 將視覺更新的邏輯獨立出來
    void UpdateNoteVisual()
    {
        float currentTime = SongManager.instance.songPosition;

        // ============================================
        // 模式 A：單擊音符 (duration == 0) 的邏輯
        // ============================================
        if (duration == 0)
        {
            float progress = (currentTime - spawnTime) / leadTime;
            transform.position = Vector3.Lerp(startPos, endPos, progress);

            if (progress > 1.2f) Destroy(gameObject);
            return; 
        }

        // ============================================
        // 模式 B：長按音符 (duration > 0) 的動態伸展邏輯
        // ============================================
        float headProgress = (currentTime - spawnTime) / leadTime;
        float tailProgress = (currentTime - (spawnTime + duration)) / leadTime;

        float visualHeadProgress = Mathf.Clamp01(headProgress);
        float visualTailProgress = Mathf.Max(0, tailProgress);

        if (visualTailProgress >= 1.0f)
        {
            Destroy(gameObject);
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
}