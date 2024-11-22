using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using DG.Tweening;
using WalkData;

[SelectionBase]
public class IdaMovement : MonoBehaviour
{
    // Tracks if the player is currently moving along a path
    public bool walking;

    // References to the nodes the player is currently on and clicked on
    [Header("Current Position")]
    public Transform currentNode;
    public Transform clickedNode;

    // Click feedback system
    [Header("Click Visualization")]
    public GameObject indicatorPrefab;
    public Color indicatorColor = new Color(1f, 1f, 1f, 0.5f);
    public float indicatorFadeTime = 0.5f;
    private GameObject currentIndicator;

    // Player movement configuration
    [Header("Movement Properties")]
    public float moveSpeed = 5f;
    public float rotationSpeed = 10f;

    // Path tracking variables
    private List<Transform> finalPath = new List<Transform>();
    private int currentIndex;
    private float moveStartTime;
    private Vector3 moveStartPos;
    private Vector3 targetPos;
    private Quaternion startRotation;
    private Quaternion targetRotation;

    private void Start()
    {
        // Initialize the current node
        RayCastDown();
        
        // Create and set up the click indicator if it doesn't exist
        InitializeClickIndicator();
    }

    private void Update()
    {
        // Check what node we're standing on
        HandleGroundDetection();

        // Process mouse clicks
        HandlePlayerInput();

        // Fade out click indicator if needed
        UpdateClickIndicator();
    }

    private void HandleGroundDetection()
    {
        // Cast a ray down to detect the current node
        RayCastDown();

        // Parent the player to moving platforms
        if (currentNode.GetComponent<Walkable>().movingGround)
        {
            transform.parent = currentNode.parent;
        }
        else
        {
            transform.parent = null;
        }
    }

    private void HandlePlayerInput()
    {
        // Check for mouse left-click
        if (!Input.GetMouseButtonDown(0))
        {
            return;
        }

        // Cast a ray from the mouse position to find the clicked node
        Ray mouseRay = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit mouseHit;

        if (Physics.Raycast(mouseRay, out mouseHit))
        {
            Walkable hitWalkable = mouseHit.transform.GetComponent<Walkable>();
            if (hitWalkable != null)
            {
                clickedNode = mouseHit.transform;

                // Stop any current movement
                DOTween.Kill(gameObject.transform);

                // Clear the path and find a new one
                finalPath.Clear();
                FindPath();

                // Show the click indicator at the clicked position
                ShowIndicator(hitWalkable.GetWalkPoint());
            }
        }
    }

    private void InitializeClickIndicator()
    {
        // Create and set up the click indicator if it doesn't exist
        if (indicatorPrefab == null || currentIndicator != null)
        {
            return;
        }

        currentIndicator = Instantiate(indicatorPrefab);
        currentIndicator.SetActive(false);

        // Set the indicator color
        Renderer renderer = currentIndicator.GetComponent<Renderer>();
        if (renderer != null)
        {
            Material material = renderer.material;
            material.color = indicatorColor;
        }
    }

