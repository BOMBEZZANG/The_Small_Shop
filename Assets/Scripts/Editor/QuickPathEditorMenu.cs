using UnityEngine;
using UnityEditor;

public static class QuickPathEditorMenu
{
    [MenuItem("Tools/NPC/Quick Path Edit/Create Path Edit Proxy")]
    public static void CreatePathEditProxy()
    {
        // Project에서 선택된 VisitorNPCData 가져오기
        VisitorNPCData selectedData = Selection.activeObject as VisitorNPCData;
        
        if (selectedData == null)
        {
            EditorUtility.DisplayDialog("오류", 
                "Project 창에서 VisitorNPCData를 선택한 후 실행하세요!", "확인");
            return;
        }
        
        // Scene에 프록시 생성
        GameObject proxy = new GameObject($"PathEdit_{selectedData.name}");
        PathEditProxy editProxy = proxy.AddComponent<PathEditProxy>(); // 수정: 타입 명시
        editProxy.targetNPCData = selectedData;
        editProxy.isEditing = true;
        
        // 프록시 선택
        Selection.activeGameObject = proxy;
        
        EditorUtility.DisplayDialog("성공", 
            $"{selectedData.name}의 경로 편집 프록시가 생성되었습니다!\n\n" +
            "이제 Scene 뷰에서 자유롭게 경로를 편집할 수 있습니다.\n" +
            "편집이 끝나면 프록시 오브젝트를 삭제하세요.", "확인");
    }
    
    [MenuItem("Tools/NPC/Quick Path Edit/타일맵 선택 방지")]
    public static void DisableTilemapSelection()
    {
        GameObject mapGrid = GameObject.Find("MapGrid");
        if (mapGrid != null)
        {
            // 수정: Tilemap 타입을 using 추가하거나 전체 네임스페이스 사용
            var tilemaps = mapGrid.GetComponentsInChildren<UnityEngine.Tilemaps.Tilemap>();
            foreach (var tilemap in tilemaps)
            {
                SceneVisibilityManager.instance.DisablePicking(tilemap.gameObject, true);
            }
            EditorUtility.DisplayDialog("완료", "타일맵 선택이 방지되었습니다.", "확인");
        }
        else
        {
            EditorUtility.DisplayDialog("오류", "MapGrid를 찾을 수 없습니다.", "확인");
        }
    }
    
    [MenuItem("Tools/NPC/Quick Path Edit/타일맵 선택 복원")]
    public static void EnableTilemapSelection()
    {
        GameObject mapGrid = GameObject.Find("MapGrid");
        if (mapGrid != null)
        {
            // 수정: Tilemap 타입을 using 추가하거나 전체 네임스페이스 사용
            var tilemaps = mapGrid.GetComponentsInChildren<UnityEngine.Tilemaps.Tilemap>();
            foreach (var tilemap in tilemaps)
            {
                SceneVisibilityManager.instance.DisablePicking(tilemap.gameObject, false);
            }
            EditorUtility.DisplayDialog("완료", "타일맵 선택이 복원되었습니다.", "확인");
        }
        else
        {
            EditorUtility.DisplayDialog("오류", "MapGrid를 찾을 수 없습니다.", "확인");
        }
    }
}