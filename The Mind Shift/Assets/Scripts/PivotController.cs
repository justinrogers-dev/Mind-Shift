using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class PivotController : MonoBehaviour
{

   void Update()
    {
        if (Input.GetKeyDown(KeyCode.Q) || Input.GetKeyDown(KeyCode.E))
        {
            transform.DOComplete();
            Vector3 currentRotation = transform.eulerAngles;
            transform.DORotate(
                new Vector3(180, 0, -90), 
                0.6f, 
                RotateMode.Fast
            ).SetEase(Ease.OutBack);
        }
    }
}
