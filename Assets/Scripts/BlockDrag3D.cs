using UnityEngine;

[RequireComponent(typeof(Collider))]
public class BlockDrag3D : MonoBehaviour
{
    public float groundY = 0.5f;
    public float dragSmooth = 30f;

    [Header("Goals")]
    public LayerMask goalLayer = ~0;


    [Header("Collision (Blocks)")]
    public LayerMask blockLayer = ~0;
    public float skin = 0.01f;
    public float castShrink = 0.02f;

    Camera cam;
    Plane dragPlane;

    bool isDragging;
    Vector3 offset;
    Vector3 startPos;
    Vector2Int startAnchorCell;

    Collider col;
    BlockType blockType;

    void Awake()
    {
        cam = Camera.main;
        dragPlane = new Plane(Vector3.up, new Vector3(0, groundY, 0));

        col = GetComponent<Collider>();
        blockType = GetComponent<BlockType>();
    }

    void Update()
    {
        if (LevelManager.Instance != null && LevelManager.Instance.IsInputLocked)
        {
            isDragging = false;
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            if (LevelManager.Instance != null && LevelManager.Instance.IsInputLocked) return;
            if (cam == null) cam = Camera.main;
            if (cam == null) return;

            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                if (hit.transform == transform)
                {
                    isDragging = true;
                    startPos = transform.position;

                    if (GridManager.Instance != null && blockType != null)
                    {
                        // drag baþlangýcýnda anchor’ý tut
                        startAnchorCell = blockType.anchorCell;
                    }

                    if (TryGetPointOnPlane(out Vector3 p))
                        offset = transform.position - p;
                }
            }
        }

        if (Input.GetMouseButtonUp(0))
        {
            if (isDragging)
            {
                isDragging = false;
                TrySnapOrReturn();
            }
        }

        if (!isDragging) return;
        if (GridManager.Instance == null) return;

