using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IdaMovement : MonoBehaviour
{
    public float rotationSpeed = 10f;
    public LayerMask walkableSurface;

    private Quaternion targetRotation;

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, Mathf.Infinity, walkableSurface))
            {
                SetNewFacingDirection(hit.point);
            }
        }

        RotateTowardsTargetDirection();
    }

    void SetNewFacingDirection(Vector3 targetPoint)
    {
        Vector3 direction = (targetPoint - transform.position).normalized;
        Vector3 platformNormal = transform.up;
        Vector3 projectedDirection = Vector3.ProjectOnPlane(direction, platformNormal).normalized;
        targetRotation = Quaternion.LookRotation(projectedDirection, platformNormal);
    }

    void RotateTowardsTargetDirection()
    {
        if (transform.rotation != targetRotation)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }

    public void UpdateOrientationAfterRotation()
    {
        Vector3 forwardDirection = Vector3.ProjectOnPlane(transform.forward, transform.up).normalized;
        targetRotation = Quaternion.LookRotation(forwardDirection, transform.up);
        transform.rotation = targetRotation;
    }
}
