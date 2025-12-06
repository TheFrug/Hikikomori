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

    private GameObject CreateSpoonAtRandomPosition(List<Vector2> existingPositions, float minDistance)
    {
        Vector2 size = drawerArea.rect.size;
        float padding = 70f;
        float halfWidth = size.x * 0.5f - padding;
        float halfHeight = size.y * 0.5f - padding;

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
        while (existingPositions.Exists(p => Vector2.Distance(p, pos) < minDistance) && safety < 100);

        existingPositions.Add(pos);

        GameObject spoon = Instantiate(spoonPrefab, spoonParent);
        spoon.name = $"Spoon_{spoonParent.childCount}";

        RectTransform spoonRect = spoon.GetComponent<RectTransform>();
        spoonRect.anchoredPosition = pos;
        spoonRect.localRotation = Quaternion.Euler(0, 0, Random.Range(-30f, 30f));
        spoonRect.localScale = Vector3.one * Random.Range(0.9f, 1.1f);

        if (spoon.TryGetComponent<spoonBehavior>(out var behavior))
        {
            behavior.insideDrawer = true;
            behavior.restPosition = spoonRect.anchoredPosition;
        }

        return spoon;
    }

    public void RefreshDrawer(int targetCount)
    {
        if (spoonParent == null)
        {
            Debug.LogWarning("SpoonDrawer: No spoonParent assigned.");
            return;
        }

        // Collect existing spoon transforms
        List<Transform> current = new List<Transform>();
        foreach (Transform child in spoonParent)
            if (child.CompareTag("Spoon"))
                current.Add(child);

        int currentCount = current.Count;

        // Build list of occupied positions (for overlap avoidance)
        List<Vector2> existingPositions = new List<Vector2>();
        foreach (var t in current)
            existingPositions.Add(((RectTransform)t).anchoredPosition);

        float minDistance = Mathf.Min(drawerArea.rect.width, drawerArea.rect.height) / (targetCount + 2);

        // Remove extras
        if (currentCount > targetCount)
        {
            int remove = currentCount - targetCount;
            for (int i = 0; i < remove; i++)
                Destroy(current[i].gameObject);
        }

        // Add missing
        if (currentCount < targetCount)
        {
            int add = targetCount - currentCount;
            for (int i = 0; i < add; i++)
                CreateSpoonAtRandomPosition(existingPositions, minDistance);
        }
    }

    private void ClearSpoons()
    {
        List<GameObject> toDestroy = new List<GameObject>();

        foreach (Transform child in spoonParent)
            if (child.CompareTag("Spoon"))
                toDestroy.Add(child.gameObject);

        foreach (var obj in toDestroy)
            Destroy(obj);
    }
}
