using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[SelectionBase]
[RequireComponent(typeof(WaypointSystem))]
public class PatrolCapsule : MonoBehaviour
{
    [SerializeField] private float speed = 0.5f;

    private WaypointSystem waypointSystem;
    private int currentPointIndex = 0;
    private int direction = 1;

    private void Awake()
    {
        waypointSystem = GetComponent<WaypointSystem>();

        Debug.Assert(waypointSystem != null, "WaypointSystem missing on patrolling capsule");
    }

    private void Update()
    {
        if(Vector3.Distance(transform.position, waypointSystem.Waypoints[currentPointIndex]) < 0.1f)
        {
            bool reachedDestination = direction == 1 ? currentPointIndex == waypointSystem.Waypoints.Count - 1 : currentPointIndex == 0;

            if(reachedDestination)
            {
                if(waypointSystem.LoopPath)
                {
                    currentPointIndex = 0;
                }
                else
                {
                    direction = -direction;
                    currentPointIndex += direction;
                }
            }
            else
            {
                currentPointIndex += direction;
            }
        }

        transform.position = Vector3.MoveTowards(transform.position, waypointSystem.Waypoints[currentPointIndex], speed * Time.deltaTime);
    }
}
