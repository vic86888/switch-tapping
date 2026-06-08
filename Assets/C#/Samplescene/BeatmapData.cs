using System.Collections.Generic;

[System.Serializable]
public class NoteData 
{
    public float time;       // 音符打擊時間
    public int direction;    // 方向 (0=上, 1=下, 2=左, 3=右)
    public int lane;         // 軌道 (0=紅圈, 1=藍圈)
    
    // 🌟 新增這行：持續時間。如果是 0 代表單擊音符，大於 0 就是長按音符
    public float duration;   
}

[System.Serializable]
public class BeatmapData 
{
    public string songName;      
    public float bpm;            
    public List<NoteData> notes; 
}