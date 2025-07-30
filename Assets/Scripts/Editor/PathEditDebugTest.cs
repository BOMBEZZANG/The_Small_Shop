// PathEditDebugTest.cs - Editor 폴더에 추가
using UnityEngine;
using UnityEditor;

public class PathEditDebugTest : EditorWindow
{
    [MenuItem("Tools/NPC/Debug/Path Edit Test")]
    static void ShowWindow()
    {
        GetWindow<PathEditDebugTest>("Debug Test");
    }
    
    void OnGUI()
    {
        if (GUILayout.Button("테스트: 현재 상태 확인"))
        {
            // 프록시 찾기
            PathEditProxy proxy = GameObject.FindObjectOfType<PathEditProxy>();
            
            if (proxy == null)
            {
                Debug.Log("❌ PathEditProxy를 찾을 수 없습니다!");
                return;
            }
            
            Debug.Log("✅ PathEditProxy 찾음!");
            Debug.Log($"- 편집 대상: {(proxy.targetNPCData != null ? proxy.targetNPCData.name : "없음")}");
            Debug.Log($"- 편집 모드: {proxy.isEditing}");
            Debug.Log($"- 경로 포인트 수: {proxy.targetNPCData?.movementPath?.Count ?? 0}");
            
            // 테스트 포인트 추가
            if (proxy.targetNPCData != null)
            {
                proxy.targetNPCData.movementPath.Add(new Vector2(5, 5));
                EditorUtility.SetDirty(proxy.targetNPCData);
                Debug.Log("✅ 테스트 포인트 (5,5) 추가됨!");
            }
        }
        
        if (GUILayout.Button("테스트: Scene 뷰 포커스"))
        {
            SceneView.lastActiveSceneView.Focus();
            Debug.Log("Scene 뷰 포커스됨");
        }
        
        EditorGUILayout.HelpBox(
            "1. 위 버튼들로 테스트\n" +
            "2. Console 창에서 로그 확인\n" +
            "3. Scene 뷰에서 (5,5) 위치에 포인트가 보이는지 확인", 
            MessageType.Info);
    }
}

// 임시 해결책: 수동 경로 추가 도구
public class ManualPathAdder : EditorWindow
{
    private VisitorNPCData targetData;
    private Vector2 newPoint = Vector2.zero;
    
    [MenuItem("Tools/NPC/Manual Path Adder")]
    static void ShowWindow()
    {
        GetWindow<ManualPathAdder>("수동 경로 추가");
    }
    
    void OnGUI()
    {
        EditorGUILayout.LabelField("수동 경로 추가 도구", EditorStyles.boldLabel);
        
        // 프록시에서 데이터 가져오기
        if (GUILayout.Button("현재 프록시에서 데이터 가져오기"))
        {
            PathEditProxy proxy = GameObject.FindObjectOfType<PathEditProxy>();
            if (proxy != null && proxy.targetNPCData != null)
            {
                targetData = proxy.targetNPCData;
            }
        }
        
        EditorGUILayout.Space();
        
        targetData = EditorGUILayout.ObjectField("NPC Data", 
            targetData, typeof(VisitorNPCData), false) as VisitorNPCData;
        
        if (targetData == null)
        {
            EditorGUILayout.HelpBox("VisitorNPCData를 선택하세요", MessageType.Warning);
            return;
        }
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField($"현재 포인트: {targetData.movementPath.Count}개");
        
        // 포인트 목록
        for (int i = 0; i < targetData.movementPath.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"{i+1}: {targetData.movementPath[i]}");
            if (GUILayout.Button("삭제", GUILayout.Width(50)))
            {
                Undo.RecordObject(targetData, "Delete Point");
                targetData.movementPath.RemoveAt(i);
                EditorUtility.SetDirty(targetData);
                SceneView.RepaintAll();
            }
            EditorGUILayout.EndHorizontal();
        }
        
        EditorGUILayout.Space();
        
        // 새 포인트 추가
        newPoint = EditorGUILayout.Vector2Field("새 포인트:", newPoint);
        
        if (GUILayout.Button("포인트 추가", GUILayout.Height(30)))
        {
            Undo.RecordObject(targetData, "Add Point");
            targetData.movementPath.Add(newPoint);
            EditorUtility.SetDirty(targetData);
            SceneView.RepaintAll();
            
            Debug.Log($"포인트 추가됨: {newPoint}");
        }
        
        // 빠른 추가 버튼들
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("빠른 추가:");
        
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("(0,0)")) AddPoint(Vector2.zero);
        if (GUILayout.Button("(5,0)")) AddPoint(new Vector2(5, 0));
        if (GUILayout.Button("(5,5)")) AddPoint(new Vector2(5, 5));
        if (GUILayout.Button("(0,5)")) AddPoint(new Vector2(0, 5));
        EditorGUILayout.EndHorizontal();
    }
    
    void AddPoint(Vector2 point)
    {
        if (targetData != null)
        {
            Undo.RecordObject(targetData, "Add Point");
            targetData.movementPath.Add(point);
            EditorUtility.SetDirty(targetData);
            SceneView.RepaintAll();
        }
    }
}