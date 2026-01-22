using UnityEngine;

public class GridManager : MonoBehaviour
{
    public static GridManager Instance;

    [Header("Grid Settings")]
    public float cellSize = 1f;

    [Tooltip("Grid'in merkezi (XZ). Genelde (0,0,0).")]
    public Vector3 origin = Vector3.zero;

    [Header("Grid Size (set by LevelData)")]
    public int width = 10;
    public int height = 10;

    void Awake() => Instance = this;

    public void Configure(int w, int h, float cs, Vector3 centerOrigin)
    {
        width = Mathf.Max(1, w);
        height = Mathf.Max(1, h);
        cellSize = Mathf.Max(0.01f, cs);
        origin = centerOrigin;
    }

    Vector3 BottomLeft()
    {
        float halfW = width * cellSize * 0.5f;
        float halfH = height * cellSize * 0.5f;
        return new Vector3(origin.x - halfW, origin.y, origin.z - halfH);
    }

    public bool IsCellInside(Vector2Int c) =>
        c.x >= 0 && c.x < width && c.y >= 0 && c.y < height;

    public bool CanPlaceRect(Vector2Int anchor, Vector2Int size)
    {
        if (size.x <= 0 || size.y <= 0) return false;
        if (anchor.x < 0 || anchor.y < 0) return false;
        if (anchor.x + size.x > width) return false;
        if (anchor.y + size.y > height) return false;
        return true;
    }

    public Vector2Int ClampAnchorForSize(Vector2Int anchor, Vector2Int size)
    {
        int maxX = Mathf.Max(0, width - size.x);
        int maxY = Mathf.Max(0, height - size.y);
        return new Vector2Int(
            Mathf.Clamp(anchor.x, 0, maxX),
            Mathf.Clamp(anchor.y, 0, maxY)
        );
    }

    public Vector2Int WorldToCell(Vector3 worldPos)
    {
        var bl = BottomLeft();
        int cx = Mathf.FloorToInt((worldPos.x - bl.x) / cellSize);
        int cz = Mathf.FloorToInt((worldPos.z - bl.z) / cellSize);
        return new Vector2Int(cx, cz);
    }

    public Vector3 CellRectToWorldCenter(Vector2Int anchor, Vector2Int size, float fixedY)
    {
        var bl = BottomLeft();

        float centerXCells = anchor.x + size.x * 0.5f;
        float centerZCells = anchor.y + size.y * 0.5f;

        float x = bl.x + centerXCells * cellSize;
        float z = bl.z + centerZCells * cellSize;

        return new Vector3(x, fixedY, z);
    }

    /// <summary>
    /// Snap position to grid for a rect footprint. Returns snapped world center and outputs snapped anchor cell.
    /// </summary>
    public Vector3 SnapToGridRect(Vector3 worldPos, Vector2Int size, float fixedY, out Vector2Int snappedAnchor)
    {
        var bl = BottomLeft();

        // world center -> "anchor cell" (rect'in anchor'ý)
        // Not: worldPos rect'in merkezidir. Anchor = center - size/2
        float axF = ((worldPos.x - bl.x) / cellSize) - (size.x * 0.5f);
        float azF = ((worldPos.z - bl.z) / cellSize) - (size.y * 0.5f);

        int ax = Mathf.RoundToInt(axF);
        int az = Mathf.RoundToInt(azF);

        snappedAnchor = ClampAnchorForSize(new Vector2Int(ax, az), size);
        return CellRectToWorldCenter(snappedAnchor, size, fixedY);
    }

}
