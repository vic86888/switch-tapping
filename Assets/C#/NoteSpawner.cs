using System.Collections.Generic;
using UnityEngine;

public class NoteSpawner : MonoBehaviour
{
    [Header("譜面與時間設定")]
    public TextAsset beatmapJson;
    public float leadTime = 2.0f; // 音符提早幾秒生成

    [Header("軌道終點設定 (請將場景中的 Lane 物件拖入)")]
    public Transform upRed;   public Transform upBlue;
    public Transform downRed; public Transform downBlue;
    public Transform leftRed; public Transform leftBlue;
    public Transform rightRed;public Transform rightBlue;

    [Header("單擊音符預製體")]
    public GameObject noteRedPrefab;
    public GameObject noteBluePrefab;

    [Header("長按音符預製體 (新增這兩行)")]
    public GameObject holdRedPrefab;
    public GameObject holdBluePrefab;

    private BeatmapData currentBeatmap;
    private Queue<NoteData> noteQueue;

    void Start()
    {
        if (beatmapJson != null)
        {
            currentBeatmap = JsonUtility.FromJson<BeatmapData>(beatmapJson.text);
            noteQueue = new Queue<NoteData>(currentBeatmap.notes);
        }
    }

    void Update()
    {
        if (SongManager.instance == null || noteQueue == null || noteQueue.Count == 0) return;

        float audioTime = SongManager.instance.songPosition;
        float spawnTime = noteQueue.Peek().time - leadTime;

        if (audioTime >= spawnTime)
        {
            NoteData nextNote = noteQueue.Dequeue();
            SpawnNote(nextNote, spawnTime);
        }
    }

    void SpawnNote(NoteData data, float spawnTime)
    {
        // 1. 判斷要生成「單擊」還是「長按」的 Prefab
        GameObject prefabToSpawn;
        if (data.duration > 0) {
            // 是長按音符
            prefabToSpawn = (data.lane == 0) ? holdRedPrefab : holdBluePrefab;
        } else {
            // 是單擊音符
            prefabToSpawn = (data.lane == 0) ? noteRedPrefab : noteBluePrefab;
        }
        
        Transform targetPoint = GetTargetPoint(data.direction, data.lane);

        if (targetPoint != null)
        {
            Vector3 spawnPos = transform.position; 

            // 計算平行的出生點
            if (data.direction == 0 || data.direction == 1) 
                spawnPos = new Vector3(targetPoint.position.x, transform.position.y, transform.position.z);
            else if (data.direction == 2 || data.direction == 3) 
                spawnPos = new Vector3(transform.position.x, targetPoint.position.y, transform.position.z);

            GameObject newNote = Instantiate(prefabToSpawn, spawnPos, Quaternion.identity);

            // --- 👇 新增/修改的部分從這裡開始 👇 ---
            
            // 預設目標點等於原本軌道的目標點
            Vector3 finalTargetPos = targetPoint.position;

            // 如果是單擊音符 (duration 為 0)
            if (data.duration <= 0) 
            {
                // 計算方塊的移動方向向量 (目標點 減掉 出生點) 並單位化 (長度變為1)
                Vector3 moveDirection = (targetPoint.position - spawnPos).normalized;
                
                // 把最終目標點，沿著移動方向往回退 0.25 格
                finalTargetPos -= moveDirection * 0.25f;
            }

            // 4. 交接資料給 NoteController 
            // ⚠️ 這裡要注意：把原本的 targetPoint.position 改傳入我們算好的 finalTargetPos
            NoteController controller = newNote.GetComponent<NoteController>();
            controller.Initialize(spawnPos, finalTargetPos, spawnTime, leadTime, data.duration, data.direction, data.lane);
            
            // --- 👆 新增/修改的部分到這裡結束 👆 ---
        }
    }

    // 輔助方法：根據 direction 和 lane 找出對應的終點 Transform
    Transform GetTargetPoint(int direction, int lane)
    {
        if (direction == 0) return (lane == 0) ? upRed : upBlue;       // 上 (0)
        if (direction == 1) return (lane == 0) ? downRed : downBlue;   // 下 (1)
        if (direction == 2) return (lane == 0) ? leftRed : leftBlue;   // 左 (2)
        if (direction == 3) return (lane == 0) ? rightRed : rightBlue; // 右 (3)
        return null;
    }
}