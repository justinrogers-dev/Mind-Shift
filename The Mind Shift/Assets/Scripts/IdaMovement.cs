using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IdaMovement : MonoBehaviour
{
    public float moveSpeed = 2f;
    public float rotationSpeed = 10f;
    public LayerMask walkableSurface;
    public float pathCheckDistance = 1f;
    public float minDistanceToTarget = 0.1f;
    public float maxPathLength = 20f; 

    private Vector3 targetPosition;
    private bool isMoving = false;
    private bool isRotating = false;
    private Quaternion targetRotation;
    public bool canMove = true;

    void Update()
    {
        if (!canMove)
        {
            return;
        }
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, Mathf.Infinity, walkableSurface))
            {
                HandleMovementInput(hit.point);
            }
        }

        if (isRotating)
        {
            HandleRotation();
        }
        else if (isMoving)
        {
            MoveTowardsTarget();
        }
    }

    void HandleMovementInput(Vector3 clickPoint)
    {
        
        Vector3 directionToClick = clickPoint - transform.position;
        directionToClick.y = 0;

        float forwardDot = Vector3.Dot(directionToClick.normalized, transform.forward);
        float rightDot = Vector3.Dot(directionToClick.normalized, transform.right);

        Vector3 moveDirection;

        if (Mathf.Abs(forwardDot) > Mathf.Abs(rightDot))
        {
    
            if (forwardDot > 0)
            {
                moveDirection = transform.forward;
            }
            else
            {
                moveDirection = -transform.forward;
                targetRotation = Quaternion.LookRotation(-transform.forward);
                isRotating = true;
            }
        }
        else
        {
        
            if (rightDot > 0)
            {
                moveDirection = transform.right;
                targetRotation = Quaternion.LookRotation(transform.right);
            }
            else
            {
                moveDirection = -transform.right;
                targetRotation = Quaternion.LookRotation(-transform.right);
            }
            isRotating = true;
        }

        Vector3 farthestPoint = FindFarthestValidPoint(moveDirection);
        if (farthestPoint != transform.position)
        {
            targetPosition = farthestPoint;
            isMoving = true;
        }
    }

    Vector3 FindFarthestValidPoint(Vector3 direction)
    {
        Vector3 currentPoint = transform.position;
        Vector3 farthestValidPoint = currentPoint;
        float distanceChecked = 0f;
        float stepSize = pathCheckDistance;

        while (distanceChecked < maxPathLength)
        {
            Vector3 nextPoint = currentPoint + direction * stepSize;
            RaycastHit hit;
            Vector3 rayStart = nextPoint + Vector3.up * 0.5f;

        
            if (Physics.Raycast(rayStart, Vector3.down, out hit, 1f, walkableSurface))
            {
            
                if (!Physics.Raycast(currentPoint + Vector3.up * 0.1f, direction, stepSize - 0.1f))
                {
                    farthestValidPoint = nextPoint;
                    currentPoint = nextPoint;
                    distanceChecked += stepSize;
                }
                else
                {
                    break;
                }
            }
            else
            {
            
                break;
            }
        }

        RaycastHit heightHit;
        if (Physics.Raycast(farthestValidPoint + Vector3.up * 0.5f, Vector3.down, out heightHit, 1f, walkableSurface))
        {
            farthestValidPoint.y = heightHit.point.y;
        }

        return farthestValidPoint;
    }

    void HandleRotation()
    {
        if (transform.rotation != targetRotation)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            
            if (Quaternion.Angle(transform.rotation, targetRotation) < 1f)
            {
                transform.rotation = targetRotation;
                isRotating = false;
            }
        }
        else
        {
            isRotating = false;
        }
    }

    void MoveTowardsTarget()
    {
        if (Vector3.Distance(transform.position, targetPosition) > minDistanceToTarget)
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                targetPosition,
                moveSpeed * Time.deltaTime
            );
        }
        else
        {
            isMoving = false;
        }
    }

    public void UpdateOrientationAfterRotation()
    {
        isMoving = false;
        isRotating = false;
    }

    void OnTriggerEnter(Collider other)
    {
        DoorController door = other.GetComponent<DoorController>();
        if (door != null && !door.isStartDoor)
        {
            canMove = false;
            door.EnterDoor(this);
        }
    }
}
