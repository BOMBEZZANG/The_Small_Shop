using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(PathEditProxy))]
public class PathEditProxyEditor : Editor
{
    private PathEditProxy proxy;
    
    void OnEnable()
    {
        proxy = (PathEditProxy)target;
        Debug.Log("PathEditProxyEditor OnEnable 호출됨!");
    }
    
    public override void OnInspectorGUI()
    {
        // 디버그 메시지 (수정된 부분)
        EditorGUILayout.HelpBox("커스텀 에디터가 작동 중입니다!", MessageType.Info); // Changed from .Success to .Info
        
        // 기본 Inspector 표시
        DrawDefaultInspector();
        
        if (proxy.targetNPCData == null)
        {
            EditorGUILayout.HelpBox("VisitorNPCData를 연결하세요!", MessageType.Warning);
            return;
        }
        
        EditorGUILayout.Space();
        
        // 편집 모드 버튼
        GUI.backgroundColor = proxy.isEditing ? Color.green : Color.white;
        if (GUILayout.Button(proxy.isEditing ? "편집 모드 ON" : "편집 모드 OFF", GUILayout.Height(40)))
        {
            proxy.isEditing = !proxy.isEditing;
            SceneView.RepaintAll();
        }
        GUI.backgroundColor = Color.white;
        
        if (proxy.isEditing)
        {
            EditorGUILayout.HelpBox(
                "Scene 뷰에서:\n" +
                "• 클릭: 포인트 추가\n" +
                "• 우클릭: 포인트 삭제", 
                MessageType.Info);
        }
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField($"경로 포인트: {proxy.targetNPCData.movementPath.Count}개");
        
        // 테스트 버튼
        if (GUILayout.Button("테스트 포인트 추가 (5,5)"))
        {
            Undo.RecordObject(proxy.targetNPCData, "Add Test Point");
            proxy.targetNPCData.movementPath.Add(new Vector2(5, 5));
            EditorUtility.SetDirty(proxy.targetNPCData);
            SceneView.RepaintAll();
            Debug.Log("테스트 포인트 추가됨!");
        }
        
        if (GUILayout.Button("경로 초기화"))
        {
            if (EditorUtility.DisplayDialog("확인", "모든 경로를 삭제하시겠습니까?", "삭제", "취소"))
            {
                Undo.RecordObject(proxy.targetNPCData, "Clear Path");
                proxy.targetNPCData.movementPath.Clear();
                EditorUtility.SetDirty(proxy.targetNPCData);
                SceneView.RepaintAll();
            }
        }
        
        EditorGUILayout.Space();
        GUI.backgroundColor = Color.red;
        if (GUILayout.Button("편집 완료 (프록시 삭제)", GUILayout.Height(30)))
        {
            DestroyImmediate(proxy.gameObject);
        }
        GUI.backgroundColor = Color.white;
    }
    
    void OnSceneGUI()
    {
        if (proxy == null || proxy.targetNPCData == null || !proxy.isEditing) 
        {
            return;
        }
        
        Event e = Event.current;
        
        // 디버그용
        if (e.type == EventType.MouseDown)
        {
            Debug.Log($"OnSceneGUI - 마우스 클릭 감지! 버튼: {e.button}");
        }
        
        // 포인트 그리기
        Handles.color = proxy.pathColor;
        for (int i = 0; i < proxy.targetNPCData.movementPath.Count; i++)
        {
            Vector3 pos = proxy.targetNPCData.movementPath[i];
            pos.z = 0;
            
            // 핸들 그리기
            if (Handles.Button(pos, Quaternion.identity, 0.5f, 0.5f, Handles.CircleHandleCap))
            {
                Debug.Log($"포인트 {i} 클릭됨!");
            }
            
            // 번호 표시
            Handles.Label(pos + Vector3.up * 0.5f, (i + 1).ToString());
        }
        
        // 마우스 클릭 처리
        if (e.type == EventType.MouseDown && e.button == 0)
        {
            Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
            Vector3 mousePos = ray.origin;
            mousePos.z = 0;
            
            Debug.Log($"Scene 클릭 위치: {mousePos}");
            
            // 포인트 추가
            Undo.RecordObject(proxy.targetNPCData, "Add Path Point");
            proxy.targetNPCData.movementPath.Add(mousePos);
            EditorUtility.SetDirty(proxy.targetNPCData);
            
            e.Use();
        }
    }
}