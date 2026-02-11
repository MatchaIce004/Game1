using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

public class CaveGenerator : MonoBehaviour
{
    [Header("Tilemaps")]
    public Tilemap wallTilemap;
    public Tilemap groundTilemap;

    [Header("Tiles")]
    public Tile wallTile;
    public Tile floorTile;

    [Header("Chest Settings")]
    public Tilemap chestTilemap;

    public Tile commonChestTile;
    public Tile uncommonChestTile;
    public Tile rareChestTile;
    public Tile epicChestTile;
    public Tile legendaryChestTile;

    public Dictionary<Vector2Int, ItemRarity> chestRarityByCell = new Dictionary<Vector2Int, ItemRarity>();

    [Header("Enemy Settings")]
    public GameObject enemyPrefab;
    public int enemyCount = 10;

    [Header("Map Settings")]
    public int width = 64;
    public int height = 64;
    [Range(0, 100)] public int fillPercent = 55;
    public int chestCount = 3;

    int[,] map;

    [Header("Seed")]
    public bool useRunSeed = true;
    public int floorSeedStep = 10007;

    [Header("Stairs Settings")]
    public Tilemap stairTilemap;
    public Tile stairUpTile;
    public Tile stairDownTile;

    [Header("Stairs Count")]
    [Range(1, 5)] public int entranceStairCount = 2;
    [Range(1, 5)] public int midStairCount = 2;
    [Range(1, 5)] public int escapeStairCount = 1;

    [Header("Mid Stairs (2<->3 shared position)")]
    public bool useMidStair = true;

    [Header("Player Spawn")]
    public Vector2Int spawnOffsetFromStair = new Vector2Int(1, 0);

    [Header("Treasure (Floor 3 only)")]
    public Tilemap treasureTilemap;
    public Tile treasureTile;

    Vector2Int lastSpawnStairCell;

    // ★追加：階段配置責務を分離
    StairPlacer stairPlacer;

    [Serializable]
    public class ChestRarityWeight
    {
        public ItemRarity rarity = ItemRarity.Common;
        [Min(0f)] public float weight = 1f;
    }

    [Header("Chest Rarity Weights")]
    public List<ChestRarityWeight> chestRarityWeights = new List<ChestRarityWeight>()
    {
        new ChestRarityWeight{ rarity = ItemRarity.Common, weight = 60f },
        new ChestRarityWeight{ rarity = ItemRarity.Uncommon, weight = 25f },
        new ChestRarityWeight{ rarity = ItemRarity.Rare, weight = 10f },
        new ChestRarityWeight{ rarity = ItemRarity.Epic, weight = 4f },
        new ChestRarityWeight{ rarity = ItemRarity.Legendary, weight = 1f },
    };

    void Start()
    {
        Generate();
    }

    void Generate()
    {
        ResetGenerationState();

        int floor = ItemManager.Instance != null ? ItemManager.Instance.currentFloor : 1;

        // seed固定（同じrun中は同じ配置 / floorで差分）
        if (useRunSeed && ItemManager.Instance != null)
        {
            if (!ItemManager.Instance.hasRunSave || ItemManager.Instance.runSave == null || ItemManager.Instance.runSave.seed == 0)
            {
                int newSeed = Environment.TickCount;
                ItemManager.Instance.SaveRun(seed: newSeed);
            }

            int baseSeed = ItemManager.Instance.runSave.seed;
            int floorSeed = baseSeed + (floor * floorSeedStep);
            UnityEngine.Random.InitState(floorSeed);
        }

        map = new int[width, height];

        RandomFill();
        Smooth();
        Draw();

        PlaceChests();
        PlaceEnemies();

        PlaceTreasureIfNeeded();    // 3Fのみ
        PlaceAllStairs_Refactored();
        PlacePlayer();              // 最後：階段横にスポーン
        HealOnDungeonEnterIfNeeded();// その後に回復（入場時のみ）
    }

