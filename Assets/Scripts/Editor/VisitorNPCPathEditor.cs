// Editor 폴더에 생성해야 합니다: VisitorNPCPathEditor.cs
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[CustomEditor(typeof(VisitorNPCData))]
public class VisitorNPCPathEditor : Editor
{
    private VisitorNPCData npcData;
    private bool isEditingPath = false;
    private int selectedPointIndex = -1;
    
    void OnEnable()
    {
        npcData = (VisitorNPCData)target;
    }
    
    public override void OnInspectorGUI()
    {
        // 기본 인스펙터 그리기
        DrawDefaultInspector();
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Path Editor", EditorStyles.boldLabel);
        
        if (!npcData.useMovementPath)
        {
            EditorGUILayout.HelpBox("Enable 'Use Movement Path' to edit path points.", MessageType.Info);
            return;
        }
        
        // 경로 편집 모드 토글
        EditorGUI.BeginChangeCheck();
        isEditingPath = EditorGUILayout.Toggle("Edit Path in Scene", isEditingPath);
        if (EditorGUI.EndChangeCheck())
        {
            SceneView.RepaintAll();
        }
        
        if (isEditingPath)
        {
            EditorGUILayout.HelpBox(
                "• Click in Scene view to add points\n" +
                "• Drag points to move them\n" +
                "• Right-click points to delete\n" +
                "• Hold Shift to insert between points", 
                MessageType.Info);
        }
        
        EditorGUILayout.Space();
        
        // 경로 지점 목록
        EditorGUILayout.LabelField($"Path Points ({npcData.movementPath.Count})", EditorStyles.boldLabel);
        
        for (int i = 0; i < npcData.movementPath.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            
            EditorGUILayout.LabelField($"Point {i + 1}:", GUILayout.Width(60));
            
            EditorGUI.BeginChangeCheck();
            Vector2 newPos = EditorGUILayout.Vector2Field("", npcData.movementPath[i]);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(npcData, "Change Path Point");
                npcData.movementPath[i] = newPos;
                EditorUtility.SetDirty(npcData);
            }
            
            // 삭제 버튼
            if (GUILayout.Button("X", GUILayout.Width(20)))
            {
                Undo.RecordObject(npcData, "Delete Path Point");
                npcData.movementPath.RemoveAt(i);
                EditorUtility.SetDirty(npcData);
            }
            
            EditorGUILayout.EndHorizontal();
        }
        
        EditorGUILayout.Space();
        
        // 유틸리티 버튼들
        EditorGUILayout.BeginHorizontal();
        
        if (GUILayout.Button("Clear Path"))
        {
            if (EditorUtility.DisplayDialog("Clear Path", 
                "Are you sure you want to clear all path points?", "Yes", "No"))
            {
                Undo.RecordObject(npcData, "Clear Path");
                npcData.movementPath.Clear();
                EditorUtility.SetDirty(npcData);
            }
        }
        
        if (GUILayout.Button("Reverse Path"))
        {
            Undo.RecordObject(npcData, "Reverse Path");
            npcData.movementPath.Reverse();
            EditorUtility.SetDirty(npcData);
        }
        
        EditorGUILayout.EndHorizontal();
        
