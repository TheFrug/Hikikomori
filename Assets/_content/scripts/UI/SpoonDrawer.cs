using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SpoonDrawer : MonoBehaviour
{
    [Header("References")]
    public RectTransform drawerArea;  // UI panel representing the drawer
    public GameObject spoonPrefab;    // UI spoon prefab
    public RectTransform spoonParent; // optional parent for spawned spoons

    [Header("Settings")]
    public int maxSpoons = 10;

    private void Awake()
    {
        if (spoonParent == null)
            spoonParent = GetComponent<RectTransform>();
    }

    public void RefreshDrawer(int spoonCount)
    {
        // Clear existing spoons
        foreach (Transform child in spoonParent)
            Destroy(child.gameObject);

        RectTransform drawerRect = drawerArea;

        // Get actual pixel-space bounds inside the parent canvas
        Vector2 size = drawerRect.rect.size;
        float halfWidth = size.x * 0.5f;
        float halfHeight = size.y * 0.5f;

        // Keep track of used positions so they don’t overlap too tightly
        List<Vector2> usedPositions = new List<Vector2>();
        float minDistance = Mathf.Min(size.x, size.y) / (spoonCount + 2); // auto-spread spacing

        for (int i = 0; i < spoonCount; i++)
        {
            Vector2 pos;
            int safety = 0;

            // find a position not too close to existing spoons
            do
            {
                pos = new Vector2(
                    Random.Range(-halfWidth * 0.9f, halfWidth * 0.9f),
                    Random.Range(-halfHeight * 0.9f, halfHeight * 0.9f)
                );
                safety++;
            }
            while (usedPositions.Exists(p => Vector2.Distance(p, pos) < minDistance) && safety < 100);

            usedPositions.Add(pos);

            // Instantiate and place
            GameObject spoon = Instantiate(spoonPrefab, spoonParent);
            RectTransform spoonRect = spoon.GetComponent<RectTransform>();

            spoonRect.anchoredPosition = pos;
            spoonRect.localRotation = Quaternion.Euler(0, 0, Random.Range(-180f, 180f));
            spoonRect.localScale = Vector3.one * Random.Range(0.9f, 1.1f);

            Debug.Log($"Spoon {i} at {spoonRect.anchoredPosition}, drawer size {size}");
        }
    }
}
