using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[System.Serializable]
public class BuildingPlacement
{
    public TileType buildingType;
    public Vector2Int size;
    public int count = 1;
    public float minDistanceFromCenter = 5f;
    public float maxDistanceFromCenter = 20f;
    public bool requiresPath = true;
}

[System.Serializable] 
public class MapGenerationSettings
{
    [Header("Basic Settings")]
    public int mapWidth = 50;
    public int mapHeight = 50;
    public int borderThickness = 2;
    
    [Header("Town Square")]
    public Vector2Int squareSize = new Vector2Int(10, 10);
    public bool centerSquare = true;
    
    [Header("Buildings")]
    public List<BuildingPlacement> buildings = new List<BuildingPlacement>();
    
    [Header("Paths")]
    public float pathDensity = 0.15f; // 0-1
    public int pathWidth = 1;
    
    [Header("Decoration")]
    public float treeDensity = 0.1f;
    public float flowerDensity = 0.05f;
    
    [Header("Seed")]
    public int seed = 12345;
    public bool useRandomSeed = false;
}

public class ProceduralMapGenerator : MonoBehaviour
{
    [Header("Generation Settings")]
    public MapGenerationSettings settings;
    
    [Header("References")]
    public MapBuilder mapBuilder;
    public AutoTileSystem autoTileSystem;
    
    private Dictionary<Vector3Int, TileType> generatedMap;
    private System.Random random;
    
    [ContextMenu("Generate Random Map")]
    public void GenerateRandomMap()
    {
        // 시드 설정
        int usedSeed = settings.useRandomSeed ? Random.Range(0, int.MaxValue) : settings.seed;
        random = new System.Random(usedSeed);
        Debug.Log($"Generating map with seed: {usedSeed}");
        
        // 맵 초기화
        generatedMap = new Dictionary<Vector3Int, TileType>();
        InitializeMap();
        
        // 생성 단계
        CreateBorder();
        CreateTownSquare();
        PlaceBuildings();
        GeneratePaths();
        AddDecorations();
        
        // 맵 적용
        ApplyGeneratedMap();
    }
    
    void InitializeMap()
    {
        for (int x = 0; x < settings.mapWidth; x++)
        {
            for (int y = 0; y < settings.mapHeight; y++)
            {
                generatedMap[new Vector3Int(x, y, 0)] = TileType.Grass;
            }
        }
    }
    
    void CreateBorder()
    {
        // 외벽
        for (int x = 0; x < settings.mapWidth; x++)
        {
            for (int y = 0; y < settings.mapHeight; y++)
            {
                if (x < settings.borderThickness || x >= settings.mapWidth - settings.borderThickness ||
                    y < settings.borderThickness || y >= settings.mapHeight - settings.borderThickness)
                {
                    // 외곽 1칸은 벽, 나머지는 나무
                    if (x == 0 || x == settings.mapWidth - 1 || y == 0 || y == settings.mapHeight - 1)
                        generatedMap[new Vector3Int(x, y, 0)] = TileType.Wall;
                    else
                        generatedMap[new Vector3Int(x, y, 0)] = TileType.TreeBorder;
                }
            }
        }
        
        // 남쪽 출구
        int exitX = settings.mapWidth / 2;
        generatedMap[new Vector3Int(exitX, 0, 0)] = TileType.Door;
        generatedMap[new Vector3Int(exitX, 1, 0)] = TileType.Path;
    }
    
    void CreateTownSquare()
    {
        if (!settings.centerSquare) return;
        
        Vector2Int center = new Vector2Int(settings.mapWidth / 2, settings.mapHeight / 2);
        Vector2Int squareStart = center - settings.squareSize / 2;
        
        // 광장 바닥
        for (int x = 0; x < settings.squareSize.x; x++)
        {
            for (int y = 0; y < settings.squareSize.y; y++)
            {
                Vector3Int pos = new Vector3Int(squareStart.x + x, squareStart.y + y, 0);
                
                // 테두리는 돌, 내부는 광장 타일
                if (x == 0 || x == settings.squareSize.x - 1 || 
                    y == 0 || y == settings.squareSize.y - 1)
                {
                    generatedMap[pos] = TileType.Stone;
                }
                else
                {
                    generatedMap[pos] = TileType.Plaza;
                }
            }
        }
        
        // 중앙 분수대
        Vector3Int fountainPos = new Vector3Int(center.x, center.y, 0);
        generatedMap[fountainPos] = TileType.Fountain;
        
        // 퀘스트 게시판
        generatedMap[new Vector3Int(center.x, center.y - 1, 0)] = TileType.QuestBoard;
    }
    
