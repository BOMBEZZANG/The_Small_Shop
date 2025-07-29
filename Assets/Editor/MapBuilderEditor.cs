using UnityEngine;
using UnityEditor;
using UnityEngine.Tilemaps;

[CustomEditor(typeof(MapBuilder))]
public class MapBuilderEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // 기본 Inspector 그리기
        DrawDefaultInspector();
        
        MapBuilder mapBuilder = (MapBuilder)target;
        
        // 구분선
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        EditorGUILayout.Space(5);
        
        // 맵 빌드 섹션
        EditorGUILayout.LabelField("Map Actions", EditorStyles.boldLabel);
        
        // 맵 데이터 체크
        if (mapBuilder.mapData == null)
        {
            EditorGUILayout.HelpBox("Map Data가 설정되지 않았습니다!", MessageType.Warning);
        }
        
        EditorGUILayout.BeginHorizontal();
        
        // Build Map 버튼
        GUI.backgroundColor = Color.green;
        if (GUILayout.Button("Build Map", GUILayout.Height(30)))
        {
            if (mapBuilder.mapData != null)
            {
                mapBuilder.BuildMap();
                SceneView.RepaintAll();
            }
            else
            {
                EditorUtility.DisplayDialog("Error", "Map Data를 먼저 설정해주세요!", "OK");
            }
        }
        
        // Clear Map 버튼
        GUI.backgroundColor = Color.red;
        if (GUILayout.Button("Clear Map", GUILayout.Height(30)))
        {
            if (EditorUtility.DisplayDialog("Clear Map", "정말로 맵을 지우시겠습니까?", "Yes", "No"))
            {
                mapBuilder.ClearMap();
                SceneView.RepaintAll();
            }
        }
        
        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space(5);
        
        // 추가 액션 버튼들
        EditorGUILayout.BeginHorizontal();
        
        // Validate Map 버튼
        if (GUILayout.Button("Validate Map"))
        {
            ValidateMap(mapBuilder);
        }
        
        // Refresh References 버튼
        if (GUILayout.Button("Refresh References"))
        {
            RefreshTilemapReferences(mapBuilder);
        }
        
        EditorGUILayout.EndHorizontal();
        
        // 상태 표시
        EditorGUILayout.Space(5);
        ShowMapStatus(mapBuilder);
    }
    
    private void ValidateMap(MapBuilder mapBuilder)
    {
        if (mapBuilder.mapData == null)
        {
            Debug.LogError("Map Data가 없습니다!");
            return;
        }
        
        // 타일맵 참조 확인
        bool hasErrors = false;
        
        if (mapBuilder.groundTilemap == null)
        {
            Debug.LogError("Ground Tilemap이 연결되지 않았습니다!");
            hasErrors = true;
        }
        
        if (mapBuilder.buildingTilemap == null)
        {
            Debug.LogError("Building Tilemap이 연결되지 않았습니다!");
            hasErrors = true;
        }
        
        if (mapBuilder.decorationTilemap == null)
        {
            Debug.LogError("Decoration Tilemap이 연결되지 않았습니다!");
            hasErrors = true;
        }
        
        if (mapBuilder.collisionTilemap == null)
        {
            Debug.LogError("Collision Tilemap이 연결되지 않았습니다!");
            hasErrors = true;
        }
        
        // 타일 에셋 확인
        var tileAssets = mapBuilder.mapData.tileAssets;
        if (tileAssets.grassTile == null)
        {
            Debug.LogWarning("Grass Tile이 설정되지 않았습니다!");
        }
        
        if (!hasErrors)
        {
            Debug.Log("맵 검증 완료! 모든 참조가 올바르게 설정되었습니다.");
        }
    }
    
    private void RefreshTilemapReferences(MapBuilder mapBuilder)
    {
        GameObject mapGrid = GameObject.Find("MapGrid");
        if (mapGrid == null)
        {
            Debug.LogError("MapGrid GameObject를 찾을 수 없습니다!");
            return;
        }
        
        // 자동으로 타일맵 찾기
        Transform groundTm = mapGrid.transform.Find("Ground Tilemap");
        Transform buildingTm = mapGrid.transform.Find("Building Tilemap");
        Transform decorationTm = mapGrid.transform.Find("Decoration Tilemap");
        Transform collisionTm = mapGrid.transform.Find("Collision Tilemap");
        
        if (groundTm) mapBuilder.groundTilemap = groundTm.GetComponent<Tilemap>();
        if (buildingTm) mapBuilder.buildingTilemap = buildingTm.GetComponent<Tilemap>();
        if (decorationTm) mapBuilder.decorationTilemap = decorationTm.GetComponent<Tilemap>();
        if (collisionTm) mapBuilder.collisionTilemap = collisionTm.GetComponent<Tilemap>();
        
        // AutoTileSystem 찾기
        if (mapBuilder.autoTileSystem == null)
        {
            mapBuilder.autoTileSystem = mapBuilder.GetComponent<AutoTileSystem>();
        }
        
        EditorUtility.SetDirty(mapBuilder);
        Debug.Log("타일맵 참조가 자동으로 연결되었습니다!");
    }
    
    private void ShowMapStatus(MapBuilder mapBuilder)
    {
        EditorGUILayout.LabelField("Status", EditorStyles.boldLabel);
        
        // 참조 상태
        bool allReferencesSet = 
            mapBuilder.groundTilemap != null &&
            mapBuilder.buildingTilemap != null &&
            mapBuilder.decorationTilemap != null &&
            mapBuilder.collisionTilemap != null &&
            mapBuilder.mapData != null;
        
        if (allReferencesSet)
        {
            EditorGUILayout.HelpBox("✓ 모든 참조가 설정되었습니다.", MessageType.Info);
        }
        else
        {
            EditorGUILayout.HelpBox("⚠ 일부 참조가 누락되었습니다. 'Refresh References' 버튼을 클릭하세요.", MessageType.Warning);
        }
        
        // 맵 데이터 정보
        if (mapBuilder.mapData != null)
        {
            EditorGUILayout.LabelField($"Map: {mapBuilder.mapData.mapName}");
            EditorGUILayout.LabelField($"Size: {mapBuilder.mapData.width} x {mapBuilder.mapData.height}");
            
            // 타일 에셋 수
            int tileCount = CountAssignedTiles(mapBuilder.mapData.tileAssets);
            EditorGUILayout.LabelField($"Assigned Tiles: {tileCount}/23");
        }
    }
    
    private int CountAssignedTiles(TileAssetDictionary tiles)
    {
        int count = 0;
        if (tiles.grassTile != null) count++;
        if (tiles.pathTile != null) count++;
        if (tiles.stoneTile != null) count++;
        if (tiles.plazaTile != null) count++;
        if (tiles.waterTile != null) count++;
        if (tiles.wallTile != null) count++;
        if (tiles.playerHouseTile != null) count++;
        if (tiles.house1Tile != null) count++;
        if (tiles.house2Tile != null) count++;
        if (tiles.shopTile != null) count++;
        if (tiles.guildTile != null) count++;
        if (tiles.chiefHouseTile != null) count++;
        if (tiles.doorTile != null) count++;
        if (tiles.tree1Tile != null) count++;
        if (tiles.tree2Tile != null) count++;
        if (tiles.flower1Tile != null) count++;
        if (tiles.flower2Tile != null) count++;
        if (tiles.flower3Tile != null) count++;
        if (tiles.cropsTile != null) count++;
        if (tiles.fountainTile != null) count++;
        if (tiles.fountainCenterTile != null) count++;
        if (tiles.questBoardTile != null) count++;
        return count;
    }
}