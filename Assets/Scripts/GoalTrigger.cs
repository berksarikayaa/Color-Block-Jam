using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GoalTrigger : MonoBehaviour
{
    GoalType goalType;

    // Ayný bloðu iki kere consume etmeyi engelle
    readonly HashSet<int> consuming = new();

    [Header("Consume FX")]
    public float consumeDuration = 0.25f;
    public float popUp = 0.25f;
    public float endScale = 0.1f;

    void Awake()
    {
        goalType = GetComponent<GoalType>();
    }

    public bool CanConsume(BlockType block)
    {
        if (block == null || goalType == null) return false;
        return block.color == goalType.color; //  tam örtüþme yok
    }

    public void Consume(BlockType block)
    {
        if (!CanConsume(block)) return;

        int id = block.gameObject.GetInstanceID();
        if (consuming.Contains(id)) return;
        consuming.Add(id);

        StartCoroutine(ConsumeRoutine(block));
    }

    IEnumerator ConsumeRoutine(BlockType block)
    {
        GameObject go = block.gameObject;

        // input/collision kapat
        var drag = go.GetComponent<BlockDrag3D>();
        if (drag != null) drag.enabled = false;

        foreach (var c in go.GetComponentsInChildren<Collider>())
            c.enabled = false;

        var rb = go.GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;

        // rendererlar (fade için)
        var renderers = go.GetComponentsInChildren<Renderer>();
        // material instance (shared material bozulmasýn)
        foreach (var r in renderers)
        {
            if (r == null) continue;
            r.material = new Material(r.material);
        }

        Vector3 startPos = go.transform.position;
        Vector3 endPos = startPos + Vector3.up * popUp;

        Vector3 startScale = go.transform.localScale;
        Vector3 targetScale = startScale * Mathf.Clamp(endScale, 0.01f, 1f);

        float t = 0f;
        while (t < consumeDuration)
        {
            t += Time.deltaTime;
            float a = Mathf.Clamp01(t / consumeDuration);

            go.transform.position = Vector3.Lerp(startPos, endPos, a);
            go.transform.localScale = Vector3.Lerp(startScale, targetScale, a);

            // alpha fade (material color varsa)
            foreach (var r in renderers)
            {
                if (r == null) continue;
                if (!r.material.HasProperty("_Color")) continue;

                Color col = r.material.color;
                col.a = Mathf.Lerp(1f, 0f, a);
                r.material.color = col;
            }

            yield return null;
        }

        Destroy(go);

        if (LevelManager.Instance != null)
            LevelManager.Instance.OnBlockConsumed();
    }

    void OnTriggerEnter(Collider other)
    {
        var block = other.GetComponentInParent<BlockType>();
        if (block == null) return;

        Consume(block);
    }
}
