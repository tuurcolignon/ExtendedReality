using System.Collections.Generic;
using System.Diagnostics;
using TMPro;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

public class NavigationController : MonoBehaviour
{
    private enum TrackingState
    {
        Tracked,
        Drifting,
        Lost
    }

    [System.Serializable]
    public struct TargetData
    {
        public string name;
        public Transform transform;
    }

    [Header("References")]
    [SerializeField] private NavMeshAgent navMeshAgent;
    [SerializeField] private ARPathArrows pathArrows; 
    [SerializeField] private float recalculateInterval = 0.5f;
    [SerializeField] private float movementThreshold = 0.5f; 
    [SerializeField] private float navMeshSearchRadius = 20.0f; // Increased to prevent warp failure
    [SerializeField] private List<TargetData> targets;
    [SerializeField] private TMP_Dropdown dropdown;
    [SerializeField] private TMP_Text debugText;
    [SerializeField] private Transform arCamera;

    [Header("Status UI")]
    [SerializeField] private Image statusImage;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private float trackingTimeout = 5.0f; // 5-second persistence

    private Transform currentTarget;
    private float nextRecalcTime;
    private NavMeshPath navMeshPath;
    private int activeTrackedTargets = 0;
    private Vector3 lastCalculatedCameraPos;

    private TrackingState trackingState = TrackingState.Lost;
    private bool isTrackingLostTimerActive = false;
    private float lostTrackingTime;

    private const int MaxDebugLines = 6;
    private readonly Queue<string> debugLines = new Queue<string>();

    private void Awake()
    {
        navMeshPath = new NavMeshPath();
        LogDebug("Awake: init NavMeshPath");
    }

    private void Start()
    {
        if (dropdown == null) return;

        dropdown.ClearOptions();
        List<string> options = new List<string>();
        foreach (var target in targets)
        {
            options.Add(target.name);
        }

        dropdown.AddOptions(options);
        dropdown.onValueChanged.AddListener(HandleDropdownInput);
    }

    private void Update()
    {
        switch (trackingState)
        {
            case TrackingState.Tracked:
                UpdateTracked();
                break;
            case TrackingState.Drifting:
                UpdateDrifting();
                break;
            case TrackingState.Lost:
                // No updates while lost.
                break;
        }
    }

    public void SetDestination(Transform target)
    {
        // 1. Save the target immediately, even if tracking is currently lost
        currentTarget = target;
        LogDebug($"SetDestination: {target.name}");
        
        // 2. If no tracking, stop here. It will calculate automatically when tracking returns.
        if (trackingState != TrackingState.Tracked)
        {
            LogDebug("SetDestination: Target queued. Waiting for tracking.");
            return;
        }

        if (!TryEnsureAgentOnNavMesh())
        {
            LogDebug("SetDestination: Could not find NavMesh near camera.");
            return;
        }

        CalculatePath();
    }

    public void OnTargetFound()
    {
        activeTrackedTargets++;
        isTrackingLostTimerActive = false;

        if (activeTrackedTargets > 0 && trackingState != TrackingState.Tracked)
        {
            trackingState = TrackingState.Tracked;
            if (statusImage != null) statusImage.color = Color.green;
            if (statusText != null) statusText.text = "Tracked";
            LogDebug("OnTargetFound: tracking enabled");

            // FORCE calculation on next frame by tricking the distance check
            lastCalculatedCameraPos = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            nextRecalcTime = 0f;
        }
    }
    public void OnTargetLost()
    {
        activeTrackedTargets = Mathf.Max(0, activeTrackedTargets - 1);
        if (activeTrackedTargets == 0)
        {
            trackingState = TrackingState.Drifting;
            isTrackingLostTimerActive = true; // Start 5-sec timer
            lostTrackingTime = Time.time;

            if (statusImage != null) statusImage.color = Color.red;
            if (statusText != null) statusText.text = "Searching...";
            LogDebug("OnTargetLost: tracking disabled, starting timer");
        }
    }

    public void HandleDropdownInput(int index)
    {
        if (index < 0 || index >= targets.Count) return;
        SetDestination(targets[index].transform);
    }

    private void CalculatePath()
    {
        if (currentTarget == null || navMeshAgent == null) return;

        if (!TryEnsureAgentOnNavMesh())
        {
            LogDebug("CalculatePath: Agent not on NavMesh. Attempting Warp...");
            return;
        }

        // Use the Agent's actual pathfinding
        NavMeshPath path = new NavMeshPath();
        if (navMeshAgent.CalculatePath(currentTarget.position, path))
        {
            if (path.status == NavMeshPathStatus.PathComplete)
            {
                pathArrows.DrawPath(path.corners); 
                LogDebug($"Path Drawn: {path.corners.Length} nodes.");
            }
            else
            {
                LogDebug("Path partial or invalid.");
            }
        }
    }

    private void LogDebug(string message)
    {
        UnityEngine.Debug.Log(message);
        if (debugText == null) return;

        debugLines.Enqueue(message);
        while (debugLines.Count > MaxDebugLines)
        {
            debugLines.Dequeue();
        }

        debugText.text = string.Join("\n", debugLines);
    }

    private void UpdateTracked()
    {
        if (navMeshAgent == null || pathArrows == null) return;
        if (currentTarget == null || Time.time < nextRecalcTime) return;
        if (arCamera == null) return;

        if (Vector3.Distance(arCamera.position, lastCalculatedCameraPos) <= movementThreshold)
        {
            nextRecalcTime = Time.time + recalculateInterval;
            return;
        }

        if (!TryEnsureAgentOnNavMesh())
        {
            LogDebug("Update: Camera too far from NavMesh.");
            nextRecalcTime = Time.time + recalculateInterval;
            return;
        }

        CalculatePath();
        lastCalculatedCameraPos = arCamera.position;
        nextRecalcTime = Time.time + recalculateInterval;
    }

    private void UpdateDrifting()
    {
        // Handle 5-second persistence clearing
        if (isTrackingLostTimerActive && Time.time - lostTrackingTime > trackingTimeout)
        {
            isTrackingLostTimerActive = false;
            trackingState = TrackingState.Lost;
            if (pathArrows != null) pathArrows.DrawPath(new Vector3[0]);
            LogDebug("Tracking timeout reached. Path cleared.");
        }
    }

    private bool TryEnsureAgentOnNavMesh()
    {
        if (navMeshAgent == null || arCamera == null) return false;

        if (!NavMesh.SamplePosition(arCamera.position, out NavMeshHit hit, navMeshSearchRadius, NavMesh.AllAreas))
        {
            if (navMeshAgent.enabled) navMeshAgent.enabled = false;
            return false;
        }

        if (!navMeshAgent.enabled) navMeshAgent.enabled = true;

        if (!navMeshAgent.isOnNavMesh)
        {
            navMeshAgent.Warp(hit.position);
        }
        else
        {
            navMeshAgent.Warp(hit.position);
        }

        return true;
    }

    private void OnGUI()
    {
        // Simple debug buttons in the top-left corner of the Game View
        if (GUILayout.Button("Simulate: Target Found"))
        {
            OnTargetFound();
        }
        if (GUILayout.Button("Simulate: Target Lost"))
        {
            OnTargetLost();
        }
    }
}