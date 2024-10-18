using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Rotate : MonoBehaviour
{
    public float rotationSpeed = 90f; 
    public Vector3 rotationAxis = Vector3.forward;
    public float rotationAngle = 90f;

    private bool isRotating = false;
    private Quaternion startRotation;
    private Quaternion targetRotation;
    private float rotationProgress = 0f;
    public IdaMovement idaMovement;
    
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            StartRotation();
        }

        if (isRotating)
        {
            rotationProgress += rotationSpeed * Time.deltaTime / rotationAngle;
            transform.rotation = Quaternion.Slerp(startRotation, targetRotation, rotationProgress);

            if (rotationProgress >= 1f)
            {
                isRotating = false;
                transform.rotation = targetRotation;
                
                if (idaMovement != null)
                {
                    idaMovement.UpdateOrientationAfterRotation();
                }
            }
        }
    }

    void StartRotation()
    {
        if (!isRotating)
        {
            isRotating = true;
            rotationProgress = 0f;
            startRotation = transform.rotation;
            targetRotation = startRotation * Quaternion.AngleAxis(rotationAngle, rotationAxis);
        }
    }
}