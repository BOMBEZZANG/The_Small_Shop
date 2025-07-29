using UnityEngine;
using UnityEngine.Tilemaps;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

public class TilemapSetupUtility : MonoBehaviour
{
#if UNITY_EDITOR
    [MenuItem("Tools/Tilemap System/Setup Complete Tilemap System")]
    public static void SetupCompleteTilemapSystem()
    {
        // 1. Grid 생성
        GameObject gridObj = GameObject.Find("MapGrid");
        if (gridObj == null)
        {
            gridObj = new GameObject("MapGrid");
            Grid grid = gridObj.AddComponent<Grid>();
            grid.cellSize = Vector3.one;
        }
        
        // 2. Tilemap 레이어 생성
        CreateOrGetTilemapLayer(gridObj, "Ground Tilemap", 0, false);
        CreateOrGetTilemapLayer(gridObj, "Building Tilemap", 1, false);
        CreateOrGetTilemapLayer(gridObj, "Decoration Tilemap", 2, false);
        CreateOrGetTilemapLayer(gridObj, "Collision Tilemap", 3, true);
        
        // 3. MapBuilder 찾기 또는 생성
        GameObject mapBuilderObj = GameObject.Find("MapBuilder");
        if (mapBuilderObj == null)
        {
            mapBuilderObj = new GameObject("MapBuilder");
        }
        
        MapBuilder mapBuilder = mapBuilderObj.GetComponent<MapBuilder>();
        if (mapBuilder == null)
        {
            mapBuilder = mapBuilderObj.AddComponent<MapBuilder>();
        }
        
        // 4. AutoTileSystem 추가
        AutoTileSystem autoTileSystem = mapBuilderObj.GetComponent<AutoTileSystem>();
        if (autoTileSystem == null)
        {
            autoTileSystem = mapBuilderObj.AddComponent<AutoTileSystem>();
        }
        
        // 5. ProceduralMapGenerator 추가
        ProceduralMapGenerator generator = mapBuilderObj.GetComponent<ProceduralMapGenerator>();
        if (generator == null)
        {
            generator = mapBuilderObj.AddComponent<ProceduralMapGenerator>();
        }
        
        // 6. 참조 자동 연결
        AssignReferences(gridObj, mapBuilder, autoTileSystem, generator);
        
        Debug.Log("타일맵 시스템 설정 완료!");
        
        // Scene 저장 프롬프트
        if (EditorUtility.DisplayDialog("Save Scene", "씬을 저장하시겠습니까?", "저장", "취소"))
        {
            EditorSceneManager.SaveOpenScenes();
        }
    }
    
    static GameObject CreateOrGetTilemapLayer(GameObject parent, string name, int sortingOrder, bool isCollision)
    {
        Transform existing = parent.transform.Find(name);
        if (existing != null)
        {
            return existing.gameObject;
        }
        
        GameObject tilemapObj = new GameObject(name);
        tilemapObj.transform.SetParent(parent.transform);
        
        Tilemap tilemap = tilemapObj.AddComponent<Tilemap>();
        TilemapRenderer renderer = tilemapObj.AddComponent<TilemapRenderer>();
        
        renderer.sortingOrder = sortingOrder;
        
        if (isCollision)
        {
            renderer.enabled = false;
            TilemapCollider2D collider = tilemapObj.AddComponent<TilemapCollider2D>();
            CompositeCollider2D composite = tilemapObj.AddComponent<CompositeCollider2D>();
            Rigidbody2D rb = tilemapObj.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.bodyType = RigidbodyType2D.Static;
            }
            collider.usedByComposite = true;
        }
        
