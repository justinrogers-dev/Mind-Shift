using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DoorController : MonoBehaviour
{
    public Transform spawnPoint;    
    public Transform stopPoint;       
    public float moveSpeed = 2f;
    public bool isStartDoor = true;
    
    private IdaMovement ida;

    void Start()
    {
        if (isStartDoor)
        {
            ida = FindObjectOfType<IdaMovement>();
            if (ida != null)
            {
                ida.gameObject.SetActive(false);
                StartCoroutine(StartSequence());
            }
        }
    }

    IEnumerator StartSequence()
    {
        yield return new WaitForSeconds(1f);

        ida.transform.position = spawnPoint.position;
        ida.transform.forward = (stopPoint.position - spawnPoint.position).normalized; // Face the direction of movement
        ida.gameObject.SetActive(true);
        ida.canMove = false;
        while (Vector3.Distance(ida.transform.position, stopPoint.position) > 0.01f)
        {
            ida.transform.position = Vector3.MoveTowards(
                ida.transform.position,
                stopPoint.position,
                moveSpeed * Time.deltaTime
            );
            yield return null;
        }
        ida.canMove = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!isStartDoor)
        {
            IdaMovement ida = other.GetComponent<IdaMovement>();
            if (ida != null)
            {
                Debug.Log("Deleting Ida");
                //Destroy(other.gameObject); 
                other.gameObject.SetActive(false);
            }
        }
    }  
}