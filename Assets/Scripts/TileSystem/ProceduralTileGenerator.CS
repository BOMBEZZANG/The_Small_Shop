using UnityEngine;
using UnityEngine.Tilemaps;
#if UNITY_EDITOR
using UnityEditor;
using System.IO;
#endif

public class ProceduralTileGenerator : MonoBehaviour
{
#if UNITY_EDITOR
    [MenuItem("Tools/Tilemap System/Generate Prototype Tiles")]
    public static void GenerateAllPrototypeTiles()
    {
        // 폴더 생성
        string spritePath = "Assets/Sprites/GeneratedTiles";
        string tilePath = "Assets/ScriptableObjects/GeneratedTiles";
        
        if (!AssetDatabase.IsValidFolder(spritePath))
        {
            AssetDatabase.CreateFolder("Assets/Sprites", "GeneratedTiles");
        }
        
        if (!AssetDatabase.IsValidFolder(tilePath))
        {
            AssetDatabase.CreateFolder("Assets/ScriptableObjects", "GeneratedTiles");
        }
        
        // 각 타일 타입별로 생성
        GenerateGroundTiles(spritePath, tilePath);
        GenerateBuildingTiles(spritePath, tilePath);
        GenerateNatureTiles(spritePath, tilePath);
        GenerateSpecialTiles(spritePath, tilePath);
        
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        Debug.Log("프로토타입 타일 생성 완료!");
    }
    
    static void GenerateGroundTiles(string spritePath, string tilePath)
    {
        // 잔디
        CreateColoredTile("Grass", new Color(0.2f, 0.8f, 0.2f), spritePath, tilePath);
        
        // 길
        CreatePatternTile("Path", new Color(0.6f, 0.5f, 0.3f), TilePattern.Cross, spritePath, tilePath);
        
        // 돌바닥
        CreatePatternTile("Stone", Color.gray, TilePattern.Brick, spritePath, tilePath);
        
        // 광장
        CreatePatternTile("Plaza", new Color(0.9f, 0.9f, 0.8f), TilePattern.Checkered, spritePath, tilePath);
        
        // 물
        CreatePatternTile("Water", new Color(0.2f, 0.5f, 0.9f, 0.8f), TilePattern.Wave, spritePath, tilePath);
    }
    
    static void GenerateBuildingTiles(string spritePath, string tilePath)
    {
        // 벽
        CreatePatternTile("Wall", new Color(0.4f, 0.3f, 0.2f), TilePattern.Brick, spritePath, tilePath);
        
        // 플레이어 집
        CreateBuildingTile("PlayerHouse", Color.blue, "P", spritePath, tilePath);
        
        // 일반 집들
        CreateBuildingTile("House1", new Color(0.8f, 0.6f, 0.4f), "H1", spritePath, tilePath);
        CreateBuildingTile("House2", new Color(0.7f, 0.5f, 0.3f), "H2", spritePath, tilePath);
        
        // 상점
        CreateBuildingTile("Shop", Color.yellow, "S", spritePath, tilePath);
        
        // 길드
        CreateBuildingTile("Guild", Color.magenta, "G", spritePath, tilePath);
        
        // 촌장 집
        CreateBuildingTile("ChiefHouse", Color.red, "C", spritePath, tilePath);
        
        // 문
        CreateIconTile("Door", new Color(0.5f, 0.3f, 0.1f), "▯", spritePath, tilePath);
    }
    
    static void GenerateNatureTiles(string spritePath, string tilePath)
    {
        // 나무들
        CreateIconTile("Tree1", new Color(0.1f, 0.6f, 0.1f), "🌲", spritePath, tilePath);
        CreateIconTile("Tree2", new Color(0.2f, 0.5f, 0.1f), "🌳", spritePath, tilePath);
        CreateIconTile("TreeBorder", new Color(0.1f, 0.4f, 0.1f), "T", spritePath, tilePath);
        
        // 꽃들
        CreateIconTile("Flower1", Color.red, "✿", spritePath, tilePath);
        CreateIconTile("Flower2", Color.yellow, "❀", spritePath, tilePath);
        CreateIconTile("Flower3", new Color(1f, 0.5f, 0f), "✾", spritePath, tilePath);
        
        // 작물
        CreateIconTile("Crops", new Color(0.8f, 0.7f, 0.3f), "▦", spritePath, tilePath);
    }
    
    static void GenerateSpecialTiles(string spritePath, string tilePath)
    {
        // 분수대
        CreateIconTile("Fountain", Color.cyan, "◉", spritePath, tilePath);
        CreateIconTile("FountainCenter", new Color(0.3f, 0.6f, 0.9f), "💧", spritePath, tilePath);
        
        // 퀘스트 게시판
        CreateIconTile("QuestBoard", new Color(0.6f, 0.4f, 0.2f), "!", spritePath, tilePath);
    }
    
