using System;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class WaypointSystem : MonoBehaviour
{
    [SerializeField] private List<Vector3> waypoints = new List<Vector3>();
    [SerializeField] private bool loopPath = false;
    [SerializeField] private bool forcePointsToGround = true;

    public bool LoopPath => loopPath;
    public bool ForcePointsToGround => forcePointsToGround;
    public List<Vector3> Waypoints => waypoints;

    public event Action<int, int> OnListCountChanged;

    public void AddPointToList(Vector3 point)
    {
        waypoints.Add(point);
        OnListCountChanged.Invoke(waypoints.Count, waypoints.Count - 1);
    }
    public void AddPointToList(Vector3 point, int index)
    {
        if(index < 0 || index > waypoints.Count)
        {
            return;
        }

        waypoints.Insert(index, point);
        OnListCountChanged.Invoke(waypoints.Count, waypoints.Count - 1);
    }

    public void RemovePoint(Vector3 point)
    {
        waypoints.Remove(point);
        OnListCountChanged.Invoke(waypoints.Count, waypoints.Count + 1);
    }

    public void RemovePointAt(int index)
    {
        waypoints.RemoveAt(index);
        OnListCountChanged.Invoke(waypoints.Count, waypoints.Count + 1);
    }
}
