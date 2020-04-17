using UnityEditor;
using UnityEngine;

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
    private const float maxDistanceFromLine = 3.0f;

    private Vector3 inspectorAddPoint = Vector3.zero;
    private int inspectorAddAtIndex = 0;

    private WaypointSystem waypointSystem;

    private Ray pointPositionRay;
    private RaycastHit pointPositionHit;
    private Ray mousePositionRay;
    private RaycastHit mousePositionHit;

    #region Constant Strings

    private const string labelWaypointSystem = "WaypointSystem";
    private const string labelInspectorEditPath = "Edit Path";
    private const string labelEditPathIcon = "EditCollider";
    private const string labelInspectorAddPoint = "Add point";
    private const string labelInspectorPosition = "Position";
    private const string labelInspectorAddAtIndex = "Add at index";

    private const string labelUndoAddWaypoint = "Add waypoint";
    private const string labelUndoRemoveWaypoint = "Remove waypoint";

    private const string labelGUIAddNewPoint = "Ctrl + click to add a new point.";
    private const string labelGUIInsertPoint = "Click to insert a point between two points.";
    private const string labelGUIRemovePoint = "Alt + click on a point to remove it.";

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
                    ToggleEditing();
                }
            }
        }
    }

    private void ToggleEditing()
    {
        editing = !editing;

        selectedPath = -1;
        closestDistance = -1;
        closestPointToMouse = Vector3.zero;
        
        Repaint();
        SceneView.RepaintAll();
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
            int pointIndex = GetPointIndex();

            if (holdingAlt)
            {
                TryRemovePoint(current, pointIndex);
            }
            else if (pointIndex == -1)
            {
                Undo.RecordObject(waypointSystem, labelUndoAddWaypoint);
                AddWaypointFromSceneView();
            }
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void AddWaypointFromSceneView()
    {
        if (holdingCtrl)
        {
            waypointSystem.AddPointToList(GetPointPosition(currentMouseWorldPoint));
        }
        else
        {
            if (Vector3.Distance(currentMouseWorldPoint, closestPointToMouse) < maxDistanceFromLine)
            {
                Vector3 pointPosition = GetPointPosition(closestPointToMouse);

                if (selectedPath == waypointSystem.Waypoints.Count)
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

    private void TryRemovePoint(Event current, int pointIndex)
    {
        Undo.RecordObject(waypointSystem, labelUndoRemoveWaypoint);
        if (pointIndex != -1)
        {
            waypointSystem.RemovePointAt(pointIndex);
        }
        current.Use();
    }

    private int GetPointIndex()
    {
        int index = 0;
        foreach (Vector3 point in waypointSystem.Waypoints)
        {
            float distance = Vector3.Distance(point, closestPointToMouse);
            if (distance < 0.5)
            {
                return index;
            }

            index++;
        }

        return -1;
    }

    private void DrawAddPointHandle()
    {
        if (editing && (selectedPath >= 0 || holdingCtrl))
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
            pointPositionRay = new Ray(currentPosition + (Vector3.up * 10), Vector3.down);

            if (Physics.Raycast(pointPositionRay, out pointPositionHit))
            {
                return pointPositionHit.point;
            }
        }

        return currentPosition;
    }

    private void SetCurrentMouseWorldPosition(SceneView sceneView)
    {
        mousePositionRay = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
        if (Physics.Raycast(mousePositionRay, out mousePositionHit))
        {
            currentMouseWorldPoint = mousePositionHit.point;
            sceneView.Repaint();
        }
    }

    void DrawWaypoints(WaypointSystem waypointSystem, Vector3 mousePosition)
    {
        Vector3 previousWaypoint = waypointSystem.transform.position;

        for (int i = 0; i < propWaypoints.arraySize; i++)
        {
            SerializedProperty prop = propWaypoints.GetArrayElementAtIndex(i);

            if (i > 0)
            {
                DrawLineBetweenPoints(previousWaypoint, prop.vector3Value, selectedPath == i);
            }

            if (i == propWaypoints.arraySize - 1 && waypointSystem.LoopPath)
            {
                DrawLineBetweenPoints(prop.vector3Value, propWaypoints.GetArrayElementAtIndex(0).vector3Value, selectedPath == i + 1);
            }

            if (editing)
            {
                DrawPositionHandleAtPoint(prop);

                if (i != 0)
                {
                    GetNearestPointOnLine(mousePosition, previousWaypoint, prop.vector3Value, i);
                }
                if (i == propWaypoints.arraySize - 1 && waypointSystem.LoopPath)
                {
                    GetNearestPointOnLine(mousePosition, prop.vector3Value, propWaypoints.GetArrayElementAtIndex(0).vector3Value, i + 1);
                }
            }
            else
            {
                DrawMarkerAtPoint(prop, i);
            }

            Handles.Label(prop.vector3Value, (i + 1).ToString(), EditorStyles.boldLabel);
            previousWaypoint = prop.vector3Value;
        }
    }

    private Vector3 GetNearestPointOnLine(Vector3 point, Vector3 start, Vector3 end, int pathIndex)
    {
        Vector3 rhs = point - start;
        Vector3 normalized = (end - start).normalized;

        float num = Vector3.Dot(normalized, rhs);

        if (num <= 0.0f)
        {
            return start;
        }

        if (num >= Vector3.Distance(start, end))
        {
            return end;
        }

        Vector3 vector = normalized * num;

        Vector3 returnPoint = start + vector;

        if (Vector3.Distance(point, returnPoint) < closestDistance || closestDistance < 0 || selectedPath == pathIndex)
        {
            closestPointToMouse = returnPoint;
            closestDistance = Vector3.Distance(point, returnPoint);
            selectedPath = pathIndex;
        }

        return returnPoint;
    }

    private void DrawMarkerAtPoint(SerializedProperty prop, int index)
    {
        if (index == 0)
        {
            Handles.color = Color.green;
        }
        else if (index == propWaypoints.arraySize - 1)
        {
            Handles.color = Color.red;
        }
        else
        {
            Handles.color = Color.black;
        }
        Handles.SphereHandleCap(-1, prop.vector3Value, Quaternion.identity, 1.0f, EventType.Repaint);

        Handles.color = Color.white;
    }

    private void DrawPositionHandleAtPoint(SerializedProperty prop)
    {
        prop.vector3Value = Handles.PositionHandle(prop.vector3Value, Quaternion.identity);

        prop.vector3Value = GetPointPosition(prop.vector3Value);
    }

    private void DrawLineBetweenPoints(Vector3 pointA, Vector3 pointB, bool isSelectedPath)
    {
        if (isSelectedPath)
        {
            Handles.color = Color.green;
        }

        Handles.DrawAAPolyLine(pointA, pointB);
        Handles.color = Color.white;
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