    // 단색 타일 생성
    static void CreateColoredTile(string name, Color color, string spritePath, string tilePath)
    {
        Texture2D texture = new Texture2D(32, 32);
        Color[] pixels = new Color[32 * 32];
        
        // 테두리 효과
        for (int y = 0; y < 32; y++)
        {
            for (int x = 0; x < 32; x++)
            {
                if (x == 0 || x == 31 || y == 0 || y == 31)
                {
                    pixels[y * 32 + x] = color * 0.7f; // 더 어두운 테두리
                }
                else
                {
                    pixels[y * 32 + x] = color;
                }
            }
        }
        
        texture.SetPixels(pixels);
        texture.Apply();
        texture.filterMode = FilterMode.Point;
        
        SaveTileAssets(texture, name, spritePath, tilePath);
    }
    
    // 패턴 타일 생성
    static void CreatePatternTile(string name, Color color, TilePattern pattern, string spritePath, string tilePath)
    {
        Texture2D texture = new Texture2D(32, 32);
        Color[] pixels = new Color[32 * 32];
        Color darkColor = color * 0.7f;
        
        for (int y = 0; y < 32; y++)
        {
            for (int x = 0; x < 32; x++)
            {
                switch (pattern)
                {
                    case TilePattern.Checkered:
                        pixels[y * 32 + x] = ((x / 16) + (y / 16)) % 2 == 0 ? color : darkColor;
                        break;
                        
                    case TilePattern.Brick:
                        bool isBrickLine = y % 8 == 0;
                        bool isBrickColumn = (x + ((y / 8) % 2) * 16) % 16 == 0;
                        pixels[y * 32 + x] = (isBrickLine || isBrickColumn) ? darkColor : color;
                        break;
                        
                    case TilePattern.Cross:
                        bool isCross = (x >= 14 && x <= 17) || (y >= 14 && y <= 17);
                        pixels[y * 32 + x] = isCross ? darkColor : color;
                        break;
                        
                    case TilePattern.Wave:
                        float wave = Mathf.Sin(x * 0.3f + y * 0.1f) * 0.5f + 0.5f;
                        pixels[y * 32 + x] = Color.Lerp(darkColor, color, wave);
                        break;
                        
                    default:
                        pixels[y * 32 + x] = color;
                        break;
                }
            }
        }
        
        texture.SetPixels(pixels);
        texture.Apply();
        texture.filterMode = FilterMode.Point;
        
        SaveTileAssets(texture, name, spritePath, tilePath);
    }
    
    // 건물 타일 생성 (색상 + 텍스트)
    static void CreateBuildingTile(string name, Color color, string text, string spritePath, string tilePath)
    {
        Texture2D texture = new Texture2D(32, 32);
        Color[] pixels = new Color[32 * 32];
        
        // 배경색
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = color;
        }
        
        // 지붕 효과 (상단 1/3)
        for (int y = 21; y < 32; y++)
        {
            for (int x = 0; x < 32; x++)
            {
                pixels[y * 32 + x] = color * 0.7f;
            }
        }
        
        // 문 표시 (하단 중앙)
        for (int y = 0; y < 8; y++)
        {
            for (int x = 12; x < 20; x++)
            {
                pixels[y * 32 + x] = Color.black;
            }
        }
        
        texture.SetPixels(pixels);
        texture.Apply();
        texture.filterMode = FilterMode.Point;
        
