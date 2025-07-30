using UnityEngine;
using UnityEditor;

public class PathEditorDebug : EditorWindow
{
    [MenuItem("Tools/Debug/Path Editor Status")]
    static void ShowWindow()
    {
        GetWindow<PathEditorDebug>("Path Editor Debug");
    }
    
    void OnGUI()
    {
        EditorGUILayout.LabelField("=== Path Editor Status ===");
        
        // 현재 선택된 오브젝트
        if (Selection.activeObject != null)
        {
            EditorGUILayout.LabelField("Selected: " + Selection.activeObject.name);
            
            VisitorNPCData data = Selection.activeObject as VisitorNPCData;
            if (data != null)
            {
                EditorGUILayout.LabelField("✓ VisitorNPCData 선택됨");
                EditorGUILayout.LabelField($"Use Movement Path: {data.useMovementPath}");
                EditorGUILayout.LabelField($"Path Points: {data.movementPath.Count}");
            }
            else
            {
                EditorGUILayout.LabelField("✗ VisitorNPCData가 아님");
            }
        }
        else
        {
            EditorGUILayout.LabelField("✗ 아무것도 선택되지 않음");
        }
        
        // Scene 뷰 상태
        if (SceneView.lastActiveSceneView != null)
        {
            EditorGUILayout.LabelField("✓ Scene View 활성");
        }
        else
        {
            EditorGUILayout.LabelField("✗ Scene View 비활성");
        }
        
        // Editor 폴더 체크
        string editorPath = "Assets/Scripts/Editor/VisitorNPCPathEditor.cs";
        if (AssetDatabase.LoadAssetAtPath<MonoScript>(editorPath) != null)
        {
            EditorGUILayout.LabelField("✓ Editor 스크립트 올바른 위치");
        }
        else
        {
            EditorGUILayout.LabelField("✗ Editor 스크립트가 Editor 폴더에 없음");
        }
    }
}