    private void ShowIndicator(Vector3 position)
    {
        if (currentIndicator == null)
        {
            return;
        }

        // Position the indicator slightly above the node and show it
        currentIndicator.transform.position = position + Vector3.up * 0.1f;
        currentIndicator.SetActive(true);

        // Set the color of the indicator
        Renderer renderer = currentIndicator.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = indicatorColor;
        }
    }

    private void UpdateClickIndicator()
    {
        // Only update the indicator when walking
        if (!walking)
        {
            return;
        }

        // Fade out the indicator over time
        if (currentIndicator != null && currentIndicator.activeInHierarchy)
        {
            Renderer renderer = currentIndicator.GetComponent<Renderer>();
            if (renderer != null)
            {
                Color fadeColor = renderer.material.color;
                fadeColor.a -= Time.deltaTime / indicatorFadeTime;

                if (fadeColor.a <= 0)
                {
                    currentIndicator.SetActive(false);
                }
                else
                {
                    renderer.material.color = fadeColor;
                }
            }
        }
    }

    private void FindPath()
    {
        // Initialize lists for pathfinding
        List<Transform> nextNodes = new List<Transform>();
        List<Transform> visitedNodes = new List<Transform>();

        // Add initial valid paths to search
        Walkable currentWalkable = currentNode.GetComponent<Walkable>();
        foreach (WalkPath path in currentWalkable.possiblePaths)
        {
            if (path.active)
            {
                nextNodes.Add(path.target);
                path.target.GetComponent<Walkable>().previousBlock = currentNode;
            }
        }

        visitedNodes.Add(currentNode);

        // Traverse nodes recursively
        TraverseNode(nextNodes, visitedNodes);

        // Build the final path to the target node
        BuildPath();
    }

    private void TraverseNode(List<Transform> nextNodes, List<Transform> visitedNodes)
    {
        // Base case - no more nodes to check
        if (!nextNodes.Any())
        {
            return;
        }

        // Get the next node to check
        Transform current = nextNodes.First();
        nextNodes.Remove(current);

        // If we found the target, stop searching
        if (current == clickedNode)
        {
            return;
        }

        // Add all connected nodes to the search
        Walkable currentWalkable = current.GetComponent<Walkable>();
        foreach (WalkPath path in currentWalkable.possiblePaths)
        {
            if (!visitedNodes.Contains(path.target) && path.active)
            {
                nextNodes.Add(path.target);
                path.target.GetComponent<Walkable>().previousBlock = current;
            }
        }

        visitedNodes.Add(current);

        // Continue searching recursively
        TraverseNode(nextNodes, visitedNodes);
    }

    private void BuildPath()
    {
        // Work backward from the target to the start node
        Transform currentPathNode = clickedNode;
        while (currentPathNode != currentNode)
        {
            finalPath.Add(currentPathNode);
            Walkable nodeWalkable = currentPathNode.GetComponent<Walkable>();

            if (nodeWalkable.previousBlock != null)
            {
                currentPathNode = nodeWalkable.previousBlock;
            }
            else
            {
                return;
            }
        }

        // Add the clicked node to the path
        finalPath.Insert(0, clickedNode);

        // Start following the path
        TraversePath();
    }

    private void TraversePath()
    {
        // Set up the movement sequence
        Sequence moveSequence = DOTween.Sequence();
        walking = true;

        // Create movement tweens for each node in the path
        for (int i = finalPath.Count - 1; i > 0; i--)
        {
            Walkable currentWalkable = finalPath[i].GetComponent<Walkable>();
            float moveDuration = 0.2f;

            // Slow down movement on stairs
            if (currentWalkable.isStair)
            {
                moveDuration *= 1.5f;
            }

            // Move to the next point
            moveSequence.Append(transform.DOMove(currentWalkable.GetWalkPoint(), moveDuration)
                .SetEase(Ease.Linear));

            // Rotate to face the movement direction unless specified not to
            if (!currentWalkable.dontRotate)
            {
                moveSequence.Join(transform.DOLookAt(finalPath[i].position, 0.1f, AxisConstraint.Y, Vector3.up));
            }
        }

        // Handle button press at the end of the path
        if (clickedNode.GetComponent<Walkable>().isButton)
        {
            moveSequence.AppendCallback(() => GameManager.instance.RotateRightPivot());
        }

        // Clear the path after completing it
        moveSequence.AppendCallback(() => ClearPath());
    }

    private void ClearPath()
    {
        // Clear all previousBlock references
        foreach (Transform pathNode in finalPath)
        {
            if (pathNode != null)
            {
                Walkable walkable = pathNode.GetComponent<Walkable>();
                if (walkable != null)
                {
                    walkable.previousBlock = null;
                }
            }
        }

        finalPath.Clear();
        walking = false;
    }

    private void RayCastDown()
    {
        // Cast a ray downward to detect the current node
        Ray playerRay = new Ray(transform.GetChild(0).position, -transform.up);
        RaycastHit playerHit;

        if (Physics.Raycast(playerRay, out playerHit))
        {
            Walkable hitWalkable = playerHit.transform.GetComponent<Walkable>();
            if (hitWalkable != null)
            {
                currentNode = playerHit.transform;
            }
        }
    }

    private void OnDrawGizmos()
    {
        // Draw the path using gizmos
        DrawPathGizmos();

        // Draw the raycast for debugging
        DrawRaycastGizmos();
    }

    private void DrawPathGizmos()
    {
        // Draw lines showing the current path
        if (finalPath == null || finalPath.Count == 0)
        {
            return;
        }

        Gizmos.color = Color.yellow;
        for (int i = 0; i < finalPath.Count - 1; i++)
        {
            if (finalPath[i] != null && finalPath[i + 1] != null)
            {
                Vector3 start = finalPath[i].GetComponent<Walkable>().GetWalkPoint();
                Vector3 end = finalPath[i + 1].GetComponent<Walkable>().GetWalkPoint();
                Gizmos.DrawLine(start, end);
            }
        }
    }

    private void DrawRaycastGizmos()
    {
        // Draw the ground detection ray
        Gizmos.color = Color.blue;
        Ray ray = new Ray(transform.GetChild(0).position, -transform.up);
        Gizmos.DrawRay(ray);
    }
}
