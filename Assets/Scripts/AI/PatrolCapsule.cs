using UnityEngine;
using UnityEngine.AI;

[SelectionBase]
[RequireComponent(typeof(WaypointSystem))]
public class PatrolCapsule : MonoBehaviour
{
    private WaypointSystem waypointSystem;
    private NavMeshAgent navMeshAgent;
    
    private int currentPointIndex = 0;
    private int direction = 1;

    private void Awake()
    {
        waypointSystem = GetComponent<WaypointSystem>();
        navMeshAgent = GetComponent<NavMeshAgent>();

        navMeshAgent.destination = waypointSystem.Waypoints[currentPointIndex];

        Debug.Assert(waypointSystem != null, "WaypointSystem missing on patrolling capsule");
    }

    private void Update()
    {
        if(navMeshAgent.remainingDistance <= navMeshAgent.stoppingDistance)
        {
            navMeshAgent.SetDestination(GetNextDestination());
        }
    }

    private Vector3 GetNextDestination()
    {
        bool reachedLastPoint = direction == 1 ? currentPointIndex == waypointSystem.Waypoints.Count - 1 : currentPointIndex == 0;

        if(reachedLastPoint)
        {
            if(waypointSystem.LoopPath)
            {
                currentPointIndex = 0;
                return waypointSystem.Waypoints[currentPointIndex];
            }
            else
            {
                direction = -direction;
            }
        }
        
        currentPointIndex += direction;

        return waypointSystem.Waypoints[currentPointIndex];
    }
}
