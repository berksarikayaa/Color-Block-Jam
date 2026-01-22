using UnityEngine;

[CreateAssetMenu(menuName = "ColorBlockJam/Level Generator Settings", fileName = "LevelGenSettings")]
public class LevelGeneratorSettings : ScriptableObject
{
    [Header("How many levels to generate")]
    public int levelCount = 20;

    [Header("Goal Placement Zones")]
    [Range(0f, 1f)] public float goalWallChance = 0.6f;
    [Min(1)] public int wallThickness = 2;
    [Min(1)] public int centerRadius = 2;



    [Header("Grid")]
    public int gridWidth = 10;
    public int gridHeight = 10;
    public float cellSize = 1f;

    [Header("Content")]
    public int pairCount = 4;              // kaç renk (block+goal)
    public int shuffleStepsBase = 6;       // level 1 karýþtýrma sayýsý
    public int shuffleStepsStep = 1;       // her level +1 gibi
    public int maxMovesBuffer = 3;         // maxMoves = shuffleSteps + buffer
    public int minDistFromOwnGoal = 2;     // block kendi goal'undan en az kaç hücre uzaða taþýnsýn

    [Header("Search / Attempts")]
    public int attemptsPerShuffleStep = 200;   // hedef hücre bulma denemesi
    public int seed = 12345;                   // sabit sonuç istersen

    [Header("Bias")]
    [Range(0f, 1f)] public float goalEdgeBias = 0.85f;   // 1 = hep kenar
    [Range(0f, 1f)] public float blockCenterBias = 0.75f; // 1 = hep merkez

}
