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
    [Tooltip("Fixes imported models that don't point forward (e.g., try 0, 90, 0)")]
    public Vector3 rotationOffset = Vector3.zero; 
    [Tooltip("Global scale adjustment for the arrows")]
    public float arrowScale = 1.0f;

    private List<GameObject> arrowPool = new List<GameObject>();
    private int activeArrows = 0;

    public void DrawPath(Vector3[] pathCorners)
    {
        activeArrows = 0;
        
        // Immediately clear arrows if the path is invalid or empty
        if (pathCorners == null || pathCorners.Length < 2) 
        { 
            HideUnusedArrows(); 
            return; 
        }

        float distanceSinceLastArrow = spacing; 
        Vector3 previousCorner = pathCorners[0];
        
        // Elevate the starting point
        previousCorner.y += heightOffset;

        for (int i = 1; i < pathCorners.Length; i++)
        {
            Vector3 currentCorner = pathCorners[i];
            
            // Elevate the current corner
            currentCorner.y += heightOffset;

            float segmentLength = Vector3.Distance(previousCorner, currentCorner);
            Vector3 segmentDirection = (currentCorner - previousCorner).normalized;

            float distanceCovered = 0;

            while (distanceCovered + distanceSinceLastArrow <= segmentLength)
            {
                distanceCovered += spacing - distanceSinceLastArrow;
                Vector3 spawnPosition = previousCorner + segmentDirection * distanceCovered;
                
                PositionArrow(spawnPosition, segmentDirection);
                distanceSinceLastArrow = 0;
            }

            distanceSinceLastArrow += (segmentLength - distanceCovered);
            previousCorner = currentCorner;
        }

        HideUnusedArrows();
    }

    private void PositionArrow(Vector3 position, Vector3 direction)
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

        activeArrows++;
    }

    private void HideUnusedArrows()
    {
        for (int i = activeArrows; i < arrowPool.Count; i++)
        {
            arrowPool[i].SetActive(false);
        }
    }
}