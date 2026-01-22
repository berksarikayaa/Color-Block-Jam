using System;
using System.Collections.Generic;
using UnityEngine;

public enum Orientation { X, Z }

[CreateAssetMenu(menuName = "ColorBlockJam/Level Data", fileName = "LevelData_01")]
public class LevelData : ScriptableObject
{
    [Header("Grid")]
    public int gridWidth = 10;
    public int gridHeight = 10;
    public float cellSize = 1f;

    [Header("Rules")]
    public int maxMoves = 10;

    [Header("Spawns")]
    public List<BlockSpawn> blocks = new();
    public List<GoalSpawn> goals = new();
}

[Serializable]
public struct BlockSpawn
{
    public BlockColor color;
    public Vector2Int cell;        // anchor cell (sol-alt anchor gibi)
    public int length;             // 1..4
    public Orientation orient;     // X veya Z
}

[Serializable]
public struct GoalSpawn
{
    public BlockColor color;
    public Vector2Int cell;        // anchor cell
    public int length;             // goal için 4
    public Orientation orient;     // X veya Z
}
