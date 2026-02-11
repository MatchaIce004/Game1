using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

public class StairPlacer
{
    readonly Tilemap stairTilemap;
    readonly Tile stairUpTile;
    readonly Tile stairDownTile;

    readonly int width;
    readonly int height;

    readonly Func<Vector2Int, bool> isFloorCell;                 // map上で床か？
    readonly Action<Vector2Int> forceCellAndNeighborsToFloor;     // 床保証（既存仕様維持）
    readonly Func<Vector2Int, bool> isBlockedCell;                // 宝箱/お宝と被り回避（任意）

    public Vector2Int LastSpawnStairCell { get; private set; }

    public StairPlacer(
        Tilemap stairTilemap,
        Tile stairUpTile,
        Tile stairDownTile,
        int width,
        int height,
        Func<Vector2Int, bool> isFloorCell,
        Action<Vector2Int> forceCellAndNeighborsToFloor,
        Func<Vector2Int, bool> isBlockedCell = null
    )
    {
        this.stairTilemap = stairTilemap;
        this.stairUpTile = stairUpTile;
        this.stairDownTile = stairDownTile;
        this.width = width;
        this.height = height;
        this.isFloorCell = isFloorCell;
        this.forceCellAndNeighborsToFloor = forceCellAndNeighborsToFloor;
        this.isBlockedCell = isBlockedCell;
    }

    public void PlaceAllStairsAndDecideSpawn(
        int floor,
        int entranceStairCount,
        int midStairCount,
        int escapeStairCount
    )
    {
        if (stairTilemap == null) return;
        if (ItemManager.Instance == null) return;

        stairTilemap.ClearAllTiles();

        // 1Fで座標決定・保存（既存仕様）
        if (floor == 1)
        {
            GenerateCells(ItemManager.Instance.entranceStairCells, entranceStairCount);
            GenerateCells(ItemManager.Instance.midStairCells, midStairCount);
            GenerateCells(ItemManager.Instance.escapeStairCells, escapeStairCount);

            ItemManager.Instance.SaveRun();
        }

        var entrance = ItemManager.Instance.entranceStairCells.Select(v => v.ToV2()).ToList();
        var mid      = ItemManager.Instance.midStairCells.Select(v => v.ToV2()).ToList();
        var escape   = ItemManager.Instance.escapeStairCells.Select(v => v.ToV2()).ToList();

        // 配置（重複呼び出しはしない）
        if (floor == 1)
        {
            PlaceCells(entrance, stairDownTile);
            PlaceCells(escape, stairUpTile);
        }
        else if (floor == 2)
        {
            PlaceCells(entrance, stairUpTile);
            PlaceCells(mid, stairDownTile);
        }
        else // 3F
        {
            PlaceCells(mid, stairUpTile);
        }

        stairTilemap.RefreshAllTiles();

        // スポーン基準は「最後に踏んだ階段セル」が真実
        LastSpawnStairCell = DecideSpawnStairCell(floor, entrance, mid, escape);
    }

    Vector2Int DecideSpawnStairCell(int floor, List<Vector2Int> entrance, List<Vector2Int> mid, List<Vector2Int> escape)
    {
        var im = ItemManager.Instance;

        // 1) lastStairCell が有効なら原則それ（戻りスポーンの根拠）
        if (im != null && im.hasLastStairCell && im.lastStairKind != ItemManager.StairKind.None)
            return im.lastStairCell;

        // 2) 初回開始などのフォールバック
        if (floor == 1)
        {
            if (escape != null && escape.Count > 0)
                return escape[UnityEngine.Random.Range(0, escape.Count)];

            if (entrance != null && entrance.Count > 0)
                return entrance[0];

            return new Vector2Int(width / 2, height / 2);
        }

        if (floor == 2)
        {
            if (entrance != null && entrance.Count > 0)
                return entrance[0];

            return new Vector2Int(width / 2, height / 2);
        }

        // floor == 3
        if (mid != null && mid.Count > 0)
            return mid[0];

        return new Vector2Int(width / 2, height / 2);
    }

    void GenerateCells(List<ItemManager.Vector2IntSerializable> list, int count)
    {
        list.Clear();

        int safety = 9999;
        while (list.Count < count && safety-- > 0)
        {
            if (!TryPickInnerFloorCell(out var c)) break;
            if (list.Any(v => v.x == c.x && v.y == c.y)) continue;
            list.Add(new ItemManager.Vector2IntSerializable(c));
        }
    }

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

            var c = new Vector2Int(x, y);

            if (!isFloorCell(c)) continue;
            if (isBlockedCell != null && isBlockedCell(c)) continue;

            cell = c;
            return true;
        }

        return false;
    }

    void PlaceCells(IEnumerable<Vector2Int> cells, Tile tile)
    {
        if (tile == null) return;

        foreach (var c in cells)
        {
            forceCellAndNeighborsToFloor?.Invoke(c);
            stairTilemap.SetTile((Vector3Int)c, tile);
        }
    }
}