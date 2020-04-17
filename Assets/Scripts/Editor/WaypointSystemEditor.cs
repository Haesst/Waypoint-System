using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;

[CustomEditor(typeof(WaypointSystem))]
public class WaypointSystemEditor : Editor
{
    private SerializedProperty propWaypoints;
    private SerializedProperty propLoopPath;
    private SerializedProperty propForcePointsToGround;

    private bool editing = false;
    private bool holdingCtrl = false;
    private bool holdingAlt = false;
    private Vector3 currentMouseWorldPoint = Vector3.zero;

    private int selectedPath = -1;
    private Vector3 closestPointToMouse;
    private float closestDistance = -1;

    private Vector3 inspectorAddPoint = Vector3.zero;
    private int inspectorAddAtIndex = 0;

    private WaypointSystem waypointSystem;

    #region Constant Strings

    const string labelWaypointSystem = "WaypointSystem";
    const string labelInspectorEditPath = "Edit Path";
    const string labelEditPathIcon = "EditCollider";
    const string labelInspectorAddPoint = "Add point";
    const string labelInspectorPosition = "Position";
    const string labelInspectorAddAtIndex = "Add at index";

    const string labelUndoAddWaypoint = "Add waypoint";
    const string labelUndoRemoveWaypoint = "Remove waypoint";

    const string labelGUIAddNewPoint = "Ctrl + click to add a new point.";
    const string labelGUIInsertPoint = "Click to insert a point between two points.";
    const string labelGUIRemovePoint = "Alt + click on a point to remove it.";

    #endregion Constant Strings

    #region Unity Methods

    private void OnEnable()
    {
        SceneView.duringSceneGui += DuringSceneGUI;
        propWaypoints = serializedObject.FindProperty("waypoints");
        propLoopPath = serializedObject.FindProperty("loopPath");
        propForcePointsToGround = serializedObject.FindProperty("forcePointsToGround");

        inspectorAddAtIndex = propWaypoints.arraySize;
        waypointSystem = target as WaypointSystem;

        waypointSystem.OnListCountChanged += OnListCountChanged;
    }
    private void OnDisable()
    {
        SceneView.duringSceneGui -= DuringSceneGUI;
        waypointSystem.OnListCountChanged -= OnListCountChanged;
    }
    #endregion Unity Methods

    private void OnListCountChanged(int count, int previousCount)
    {
        if(inspectorAddAtIndex == previousCount)
        {
            inspectorAddAtIndex = count;
        }
    }

    #region Inspector
    public override void OnInspectorGUI()
    {

        serializedObject.Update();

        DrawInspectorWaypointSystem();
        DrawInspectorManualPointInsertion();
        DrawInspectorPropertyFields();

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawInspectorWaypointSystem()
    {
        using (new GUILayout.VerticalScope(EditorStyles.helpBox))
        {
            GUILayout.Label(labelWaypointSystem, EditorStyles.boldLabel);
            GUILayout.Space(10f);
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label(labelInspectorEditPath);
                if (GUILayout.Button(EditorGUIUtility.IconContent(labelEditPathIcon), EditorStyles.miniButton))
                {
                    editing = !editing;
                    Repaint();
                    SceneView.RepaintAll();
                }
            }
        }
    }

    private void DrawInspectorManualPointInsertion()
    {
        using (new GUILayout.VerticalScope(EditorStyles.helpBox))
        {
            GUILayout.Label(labelInspectorAddPoint, EditorStyles.boldLabel);
            GUILayout.Space(10f);
            inspectorAddPoint = EditorGUILayout.Vector3Field(labelInspectorPosition, inspectorAddPoint);
            inspectorAddAtIndex = EditorGUILayout.IntField(labelInspectorAddAtIndex, inspectorAddAtIndex);

            inspectorAddAtIndex = Mathf.Clamp(inspectorAddAtIndex, 0, waypointSystem.Waypoints.Count);

            if (GUILayout.Button(labelInspectorAddPoint))
            {
                Vector3 pointPosition = GetPointPosition(inspectorAddPoint);

                Undo.RecordObject(waypointSystem, labelUndoAddWaypoint);
                waypointSystem.AddPointToList(pointPosition, inspectorAddAtIndex);

                Repaint();
                SceneView.RepaintAll();
            }
        }
    }

    private void DrawInspectorPropertyFields()
    {
        using (new GUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.PropertyField(propWaypoints);
            EditorGUILayout.PropertyField(propLoopPath);
            EditorGUILayout.PropertyField(propForcePointsToGround);
        }
    }

    #endregion Inspector

