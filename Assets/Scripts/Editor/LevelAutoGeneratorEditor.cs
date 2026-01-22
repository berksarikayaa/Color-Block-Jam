#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class LevelAutoGeneratorEditor
{
    private const string SettingsPath = "Assets/LevelGenSettings.asset";
    private const string OutputFolder = "Assets/Resources/Levels";

    [MenuItem("Tools/ColorBlockJam/Generate Puzzle Levels (Wall+Center Goals)")]
    public static void Generate()
    {
        var settings = AssetDatabase.LoadAssetAtPath<LevelGeneratorSettings>(SettingsPath);
        if (settings == null)
        {
            Debug.LogError($"LevelGenSettings bulunamadý: {SettingsPath}");
            return;
        }

        EnsureFolder("Assets/Resources");
        EnsureFolder(OutputFolder);

        var rng = new System.Random(settings.seed);

        var colors = (BlockColor[])Enum.GetValues(typeof(BlockColor));
        int pairs = Mathf.Clamp(settings.pairCount, 1, colors.Length);

        for (int levelIndex = 0; levelIndex < settings.levelCount; levelIndex++)
        {
            int shuffleSteps = Mathf.Max(1, settings.shuffleStepsBase + levelIndex * settings.shuffleStepsStep);

            int w = settings.gridWidth;
            int h = settings.gridHeight;

            // distinct colors per level
            var pool = colors.ToList();
            Shuffle(pool, rng);
            var pickedColors = pool.Take(pairs).ToArray();

            // Use occ only to prevent overlap while placing goals initially
            bool[,] occGoals = new bool[w, h];

            var goals = new List<GoalSpawn>(pairs);
            var blocks = new List<BlockSpawn>(pairs);

            // --------------------------
            // 1) PLACE GOALS (1x4) in WALL or CENTER zones
            // --------------------------
            for (int i = 0; i < pairs; i++)
            {
                BlockColor c = pickedColors[i];

                bool placed = false;
                for (int attempt = 0; attempt < 800 && !placed; attempt++)
                {
                    Orientation o;
                    Vector2Int anchor;
                    int len = 4;

                    // wall mý center mý?
                    bool wantWall = rng.NextDouble() < settings.goalWallChance;

                    if (wantWall)
                    {
                        // Duvar seç
                        WallSide side = (WallSide)rng.Next(0, 4);
                        o = WallOrientation(side);
                        Vector2Int size = SizeFromLenWall(len, o);

                        // Duvara flush anchor üret
                        anchor = PickWallAnchor(rng, w, h, len, side);

                        // Top/Right için anchor’ýn "w-1 / h-1" olmasý rect taþýrabilir.
                        // Bu yüzden Right/Top’ta anchor’ý size’a göre düzelt:
                        if (side == WallSide.Right) anchor.x = w - size.x;
                        if (side == WallSide.Top) anchor.y = h - size.y;

                        if (!CanPlaceRect(occGoals, w, h, anchor, size))
                            continue;

                        PlaceRect(occGoals, anchor, size, true);
                        goals.Add(new GoalSpawn { color = c, cell = anchor, length = len, orient = o });
                        blocks.Add(new BlockSpawn { color = c, cell = anchor, length = len, orient = o });
                        placed = true;
                    }
                    else
                    {
                        // Center: merkez yakýnýnda anchor arayalým
                        o = (rng.NextDouble() < 0.5) ? Orientation.X : Orientation.Z;
                        Vector2Int size = SizeFromLenWall(len, o);

                        for (int inner = 0; inner < 200; inner++)
                        {
                            int ax = rng.Next(0, w - size.x + 1);
                            int ay = rng.Next(0, h - size.y + 1);
                            anchor = new Vector2Int(ax, ay);

                            if (!RectCenterCloseToGridCenter(anchor, size, w, h, settings.centerRadius))
                                continue;

                            if (!CanPlaceRect(occGoals, w, h, anchor, size))
                                continue;

                            PlaceRect(occGoals, anchor, size, true);
                            goals.Add(new GoalSpawn { color = c, cell = anchor, length = len, orient = o });
                            blocks.Add(new BlockSpawn { color = c, cell = anchor, length = len, orient = o });
                            placed = true;
                            break;
                        }
                    }


                    if (!placed)
                    Debug.LogWarning($"Goal place failed on level {levelIndex + 1}. Grid’i büyüt veya wall/center ayarlarýný yumuþat.");
            }

            // --------------------------
            // 2) SHUFFLE BLOCKS away (blocks-only occupancy)
            // --------------------------
            bool[,] blockOcc = new bool[w, h];
            foreach (var b in blocks)
                PlaceRect(blockOcc, b.cell, SizeFrom(b.length, b.orient), true);

            var goalMap = goals.ToDictionary(g => g.color, g => g);

            for (int step = 0; step < shuffleSteps; step++)
            {
                int bi = rng.Next(blocks.Count);
                var b = blocks[bi];

                var from = b.cell;
                var fromSize = SizeFrom(b.length, b.orient);

                PlaceRect(blockOcc, from, fromSize, false); // temporarily remove

                bool moved = false;
                var ownGoal = goalMap[b.color];

                for (int attempt = 0; attempt < settings.attemptsPerShuffleStep; attempt++)
                {
                    int newLen = rng.Next(1, 5); // 1..4
                    Orientation newOri = (rng.NextDouble() < 0.5) ? Orientation.X : Orientation.Z;
                    Vector2Int newSize = SizeFrom(newLen, newOri);

                    // biased target anchor (center + near blocks)
                    Vector2Int to = RandomAnchorBiased(
                        rng, w, h, settings.blockCenterBias,
                        cell =>
                        {
                            float wc = CenterScore(cell, w, h);
                            float wn = NearOtherBlocksScore(cell, blocks);
                            return Mathf.Clamp01(0.65f * wc + 0.35f * wn);
                        }
                    );

                    to = ClampAnchor(to, w, h, newSize);

                    if (Manhattan(to, ownGoal.cell) < settings.minDistFromOwnGoal) continue;
                    if (!CanPlaceRect(blockOcc, w, h, to, newSize)) continue;

                    // footprint path (simple): path uses fromSize footprint
                    if (!HasPathFootprint(from, fromSize, to, blockOcc, w, h)) continue;

                    // accept
                    b.cell = to;
                    b.length = newLen;
                    b.orient = newOri;
                    blocks[bi] = b;

                    PlaceRect(blockOcc, to, newSize, true);
                    moved = true;
                    break;
                }

                if (!moved)
                {
                    // restore
                    PlaceRect(blockOcc, from, fromSize, true);
                    step--; // retry
                    if (step < -20) break;
                }
            }

            // --------------------------
            // 3) WRITE ASSET
            // --------------------------
            var level = ScriptableObject.CreateInstance<LevelData>();
            level.gridWidth = w;
            level.gridHeight = h;
            level.cellSize = settings.cellSize;
            level.maxMoves = shuffleSteps + Mathf.Max(0, settings.maxMovesBuffer);
            level.goals = goals;
            level.blocks = blocks;

            string assetPath = $"{OutputFolder}/LevelData_{(levelIndex + 1).ToString("D2")}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<LevelData>(assetPath);
            if (existing != null) AssetDatabase.DeleteAsset(assetPath);

            AssetDatabase.CreateAsset(level, assetPath);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Generated {settings.levelCount} levels -> {OutputFolder}");
    }

    // -------------------------------------------------
    // ZONE CHECKS (RECT-BASED)
    // -------------------------------------------------

    // "Duvar" = rect'in herhangi bir kenarý duvar bandýna girsin
    // wallThickness=2 ise ilk 2 hücre bandý "duvar" sayýlýr.
    static bool RectTouchesWall(Vector2Int anchor, Vector2Int size, int w, int h, int wallThickness)
    {
        wallThickness = Mathf.Max(1, wallThickness);

        int left = anchor.x;
        int right = anchor.x + size.x - 1;
        int bottom = anchor.y;
        int top = anchor.y + size.y - 1;

        bool touchesLeft = left < wallThickness;
        bool touchesRight = right >= (w - wallThickness);
        bool touchesBottom = bottom < wallThickness;
        bool touchesTop = top >= (h - wallThickness);

        return touchesLeft || touchesRight || touchesBottom || touchesTop;
    }

    // "Orta" = rect merkezinin grid merkezine Manhattan mesafesi centerRadius içinde olsun
    static bool RectInCenterZone(Vector2Int anchor, Vector2Int size, int w, int h, int centerRadius)
    {
        centerRadius = Mathf.Max(1, centerRadius);

        float rectCx = anchor.x + size.x * 0.5f;
        float rectCy = anchor.y + size.y * 0.5f;

        float gridCx = (w) * 0.5f;
        float gridCy = (h) * 0.5f;

        int d = Mathf.Abs(Mathf.RoundToInt(rectCx - gridCx)) + Mathf.Abs(Mathf.RoundToInt(rectCy - gridCy));
        return d <= centerRadius;
    }

    // -------------------------------------------------
    // PATH (anchor BFS with fixed footprint)
    // -------------------------------------------------
    static bool HasPathFootprint(Vector2Int fromAnchor, Vector2Int footprintSize, Vector2Int toAnchor, bool[,] occ, int w, int h)
    {
        var q = new Queue<Vector2Int>();
        var visited = new bool[w, h];

        q.Enqueue(fromAnchor);
        visited[fromAnchor.x, fromAnchor.y] = true;

        while (q.Count > 0)
        {
            var cur = q.Dequeue();
            if (cur == toAnchor) return true;

            Try(cur.x + 1, cur.y);
            Try(cur.x - 1, cur.y);
            Try(cur.x, cur.y + 1);
            Try(cur.x, cur.y - 1);
        }

        return false;

        void Try(int nx, int ny)
        {
            if (nx < 0 || ny < 0 || nx >= w || ny >= h) return;
            if (visited[nx, ny]) return;

            var a = new Vector2Int(nx, ny);
            if (!FitsAndEmpty(a, footprintSize, occ, w, h)) return;

            visited[nx, ny] = true;
            q.Enqueue(a);
        }
    }

    static bool FitsAndEmpty(Vector2Int anchor, Vector2Int size, bool[,] occ, int w, int h)
    {
        if (anchor.x < 0 || anchor.y < 0) return false;
        if (anchor.x + size.x > w) return false;
        if (anchor.y + size.y > h) return false;

        for (int x = 0; x < size.x; x++)
            for (int y = 0; y < size.y; y++)
                if (occ[anchor.x + x, anchor.y + y]) return false;

        return true;
    }
    }

    // -------------------------------------------------
    // GENERATOR HELPERS
    // -------------------------------------------------

    enum WallSide { Left, Right, Bottom, Top }
    static Vector2Int PickWallAnchor(System.Random rng, int w, int h, int len, WallSide side)
    {
        // Wall thickness'i 1 gibi düþün: tamamen flush.
        // Sol/Sað: Z yönünde 1xlen
        // Alt/Üst: X yönünde lenx1
        switch (side)
        {
            case WallSide.Left:
                {
                    int x = 0;
                    int y = rng.Next(0, h - len + 1);
                    return new Vector2Int(x, y);
                }
            case WallSide.Right:
                {
                    int x = w - 1; // 1 hücre geniþlikte olduðu için anchor x = w-1
                    int y = rng.Next(0, h - len + 1);
                    return new Vector2Int(x, y);
                }
            case WallSide.Bottom:
                {
                    int y = 0;
                    int x = rng.Next(0, w - len + 1);
                    return new Vector2Int(x, y);
                }
            default: // Top
                {
                    int y = h - 1;
                    int x = rng.Next(0, w - len + 1);
                    return new Vector2Int(x, y);
                }
        }
    }

    static Orientation WallOrientation(WallSide side)
    {
        // Sol/Sað duvarda dikey (Z), üst/alt duvarda yatay (X)
        return (side == WallSide.Left || side == WallSide.Right) ? Orientation.Z : Orientation.X;
    }

    static Vector2Int SizeFromLenWall(int len, Orientation o)
    {
        return o == Orientation.X ? new Vector2Int(len, 1) : new Vector2Int(1, len);
    }

    // Center anchor: rect merkezi grid merkezine yakýn olsun
    static bool RectCenterCloseToGridCenter(Vector2Int anchor, Vector2Int size, int w, int h, int radius)
    {
        float rcx = anchor.x + size.x * 0.5f;
        float rcy = anchor.y + size.y * 0.5f;

        float gcx = w * 0.5f;
        float gcy = h * 0.5f;

        int d = Mathf.Abs(Mathf.RoundToInt(rcx - gcx)) + Mathf.Abs(Mathf.RoundToInt(rcy - gcy));
        return d <= radius;
    }

    static Vector2Int SizeFrom(int length, Orientation o)
    {
        int len = Mathf.Max(1, length);
        return o == Orientation.X ? new Vector2Int(len, 1) : new Vector2Int(1, len);
    }

    static List<Vector2Int> BuildAnchorsThatFit(int w, int h, Vector2Int size)
    {
        var list = new List<Vector2Int>();
        int maxX = w - size.x;
        int maxY = h - size.y;
        for (int y = 0; y <= maxY; y++)
            for (int x = 0; x <= maxX; x++)
                list.Add(new Vector2Int(x, y));
        return list;
    }

    static Vector2Int ClampAnchor(Vector2Int anchor, int w, int h, Vector2Int size)
    {
        int maxX = Mathf.Max(0, w - size.x);
        int maxY = Mathf.Max(0, h - size.y);
        return new Vector2Int(Mathf.Clamp(anchor.x, 0, maxX), Mathf.Clamp(anchor.y, 0, maxY));
    }

    static bool CanPlaceRect(bool[,] occ, int w, int h, Vector2Int anchor, Vector2Int size)
    {
        if (anchor.x < 0 || anchor.y < 0) return false;
        if (anchor.x + size.x > w) return false;
        if (anchor.y + size.y > h) return false;

        for (int x = 0; x < size.x; x++)
            for (int y = 0; y < size.y; y++)
                if (occ[anchor.x + x, anchor.y + y]) return false;

        return true;
    }

    static void PlaceRect(bool[,] occ, Vector2Int anchor, Vector2Int size, bool value)
    {
        for (int x = 0; x < size.x; x++)
            for (int y = 0; y < size.y; y++)
                occ[anchor.x + x, anchor.y + y] = value;
    }

    static Vector2Int RandomAnchorBiased(System.Random rng, int w, int h, float bias, Func<Vector2Int, float> weightFn)
    {
        bias = Mathf.Clamp01(bias);
        if (bias <= 0.001f)
            return new Vector2Int(rng.Next(w), rng.Next(h));

        float total = 0f;
        float[] ws = new float[w * h];

        for (int i = 0; i < w * h; i++)
        {
            int x = i % w;
            int y = i / w;

            float w0 = Mathf.Clamp01(weightFn(new Vector2Int(x, y)));
            float finalW = (1f - bias) * 1f + bias * Mathf.Max(0.001f, w0);

            ws[i] = finalW;
            total += finalW;
        }

        double pick = rng.NextDouble() * total;
        float acc = 0f;
        for (int i = 0; i < ws.Length; i++)
        {
            acc += ws[i];
            if (pick <= acc)
                return new Vector2Int(i % w, i / w);
        }

        return new Vector2Int(rng.Next(w), rng.Next(h));
    }

    static int Manhattan(Vector2Int a, Vector2Int b) =>
        Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);

    static float CenterScore(Vector2Int c, int w, int h)
    {
        float cx = (w - 1) * 0.5f;
        float cy = (h - 1) * 0.5f;
        float dx = Mathf.Abs(c.x - cx);
        float dy = Mathf.Abs(c.y - cy);

        float maxDx = cx;
        float maxDy = cy;

        float nx = maxDx <= 0 ? 0 : dx / maxDx;
        float ny = maxDy <= 0 ? 0 : dy / maxDy;

        float dist01 = Mathf.Clamp01((nx + ny) * 0.5f);
        return 1f - dist01;
    }

    static float NearOtherBlocksScore(Vector2Int c, List<BlockSpawn> blocks)
    {
        int best = int.MaxValue;
        for (int i = 0; i < blocks.Count; i++)
        {
            var b = blocks[i].cell;
            int d = Mathf.Abs(c.x - b.x) + Mathf.Abs(c.y - b.y);
            if (d < best) best = d;
        }
        if (best == int.MaxValue) return 0f;
        return Mathf.Clamp01(1f / Mathf.Max(1, best));
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;

        var parts = path.Split('/');
        string cur = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = $"{cur}/{parts[i]}";
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(cur, parts[i]);
            cur = next;
        }
    }

    static void Shuffle<T>(IList<T> list, System.Random rng)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
#endif
