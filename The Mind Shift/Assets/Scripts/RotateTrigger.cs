using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotateTrigger : MonoBehaviour
{
    public Rotate rotatingPlatform;
    private bool hasTriggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (!hasTriggered && other.GetComponent<IdaMovement>() != null)
        {
            if (rotatingPlatform != null)
            {
                hasTriggered = true;
                // rotatingPlatform.TriggerRotation();
            }
        }
    }
}
