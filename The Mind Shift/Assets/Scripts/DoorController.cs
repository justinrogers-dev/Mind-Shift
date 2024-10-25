using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DoorController : MonoBehaviour
{
    public bool isStartDoor = true;
    public float entranceDelay = 1f;
    public float entranceSpeed = 2f;
    public float entranceDistance = 2f;
    public float heightOffset = 0.5f;

    private IdaMovement ida;
    private Vector3 behindDoorPosition;
    private Vector3 targetPosition;
    private bool isEntering = false;
    public Vector3 walkDirection = Vector3.right;
    // Start is called before the first frame update
    void Start()
    {
        if (isStartDoor)
        {
            ida = FindObjectOfType<IdaMovement>();
            if (ida != null)
            {
                RaycastHit hit;
                if (Physics.Raycast(transform.position + Vector3.up, Vector3.down, out hit))
                {
                    float platformHeight = hit.point.y;
                    
                    behindDoorPosition = transform.position - walkDirection * 1f;
                    behindDoorPosition.y = platformHeight + heightOffset;
                    
                    targetPosition = transform.position + walkDirection * entranceDistance;
                    targetPosition.y = platformHeight + heightOffset;

                    ida.transform.position = behindDoorPosition;
                    ida.transform.forward = walkDirection;
                    ida.canMove = false;

                    Invoke("StartEntering", entranceDelay);
                }
            }
        }
    }

    void StartEntering()
    {
        isEntering = true;
    }

    // Update is called once per frame
    void Update()
    {
        if (isEntering && ida != null)
        {
            ida.transform.position = Vector3.MoveTowards(ida.transform.position, targetPosition, entranceSpeed * Time.deltaTime);
            if (Vector3.Distance(ida.transform.position, targetPosition) < 0.01f)
            {
                isEntering = false;
                ida.canMove = true;
            }
        }
    }

    public void EnterDoor(IdaMovement enteringIda)
    {
        if (!isStartDoor)
        {
            Debug.Log("Level complete");
            enteringIda.gameObject.SetActive(false);
        }
    }
}
