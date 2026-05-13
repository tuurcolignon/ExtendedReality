using UnityEngine;
using System.Collections.Generic;

public class ARPathArrows : MonoBehaviour
{
    [Header("3D Arrow Settings")]
    public GameObject arrowPrefab;
    [Tooltip("Distance between each arrow")]
    public float spacing = 1.0f; 
    [Tooltip("Lifts the arrows off the floor to prevent clipping")]
    public float heightOffset = 0.05f; 
    [Tooltip("Fixes imported models that don't point forward (e.g., try 90, 90, 0)")]
    public Vector3 rotationOffset = Vector3.zero; 
    [Tooltip("Global scale adjustment for the arrows")]
    public float arrowScale = 30f;

    private List<GameObject> arrowPool = new List<GameObject>();
    private int activeArrows = 0;
    private MaterialPropertyBlock arrowPropertyBlock;

    private void Awake()
    {
        arrowPropertyBlock = new MaterialPropertyBlock();
    }

    public void DrawPath(Vector3[] pathCorners)
    {
        activeArrows = 0;
        
        if (pathCorners == null || pathCorners.Length < 2) 
        { 
            HideUnusedArrows(); 
            return; 
        }

        float totalPathLength = 0f;
        for (int i = 0; i < pathCorners.Length - 1; i++)
        {
            totalPathLength += Vector3.Distance(pathCorners[i], pathCorners[i + 1]);
        }

        // Start 'spacing' meters away so the first arrow doesn't spawn inside the camera
        float distanceToNextArrow = spacing; 
        float cumulativeDistance = 0f;

        for (int i = 0; i < pathCorners.Length - 1; i++)
        {
            Vector3 startCorner = pathCorners[i];
            Vector3 endCorner = pathCorners[i + 1];

            // Elevate both corners to prevent floor clipping
            startCorner.y += heightOffset;
            endCorner.y += heightOffset;

            float segmentLength = Vector3.Distance(startCorner, endCorner);
            Vector3 segmentDirection = (endCorner - startCorner).normalized;

            float distanceCoveredOnSegment = 0f;

            // Spawn arrows as long as they fit within the current segment
            while (distanceCoveredOnSegment + distanceToNextArrow <= segmentLength)
            {
                distanceCoveredOnSegment += distanceToNextArrow;
                Vector3 spawnPosition = startCorner + segmentDirection * distanceCoveredOnSegment;
                
                float currentCumulativeDistance = cumulativeDistance + distanceCoveredOnSegment;
                float t = totalPathLength > 0f ? currentCumulativeDistance / totalPathLength : 1f;
                Color arrowColor = Color.Lerp(Color.red, Color.green, t);

                PositionArrow(spawnPosition, segmentDirection, arrowColor);
                
                // Reset timer for the next arrow
                distanceToNextArrow = spacing;
            }

            // Calculate how much distance is left on this segment, 
            // and subtract it from the wait time for the next arrow
            float remainingOnSegment = segmentLength - distanceCoveredOnSegment;
            distanceToNextArrow -= remainingOnSegment;

            cumulativeDistance += segmentLength;
        }

        HideUnusedArrows();
    }
    private void PositionArrow(Vector3 position, Vector3 direction, Color color)
    {
        GameObject arrow;
        
        // Pull from pool or instantiate new
        if (activeArrows < arrowPool.Count)
        {
            arrow = arrowPool[activeArrows];
            arrow.SetActive(true);
        }
        else
        {
            arrow = Instantiate(arrowPrefab, transform);
            arrowPool.Add(arrow);
        }

        // Apply Position & Scale
        arrow.transform.position = position;
        arrow.transform.localScale = Vector3.one * arrowScale;

        // Apply Rotation + Custom Offset
        if (direction != Vector3.zero)
        {
            Quaternion baseRotation = Quaternion.LookRotation(direction);
            arrow.transform.rotation = baseRotation * Quaternion.Euler(rotationOffset);
        }

        ApplyArrowColor(arrow, color);

        activeArrows++;
    }

    private void HideUnusedArrows()
    {
        for (int i = activeArrows; i < arrowPool.Count; i++)
        {
            arrowPool[i].SetActive(false);
        }
    }

    private void ApplyArrowColor(GameObject arrow, Color color)
    {
        Renderer arrowRenderer = arrow.GetComponentInChildren<Renderer>();
        if (arrowRenderer == null)
        {
            return;
        }

        arrowPropertyBlock.Clear();
        arrowPropertyBlock.SetColor("_Color", color);
        arrowPropertyBlock.SetColor("_BaseColor", color);
        arrowRenderer.SetPropertyBlock(arrowPropertyBlock);
    }
}