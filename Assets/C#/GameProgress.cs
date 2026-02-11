using System;
using System.IO;
using UnityEngine;

public static class GameProgress
{
    [Serializable]
    public class Progress
    {
        // ===== 追加ステータス =====

        public int clearCount = 0;        // クリア回数
        public int totalTreasure = 0;     // お宝取得総数（＝クリア回数でもOK）
        public float bestTime = 0f;       // 最速クリアタイム（0は未記録）
        public bool hasClearedOnce = false; // 初クリア済みフラグ（タイトル演出用など）

        // 将来ここに自由に追加できる
        // public int totalDeaths;
        // public int totalRuns;
    }

    private static string PathFile =>
        Path.Combine(Application.persistentDataPath, "goal.json");

    // =========================
    // Load
    // =========================
    public static Progress Load()
    {
        if (!File.Exists(PathFile))
            return new Progress();

        return JsonUtility.FromJson<Progress>(File.ReadAllText(PathFile)) ?? new Progress();
    }

    // =========================
    // Save
    // =========================
    public static void Save(Progress p)
    {
        File.WriteAllText(PathFile, JsonUtility.ToJson(p, true));
    }

    // =========================
    // クリア時に呼ぶ（←これが便利ポイント）
    // =========================
    public static void RecordClear(float clearTime)
    {
        var p = Load();

        p.clearCount++;
        p.totalTreasure++;
        p.hasClearedOnce = true;

        if (p.bestTime <= 0f || clearTime < p.bestTime)
            p.bestTime = clearTime;

        Save(p);

        Debug.Log($"[CLEAR SAVE] count={p.clearCount} best={p.bestTime:F2}");
    }

    // =========================
    // 全消去（はじめから）
    // =========================
    public static void Clear()
    {
        if (File.Exists(PathFile))
            File.Delete(PathFile);
    }
}
