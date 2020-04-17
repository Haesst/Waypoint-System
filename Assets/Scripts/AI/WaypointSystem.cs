using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[ExecuteAlways]
public class WaypointSystem : MonoBehaviour
{
    [SerializeField] private List<Vector3> waypoints = new List<Vector3>();
    [SerializeField] private bool loopPath = false;
    [SerializeField] private bool forcePointsToGround = true;

    public bool LoopPath => loopPath;
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

    //private void OnEnable()
    //{
    //    if(waypoints.Count <= 0)
    //    {
    //        waypoints.Add(transform.position);
    //    }

    //    so = new SerializedObject(this);
    //    propWaypoints = so.FindProperty("propWaypoints");
    //}

    //private void OnGUI()
    //{
    //    so.Update();
    //    for (int i = 0; i < propWaypoints.arraySize; i++)
    //    {
    //        SerializedProperty prop = propWaypoints.GetArrayElementAtIndex(i);
    //        prop.vector3Value = Handles.PositionHandle(prop.vector3Value, Quaternion.identity);
    //    }
    //    so.ApplyModifiedProperties();
    //}
}
