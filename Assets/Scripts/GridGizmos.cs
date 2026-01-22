using UnityEngine;

[ExecuteAlways]
public class GridGizmos : MonoBehaviour
{
    public GridManager grid;

    [Header("Grid Visual")]
    public int width = 10;   // kaç hücre (x yönü)
    public int height = 10;  // kaç hücre (z yönü)
    public float y = 0.01f;  // çizgiyi zeminden azýcýk yukarý al (z-fighting olmasýn)

    void OnDrawGizmos()
    {
        if (grid == null) grid = GetComponent<GridManager>();
        if (grid == null) return;

        float cs = grid.cellSize;
        Vector3 o = grid.origin;

        // grid'i origin merkezli çizelim:
        // toplam geniþlik = width * cellSize
        float halfW = (width * cs) * 0.5f;
        float halfH = (height * cs) * 0.5f;

        Vector3 start = new Vector3(o.x - halfW, y, o.z - halfH);

        // Dikey çizgiler (X sabit, Z deðiþir)
        for (int x = 0; x <= width; x++)
        {
            Vector3 p1 = start + new Vector3(x * cs, 0, 0);
            Vector3 p2 = start + new Vector3(x * cs, 0, height * cs);
            Gizmos.DrawLine(p1, p2);
        }

        // Yatay çizgiler (Z sabit, X deðiþir)
        for (int z = 0; z <= height; z++)
        {
            Vector3 p1 = start + new Vector3(0, 0, z * cs);
            Vector3 p2 = start + new Vector3(width * cs, 0, z * cs);
            Gizmos.DrawLine(p1, p2);
        }
    }
}
