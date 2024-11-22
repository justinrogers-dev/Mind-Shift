using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LevelManagement;
using WalkData;

public class Walkable : MonoBehaviour
{
    // List of possible paths from this block
    public List<WalkPath> possiblePaths = new List<WalkPath>();

    // Reference to the previous block in the path
    public Transform previousBlock;

    // Block properties to define its behavior
    [Header("Block Type")]
    public bool isStair;
    public bool movingGround;
    public bool isButton;
    public bool dontRotate;

    // Offset settings for position calculations
    [Header("Position Settings")]
    public float walkPointOffset = 0.5f;
    public float stairOffset = 0.4f;

    public Vector3 GetWalkPoint()
    {
        // Determine the height offset based on whether the block is a stair
        float heightOffset;
        if (isStair)
        {
            heightOffset = stairOffset;
        }
        else
        {
            heightOffset = 0;
        }

        // Calculate the walk point position based on offsets
        Vector3 baseOffset = transform.up * walkPointOffset;
        Vector3 stairHeight = transform.up * heightOffset;

        return transform.position + baseOffset + stairHeight;
    }

    private void OnDrawGizmos()
    {
        // Draw the walk point for visualization
        DrawWalkPoint();

        // Draw lines to visualize paths
        DrawPaths();
    }

    private void DrawWalkPoint()
    {
        // Set the color for the walk point visualization
        Gizmos.color = Color.blue;

        // Determine the height for visualization based on block type
        float heightVisualization;
        if (isStair)
        {
            heightVisualization = 0.4f;
        }
        else
        {
            heightVisualization = 0;
        }

        // Draw a sphere at the walk point location
        Gizmos.DrawSphere(GetWalkPoint(), 0.1f);
    }

    private void DrawPaths()
    {
        // Return if there are no paths to visualize
        if (possiblePaths == null)
            return;

        foreach (WalkPath path in possiblePaths)
        {
            // Return if the target path is null
            if (path.target == null)
                return;

            // Set the color of the path based on its active status
            Color pathColor;
            if (path.active)
            {
                pathColor = Color.black;
            }
            else
            {
                pathColor = Color.clear;
            }

            Gizmos.color = pathColor;

            // Get the start and end points of the path
            Vector3 startPoint = GetWalkPoint();
            Vector3 endPoint = path.target.GetComponent<Walkable>().GetWalkPoint();

            // Draw a line between the start and end points
            Gizmos.DrawLine(startPoint, endPoint);
        }
    }
}
