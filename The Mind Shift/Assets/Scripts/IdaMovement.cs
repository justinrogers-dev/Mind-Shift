using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using DG.Tweening;
using WalkData;

[SelectionBase]
public class IdaMovement : MonoBehaviour
{
    [Header("State")]
    public bool walking;
    private bool isRotating;
    private Vector3 previousPosition;

    
    [Header("Node References")]
    public Transform currentNode;
    public Transform clickedNode;


    [Header("Click Visualization")]
    public GameObject indicatorPrefab;
    public Color indicatorColor = new Color(1f, 1f, 1f, 0.5f);
    public float indicatorFadeTime = 0.5f;
    private GameObject currentIndicator;


    [Header("Movement Properties")]
    public float baseMovementSpeed = 5f;
    public float rotationSpeed = 180f;
    public float stairSpeedMultiplier = 1.5f;
    public float rotationDuration = 0.3f;
    public float jumpHeight = 0.2f;
    public float smoothTransitionDistance = 0.1f;

    
    [Header("Movement Curves")]
    public AnimationCurve movementCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    public AnimationCurve rotationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    public AnimationCurve jumpCurve = new AnimationCurve(
        new Keyframe(0, 0),
        new Keyframe(0.5f, 1),
        new Keyframe(1, 0)
    );

    // Path tracking using queues
    private Queue<Transform> pathQueue = new Queue<Transform>();
    private HashSet<Transform> visitedNodes = new HashSet<Transform>();
    private Queue<Vector3> movementPoints = new Queue<Vector3>();
    private Vector3 currentTargetPosition;
    private float currentMoveDuration;
    private float moveTimer;

    private void Start()
    {
        InitializeComponents();
    }


    private void InitializeComponents()
    {
        RayCastDown();
        InitializeClickIndicator();
        previousPosition = transform.position;
        SetupAnimationCurves();
    }

    // Setup animation curves for movement
    private void SetupAnimationCurves()
    {
        movementCurve.postWrapMode = WrapMode.Once;
        jumpCurve.postWrapMode = WrapMode.Once;
        rotationCurve.postWrapMode = WrapMode.Once;
    }

    private void Update()
    {
        HandleGroundDetection();
        if (!walking)
        {
            HandlePlayerInput();
        }
        UpdateClickIndicator();
    }

    private void HandleGroundDetection()
    {
        RayCastDown();
        UpdateParenting();
    }

