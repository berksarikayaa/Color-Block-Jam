using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance;

    [System.Serializable]
    public class PrefabByColor
    {
        public BlockColor color;
        public GameObject prefab;
    }

    [Header("Levels (optional - auto loads if empty/null)")]
    public LevelData[] levels;
    public int currentLevelIndex = 0; // 0 = Level 1

    [Header("Prefabs (fallback)")]
    public GameObject defaultBlockPrefab;
    public GameObject defaultGoalPrefab;

    [Header("Prefabs By Color (optional)")]
    public PrefabByColor[] blockPrefabs;
    public PrefabByColor[] goalPrefabs;

    [Header("Parents (optional)")]
    public Transform blocksParent;
    public Transform goalsParent;

    [Header("Gameplay")]
    public float groundY = 0.5f;

    [Header("UI (optional)")]
    public UIController ui;

    public int Moves { get; private set; }
    public int MaxMoves { get; private set; }
    public bool IsInputLocked => levelFailed || levelCompleted;

    int totalBlocksInLevel;
    int consumedCount;
    bool levelFailed;
    bool levelCompleted;

    readonly List<GameObject> spawned = new();

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        if (ui == null) ui = FindFirstObjectByType<UIController>();

        if (levels == null || levels.Length == 0 || levels.Any(l => l == null))
        {
            levels = Resources.LoadAll<LevelData>("Levels")
                              .Where(l => l != null)
                              .OrderBy(l => l.name)
                              .ToArray();
            Debug.Log($"Loaded {levels.Length} LevelData from Resources/Levels");
        }

        if (levels == null || levels.Length == 0)
        {
            Debug.LogError("Resources/Levels altýnda LevelData yok.");
            return;
        }

        LoadLevel(currentLevelIndex);
    }

    public void LoadLevel(int index)
    {
        currentLevelIndex = Mathf.Clamp(index, 0, levels.Length - 1);
        LevelData data = levels[currentLevelIndex];
        if (data == null)
        {
            Debug.LogError("LevelManager: LevelData null.");
            return;
        }

        EnsureParents();
        ClearSpawned();

        Moves = 0;
        consumedCount = 0;
        levelFailed = false;
        levelCompleted = false;

        MaxMoves = Mathf.Max(1, data.maxMoves);
        totalBlocksInLevel = data.blocks != null ? data.blocks.Count : 0;

        if (GridManager.Instance != null)
        {
            GridManager.Instance.Configure(data.gridWidth, data.gridHeight, data.cellSize, Vector3.zero);
        }
        else
        {
            Debug.LogError("GridManager.Instance yok.");
            return;
        }

        //  Occupancy (goals+blocks overlap olmasýn)
        bool[,] occAll = new bool[data.gridWidth, data.gridHeight];

        // Spawn Goals
        if (data.goals != null)
        {
            foreach (var g in data.goals)
            {
                Vector2Int size = g.orient == Orientation.X
                    ? new Vector2Int(Mathf.Max(1, g.length), 1)
                    : new Vector2Int(1, Mathf.Max(1, g.length));

                Vector2Int anchor = GridManager.Instance.ClampAnchorForSize(g.cell, size);

                // overlap fix
                if (!GridManager.Instance.CanPlaceRect(anchor, size) || !CanPlaceRect(occAll, anchor, size))
                {
                    if (!TryFindFreeAnchor(occAll, size, out anchor))
                    {
                        Debug.LogWarning("No space for Goal, skipped.");
                        continue;
                    }
                }

                MarkRect(occAll, anchor, size, true);

                GameObject prefab = GetGoalPrefab(g.color);
                if (prefab == null) { Debug.LogError("Goal prefab missing."); continue; }

                Vector3 pos = GridManager.Instance.CellRectToWorldCenter(anchor, size, groundY);
                GameObject goalObj = Instantiate(prefab, pos, Quaternion.identity, goalsParent);
                spawned.Add(goalObj);

                var gt = goalObj.GetComponent<GoalType>();
                if (gt != null)
                {
                    gt.color = g.color;
                    gt.length = Mathf.Max(1, g.length);
                    gt.orient = g.orient;
                    gt.anchorCell = anchor;
                }

                ApplyFootprintTransform(goalObj.transform, size, data.cellSize);
            }
        }

        // Spawn Blocks
        if (data.blocks != null)
        {
            foreach (var b in data.blocks)
            {
                Vector2Int size = b.orient == Orientation.X
                    ? new Vector2Int(Mathf.Max(1, b.length), 1)
                    : new Vector2Int(1, Mathf.Max(1, b.length));

                Vector2Int anchor = GridManager.Instance.ClampAnchorForSize(b.cell, size);

                // overlap fix
                if (!GridManager.Instance.CanPlaceRect(anchor, size) || !CanPlaceRect(occAll, anchor, size))
                {
                    if (!TryFindFreeAnchor(occAll, size, out anchor))
                    {
                        Debug.LogWarning("No space for Block, skipped.");
                        continue;
                    }
                }

                MarkRect(occAll, anchor, size, true);

                GameObject prefab = GetBlockPrefab(b.color);
                if (prefab == null) { Debug.LogError("Block prefab missing."); continue; }

                Vector3 pos = GridManager.Instance.CellRectToWorldCenter(anchor, size, groundY);
                GameObject blockObj = Instantiate(prefab, pos, Quaternion.identity, blocksParent);
                spawned.Add(blockObj);

                var bt = blockObj.GetComponent<BlockType>();
                if (bt != null)
                {
                    bt.color = b.color;
                    bt.length = Mathf.Max(1, b.length);
                    bt.orient = b.orient;
                    bt.anchorCell = anchor;
                }

                ApplyFootprintTransform(blockObj.transform, size, data.cellSize);
            }
        }

        if (ui != null)
        {
            ui.SetLevel(currentLevelIndex + 1);
            ui.SetMoves(Moves);
            ui.HideLevelComplete();
            ui.HideFail();
        }

        Debug.Log($"Loaded Level {currentLevelIndex + 1} | Blocks:{totalBlocksInLevel} | MaxMoves:{MaxMoves}");
    }

    // --------------------------
    // Occupancy helpers
    // --------------------------
    bool CanPlaceRect(bool[,] occ, Vector2Int anchor, Vector2Int size)
    {
        for (int x = 0; x < size.x; x++)
            for (int y = 0; y < size.y; y++)
                if (occ[anchor.x + x, anchor.y + y]) return false;

        return true;
    }

    void MarkRect(bool[,] occ, Vector2Int anchor, Vector2Int size, bool value)
    {
        for (int x = 0; x < size.x; x++)
            for (int y = 0; y < size.y; y++)
                occ[anchor.x + x, anchor.y + y] = value;
    }

    bool TryFindFreeAnchor(bool[,] occ, Vector2Int size, out Vector2Int freeAnchor)
    {
        int w = GridManager.Instance.width;
        int h = GridManager.Instance.height;

        for (int y = 0; y <= h - size.y; y++)
            for (int x = 0; x <= w - size.x; x++)
            {
                var a = new Vector2Int(x, y);
                if (CanPlaceRect(occ, a, size))
                {
                    freeAnchor = a;
                    return true;
                }
            }

        freeAnchor = Vector2Int.zero;
        return false;
    }

    // --------------------------
    // Spawn helpers
    // --------------------------
    void ApplyFootprintTransform(Transform t, Vector2Int sizeCells, float cellSize)
    {
        Vector3 s = t.localScale;
        s.x = sizeCells.x * cellSize;
        s.z = sizeCells.y * cellSize;
        t.localScale = s;
    }

    void EnsureParents()
    {
        if (blocksParent == null) blocksParent = new GameObject("BlocksRoot").transform;
        if (goalsParent == null) goalsParent = new GameObject("GoalsRoot").transform;
    }

    void ClearSpawned()
    {
        for (int i = spawned.Count - 1; i >= 0; i--)
            if (spawned[i] != null) Destroy(spawned[i]);
        spawned.Clear();

        ClearChildren(blocksParent);
        ClearChildren(goalsParent);
    }

    void ClearChildren(Transform parent)
    {
        if (parent == null) return;
        for (int i = parent.childCount - 1; i >= 0; i--)
            Destroy(parent.GetChild(i).gameObject);
    }

    GameObject GetBlockPrefab(BlockColor color)
    {
        if (blockPrefabs != null)
        {
            for (int i = 0; i < blockPrefabs.Length; i++)
            {
                var p = blockPrefabs[i];
                if (p != null && p.color == color && p.prefab != null) return p.prefab;
            }
        }
        return defaultBlockPrefab;
    }

    GameObject GetGoalPrefab(BlockColor color)
    {
        if (goalPrefabs != null)
        {
            for (int i = 0; i < goalPrefabs.Length; i++)
            {
                var p = goalPrefabs[i];
                if (p != null && p.color == color && p.prefab != null) return p.prefab;
            }
        }
        return defaultGoalPrefab;
    }

    // --------------------------
    // Gameplay events
    // --------------------------
    public void RegisterMove()
    {
        if (IsInputLocked) return;

        Moves++;
        if (ui != null) ui.SetMoves(Moves);

        if (Moves >= MaxMoves)
        {
            levelFailed = true;
            if (ui != null) ui.ShowFail();
        }
    }

    public void OnBlockConsumed()
    {
        if (IsInputLocked) return;

        consumedCount++;
        if (consumedCount >= totalBlocksInLevel)
        {
            levelCompleted = true;
            if (ui != null) ui.ShowLevelComplete();
        }
    }

    // --------------------------
    // UI buttons
    // --------------------------
    public void RestartLevel() => LoadLevel(currentLevelIndex);

    public void NextLevel()
    {
        int next = currentLevelIndex + 1;
        if (next >= levels.Length) next = 0;
        LoadLevel(next);
    }
}
