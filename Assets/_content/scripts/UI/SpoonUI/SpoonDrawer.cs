using System.Collections.Generic;
using UnityEngine;

public class SpoonDrawer : MonoBehaviour
{
    [Header("References")]
    public RectTransform drawerArea;
    public GameObject spoonPrefab;
    public RectTransform spoonParent;

    [Header("Settings")]
    public int maxSpoons = 10;

    public static SpoonDrawer Instance { get; private set; }

    private void Awake()
    {
        Instance = this;

        if (spoonParent == null)
            spoonParent = GetComponent<RectTransform>();
    }

    /// <summary>
    /// Refresh to show exactly `spoonCount` active spoons in the drawer.
    /// This method preserves positions of existing ACTIVE spoons and only adds or removes the delta.
    /// Inactive (spent) spoon GameObjects are ignored and not reused.
    /// </summary>
    public void RefreshDrawer(int spoonCount)
    {
        if (spoonParent == null)
        {
            Debug.LogWarning("SpoonDrawer: No spoonParent assigned.");
            return;
        }

        if (drawerArea == null)
        {
            Debug.LogWarning("SpoonDrawer: No drawerArea assigned.");
            return;
        }

        if (spoonPrefab == null)
        {
            Debug.LogWarning("SpoonDrawer: No spoonPrefab assigned.");
            return;
        }

        spoonCount = Mathf.Clamp(spoonCount, 0, maxSpoons);

        // Get list of currently active (not spent) spoon children
        List<GameObject> activeSpoons = new List<GameObject>();
        foreach (Transform child in spoonParent)
        {
            if (!child.gameObject.CompareTag("Spoon")) continue;
            if (!child.gameObject.activeSelf) continue; // spent/inactive — ignore
            activeSpoons.Add(child.gameObject);
        }

        int activeCount = activeSpoons.Count;

        if (activeCount == spoonCount)
        {
            // Nothing to do
            return;
        }
        else if (activeCount < spoonCount)
        {
            // Need to spawn (spoonCount - activeCount) new spoons
            int toSpawn = spoonCount - activeCount;
            SpawnAdditionalSpoons(toSpawn, activeSpoons);
        }
        else // activeCount > spoonCount
        {
            // Need to remove extras (destroy some active spoons)
            int toRemove = activeCount - spoonCount;
            // remove the last ones (arbitrary, but stable)
            for (int i = activeSpoons.Count - 1; i >= 0 && toRemove > 0; i--, toRemove--)
            {
                if (activeSpoons[i] != null)
                    Destroy(activeSpoons[i]);
            }
        }
    }

    private void SpawnAdditionalSpoons(int count, List<GameObject> existing)
    {
        if (count <= 0) return;

        // Build list of occupied positions to avoid overlap
        List<Vector2> usedPositions = new List<Vector2>();
        foreach (var go in existing)
        {
            var rt = go.GetComponent<RectTransform>();
            if (rt != null) usedPositions.Add(rt.anchoredPosition);
        }

        Vector2 size = drawerArea.rect.size;
        float padding = 70f;
        float halfWidth = Mathf.Max(0f, size.x * 0.5f - padding);
        float halfHeight = Mathf.Max(0f, size.y * 0.5f - padding);
        float minDistance = Mathf.Min(Mathf.Max(1f, size.x), Mathf.Max(1f, size.y)) / (Mathf.Max(3, existing.Count + count + 2));

        for (int i = 0; i < count; i++)
        {
            Vector2 pos;
            int safety = 0;
            do
            {
                pos = new Vector2(
                    Random.Range(-halfWidth, halfWidth),
                    Random.Range(-halfHeight, halfHeight)
                );
                safety++;
            }
            while (usedPositions.Exists(p => Vector2.Distance(p, pos) < minDistance) && safety < 100);

            usedPositions.Add(pos);

            GameObject spoon = Instantiate(spoonPrefab, spoonParent);
            spoon.name = $"Spoon_{System.DateTime.Now.Ticks % 1000000}_{i}";

            // Ensure tag is set
            try
            {
                spoon.tag = "Spoon";
            }
            catch { }

            RectTransform spoonRect = spoon.GetComponent<RectTransform>();
            if (spoonRect == null) continue;

            spoonRect.anchoredPosition = pos;
            spoonRect.localRotation = Quaternion.Euler(0, 0, Random.Range(-30f, 30f));
            spoonRect.localScale = Vector3.one * Random.Range(0.9f, 1.1f);

            if (spoon.TryGetComponent<spoonBehavior>(out var behavior))
            {
                behavior.insideDrawer = true;
                behavior.restPosition = spoonRect.anchoredPosition;
            }
        }
    }
}