        SaveTileAssets(texture, name, spritePath, tilePath);
    }
    
    // 아이콘 타일 생성
    static void CreateIconTile(string name, Color color, string icon, string spritePath, string tilePath)
    {
        Texture2D texture = new Texture2D(32, 32);
        Color[] pixels = new Color[32 * 32];
        
        // 배경
        Color bgColor = new Color(color.r * 0.3f, color.g * 0.3f, color.b * 0.3f, 0.5f);
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = bgColor;
        }
        
        // 중앙에 원형 아이콘 영역
        Vector2 center = new Vector2(16, 16);
        float radius = 12;
        
        for (int y = 0; y < 32; y++)
        {
            for (int x = 0; x < 32; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                if (distance <= radius)
                {
                    pixels[y * 32 + x] = color;
                }
                else if (distance <= radius + 1)
                {
                    pixels[y * 32 + x] = color * 0.7f; // 테두리
                }
            }
        }
        
        texture.SetPixels(pixels);
        texture.Apply();
        texture.filterMode = FilterMode.Point;
        
        SaveTileAssets(texture, name, spritePath, tilePath);
    }
    
    // 텍스처를 스프라이트와 타일 에셋으로 저장
    static void SaveTileAssets(Texture2D texture, string name, string spritePath, string tilePath)
    {
        // PNG로 저장
        byte[] pngData = texture.EncodeToPNG();
        string pngPath = $"{spritePath}/{name}_Tile.png";
        File.WriteAllBytes(pngPath, pngData);
        
        AssetDatabase.ImportAsset(pngPath);
        
        // 스프라이트 설정
        TextureImporter importer = AssetImporter.GetAtPath(pngPath) as TextureImporter;
        if (importer != null)
        {
            importer.spritePixelsPerUnit = 32;
            importer.textureType = TextureImporterType.Sprite;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }
        
        // Tile 에셋 생성
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(pngPath);
        if (sprite != null)
        {
            Tile tileAsset = ScriptableObject.CreateInstance<Tile>();
            tileAsset.sprite = sprite;
            
            string tileAssetPath = $"{tilePath}/{name}_Tile.asset";
            AssetDatabase.CreateAsset(tileAsset, tileAssetPath);
        }
        
        // 임시 텍스처 삭제
        DestroyImmediate(texture);
    }
    
    enum TilePattern
    {
        Solid,
        Checkered,
        Brick,
        Cross,
        Wave
    }
    
    // 모든 생성된 타일을 MapData에 자동 연결
    [MenuItem("Tools/Tilemap System/Auto Assign Generated Tiles")]
    public static void AutoAssignGeneratedTiles()
    {
        // FirstVillageMap 찾기
        MapData mapData = AssetDatabase.LoadAssetAtPath<MapData>("Assets/ScriptableObjects/MapData/FirstVillageMap.asset");
        if (mapData == null)
        {
            Debug.LogError("FirstVillageMap을 찾을 수 없습니다!");
            return;
        }
        
        string tilePath = "Assets/ScriptableObjects/GeneratedTiles";
        
        // 각 타일 자동 할당
        mapData.tileAssets.grassTile = LoadTile(tilePath, "Grass_Tile");
        mapData.tileAssets.pathTile = LoadTile(tilePath, "Path_Tile");
        mapData.tileAssets.stoneTile = LoadTile(tilePath, "Stone_Tile");
        mapData.tileAssets.plazaTile = LoadTile(tilePath, "Plaza_Tile");
        mapData.tileAssets.waterTile = LoadTile(tilePath, "Water_Tile");
        
        mapData.tileAssets.wallTile = LoadTile(tilePath, "Wall_Tile");
        mapData.tileAssets.playerHouseTile = LoadTile(tilePath, "PlayerHouse_Tile");
        mapData.tileAssets.house1Tile = LoadTile(tilePath, "House1_Tile");
        mapData.tileAssets.house2Tile = LoadTile(tilePath, "House2_Tile");
        mapData.tileAssets.shopTile = LoadTile(tilePath, "Shop_Tile");
        mapData.tileAssets.guildTile = LoadTile(tilePath, "Guild_Tile");
        mapData.tileAssets.chiefHouseTile = LoadTile(tilePath, "ChiefHouse_Tile");
        mapData.tileAssets.doorTile = LoadTile(tilePath, "Door_Tile");
        
        mapData.tileAssets.tree1Tile = LoadTile(tilePath, "Tree1_Tile");
        mapData.tileAssets.tree2Tile = LoadTile(tilePath, "Tree2_Tile");
        mapData.tileAssets.flower1Tile = LoadTile(tilePath, "Flower1_Tile");
        mapData.tileAssets.flower2Tile = LoadTile(tilePath, "Flower2_Tile");
        mapData.tileAssets.flower3Tile = LoadTile(tilePath, "Flower3_Tile");
        mapData.tileAssets.cropsTile = LoadTile(tilePath, "Crops_Tile");
        
        mapData.tileAssets.fountainTile = LoadTile(tilePath, "Fountain_Tile");
        mapData.tileAssets.fountainCenterTile = LoadTile(tilePath, "FountainCenter_Tile");
        mapData.tileAssets.questBoardTile = LoadTile(tilePath, "QuestBoard_Tile");
        
        EditorUtility.SetDirty(mapData);
        AssetDatabase.SaveAssets();
        
        Debug.Log("생성된 타일이 MapData에 자동 할당되었습니다!");
    }
    
    static TileBase LoadTile(string path, string name)
    {
        return AssetDatabase.LoadAssetAtPath<TileBase>($"{path}/{name}.asset");
    }
#endif
}