    // =========================
    // Map generation
    // =========================
    void RandomFill()
    {
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                map[x, y] = 0;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (x == 0 || x == width - 1 || y == 0 || y == height - 1)
                {
                    map[x, y] = 1;
                }
                else
                {
                    if (UnityEngine.Random.Range(0, 100) < fillPercent)
                        map[x, y] = 1;
                }
            }
        }
    }

    void Smooth()
    {
        for (int i = 0; i < 5; i++)
        {
            int[,] newMap = new int[width, height];

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    int walls = CountWalls(x, y);
                    newMap[x, y] = (walls > 4) ? 1 : 0;
                }
            }

            map = newMap;
        }
    }

    int CountWalls(int x, int y)
    {
        int count = 0;

        for (int nx = x - 1; nx <= x + 1; nx++)
        {
            for (int ny = y - 1; ny <= y + 1; ny++)
            {
                if (nx < 0 || nx >= width || ny < 0 || ny >= height)
                    count++;
                else if (nx != x || ny != y)
                    count += map[nx, ny];
            }
        }

        return count;
    }

    void Draw()
    {
        wallTilemap.ClearAllTiles();
        groundTilemap.ClearAllTiles();

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector3Int pos = new Vector3Int(x, y, 0);

                if (map[x, y] == 1)
                    wallTilemap.SetTile(pos, wallTile);
                else
                    groundTilemap.SetTile(pos, floorTile);
            }
        }

        wallTilemap.RefreshAllTiles();
        groundTilemap.RefreshAllTiles();
    }

    // =========================
    // Chests
    // =========================
    Tile GetChestTileByRarity(ItemRarity r)
    {
        switch (r)
        {
            case ItemRarity.Legendary: return legendaryChestTile;
            case ItemRarity.Epic:      return epicChestTile;
            case ItemRarity.Rare:      return rareChestTile;
            case ItemRarity.Uncommon:  return uncommonChestTile;
            default:                   return commonChestTile;
        }
    }

    ItemRarity RollChestRarity()
    {
        if (chestRarityWeights == null || chestRarityWeights.Count == 0)
            return ItemRarity.Common;

        float sum = 0f;
        foreach (var w in chestRarityWeights)
            if (w != null && w.weight > 0f) sum += w.weight;

        if (sum <= 0f) return ItemRarity.Common;

        float r = UnityEngine.Random.value * sum;
        float acc = 0f;

        foreach (var w in chestRarityWeights)
        {
            if (w == null || w.weight <= 0f) continue;
            acc += w.weight;
            if (r <= acc) return w.rarity;
        }

        return chestRarityWeights.FirstOrDefault(w => w != null && w.weight > 0f)?.rarity ?? ItemRarity.Common;
    }

    public ItemRarity GetChestRarity(Vector2Int cell)
    {
        if (chestRarityByCell != null && chestRarityByCell.TryGetValue(cell, out var r))
            return r;

        return ItemRarity.Common;
    }

    void PlaceChests()
    {
        if (chestTilemap == null) return;

        chestTilemap.ClearAllTiles();
        chestRarityByCell.Clear();

        int placed = 0;
        int safety = 5000;

        while (placed < chestCount && safety-- > 0)
        {
            int x = UnityEngine.Random.Range(2, width - 2);
            int y = UnityEngine.Random.Range(2, height - 2);

            if (map[x, y] != 0) continue;

            Vector2Int cell = new Vector2Int(x, y);

            var r = RollChestRarity();
            chestRarityByCell[cell] = r;

            Tile tile = GetChestTileByRarity(r);
            if (tile == null) tile = commonChestTile;

            chestTilemap.SetTile((Vector3Int)cell, tile);
            placed++;
        }

        chestTilemap.RefreshAllTiles();
    }

    // =========================
    // Enemies
    // =========================
    void PlaceEnemies()
    {
        if (enemyPrefab == null) return;

        var old = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (var e in old) Destroy(e);

        int placed = 0;
        int safety = 5000;

        while (placed < enemyCount && safety-- > 0)
        {
            int x = UnityEngine.Random.Range(2, width - 2);
            int y = UnityEngine.Random.Range(2, height - 2);

            if (map[x, y] != 0) continue;

            if (chestTilemap != null && chestTilemap.GetTile(new Vector3Int(x, y, 0)) != null) continue;

            Vector3 worldPos = groundTilemap.GetCellCenterWorld(new Vector3Int(x, y, 0));
            Instantiate(enemyPrefab, worldPos, Quaternion.identity);

            placed++;
        }
    }

    // =========================
    // Player spawn
    // =========================
    void PlacePlayer()
    {
        GameObject player = GameObject.FindWithTag("Player");
        if (player == null)
        {
            Debug.LogError("Player がシーンにいません！");
            return;
        }

        ForceCellAndNeighborsToFloor(lastSpawnStairCell);

        Vector2Int spawnCell = lastSpawnStairCell + spawnOffsetFromStair;
        spawnCell.x = Mathf.Clamp(spawnCell.x, 1, width - 2);
        spawnCell.y = Mathf.Clamp(spawnCell.y, 1, height - 2);
        MakeFloor(spawnCell.x, spawnCell.y);
        RefreshFloorAt(spawnCell.x, spawnCell.y);

        Vector3 world = groundTilemap.GetCellCenterWorld(new Vector3Int(spawnCell.x, spawnCell.y, 0));
        player.transform.position = world;
    }

    // =========================
    // Treasure (3F only)
    // =========================
    void PlaceTreasureIfNeeded()
    {
        if (ItemManager.Instance == null) return;
        if (ItemManager.Instance.currentFloor != 3) return;

        if (treasureTilemap == null || treasureTile == null) return;

        if (ItemManager.Instance.runHasTreasure) return;

        if (!TryPickInnerFloorCell(out var cell)) return;

        ForceCellAndNeighborsToFloor(cell);
        treasureTilemap.SetTile((Vector3Int)cell, treasureTile);
        treasureTilemap.RefreshAllTiles();
    }

    // =========================
    // Helpers
    // =========================
    bool TryPickInnerFloorCell(out Vector2Int cell)
    {
        cell = default;

        int minX = 2;
        int maxX = width - 3;
        int minY = 2;
        int maxY = height - 3;

        if (maxX <= minX || maxY <= minY) return false;

        int safety = 5000;
        while (safety-- > 0)
        {
            int x = UnityEngine.Random.Range(minX, maxX + 1);
            int y = UnityEngine.Random.Range(minY, maxY + 1);

            if (map[x, y] != 0) continue;

            if (chestTilemap != null && chestTilemap.GetTile(new Vector3Int(x, y, 0)) != null) continue;

            cell = new Vector2Int(x, y);
            return true;
        }

        return false;
    }

    void ForceCellAndNeighborsToFloor(Vector2Int c)
    {
        c.x = Mathf.Clamp(c.x, 1, width - 2);
        c.y = Mathf.Clamp(c.y, 1, height - 2);

        MakeFloor(c.x, c.y);
        MakeFloor(c.x + 1, c.y);
        MakeFloor(c.x - 1, c.y);
        MakeFloor(c.x, c.y + 1);
        MakeFloor(c.x, c.y - 1);

        RefreshFloorAt(c.x, c.y);
        RefreshFloorAt(c.x + 1, c.y);
        RefreshFloorAt(c.x - 1, c.y);
        RefreshFloorAt(c.x, c.y + 1);
        RefreshFloorAt(c.x, c.y - 1);

        wallTilemap.RefreshAllTiles();
        groundTilemap.RefreshAllTiles();
    }

    void MakeFloor(int x, int y)
    {
        if (x <= 0 || x >= width - 1 || y <= 0 || y >= height - 1) return;
        map[x, y] = 0;
    }

    void RefreshFloorAt(int x, int y)
    {
        if (x <= 0 || x >= width - 1 || y <= 0 || y >= height - 1) return;

        wallTilemap.SetTile(new Vector3Int(x, y, 0), null);
        groundTilemap.SetTile(new Vector3Int(x, y, 0), floorTile);
    }

    void ResetGenerationState()
    {
        if (wallTilemap) wallTilemap.ClearAllTiles();
        if (groundTilemap) groundTilemap.ClearAllTiles();
        if (chestTilemap) chestTilemap.ClearAllTiles();
        if (stairTilemap) stairTilemap.ClearAllTiles();
        if (treasureTilemap) treasureTilemap.ClearAllTiles();

        if (chestRarityByCell != null) chestRarityByCell.Clear();

        var enemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (var e in enemies)
            Destroy(e);
    }

    // =========================
    // Stairs (refactored)
    // =========================
    void PlaceAllStairs_Refactored()
    {
        if (stairTilemap == null) return;
        if (ItemManager.Instance == null) return;

        int floor = ItemManager.Instance.currentFloor;

        bool IsFloor(Vector2Int c)
        {
            if (c.x < 0 || c.x >= width || c.y < 0 || c.y >= height) return false;
            return map[c.x, c.y] == 0;
        }

        bool IsBlocked(Vector2Int c)
        {
            if (chestTilemap != null && chestTilemap.GetTile(new Vector3Int(c.x, c.y, 0)) != null) return true;
            if (treasureTilemap != null && treasureTilemap.GetTile(new Vector3Int(c.x, c.y, 0)) != null) return true;
            return false;
        }

        stairPlacer = new StairPlacer(
            stairTilemap,
            stairUpTile,
            stairDownTile,
            width,
            height,
            isFloorCell: IsFloor,
            forceCellAndNeighborsToFloor: ForceCellAndNeighborsToFloor,
            isBlockedCell: IsBlocked
        );

        stairPlacer.PlaceAllStairsAndDecideSpawn(
            floor,
            entranceStairCount,
            midStairCount,
            escapeStairCount
        );

        lastSpawnStairCell = stairPlacer.LastSpawnStairCell;
    }

    void HealOnDungeonEnterIfNeeded()
    {
        if (ItemManager.Instance == null) return;
        if (!ItemManager.Instance.healOnNextDungeonEnter) return;

        GameObject player = GameObject.FindWithTag("Player");
        if (player == null) return;

        var hp = player.GetComponent<PlayerHealth>();
        if (hp != null) hp.SetFullHP();

        ItemManager.Instance.healOnNextDungeonEnter = false;
        ItemManager.Instance.SaveRun();
    }

}