        return tilemapObj;
    }
    
    static void AssignReferences(GameObject gridObj, MapBuilder mapBuilder, 
        AutoTileSystem autoTileSystem, ProceduralMapGenerator generator)
    {
        // MapBuilder 참조 설정
        Transform groundTm = gridObj.transform.Find("Ground Tilemap");
        Transform buildingTm = gridObj.transform.Find("Building Tilemap");
        Transform decorationTm = gridObj.transform.Find("Decoration Tilemap");
        Transform collisionTm = gridObj.transform.Find("Collision Tilemap");
        
        if (groundTm) mapBuilder.groundTilemap = groundTm.GetComponent<Tilemap>();
        if (buildingTm) mapBuilder.buildingTilemap = buildingTm.GetComponent<Tilemap>();
        if (decorationTm) mapBuilder.decorationTilemap = decorationTm.GetComponent<Tilemap>();
        if (collisionTm) mapBuilder.collisionTilemap = collisionTm.GetComponent<Tilemap>();
        
        // AutoTileSystem 참조 설정
        autoTileSystem.mapBuilder = mapBuilder;
        mapBuilder.autoTileSystem = autoTileSystem;
        
        // Generator 참조 설정
        generator.mapBuilder = mapBuilder;
        generator.autoTileSystem = autoTileSystem;
        
        EditorUtility.SetDirty(mapBuilder);
        EditorUtility.SetDirty(autoTileSystem);
        EditorUtility.SetDirty(generator);
    }
    
    [MenuItem("Tools/Tilemap System/Create Sample Map Data")]
    public static void CreateSampleMapData()
    {
        CreateFolderIfNeeded("Assets/ScriptableObjects/MapData");
        
        // FirstVillageMap 생성
        MapData mapData = ScriptableObject.CreateInstance<MapData>();
        mapData.mapName = "First Village";
        mapData.width = 50;
        mapData.height = 50;
        
        // ASCII 맵 레이아웃 설정
        mapData.mapLayoutText = GetSampleMapLayout();
        
        string path = "Assets/ScriptableObjects/MapData/FirstVillageMap.asset";
        AssetDatabase.CreateAsset(mapData, path);
        AssetDatabase.SaveAssets();
        
        Debug.Log($"Map Data 생성 완료: {path}");
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = mapData;
    }
    
    [MenuItem("Tools/Tilemap System/Create Sample Tile Rules")]
    public static void CreateSampleTileRules()
    {
        CreateFolderIfNeeded("Assets/ScriptableObjects/TileRules");
        
        // Path Rule
        CreateTileRule("PathRule", TileType.Path);
        
        // Wall Rule  
        CreateTileRule("WallRule", TileType.Wall);
        
        // Water Rule
        CreateTileRule("WaterRule", TileType.Water);
        
        Debug.Log("Tile Rules 생성 완료!");
    }
    
    static void CreateTileRule(string name, TileType type)
    {
        TileRule rule = ScriptableObject.CreateInstance<TileRule>();
        rule.tileType = type;
        
        string path = $"Assets/ScriptableObjects/TileRules/{name}.asset";
        AssetDatabase.CreateAsset(rule, path);
    }
    
    static void CreateFolderIfNeeded(string folderPath)
    {
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            string[] folders = folderPath.Split('/');
            string currentPath = folders[0];
            
            for (int i = 1; i < folders.Length; i++)
            {
                string nextPath = currentPath + "/" + folders[i];
                if (!AssetDatabase.IsValidFolder(nextPath))
                {
                    AssetDatabase.CreateFolder(currentPath, folders[i]);
                }
                currentPath = nextPath;
            }
        }
    }
    
    static string GetSampleMapLayout()
    {
        return @"##################################################
##TTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTT##
##T..........................................T##
##T.HHH......ccc........SSS...............T##
##T.HHH......ccc........SSS...............T##
##T.HHH......ccc........SSS...............T##
##T...D........................D..........T##
##T.......................................ff..T##
##T............================.........ff..T##
##T............=~~~~~~~~~~~~~~=...........T##
##T.hhh.......=~+++++++++++~~=.GGG........T##
##T.hhh.......=~+*******+~~=.GGG........T##
##T.hhh.......=~+*@@@*+~~=.GGG........T##
##T...D.......=~+*@Q@*+~~=...D...........T##
##T............=~+*@@@*+~~=...............T##
##T............=~+*****+~~=............F..T##
##T............=~++++++++~~=...........FF.T##
##T............=~~~~~~~~~~~~~~=..........F..T##
##T............=======D========...........T##
##T..........................................T##
##T.........---------------------..........T##
##T..........................................T##
##T.CCC......................HHH..........T##
##T.CCC...........YYY........HPH..........T##
##T.CCC...........YYY........HHH..........T##
##T...D...........YYY...........D..........T##
##T..........................................T##
##T..........................................T##
##TTTTTTTTTTTTTTTTTTTTTDTTTTTTTTTTTTTTTTTTTTTT##
##################################################";
    }
#endif
}