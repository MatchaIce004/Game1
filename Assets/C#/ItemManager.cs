using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ItemManager : MonoBehaviour
{
    public static ItemManager Instance;

    // ★追加：型安全な階段種別
    public enum StairKind { None, Entrance, Mid, Escape }

    public List<Vector2IntSerializable> midStairCells = new List<Vector2IntSerializable>();

    // ラン中にお宝を所持しているか（出口でクリア判定に使う）
    public bool runHasTreasure = false;

    // 入場時に1回だけ回復する
    public bool healOnNextDungeonEnter = false;

    [Header("Floor Progress")]
    public int currentFloor = 1;
    public int maxFloor = 3;

    [Header("Database")]
    public ItemDatabase database;

    [Header("Treasure State")]
    public bool treasurePicked = false;

    [Header("Stairs (runtime)")]
    public Vector2Int lastStairCell = Vector2Int.zero;
    public bool hasLastStairCell = false;

    public List<Vector2IntSerializable> entranceStairCells = new List<Vector2IntSerializable>();
    public List<Vector2IntSerializable> escapeStairCells = new List<Vector2IntSerializable>();

    // ★変更：string → enum
    public StairKind lastStairKind = StairKind.None;

    [Serializable]
    public class FloorSeedData
    {
        public int floor;
        public int seed;
    }

    [Serializable]
    public class FloorOpenedChestsData
    {
        public int floor;
        public List<Vector2IntSerializable> openedChests = new List<Vector2IntSerializable>();
    }

    [Serializable]
    public class Vector2IntSerializable
    {
        public int x;
        public int y;
        public Vector2IntSerializable(int x, int y) { this.x = x; this.y = y; }
        public Vector2IntSerializable(Vector2Int v) { x = v.x; y = v.y; }
        public Vector2Int ToV2() => new Vector2Int(x, y);
    }

    // floor -> seed
    private Dictionary<int, int> floorSeedMap = new Dictionary<int, int>();

    // floor -> opened chest cells
    private Dictionary<int, HashSet<Vector2Int>> openedChestsMap = new Dictionary<int, HashSet<Vector2Int>>();

    // =========================
    // 永続：図鑑（絶対消えない）
    // =========================
    public HashSet<string> encyclopediaUnlockedIds = new HashSet<string>();

    // =========================
    // 進行：所持（3回目死亡で消える）
    // =========================
    public List<ItemData> ownedPermanent = new List<ItemData>();

    // =========================
    // ラン：持ち込み／保留Loot
    // =========================
    public List<ItemData> runLoadout = new List<ItemData>();
    public List<ItemData> runPendingLoot = new List<ItemData>();

    public int deathCount = 0;

    [Header("Loadout Candidates (per run)")]
    public int poolSize = 20;
    public List<string> runCandidateIds = new List<string>();

    [Header("Run Discovered (reveal in loadout)")]
    public HashSet<string> runDiscoveredIds = new HashSet<string>();

    [Header("Chest Drop Pool (unique per run)")]
    public List<string> remainingChestDropIds = new List<string>();

    [Header("Starting Bonus (unlock + owned)")]
    public List<string> startingOwnedIds = new List<string>();
    public List<string> startingUnlockOnlyIds = new List<string>();
    public bool applyStartingBonusOnce = true;
    private const string StartingBonusAppliedKey = "StartingBonusApplied_v1";

    public bool hasRunSave = false;
    public RunSaveData runSave = new RunSaveData();

    [Serializable]
    public class RunSaveData
    {
        public string sceneName = "GameScene";
        public float timer = 0f;
        public int deathCount = 0;
        public bool treasurePicked = false;
        public bool healOnNextDungeonEnter = false;
        public bool hasLastStairCell = false;
        public Vector2IntSerializable lastStairCell = new Vector2IntSerializable(0, 0);

        public StairKind lastStairKind = StairKind.None;

        public List<Vector2IntSerializable> midStairCells = new List<Vector2IntSerializable>();
        public List<string> runLoadoutIds = new List<string>();
        public List<string> runPendingLootIds = new List<string>();

        public List<Vector2IntSerializable> entranceStairCells = new List<Vector2IntSerializable>();
        public List<Vector2IntSerializable> escapeStairCells = new List<Vector2IntSerializable>();

        public List<string> runCandidateIds = new List<string>();
        public List<string> runDiscoveredIds = new List<string>();
        public List<string> remainingChestDropIds = new List<string>();

        public List<FloorSeedData> floorSeeds = new List<FloorSeedData>();
        public List<FloorOpenedChestsData> openedChestsByFloor = new List<FloorOpenedChestsData>();

        public int currentFloor = 1;

        public int seed = 0;
        public bool runHasTreasure = false;
    }

    [Serializable]
    public class EncyclopediaSave
    {
        public List<string> unlockedIds = new List<string>();
    }

    [Serializable]
    public class ProgressSave
    {
        public List<string> ownedIds = new List<string>();
        public int deathCount = 0;
    }

    private string EncyclopediaPath => Path.Combine(Application.persistentDataPath, "encyclopedia.json");
    private string ProgressPath => Path.Combine(Application.persistentDataPath, "progress.json");
    private string RunPath => Path.Combine(Application.persistentDataPath, "run.json");

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            LoadEncyclopedia();
            LoadProgress();
            LoadRun();

            ApplyStartingBonus(force: false);

            if (runCandidateIds == null || runCandidateIds.Count == 0)
                BuildRunCandidates(poolSize);

            if (remainingChestDropIds == null || remainingChestDropIds.Count == 0)
                BuildChestDropPoolFromCandidates();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // ★追加：セル→階段種別判定（Tilemap依存を外す）
    public StairKind GetStairKindAtCell(Vector2Int cell)
    {
        if (entranceStairCells != null && entranceStairCells.Any(v => v.x == cell.x && v.y == cell.y))
            return StairKind.Entrance;

        if (midStairCells != null && midStairCells.Any(v => v.x == cell.x && v.y == cell.y))
            return StairKind.Mid;

        if (escapeStairCells != null && escapeStairCells.Any(v => v.x == cell.x && v.y == cell.y))
            return StairKind.Escape;

        return StairKind.None;
    }

    public ItemData FindByID(string id)
    {
        if (database == null || database.allItems == null) return null;
        return database.allItems.FirstOrDefault(i => i != null && i.id == id);
    }

    void ApplyStartingBonus(bool force)
    {
        if (database == null || database.allItems == null) return;

        if (!force && applyStartingBonusOnce && PlayerPrefs.GetInt(StartingBonusAppliedKey, 0) == 1)
            return;

        foreach (var id in startingUnlockOnlyIds)
        {
            if (string.IsNullOrEmpty(id)) continue;
            encyclopediaUnlockedIds.Add(id);
        }

        foreach (var id in startingOwnedIds)
        {
            if (string.IsNullOrEmpty(id)) continue;

            var item = FindByID(id);
            if (item == null) continue;

            encyclopediaUnlockedIds.Add(item.id);

            if (!ownedPermanent.Any(x => x != null && x.id == item.id))
                ownedPermanent.Add(item);
        }

        SaveEncyclopedia();
        SaveProgress();

        if (!force && applyStartingBonusOnce)
        {
            PlayerPrefs.SetInt(StartingBonusAppliedKey, 1);
            PlayerPrefs.Save();
        }
    }

    public void UnlockEncyclopedia(ItemData item)
    {
        if (item == null) return;
        if (encyclopediaUnlockedIds.Add(item.id))
            SaveEncyclopedia();
    }

    public void AddPermanent(ItemData item)
    {
        if (item == null) return;

        encyclopediaUnlockedIds.Add(item.id);

        if (!ownedPermanent.Any(x => x != null && x.id == item.id))
        {
            ownedPermanent.Add(item);
            SaveProgress();
            SaveEncyclopedia();
        }
    }

    public bool HasItemAnywhere(string id)
    {
        if (string.IsNullOrEmpty(id)) return false;

        bool inOwned = ownedPermanent.Any(x => x != null && x.id == id);
        bool inLoadout = runLoadout.Any(x => x != null && x.id == id);
        bool inPending = runPendingLoot.Any(x => x != null && x.id == id);

        return inOwned || inLoadout || inPending;
    }

    public void AddPendingLoot(ItemData item)
    {
        if (item == null) return;

        UnlockEncyclopedia(item);
        runDiscoveredIds.Add(item.id);

        runPendingLoot.Add(item);
        SaveRun();
    }

    public void OnEscapeSuccess()
    {
        foreach (var it in runPendingLoot)
            AddPermanent(it);

        foreach (var it in runLoadout)
        {
            if (it == null) continue;
            if (!ownedPermanent.Any(x => x != null && x.id == it.id))
                ownedPermanent.Add(it);
        }

        runLoadout.Clear();
        runPendingLoot.Clear();
        runHasTreasure = false;

        SaveProgress();
        ClearRunSave();
    }

    public void OnPlayerDied()
    {
        runHasTreasure = false;
        deathCount++;

        if (GameTimer.Instance != null) GameTimer.Instance.StopTimer();

        if (deathCount < 3)
        {
            runLoadout.Clear();
            runPendingLoot.Clear();

            ClearRunSave();
            SaveProgress();

            SceneManager.LoadScene("LoadoutScene");
            return;
        }

        FullResetProgressKeepEncyclopedia();
        SceneManager.LoadScene("TitleScene");
    }

    public void FullResetProgressKeepEncyclopedia()
    {
        ownedPermanent.Clear();
        runLoadout.Clear();
        runPendingLoot.Clear();
        runHasTreasure = false;

        deathCount = 0;
        currentFloor = 1;

        runCandidateIds.Clear();
        runDiscoveredIds.Clear();
        remainingChestDropIds.Clear();

        ClearRunSave();

        if (File.Exists(ProgressPath)) File.Delete(ProgressPath);
        if (File.Exists(RunPath)) File.Delete(RunPath);

        ApplyStartingBonus(force: true);

        SaveProgress();
        SaveEncyclopedia();
        hasLastStairCell = false;
        lastStairCell = Vector2Int.zero;
        entranceStairCells.Clear();
    }

    public void NewGame()
    {
        FullResetProgressKeepEncyclopedia();

        treasurePicked = false;
        runHasTreasure = false;
        hasLastStairCell = false;
        lastStairCell = Vector2Int.zero;
        entranceStairCells.Clear();

        currentFloor = 1;

        InitializeFloorSeeds();

        if (openedChestsMap == null) openedChestsMap = new Dictionary<int, HashSet<Vector2Int>>();
        openedChestsMap.Clear();

        BuildRunCandidates(poolSize);
        BuildChestDropPoolFromCandidates();

        SaveProgress();
        SaveRun();

        SceneManager.LoadScene("LoadoutScene");
    }

    public bool CanContinue()
    {
        return hasRunSave && File.Exists(RunPath);
    }

    public void ContinueGame()
    {
        if (!CanContinue())
        {
            Debug.LogWarning("続きからデータがありません。");
            SceneManager.LoadScene("TitleScene");
            return;
        }

        LoadRun();
        LoadProgress();

        if (runCandidateIds == null || runCandidateIds.Count == 0)
            BuildRunCandidates(poolSize);

        if (remainingChestDropIds == null || remainingChestDropIds.Count == 0)
            BuildChestDropPoolFromCandidates();

        SaveRun();
        SceneManager.LoadScene("LoadoutScene");
    }

    public void BuildRunCandidates(int totalCount)
    {
        if (database == null || database.allItems == null)
        {
            Debug.LogWarning("BuildRunCandidates: database が未設定です");
            return;
        }

        var allIds = database.allItems
            .Where(x => x != null && !string.IsNullOrEmpty(x.id))
            .Select(x => x.id)
            .Distinct()
            .ToList();

        if (allIds.Count == 0) return;

        int target = Mathf.Min(totalCount, allIds.Count);

        runCandidateIds = allIds
            .OrderBy(_ => UnityEngine.Random.value)
            .Take(target)
            .ToList();

        runDiscoveredIds.Clear();
    }

    public void BuildChestDropPoolFromCandidates()
    {
        remainingChestDropIds = (runCandidateIds ?? new List<string>())
            .Where(id => !string.IsNullOrEmpty(id))
            .Distinct()
            .ToList();
    }

    public void SaveEncyclopedia()
    {
        var data = new EncyclopediaSave { unlockedIds = encyclopediaUnlockedIds.ToList() };
        File.WriteAllText(EncyclopediaPath, JsonUtility.ToJson(data, true));
    }

    public void LoadEncyclopedia()
    {
        encyclopediaUnlockedIds.Clear();
        if (!File.Exists(EncyclopediaPath)) return;

        var json = File.ReadAllText(EncyclopediaPath);
        var data = JsonUtility.FromJson<EncyclopediaSave>(json);
        if (data?.unlockedIds == null) return;

        foreach (var id in data.unlockedIds)
            encyclopediaUnlockedIds.Add(id);
    }

    public void SaveProgress()
    {
        var data = new ProgressSave
        {
            ownedIds = ownedPermanent.Where(x => x != null).Select(x => x.id).ToList(),
            deathCount = deathCount
        };

        File.WriteAllText(ProgressPath, JsonUtility.ToJson(data, true));
    }

    public void LoadProgress()
    {
        ownedPermanent.Clear();
        deathCount = 0;

        if (!File.Exists(ProgressPath)) return;

        var json = File.ReadAllText(ProgressPath);
        var data = JsonUtility.FromJson<ProgressSave>(json);
        if (data == null) return;

        deathCount = data.deathCount;

        if (data.ownedIds != null)
        {
            foreach (var id in data.ownedIds)
            {
                var item = FindByID(id);
                if (item != null) ownedPermanent.Add(item);
            }
        }
    }

    public void SaveRun(string currentSceneName = null, int seed = 0)
    {
        if (!hasRunSave)
        {
            hasRunSave = true;
            runSave = new RunSaveData();
        }

        if (!string.IsNullOrEmpty(currentSceneName))
            runSave.sceneName = currentSceneName;

        runSave.deathCount = deathCount;
        runSave.timer = GameTimer.Instance != null ? GameTimer.Instance.GetTime() : runSave.timer;

        runSave.currentFloor = currentFloor;

        runSave.hasLastStairCell = hasLastStairCell;
        runSave.lastStairCell = new Vector2IntSerializable(lastStairCell);
        runSave.lastStairKind = lastStairKind;

        runSave.entranceStairCells = entranceStairCells ?? new List<Vector2IntSerializable>();
        runSave.escapeStairCells = escapeStairCells ?? new List<Vector2IntSerializable>();
        runSave.midStairCells = midStairCells ?? new List<Vector2IntSerializable>();

        runSave.runLoadoutIds = runLoadout.Where(x => x != null).Select(x => x.id).ToList();
        runSave.runPendingLootIds = runPendingLoot.Where(x => x != null).Select(x => x.id).ToList();

        runSave.runCandidateIds = (runCandidateIds ?? new List<string>()).ToList();
        runSave.runDiscoveredIds = (runDiscoveredIds ?? new HashSet<string>()).ToList();
        runSave.remainingChestDropIds = (remainingChestDropIds ?? new List<string>()).ToList();

        runSave.runHasTreasure = runHasTreasure;
        runSave.treasurePicked = treasurePicked;
        runSave.healOnNextDungeonEnter = healOnNextDungeonEnter;

        // seed（run全体seed）
        if (seed != 0) runSave.seed = seed;

        // floorSeedMap 保存
        runSave.floorSeeds = new List<FloorSeedData>();
        foreach (var kv in floorSeedMap)
            runSave.floorSeeds.Add(new FloorSeedData { floor = kv.Key, seed = kv.Value });

        // openedChestsMap 保存
        runSave.openedChestsByFloor = new List<FloorOpenedChestsData>();
        foreach (var kv in openedChestsMap)
        {
            var d = new FloorOpenedChestsData { floor = kv.Key };
            d.openedChests = kv.Value.Select(v => new Vector2IntSerializable(v)).ToList();
            runSave.openedChestsByFloor.Add(d);
        }

        File.WriteAllText(RunPath, JsonUtility.ToJson(runSave, true));
    }

    public void LoadRun()
    {
        hasRunSave = false;
        runSave = new RunSaveData();

        runLoadout.Clear();
        runPendingLoot.Clear();

        runCandidateIds = new List<string>();
        remainingChestDropIds = new List<string>();
        runDiscoveredIds = new HashSet<string>();

        if (!File.Exists(RunPath)) return;

        var json = File.ReadAllText(RunPath);
        var data = JsonUtility.FromJson<RunSaveData>(json);
        if (data == null) return;

        hasRunSave = true;
        runSave = data;

        currentFloor = data.currentFloor <= 0 ? 1 : data.currentFloor;

        runHasTreasure = data.runHasTreasure;
        treasurePicked = data.treasurePicked;
        healOnNextDungeonEnter = data.healOnNextDungeonEnter;

        hasLastStairCell = data.hasLastStairCell;
        lastStairCell = data.lastStairCell != null ? data.lastStairCell.ToV2() : Vector2Int.zero;
        lastStairKind = data.lastStairKind;

        entranceStairCells = data.entranceStairCells ?? new List<Vector2IntSerializable>();
        escapeStairCells = data.escapeStairCells ?? new List<Vector2IntSerializable>();
        midStairCells = data.midStairCells ?? new List<Vector2IntSerializable>();

        runCandidateIds = data.runCandidateIds ?? new List<string>();
        remainingChestDropIds = data.remainingChestDropIds ?? new List<string>();
        runDiscoveredIds = new HashSet<string>(data.runDiscoveredIds ?? new List<string>());

        foreach (var id in data.runLoadoutIds ?? new List<string>())
        {
            var item = FindByID(id);
            if (item != null) runLoadout.Add(item);
        }

        foreach (var id in data.runPendingLootIds ?? new List<string>())
        {
            var item = FindByID(id);
            if (item != null) runPendingLoot.Add(item);
        }

        deathCount = data.deathCount;

        // seed復元
        floorSeedMap = new Dictionary<int, int>();
        if (data.floorSeeds != null)
        {
            foreach (var fs in data.floorSeeds)
                floorSeedMap[fs.floor] = fs.seed;
        }

        // opened復元
        openedChestsMap = new Dictionary<int, HashSet<Vector2Int>>();
        if (data.openedChestsByFloor != null)
        {
            foreach (var od in data.openedChestsByFloor)
            {
                var set = new HashSet<Vector2Int>();
                if (od.openedChests != null)
                    foreach (var v in od.openedChests)
                        set.Add(v.ToV2());
                openedChestsMap[od.floor] = set;
            }
        }
    }

    public void ClearRunSave()
    {
        hasRunSave = false;
        runHasTreasure = false;
        treasurePicked = false;
        healOnNextDungeonEnter = false;
        hasLastStairCell = false;
        lastStairCell = Vector2Int.zero;
        lastStairKind = StairKind.None;

        entranceStairCells.Clear();
        escapeStairCells.Clear();
        midStairCells.Clear();

        runSave = new RunSaveData();
        if (File.Exists(RunPath)) File.Delete(RunPath);

        runLoadout.Clear();
        runPendingLoot.Clear();

        currentFloor = 1;

        if (floorSeedMap != null) floorSeedMap.Clear();
        if (openedChestsMap != null) openedChestsMap.Clear();

        runDiscoveredIds.Clear();
    }

    public ItemData DrawFromChest(ItemRarity chestRarity)
    {
        if (remainingChestDropIds == null || remainingChestDropIds.Count == 0)
            BuildChestDropPoolFromCandidates();

        if (remainingChestDropIds == null || remainingChestDropIds.Count == 0)
            return null;

        var eligibleIds = remainingChestDropIds
            .Select(id => FindByID(id))
            .Where(item => item != null && item.rarity >= chestRarity)
            .Select(item => item.id)
            .ToList();

        if (eligibleIds.Count == 0)
            return null;

        string pickedId = eligibleIds[UnityEngine.Random.Range(0, eligibleIds.Count)];
        remainingChestDropIds.RemoveAll(x => x == pickedId);

        if (HasItemAnywhere(pickedId))
            return null;

        return FindByID(pickedId);
    }

    public void GoNextFloor()
    {
        currentFloor++;
        runHasTreasure = false;
        SceneManager.LoadScene("GameScene");
    }

    public int GetFloorSeed(int floor)
    {
        if (floorSeedMap == null) floorSeedMap = new Dictionary<int, int>();

        if (!floorSeedMap.TryGetValue(floor, out int seed))
        {
            seed = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
            floorSeedMap[floor] = seed;
        }
        return seed;
    }

    public void InitializeFloorSeeds()
    {
        if (floorSeedMap == null) floorSeedMap = new Dictionary<int, int>();
        floorSeedMap.Clear();

        for (int f = 1; f <= maxFloor; f++)
            floorSeedMap[f] = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
    }

    private HashSet<Vector2Int> GetOpenedSet(int floor)
    {
        if (openedChestsMap == null) openedChestsMap = new Dictionary<int, HashSet<Vector2Int>>();

        if (!openedChestsMap.TryGetValue(floor, out var set))
        {
            set = new HashSet<Vector2Int>();
            openedChestsMap[floor] = set;
        }
        return set;
    }

    public void MarkChestOpened(Vector2Int cell)
    {
        GetOpenedSet(currentFloor).Add(cell);
        SaveRun();
    }

    public bool IsChestOpened(Vector2Int cell)
    {
        return GetOpenedSet(currentFloor).Contains(cell);
    }

    public List<Vector2Int> GetOpenedChestsForCurrentFloor()
    {
        return GetOpenedSet(currentFloor).ToList();
    }

    public void ChangeFloor(int delta)
    {
        int next = Mathf.Clamp(currentFloor + delta, 1, maxFloor);
        if (next == currentFloor) return;

        currentFloor = next;

        SaveRun();
        SceneManager.LoadScene("GameScene");
    }
}