        // 경로 통계
        if (npcData.movementPath.Count > 1)
        {
            float totalDistance = CalculateTotalPathDistance();
            float estimatedTime = totalDistance / npcData.pathMoveSpeed;
            float totalTime = estimatedTime + (npcData.waitTimeAtPoint * npcData.movementPath.Count);
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Path Statistics", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Total Distance: {totalDistance:F2} units");
            EditorGUILayout.LabelField($"Movement Time: {estimatedTime:F2} seconds");
            EditorGUILayout.LabelField($"Total Time (with waits): {totalTime:F2} seconds");
        }
    }
    
    void OnSceneGUI()
    {
        if (!npcData.useMovementPath || !isEditingPath) return;
        
        Event e = Event.current;
        
        // 경로 지점 그리기 및 편집
        for (int i = 0; i < npcData.movementPath.Count; i++)
        {
            Vector3 worldPos = npcData.movementPath[i];
            worldPos.z = 0; // 2D이므로 z는 0
            
            // 핸들 그리기
            Handles.color = (i == selectedPointIndex) ? Color.yellow : npcData.pathColor;
            
            EditorGUI.BeginChangeCheck();
            var fmh_140_17_638894828190828661 = Quaternion.identity; Vector3 newWorldPos = Handles.FreeMoveHandle(
                worldPos, 
                0.5f, 
                Vector3.one * 0.5f, 
                Handles.CircleHandleCap
            );
            
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(npcData, "Move Path Point");
                npcData.movementPath[i] = new Vector2(newWorldPos.x, newWorldPos.y);
                EditorUtility.SetDirty(npcData);
            }
            
            // 라벨 표시
            Handles.Label(worldPos + Vector3.up * 0.5f, $"{i + 1}");
        }
        
        // 마우스 이벤트 처리
        if (e.type == EventType.MouseDown)
        {
            Vector3 mousePos = HandleUtility.GUIPointToWorldRay(e.mousePosition).origin;
            mousePos.z = 0;
            
            if (e.button == 0) // 왼쪽 클릭
            {
                if (e.shift && npcData.movementPath.Count > 1)
                {
                    // Shift + 클릭: 가장 가까운 두 점 사이에 삽입
                    InsertPointAtNearestSegment(mousePos);
                }
                else
                {
                    // 일반 클릭: 끝에 추가
                    Undo.RecordObject(npcData, "Add Path Point");
                    npcData.movementPath.Add(mousePos);
                    EditorUtility.SetDirty(npcData);
                }
                e.Use();
            }
            else if (e.button == 1) // 오른쪽 클릭
            {
                // 가장 가까운 점 삭제
                int nearestIndex = GetNearestPointIndex(mousePos);
                if (nearestIndex >= 0 && Vector3.Distance(mousePos, npcData.movementPath[nearestIndex]) < 1f)
                {
                    Undo.RecordObject(npcData, "Delete Path Point");
                    npcData.movementPath.RemoveAt(nearestIndex);
                    EditorUtility.SetDirty(npcData);
                    e.Use();
                }
            }
        }
        
        // 경로 선 그리기
        if (npcData.movementPath.Count > 1)
        {
            Handles.color = npcData.pathColor;
            for (int i = 0; i < npcData.movementPath.Count - 1; i++)
            {
                Vector3 start = npcData.movementPath[i];
                Vector3 end = npcData.movementPath[i + 1];
                Handles.DrawLine(start, end);
                
                // 방향 화살표
                Vector3 mid = (start + end) / 2f;
                Vector3 dir = (end - start).normalized;
                Handles.ArrowHandleCap(0, mid, Quaternion.LookRotation(dir), 0.5f, EventType.Repaint);
            }
            
            // 반복 경로면 마지막에서 첫 번째로 연결
            if (npcData.loopPath)
            {
                Vector3 last = npcData.movementPath[npcData.movementPath.Count - 1];
                Vector3 first = npcData.movementPath[0];
                Handles.DrawDottedLine(last, first, 2f);
            }
        }
        
        SceneView.RepaintAll();
    }
    
    private int GetNearestPointIndex(Vector3 position)
    {
        int nearestIndex = -1;
        float nearestDistance = float.MaxValue;
        
        for (int i = 0; i < npcData.movementPath.Count; i++)
        {
            float distance = Vector3.Distance(position, npcData.movementPath[i]);
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestIndex = i;
            }
        }
        
        return nearestIndex;
    }
    
    private void InsertPointAtNearestSegment(Vector3 position)
    {
        if (npcData.movementPath.Count < 2) return;
        
        int insertIndex = 1;
        float nearestDistance = float.MaxValue;
        
        for (int i = 0; i < npcData.movementPath.Count - 1; i++)
        {
            Vector3 start = npcData.movementPath[i];
            Vector3 end = npcData.movementPath[i + 1];
            
            float distance = HandleUtility.DistancePointToLineSegment(position, start, end);
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                insertIndex = i + 1;
            }
        }
        
        Undo.RecordObject(npcData, "Insert Path Point");
        npcData.movementPath.Insert(insertIndex, position);
        EditorUtility.SetDirty(npcData);
    }
    
    private float CalculateTotalPathDistance()
    {
        float totalDistance = 0f;
        
        for (int i = 0; i < npcData.movementPath.Count - 1; i++)
        {
            totalDistance += Vector2.Distance(npcData.movementPath[i], npcData.movementPath[i + 1]);
        }
        
        if (npcData.loopPath && npcData.movementPath.Count > 1)
        {
            totalDistance += Vector2.Distance(
                npcData.movementPath[npcData.movementPath.Count - 1], 
                npcData.movementPath[0]
            );
        }
        
        return totalDistance;
    }
}
#endif