    #region SceneGUI
    private void DuringSceneGUI(SceneView sceneView)
    {
        if (editing)
        {
            HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
        }

        Event current = Event.current;
        holdingCtrl = (Event.current.modifiers & EventModifiers.Control) != 0;
        holdingAlt = (Event.current.modifiers & EventModifiers.Alt) != 0;

        SetCurrentMouseWorldPosition(sceneView);


        serializedObject.Update();

        DrawWaypoints(waypointSystem, currentMouseWorldPoint);
        DrawInformationBox(waypointSystem);
        DrawAddPointHandle();

        if (current.type == EventType.MouseDown && current.button == 0 && editing)
        {
            bool listContainsPoint = false;
            int pointIndex = -1;

            int index = 0;
            foreach (Vector3 point in waypointSystem.Waypoints)
            {
                float distance = Vector3.Distance(point, closestPointToMouse);
                if (distance < 0.5)
                {
                    listContainsPoint = true;
                    pointIndex = index;
                    break;
                }

                index++;
            }

            if (holdingAlt)
            {
                Undo.RecordObject(waypointSystem, labelUndoRemoveWaypoint);
                if (listContainsPoint && pointIndex != -1)
                {
                    waypointSystem.RemovePointAt(pointIndex);
                }
                current.Use();
            }
            else if (!listContainsPoint)
            {
                Undo.RecordObject(waypointSystem, labelUndoAddWaypoint);
                if (holdingCtrl)
                {
                    waypointSystem.AddPointToList(GetPointPosition(currentMouseWorldPoint));
                }
                else
                {
                    Vector3 pointPosition = GetPointPosition(closestPointToMouse);

                    if(selectedPath == waypointSystem.Waypoints.Count)
                    {
                        waypointSystem.AddPointToList(pointPosition);
                    }
                    else
                    {
                        waypointSystem.AddPointToList(pointPosition, selectedPath);
                    }
                }
            }
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawAddPointHandle()
    {
        if (selectedPath >= 0 || holdingCtrl)
        {
            Handles.color = Color.green;
            Handles.CubeHandleCap(-1, holdingCtrl ? currentMouseWorldPoint : closestPointToMouse, Quaternion.identity, 0.5f, EventType.Repaint);
            Handles.color = Color.white;
        }
    }

    private Vector3 GetPointPosition(Vector3 currentPosition)
    {
        if (propForcePointsToGround.boolValue)
        {
            Ray ray = new Ray(currentPosition + (Vector3.up * 10), Vector3.down);

            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                return hit.point;
            }
        }

        return currentPosition;
    }

    private void SetCurrentMouseWorldPosition(SceneView sceneView)
    {
        Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            currentMouseWorldPoint = hit.point;
            sceneView.Repaint();
        }
    }

    private Vector3 GetNearestPointOnEdge(Vector3 point, Vector3 start, Vector3 end, int pathIndex)
    {
        Vector3 rhs = point - start;
        Vector3 normalized = (end - start).normalized;

        float num = Vector3.Dot(normalized, rhs);

        if(num <= 0.0f)
        {
            return start;
        }

        if(num >= Vector3.Distance(start, end))
        {
            return end;
        }

        Vector3 vector = normalized * num;

        Vector3 returnPoint = start + vector;

        if(Vector3.Distance(point, returnPoint) < closestDistance || closestDistance < 0 || selectedPath == pathIndex)
        {
            closestPointToMouse = returnPoint;
            closestDistance = Vector3.Distance(point, returnPoint);
            selectedPath = pathIndex;
        }

        return returnPoint;
    }

    void DrawWaypoints(WaypointSystem waypointSystem, Vector3 mousePosition)
    {
        Vector3 previousWaypoint = waypointSystem.transform.position;

        for (int i = 0; i < propWaypoints.arraySize; i++)
        {
            SerializedProperty prop = propWaypoints.GetArrayElementAtIndex(i);

            if (i > 0)
            {
                if (selectedPath == i)
                {
                    Handles.color = Color.green;
                }
                Handles.DrawAAPolyLine(previousWaypoint, prop.vector3Value);
                Handles.color = Color.white;
            }

            if (i == propWaypoints.arraySize - 1 && waypointSystem.LoopPath)
            {
                if (selectedPath == i + 1)
                {
                    Handles.color = Color.green;
                }
                Handles.DrawAAPolyLine(prop.vector3Value, propWaypoints.GetArrayElementAtIndex(0).vector3Value);
                Handles.color = Color.white;
            }

            if (editing)
            {
                prop.vector3Value = Handles.PositionHandle(prop.vector3Value, Quaternion.identity);

                Ray ray = new Ray(prop.vector3Value + (Vector3.up * 20), Vector3.down);
                if(Physics.Raycast(ray, out RaycastHit hit))
                {
                    prop.vector3Value = hit.point;
                }
            }
            else
            {
                if (i == 0)
                {
                    Handles.color = Color.green;
                }
                else if (i == propWaypoints.arraySize - 1)
                {
                    Handles.color = Color.red;
                }
                else
                {
                    Handles.color = Color.black;
                }
                Handles.SphereHandleCap(-1, prop.vector3Value, Quaternion.identity, 1.0f, EventType.Repaint);
            }

            Handles.Label(prop.vector3Value, (i + 1).ToString(), EditorStyles.boldLabel);

            if (i != 0)
            {
                GetNearestPointOnEdge(mousePosition, previousWaypoint, prop.vector3Value, i);
            }
            if (i == propWaypoints.arraySize - 1 && waypointSystem.LoopPath)
            {
                GetNearestPointOnEdge(mousePosition, prop.vector3Value, propWaypoints.GetArrayElementAtIndex(0).vector3Value, i + 1);
            }

            Handles.color = Color.white;
            previousWaypoint = prop.vector3Value;
        }
    }

    void DrawInformationBox(WaypointSystem waypointSystem)
    {
        Rect size = new Rect(0, 0, 300, 200);
        float sizeButton = 20;
        Handles.BeginGUI();

        GUI.BeginGroup(new Rect(Screen.width - size.width - 10, Screen.height - size.height - 50, size.width, size.height));
        GUI.Box(size, labelWaypointSystem);

        Rect rc = new Rect(0, sizeButton, size.width, sizeButton);
        GUI.Label(rc, labelGUIAddNewPoint);
        rc.y += sizeButton;

        GUI.Label(rc, labelGUIInsertPoint);
        rc.y += sizeButton;

        GUI.Label(rc, labelGUIRemovePoint);
        rc.y += sizeButton;

        GUI.Label(rc, $"Total amount of points: {waypointSystem.Waypoints.Count}");
        rc.y += sizeButton;

        GUI.EndGroup();
        Handles.EndGUI();
    }
    #endregion SceneGUI
}
