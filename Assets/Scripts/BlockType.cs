using UnityEngine;

public class BlockType : MonoBehaviour
{
    public BlockColor color = BlockColor.Blue;

    [Header("Footprint")]
    public int length = 1;                  // 1..4
    public Orientation orient = Orientation.X;

    [Header("Runtime (set by snapping/level load)")]
    public Vector2Int anchorCell;

    public Vector2Int GetSizeCells()
    {
        int len = Mathf.Max(1, length);
        return orient == Orientation.X
            ? new Vector2Int(len, 1)
            : new Vector2Int(1, len);
    }
}
