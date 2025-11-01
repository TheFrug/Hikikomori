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

    public void RefreshDrawer(int spoonCount)
    {
        if (spoonParent == null)
        {
            Debug.LogWarning("SpoonDrawer: No spoonParent assigned.");
            return;
        }

        ClearSpoons();

        SpawnSpoons(spoonCount);
    }

    private void ClearSpoons()
    {
        List<GameObject> toDestroy = new List<GameObject>();

        foreach (Transform child in spoonParent)
            if (child.name.ToLower().Contains("spoon"))
                toDestroy.Add(child.gameObject);

        foreach (var obj in toDestroy)
            Destroy(obj);
    }

    private void SpawnSpoons(int count)
    {
        Vector2 size = drawerArea.rect.size;
        float padding = 70f; // adjust as needed
        float halfWidth = size.x * 0.5f - padding;
        float halfHeight = size.y * 0.5f - padding;
        float minDistance = Mathf.Min(size.x, size.y) / (count + 2);

        List<Vector2> usedPositions = new List<Vector2>();

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
            spoon.name = $"Spoon_{i}";

            RectTransform spoonRect = spoon.GetComponent<RectTransform>();
            spoonRect.anchoredPosition = pos;

            // Limit rotation to -30 to 30 degrees
            spoonRect.localRotation = Quaternion.Euler(0, 0, Random.Range(-30f, 30f));

            // Slight scale variation
            spoonRect.localScale = Vector3.one * Random.Range(0.9f, 1.1f);

            // Initialize spoon behavior
            if (spoon.TryGetComponent<spoonBehavior>(out var behavior))
            {
                behavior.insideDrawer = true;
                behavior.restPosition = spoonRect.anchoredPosition;
            }

            Debug.Log($"Spoon {i} spawned at {pos}");
        }
    }
}