        if (TryGetPointOnPlane(out Vector3 hitPoint))
        {
            Vector3 desired = hitPoint + offset;
            desired.y = groundY;

            Vector3 current = transform.position;
            Vector3 target = ResolveBlockedMove(current, desired);

            transform.position = Vector3.Lerp(transform.position, target, Time.deltaTime * dragSmooth);
        }
    }

    // ---------------------------
    // Move: BoxCast + slide
    // ---------------------------
    Vector3 ResolveBlockedMove(Vector3 from, Vector3 to)
    {
        Vector3 delta = to - from;
        delta.y = 0f;

        float dist = delta.magnitude;
        if (dist < 0.0001f) return from;

        Vector3 dir = delta / dist;

        Bounds b = col.bounds;
        Vector3 halfExtents = b.extents - Vector3.one * castShrink;
        halfExtents = new Vector3(
            Mathf.Max(0.001f, halfExtents.x),
            Mathf.Max(0.001f, halfExtents.y),
            Mathf.Max(0.001f, halfExtents.z)
        );

        Vector3 origin = new Vector3(from.x, b.center.y, from.z);
        Quaternion orientation = transform.rotation;

        if (Physics.BoxCast(origin, halfExtents, dir, out RaycastHit hit, orientation, dist,
                blockLayer, QueryTriggerInteraction.Ignore))
        {
            if (IsSelf(hit.collider)) return to;

            float allowed = Mathf.Max(0f, hit.distance - skin);
            Vector3 firstStop = from + dir * allowed;
            firstStop.y = groundY;

            Vector3 remaining = to - firstStop;
            remaining.y = 0f;

            Vector3 slide = Vector3.ProjectOnPlane(remaining, hit.normal);
            if (slide.sqrMagnitude < 0.000001f) return firstStop;

            Vector3 slideTo = firstStop + slide;
            slideTo.y = groundY;

            Vector3 slideDelta = slideTo - firstStop;
            float slideDist = slideDelta.magnitude;
            if (slideDist < 0.0001f) return firstStop;

            Vector3 slideDir = slideDelta / slideDist;
            Vector3 slideOrigin = new Vector3(firstStop.x, b.center.y, firstStop.z);

            if (Physics.BoxCast(slideOrigin, halfExtents, slideDir, out RaycastHit hit2, orientation, slideDist,
                    blockLayer, QueryTriggerInteraction.Ignore))
            {
                if (!IsSelf(hit2.collider))
                {
                    float allowed2 = Mathf.Max(0f, hit2.distance - skin);
                    Vector3 secondStop = firstStop + slideDir * allowed2;
                    secondStop.y = groundY;
                    return secondStop;
                }
            }

            return slideTo;
        }

        return to;
    }

    bool IsSelf(Collider other)
    {
        if (other == null) return false;
        return other.transform == transform || other.transform.IsChildOf(transform);
    }

    // ---------------------------
    // Snap for footprint + overlap check
    // ---------------------------
    void TrySnapOrReturn()
    {
        if (GridManager.Instance == null || blockType == null)
        {
            transform.position = startPos;
            return;
        }

        Vector2Int size = blockType.GetSizeCells();

        Vector3 snapped = GridManager.Instance.SnapToGridRect(transform.position, size, groundY, out Vector2Int snappedAnchor);

        // overlap check with OverlapBox (ignore self)
        if (IsPlacementFree(snapped, size))
        {
            transform.position = snapped;
            blockType.anchorCell = snappedAnchor;

            if (LevelManager.Instance != null)
                LevelManager.Instance.RegisterMove();

            TryConsumeIfOnGoal(); //  yeni
        }

        else
        {
            // geri dön
            transform.position = startPos;
            blockType.anchorCell = startAnchorCell;
        }
    }
    void TryConsumeIfOnGoal()
    {
        // Blok footprint'inin merkezinde goal trigger var mý bak
        Vector2Int sizeCells = blockType.GetSizeCells();
        float cs = GridManager.Instance.cellSize;

        // footprint half extents (goal colliderýný yakalamak için biraz daha küçük/temiz)
        Vector3 halfExtents = new Vector3(
            (sizeCells.x * cs) * 0.5f - 0.02f,
            0.6f, // goal collider yüksekliði için yeterli bir deðer
            (sizeCells.y * cs) * 0.5f - 0.02f
        );

        Vector3 center = transform.position;
        center.y = transform.position.y; // ayný seviyede

        // Trigger'larý da dahil et
        Collider[] hits = Physics.OverlapBox(center, halfExtents, Quaternion.identity, goalLayer, QueryTriggerInteraction.Collide);

        foreach (var h in hits)
        {
            if (h == null) continue;

            var goal = h.GetComponentInParent<GoalTrigger>();
            if (goal == null) continue;

            if (goal.CanConsume(blockType))
            {
                goal.Consume(blockType);
                return;
            }
        }

        Debug.Log($"Goal check hits: {hits.Length} | blockAnchor={blockType.anchorCell} len={blockType.length} ori={blockType.orient} col={blockType.color}");

    }

    bool IsPlacementFree(Vector3 snappedCenter, Vector2Int sizeCells)
    {
        // Collider boyutunu hedef scale'a göre tahmin et:
        // 1x1 cube varsayýmýyla: x = size.x*cellSize, z = size.y*cellSize
        float cs = GridManager.Instance.cellSize;

        Vector3 halfExtents = new Vector3(
            (sizeCells.x * cs) * 0.5f - 0.01f,
            col.bounds.extents.y,
            (sizeCells.y * cs) * 0.5f - 0.01f
        );

        Vector3 center = new Vector3(snappedCenter.x, col.bounds.center.y, snappedCenter.z);

        Collider[] hits = Physics.OverlapBox(center, halfExtents, transform.rotation, blockLayer, QueryTriggerInteraction.Ignore);
        foreach (var h in hits)
        {
            if (h == null) continue;
            if (IsSelf(h)) continue;
            if (h.CompareTag("Block")) return false;
        }
        return true;
    }

    bool TryGetPointOnPlane(out Vector3 point)
    {
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        if (dragPlane.Raycast(ray, out float enter))
        {
            point = ray.GetPoint(enter);
            return true;
        }
        point = Vector3.zero;
        return false;
    }
}
