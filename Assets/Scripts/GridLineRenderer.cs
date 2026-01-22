using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class GridLineRenderer : MonoBehaviour
{
    [Header("References")]
    public GridManager grid;

    [Header("Grid Size (cells)")]
    public int width = 10;   // X yönü hücre sayýsý
    public int height = 10;  // Z yönü hücre sayýsý

    [Header("Visual")]
    public Color lineColor = new Color(0.2f, 0.9f, 0.9f, 0.5f);
    public float lineWidth = 0.03f;
    public float y = 0.01f; // zeminin biraz üstü (z-fighting olmasýn)
    public bool visibleInGame = true;

    [Header("Performance")]
    public bool rebuildEveryFrameInEditor = true; // editörde ayar deðiþince anýnda güncellensin

    readonly List<LineRenderer> lines = new List<LineRenderer>();
    Material lineMat;

    void OnEnable()
    {
        if (grid == null) grid = GetComponent<GridManager>();
        EnsureMaterial();
        Rebuild();
    }

    void OnDisable()
    {
        // Editörde “hayalet” obje kalmasýn
        for (int i = 0; i < lines.Count; i++)
        {
            if (lines[i] != null)
            {
                if (Application.isPlaying) Destroy(lines[i].gameObject);
                else DestroyImmediate(lines[i].gameObject);
            }
        }
        lines.Clear();
    }

    void Update()
    {
        if (!visibleInGame)
        {
            SetLinesEnabled(false);
            return;
        }

        SetLinesEnabled(true);

#if UNITY_EDITOR
        if (!Application.isPlaying && rebuildEveryFrameInEditor)
        {
            // Inspector deðiþiklikleri anýnda yansýsýn
            Rebuild();
        }
#endif
    }

    void SetLinesEnabled(bool enabled)
    {
        for (int i = 0; i < lines.Count; i++)
            if (lines[i] != null) lines[i].enabled = enabled;
    }

    void EnsureMaterial()
    {
        if (lineMat != null) return;

        // Basit ve her pipeline’da çalýþan unlit bir shader deniyoruz
        Shader sh = Shader.Find("Unlit/Color");
        if (sh == null)
        {
            // Bazý projelerde Unlit/Color olmayabilir; fallback:
            sh = Shader.Find("Sprites/Default");
        }

        lineMat = new Material(sh);
        lineMat.color = lineColor;
    }

    public void Rebuild()
    {
        if (grid == null) grid = GetComponent<GridManager>();
        if (grid == null) return;

        EnsureMaterial();

        // Gerekli çizgi sayýsý: dikey (width+1) + yatay (height+1)
        int needed = (width + 1) + (height + 1);
        EnsureLineCount(needed);

        float cs = grid.cellSize;
        Vector3 o = grid.origin;
        Vector3 start = new Vector3(o.x, y, o.z); // origin = sol-alt köþe


        int idx = 0;

        // Dikey çizgiler
        for (int x = 0; x <= width; x++)
        {
            Vector3 p1 = start + new Vector3(x * cs, 0, 0);
            Vector3 p2 = start + new Vector3(x * cs, 0, height * cs);
            SetupLine(lines[idx++], p1, p2);
        }

        // Yatay çizgiler
        for (int z = 0; z <= height; z++)
        {
            Vector3 p1 = start + new Vector3(0, 0, z * cs);
            Vector3 p2 = start + new Vector3(width * cs, 0, z * cs);
            SetupLine(lines[idx++], p1, p2);
        }
    }

    void EnsureLineCount(int needed)
    {
        // Eksik çizgileri oluþtur
        while (lines.Count < needed)
        {
            GameObject go = new GameObject("GridLine");
            go.transform.SetParent(transform, false);

            var lr = go.AddComponent<LineRenderer>();
            lr.positionCount = 2;
            lr.useWorldSpace = true;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows = false;
            lr.alignment = LineAlignment.View; // kameraya bakan daha temiz görünür
            lr.numCapVertices = 4;

            lines.Add(lr);
        }

        // Fazla çizgileri sil
        for (int i = lines.Count - 1; i >= needed; i--)
        {
            if (lines[i] != null)
            {
                if (Application.isPlaying) Destroy(lines[i].gameObject);
                else DestroyImmediate(lines[i].gameObject);
            }
            lines.RemoveAt(i);
        }
    }

    void SetupLine(LineRenderer lr, Vector3 p1, Vector3 p2)
    {
        if (lr == null) return;

        lr.sharedMaterial = lineMat;

        lr.startWidth = lineWidth;
        lr.endWidth = lineWidth;

        // renk (alpha dahil)
        lr.startColor = lineColor;
        lr.endColor = lineColor;

        lr.SetPosition(0, p1);
        lr.SetPosition(1, p2);
    }
}
