using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using Vuforia;

public class NavigationController : MonoBehaviour
{
    [System.Serializable]
    public struct TargetData
    {
        public string name;
        public Transform transform;
    }

    [Header("References")]
    [SerializeField] private NavMeshAgent navMeshAgent;
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private float recalculateInterval = 0.5f;
    [SerializeField] private List<TargetData> targets;
    [SerializeField] private TMP_Dropdown dropdown;
    [SerializeField] private TMP_Text debugText;
    [SerializeField] private float navMeshLogInterval = 1.0f;
    [SerializeField] private Transform arCamera;

    [Header("Status UI")]
    [SerializeField] private UnityEngine.UI.Image statusImage;
    [SerializeField] private TMP_Text statusText;

    private Transform currentTarget;
    private float nextRecalcTime;
    private NavMeshPath navMeshPath;
    [SerializeField] private bool isTracking;
    private float nextNavMeshLogTime;
    private int activeTrackedTargets = 0;

    private void LogMessage(string message, bool isWarning = false)
    {
        if (debugText != null)
        {
            debugText.text = message;
        }

        if (isWarning)
        {
            Debug.LogWarning(message);
        }
        else
        {
            Debug.Log(message);
        }
    }

    private void Awake()
    {
        navMeshPath = new NavMeshPath();
    }

    private void Start()
    {
        if (dropdown == null)
        {
            LogMessage("NavigationController: Dropdown reference is missing.", true);
            return;
        }

        dropdown.ClearOptions();
        List<string> options = new List<string>();
        for (int i = 0; i < targets.Count; i++)
        {
            options.Add(targets[i].name);
        }

        dropdown.AddOptions(options);
        dropdown.onValueChanged.AddListener(HandleDropdownInput);
    }

    private void Update()
    {
        if (navMeshAgent == null || lineRenderer == null) return;
        if (!isTracking) return;

        // Throttle path calculation to save CPU
        if (currentTarget != null && Time.time >= nextRecalcTime)
        {
            if (arCamera == null)
            {
                if (Time.time >= nextNavMeshLogTime)
                {
                    LogMessage("NavigationController: AR camera reference is missing.", true);
                    nextNavMeshLogTime = Time.time + navMeshLogInterval;
                }
            }
            else
            {
                NavMeshHit hit;
                if (NavMesh.SamplePosition(arCamera.position, out hit, 5.0f, NavMesh.AllAreas))
                {
                    navMeshAgent.Warp(hit.position);
                }
            }

            CalculatePath();
            nextRecalcTime = Time.time + recalculateInterval;
        }

        // Draw path
        if (navMeshAgent.hasPath && navMeshAgent.path != null)
        {
            Vector3[] corners = navMeshAgent.path.corners;
            for (int i = 0; i < corners.Length; i++)
            {
                corners[i].y += 0.05f;
            }

            lineRenderer.positionCount = corners.Length;
            lineRenderer.SetPositions(corners); // More efficient than a for-loop
        }
        else
        {
            lineRenderer.positionCount = 0;
        }
    }

    public void SetDestination(Transform target)
    {
        if (!isTracking) return;

        currentTarget = target;
        if (currentTarget != null)
        {
            LogMessage("NavigationController: Set destination to " + currentTarget.name + ".");
        }

        CalculatePath(); // Calculate immediately on button press
    }

    public void OnTargetFound()
    {
        activeTrackedTargets++;
        if (activeTrackedTargets > 0)
        {
            isTracking = true;
            LogMessage("NavigationController: Target found, tracking enabled.");
            if (statusImage != null)
            {
                statusImage.color = Color.green;
            }

            if (statusText != null)
            {
                statusText.text = "Tracked";
            }
        }
    }

    public void OnTargetLost()
    {
        activeTrackedTargets = Mathf.Max(0, activeTrackedTargets - 1);
        if (activeTrackedTargets == 0)
        {
            isTracking = false;
            LogMessage("NavigationController: Target lost, tracking disabled.", true);
            if (statusImage != null)
            {
                statusImage.color = Color.red;
            }

            if (statusText != null)
            {
                statusText.text = "Searching...";
            }

            if (lineRenderer != null)
            {
                lineRenderer.positionCount = 0;
            }
        }
    }

    public void HandleDropdownInput(int index)
    {
        if (index < 0 || index >= targets.Count)
        {
            LogMessage("NavigationController: Dropdown index out of range: " + index, true);
            return;
        }

        SetDestination(targets[index].transform);
    }

    private void CalculatePath()
    {
        if (currentTarget == null) return;

        if (navMeshAgent.CalculatePath(currentTarget.position, navMeshPath))
        {
            LogMessage("NavigationController: Path calculated. Corners: " + navMeshPath.corners.Length);
            navMeshAgent.SetPath(navMeshPath);
        }
        else
        {
            LogMessage("NavigationController: Failed to calculate path to target: " + currentTarget.name, true);
        }
    }
}