    void PlaceBuildings()
    {
        List<Rect> occupiedAreas = new List<Rect>();
        
        // 광장 영역 추가
        Vector2Int center = new Vector2Int(settings.mapWidth / 2, settings.mapHeight / 2);
        Vector2Int squareStart = center - settings.squareSize / 2;
        occupiedAreas.Add(new Rect(squareStart.x, squareStart.y, settings.squareSize.x, settings.squareSize.y));
        
        foreach (var building in settings.buildings)
        {
            for (int i = 0; i < building.count; i++)
            {
                bool placed = false;
                int attempts = 0;
                
                while (!placed && attempts < 100)
                {
                    // 랜덤 위치 선정
                    float angle = (float)(random.NextDouble() * Mathf.PI * 2);
                    float distance = Mathf.Lerp(building.minDistanceFromCenter, 
                                               building.maxDistanceFromCenter, 
                                               (float)random.NextDouble());
                    
                    int x = center.x + Mathf.RoundToInt(Mathf.Cos(angle) * distance);
                    int y = center.y + Mathf.RoundToInt(Mathf.Sin(angle) * distance);
                    
                    Rect buildingRect = new Rect(x, y, building.size.x, building.size.y);
                    
                    // 충돌 체크
                    if (IsAreaClear(buildingRect, occupiedAreas) && IsWithinBounds(buildingRect))
                    {
                        PlaceBuildingAt(new Vector2Int(x, y), building);
                        occupiedAreas.Add(buildingRect);
                        placed = true;
                    }
                    
                    attempts++;
                }
                
                if (!placed)
                    Debug.LogWarning($"Failed to place {building.buildingType}");
            }
        }
    }
    
    void PlaceBuildingAt(Vector2Int position, BuildingPlacement building)
    {
        // 건물 배치
        for (int x = 0; x < building.size.x; x++)
        {
            for (int y = 0; y < building.size.y; y++)
            {
                Vector3Int pos = new Vector3Int(position.x + x, position.y + y, 0);
                generatedMap[pos] = building.buildingType;
            }
        }
        
        // 문 배치 (건물 남쪽 중앙)
        int doorX = position.x + building.size.x / 2;
        int doorY = position.y - 1;
        if (doorY >= 0)
        {
            generatedMap[new Vector3Int(doorX, doorY, 0)] = TileType.Door;
        }
    }
    
    void GeneratePaths()
    {
        // A* 알고리즘으로 주요 지점 연결
        List<Vector2Int> keyPoints = GetKeyPoints();
        
        // 모든 주요 지점을 광장과 연결
        Vector2Int squareCenter = new Vector2Int(settings.mapWidth / 2, settings.mapHeight / 2);
        
        foreach (var point in keyPoints)
        {
            List<Vector2Int> path = FindPath(point, squareCenter);
            foreach (var pathPoint in path)
            {
                Vector3Int pos = new Vector3Int(pathPoint.x, pathPoint.y, 0);
                if (generatedMap[pos] == TileType.Grass)
                {
                    generatedMap[pos] = TileType.Path;
                }
            }
        }
    }
    
    void AddDecorations()
    {
        List<Vector3Int> grassTiles = generatedMap
            .Where(kvp => kvp.Value == TileType.Grass)
            .Select(kvp => kvp.Key)
            .ToList();
        
        // 나무 배치
        int treeCount = Mathf.RoundToInt(grassTiles.Count * settings.treeDensity);
        for (int i = 0; i < treeCount && grassTiles.Count > 0; i++)
        {
            int index = random.Next(grassTiles.Count);
            generatedMap[grassTiles[index]] = random.Next(2) == 0 ? TileType.Tree1 : TileType.Tree2;
            grassTiles.RemoveAt(index);
        }
        
        // 꽃 배치
        int flowerCount = Mathf.RoundToInt(grassTiles.Count * settings.flowerDensity);
        for (int i = 0; i < flowerCount && grassTiles.Count > 0; i++)
        {
            int index = random.Next(grassTiles.Count);
            TileType flowerType = (TileType)random.Next((int)TileType.Flower1, (int)TileType.Flower3 + 1);
            generatedMap[grassTiles[index]] = flowerType;
            grassTiles.RemoveAt(index);
        }
    }
    
    void ApplyGeneratedMap()
    {
        mapBuilder.ClearMap();
        autoTileSystem.ApplyAutoTiling(generatedMap);
    }
    
    // 헬퍼 메서드들
    bool IsAreaClear(Rect area, List<Rect> occupiedAreas)
    {
        foreach (var occupied in occupiedAreas)
        {
            if (area.Overlaps(occupied))
                return false;
        }
        return true;
    }
    
    bool IsWithinBounds(Rect area)
    {
        return area.xMin >= settings.borderThickness + 1 &&
               area.xMax < settings.mapWidth - settings.borderThickness - 1 &&
               area.yMin >= settings.borderThickness + 1 &&
               area.yMax < settings.mapHeight - settings.borderThickness - 1;
    }
    
    List<Vector2Int> GetKeyPoints()
    {
        List<Vector2Int> points = new List<Vector2Int>();
        
        // 건물 입구들
        foreach (var kvp in generatedMap)
        {
            if (kvp.Value == TileType.Door)
            {
                points.Add(new Vector2Int(kvp.Key.x, kvp.Key.y));
            }
        }
        
        // 맵 출구
        points.Add(new Vector2Int(settings.mapWidth / 2, 1));
        
        return points;
    }
    
    List<Vector2Int> FindPath(Vector2Int start, Vector2Int end)
    {
        // 간단한 직선 경로 (실제로는 A* 알고리즘 구현 필요)
        List<Vector2Int> path = new List<Vector2Int>();
        
        Vector2Int current = start;
        while (current != end)
        {
            if (current.x < end.x) current.x++;
            else if (current.x > end.x) current.x--;
            else if (current.y < end.y) current.y++;
            else if (current.y > end.y) current.y--;
            
            path.Add(current);
        }
        
        return path;
    }
}