    private void UpdateParenting()
    {
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
        if (!Input.GetMouseButtonDown(0)) return;

        Ray mouseRay = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(mouseRay, out RaycastHit mouseHit))
        {
            ProcessNodeClick(mouseHit);
        }
    }

    private void ProcessNodeClick(RaycastHit hit)
    {
        Walkable hitWalkable = hit.transform.GetComponent<Walkable>();
        if (hitWalkable == null) return;

        // Stop current movement and setup new path
        DOTween.Kill(gameObject.transform);
        clickedNode = hit.transform;
        ResetPathfinding();
        
        // Find path and start movement
        if (FindPath())
        {
            ShowIndicator(hitWalkable.GetWalkPoint());
            StartMovementSequence();
        }
    }

    private void ResetPathfinding()
    {
        // Reset pathfinding data
        pathQueue.Clear();
        visitedNodes.Clear();
        movementPoints.Clear();
    }

    private bool FindPath()
    {
        Queue<Transform> searchQueue = new Queue<Transform>();
        Dictionary<Transform, Transform> previousNodes = new Dictionary<Transform, Transform>();

        searchQueue.Enqueue(currentNode);
        visitedNodes.Add(currentNode);

        // Perform BFS search for the target node
        while (searchQueue.Count > 0)
        {
            Transform current = searchQueue.Dequeue();
            
            // Check if the target node is found
            if (current == clickedNode)
            {
                BuildPath(previousNodes);
                return true;
            }

            // Add all valid paths to the search queue
            foreach (WalkPath path in current.GetComponent<Walkable>().possiblePaths)
            {
                if (IsValidPath(path))
                {
                    ProcessValidPath(path, searchQueue, previousNodes, current);
                }
            }
        }

        return false;
    }

    private bool IsValidPath(WalkPath path)
    {
        // Check if the path is active and not visited
        return path.active && !visitedNodes.Contains(path.target);
    }

    // Process valid path and add to the search queue
    private void ProcessValidPath(WalkPath path, Queue<Transform> queue, 
        Dictionary<Transform, Transform> previous, Transform current)
    {
        // Add path to the search queue
        queue.Enqueue(path.target);
        visitedNodes.Add(path.target);
        previous[path.target] = current;
    }

    private void BuildPath(Dictionary<Transform, Transform> previousNodes)
    {
        // Build path from the target node to the current node
        Transform current = clickedNode;
        List<Transform> path = new List<Transform>();

        // Traverse the path in reverse order
        while (current != null && current != currentNode)
        {
            // Add current node to the path
            path.Add(current);
            // Move to the previous node
            previousNodes.TryGetValue(current, out current);
        }

        // Reverse the path and add to the queue
        path.Reverse();
        foreach (Transform node in path)
        {
            pathQueue.Enqueue(node);
        }
    }

    private void StartMovementSequence()
    {
        if (pathQueue.Count == 0){
            return;
        }

        walking = true;
        Transform nextNode = pathQueue.Dequeue();
        ProcessNextNode(nextNode);
    }

    private void ProcessNextNode(Transform node)
    {
        Walkable walkable = node.GetComponent<Walkable>();
        Vector3 targetPosition = walkable.GetWalkPoint();
        Vector3 moveDirection = (targetPosition - transform.position).normalized;

        // Validate and fix positions
        if (!IsValidPosition(targetPosition))
        {
            Debug.LogWarning("Invalid target position detected, using fallback position");
            targetPosition = GetSafePosition(walkable);
            moveDirection = (targetPosition - transform.position).normalized;
        }

        // Create movement sequence
        Sequence moveSequence = DOTween.Sequence();

        // Add the rotation
        if (!walkable.dontRotate && moveDirection != Vector3.zero)
        {
            AddRotationToSequence(moveSequence, moveDirection);
        }

        // Add movement with a safe path
        AddSafeMovementToSequence(moveSequence, targetPosition, walkable);

        // Update movement and handle node completion
        moveSequence.OnComplete(() => HandleNodeComplete(walkable));
    }

    private Vector3 GetSafePosition(Walkable walkable)
    {
        // Get base position from transform
        Vector3 basePosition = walkable.transform.position;
        
        // Add offsets for height and stairs
        float heightOffset = walkable.walkPointOffset;
        float stairOffset = 0f;
        if (walkable.isStair)
        {
            stairOffset = walkable.stairOffset;
        }
        // Return the safe position
        return new Vector3(
            basePosition.x,
            basePosition.y + heightOffset + stairOffset,
            basePosition.z
        );
    }

    private void AddSafeMovementToSequence(Sequence sequence, Vector3 targetPos, Walkable walkable)
    {
        // Calculate distance and duration for movement
        float distance = Vector3.Distance(transform.position, targetPos);
        float duration = CalculateMovementDuration(distance, walkable);

        // Check if the target position is a stair
        if (walkable.isStair)
        {
            try
            {
                // Create safe path points
                Vector3[] pathPoints = CreateSafeStairPath(transform.position, targetPos);
                
                // Validate all points in the path
                if (ValidatePathPoints(pathPoints))
                {
                    sequence.Append(
                        transform.DOPath(pathPoints, duration, PathType.CatmullRom)
                        .SetEase(Ease.InOutSine)
                    );
                }
                else
                {
                    // Direct movement if path points are invalid
                    sequence.Append(
                        transform.DOMove(targetPos, duration)
                        .SetEase(movementCurve)
                    );
                }
            }
            catch
            {
                // Direct movement if path points are invalid
                sequence.Append(
                    transform.DOMove(targetPos, duration)
                    .SetEase(movementCurve)
                );
            }
        }
        else
        {
            // Direct movement for non-stair paths
            sequence.Append(
                transform.DOMove(targetPos, duration)
                .SetEase(movementCurve)
            );
        }
    }

    private Vector3[] CreateSafeStairPath(Vector3 start, Vector3 end)
    {
        // Ensure valid start and end positions
        if (!IsValidPosition(start) || !IsValidPosition(end))
        {
            Debug.LogError("Invalid start or end position for stair path");
            return new Vector3[] { transform.position, end };
        }

        // Calculate safe midpoint
        Vector3 midPoint = (start + end) * 0.5f;
        float maxHeight = Mathf.Max(start.y, end.y) + jumpHeight;
        midPoint.y = maxHeight;

        return new Vector3[]
        {
            start,
            Vector3.Lerp(start, midPoint, 0.25f),
            midPoint,
            Vector3.Lerp(midPoint, end, 0.75f),
            end
        };
    }

    private bool ValidatePathPoints(Vector3[] points)
    {
        // Check if all points are valid positions
        foreach (Vector3 point in points)
        {
            if (!IsValidPosition(point))
            {
                return false;
            }
        }
        return true;
    }

    private bool IsValidPosition(Vector3 position)
    {
        // Check for valid and finite positions
        return !float.IsNaN(position.x) && 
            !float.IsNaN(position.y) && 
            !float.IsNaN(position.z) &&
            !float.IsInfinity(position.x) && 
            !float.IsInfinity(position.y) && 
            !float.IsInfinity(position.z);
    }

    private void AddRotationToSequence(Sequence sequence, Vector3 direction)
    {
        Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
        sequence.Append(
            transform.DORotateQuaternion(targetRotation, rotationDuration)
            .SetEase(rotationCurve)
        );
    }


    private float CalculateMovementDuration(float distance, Walkable walkable)
    {
        // Calculate movement duration based on distance and speed
        float baseTime = distance / baseMovementSpeed;
        return walkable.isStair ? baseTime * stairSpeedMultiplier : baseTime;
    }

    private void UpdateMovement(Walkable walkable)
    {
        //Update movement behavior
        // If rotation is enabled, update rotation
        if (!walkable.dontRotate)
        {
            UpdateRotationDuringMovement();
        }
        // If the target position is a stair, apply jump arc
        if (walkable.isStair)
        {
            ApplyJumpArc();
        }
    }

    private void UpdateRotationDuringMovement()
    {
        // Updates the object's rotation to match its movement direction.
        Vector3 movement = (transform.position - previousPosition);
        // Check if the object is moving
        if (movement.magnitude > 0.001f)
        {
            // Calculate the target rotation based on the movement direction
            Quaternion targetRotation = Quaternion.LookRotation(movement.normalized, Vector3.up);
            // Rotate towards the target rotation
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );
        }
        previousPosition = transform.position;
    }

    private void ApplyJumpArc()
    {
        // Apply a vertical offset to replicate a jump during movement
        float moveProgress = moveTimer / currentMoveDuration;
        float jumpOffset = jumpCurve.Evaluate(moveProgress) * jumpHeight;
        Vector3 position = transform.position;
        position.y += jumpOffset;
        transform.position = position;
    }

    private void HandleNodeComplete(Walkable walkable)
    {
        // Handle node completion and check if the player reaches the final button
        if (walkable.isButton)
        {
            if (walkable.gameObject.name == "18final")
            {
                GameManager.instance.OnFinalButtonReached();
            }
            else
            {
                GameManager.instance.RotateRightPivot();
            }
        }

        if (pathQueue.Count > 0)
        {
            StartMovementSequence();
        }
        else
        {
            CompleteMovement();
        }
    }

    private void CompleteMovement()
    {
        // Complete the movement sequence and reset pathfinding
        walking = false;
        ResetPathfinding();
    }


    // Initialize the click indicator object
    private void InitializeClickIndicator()
    {
        // Null check for the indicator prefab
        if (indicatorPrefab == null || currentIndicator != null){
            return;
        }
        
        // Instantiate the indicator prefab
        currentIndicator = Instantiate(indicatorPrefab);
        currentIndicator.SetActive(false);

        // Retrieve the renderer and set the color
        Renderer renderer = currentIndicator.GetComponent<Renderer>();
        if (renderer != null)
        {
            Material material = renderer.material;
            material.color = indicatorColor;
        }
    }

    private void ShowIndicator(Vector3 position)
    {
        // Null check for the indicator prefab
        if (currentIndicator == null){
            return;
        }

        // Set the position and activate the indicator
        currentIndicator.transform.position = position + Vector3.up * 0.1f;
        currentIndicator.SetActive(true);

        // Set the color of the indicator
        Renderer renderer = currentIndicator.GetComponent<Renderer>();
        // Null check for the renderer
        if (renderer != null)
        {
            renderer.material.color = indicatorColor;
        }
    }

    private void UpdateClickIndicator()
    {
        // Check if the player is walking or the indicator is inactive
        if (!walking || currentIndicator == null || !currentIndicator.activeInHierarchy){
            return;
        }

        //Retrieve the renderer
        Renderer renderer = currentIndicator.GetComponent<Renderer>();
        if (renderer != null)
        {
            // Fade out the indicator over time
            Color fadeColor = renderer.material.color;
            fadeColor.a -= Time.deltaTime / indicatorFadeTime;

            // Deactivate the indicator if the alpha is zero
            if (fadeColor.a <= 0)
            {
                currentIndicator.SetActive(false);
            }
            else
            {
                // Set the new color with the faded alpha
                renderer.material.color = fadeColor;
            }
        }
    }

    private void RayCastDown()
    {
        // Cast a ray downwards to detect the current node
        Ray playerRay = new Ray(transform.GetChild(0).position, -transform.up);
        // Check if the player is on a walkable node
        if (Physics.Raycast(playerRay, out RaycastHit playerHit))
        {
            Walkable hitWalkable = playerHit.transform.GetComponent<Walkable>();
            // Update the current node if a walkable node is detected
            if (hitWalkable != null)
            {
                currentNode = playerHit.transform;
            }
        }
    }

    // Visualize the path and movement gizmos in the scene view
    private void OnDrawGizmos()
    {
        DrawPathGizmos();
        DrawMovementGizmos();
    }

    // Visualize the path and movement gizmos in the scene view
    private void DrawPathGizmos()
    {
        if (pathQueue == null || pathQueue.Count == 0) return;

        Gizmos.color = Color.yellow;
        Transform[] pathArray = pathQueue.ToArray();
        for (int i = 0; i < pathArray.Length - 1; i++)
        {
            if (pathArray[i] != null && pathArray[i + 1] != null)
            {
                Vector3 start = pathArray[i].GetComponent<Walkable>().GetWalkPoint();
                Vector3 end = pathArray[i + 1].GetComponent<Walkable>().GetWalkPoint();
                Gizmos.DrawLine(start, end);
            }
        }
    }

    // Visualize the path and movement gizmos in the scene view
    private void DrawMovementGizmos()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(transform.position + Vector3.up * 0.5f, -Vector3.up);
    }
}

