using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Tilemaps;

// MapGenerationSettings와 BuildingPlacement는 별도 파일에 정의되어 있으므로 제거
public class ProceduralMapGenerator : MonoBehaviour
{
    [Header("Generation Settings")]
    [SerializeField] private MapGenerationSettings settings; // ScriptableObject로 변경
    
    [Header("References")]
    public MapBuilder mapBuilder;
    public AutoTileSystem autoTileSystem;
    
    [Header("Debug")]
    [SerializeField] private bool showGenerationLog = true;
    [SerializeField] private bool saveGeneratedLayout = false;
    
    private Dictionary<Vector3Int, TileType> generatedMap;
    private System.Random random;
    
    [ContextMenu("Generate Random Map")]
    public void GenerateRandomMap()
    {
        if (settings == null)
        {
            Debug.LogError("Generation Settings가 설정되지 않았습니다!");
            return;
        }
        
        if (mapBuilder == null)
        {
            Debug.LogError("MapBuilder가 설정되지 않았습니다!");
            return;
        }
        
        // 시드 설정
        int usedSeed = settings.useRandomSeed ? Random.Range(0, int.MaxValue) : settings.seed;
        random = new System.Random(usedSeed);
        
        if (showGenerationLog)
            Debug.Log($"맵 생성 시작 - Seed: {usedSeed}");
        
        // 맵 초기화
        generatedMap = new Dictionary<Vector3Int, TileType>();
        InitializeMap();
        
        // 생성 단계
        CreateBorder();
        CreateTownSquare();
        PlaceBuildings();
        GeneratePaths();
        AddDecorations();
        
        // 생성된 맵 검증
        if (ValidateGeneratedMap())
        {
            // 맵 적용
            ApplyGeneratedMap();
            
            // 선택적: 생성된 레이아웃 저장
            if (saveGeneratedLayout)
            {
                SaveGeneratedLayout();
            }
            
            if (showGenerationLog)
                Debug.Log($"맵 생성 완료! (크기: {settings.mapWidth}x{settings.mapHeight})");
        }
        else
        {
            Debug.LogError("생성된 맵이 유효하지 않습니다!");
        }
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
        
        if (showGenerationLog)
            Debug.Log("테두리 생성 완료");
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
        
        if (showGenerationLog)
            Debug.Log($"마을 광장 생성 완료 (크기: {settings.squareSize.x}x{settings.squareSize.y})");
    }
    
    void PlaceBuildings()
    {
        List<Rect> occupiedAreas = new List<Rect>();
        
        // 광장 영역 추가
        Vector2Int center = new Vector2Int(settings.mapWidth / 2, settings.mapHeight / 2);
        Vector2Int squareStart = center - settings.squareSize / 2;
        occupiedAreas.Add(new Rect(squareStart.x, squareStart.y, settings.squareSize.x, settings.squareSize.y));
        
        int totalPlaced = 0;
        foreach (var building in settings.buildings)
        {
            int placedCount = 0;
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
                        placedCount++;
                    }
                    
                    attempts++;
                }
                
                if (!placed && showGenerationLog)
                    Debug.LogWarning($"{building.buildingType} 배치 실패");
            }
            
            if (showGenerationLog && placedCount > 0)
                Debug.Log($"{building.buildingType} {placedCount}개 배치 완료");
                
            totalPlaced += placedCount;
        }
        
        if (showGenerationLog)
            Debug.Log($"총 {totalPlaced}개 건물 배치 완료");
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
        // 주요 지점 찾기
        List<Vector2Int> keyPoints = GetKeyPoints();
        
        // 모든 주요 지점을 광장과 연결
        Vector2Int squareCenter = new Vector2Int(settings.mapWidth / 2, settings.mapHeight / 2);
        
        int pathCount = 0;
        foreach (var point in keyPoints)
        {
            List<Vector2Int> path = FindPath(point, squareCenter);
            foreach (var pathPoint in path)
            {
                Vector3Int pos = new Vector3Int(pathPoint.x, pathPoint.y, 0);
                if (generatedMap.ContainsKey(pos) && generatedMap[pos] == TileType.Grass)
                {
                    generatedMap[pos] = TileType.Path;
                    pathCount++;
                }
            }
        }
        
        if (showGenerationLog)
            Debug.Log($"경로 생성 완료 - {pathCount}개 타일");
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
        
        if (showGenerationLog)
            Debug.Log($"장식 배치 완료 - 나무: {treeCount}개, 꽃: {flowerCount}개");
    }
    
    void ApplyGeneratedMap()
    {
        mapBuilder.ClearMap();
        
        if (autoTileSystem != null && mapBuilder.useAutoTiling)
        {
            autoTileSystem.ApplyAutoTiling(generatedMap);
        }
        else
        {
            // 수동 배치
            foreach (var kvp in generatedMap)
            {
                TileBase tile = mapBuilder.mapData.tileAssets.GetTile(kvp.Value);
                if (tile != null)
                {
                    var tilemap = mapBuilder.GetTilemapForTileType(kvp.Value);
                    tilemap.SetTile(kvp.Key, tile);
                }
            }
        }
    }
    
    // 생성된 맵 검증
    bool ValidateGeneratedMap()
    {
        // 필수 건물 확인
        bool hasPlayerHouse = generatedMap.Any(kvp => kvp.Value == TileType.PlayerHouse);
        bool hasShop = generatedMap.Any(kvp => kvp.Value == TileType.Shop);
        bool hasExit = generatedMap.Any(kvp => kvp.Value == TileType.Door);
        
        if (!hasExit)
        {
            Debug.LogError("출구가 없습니다!");
            return false;
        }
        
        // 플레이어 집이 없으면 자동 배치
        if (!hasPlayerHouse)
        {
            PlacePlayerHouse();
        }
        
        return true;
    }
    
    // 플레이어 집 자동 배치
    void PlacePlayerHouse()
    {
        // 남쪽 출구 근처에 배치
        int exitX = settings.mapWidth / 2;
        Vector2Int playerHousePos = new Vector2Int(exitX + 5, 5);
        
        BuildingPlacement playerHouse = new BuildingPlacement
        {
            buildingType = TileType.PlayerHouse,
            size = new Vector2Int(3, 3)
        };
        
        PlaceBuildingAt(playerHousePos, playerHouse);
        
        if (showGenerationLog)
            Debug.Log("플레이어 집 자동 배치 완료");
    }
    
    // 생성된 레이아웃을 텍스트로 저장
    void SaveGeneratedLayout()
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        
        for (int y = settings.mapHeight - 1; y >= 0; y--)
        {
            for (int x = 0; x < settings.mapWidth; x++)
            {
                Vector3Int pos = new Vector3Int(x, y, 0);
                TileType type = generatedMap.ContainsKey(pos) ? generatedMap[pos] : TileType.Empty;
                sb.Append(GetSymbolForTileType(type));
            }
            if (y > 0) sb.AppendLine();
        }
        
        // 생성된 텍스트를 로그로 출력 (실제로는 파일로 저장 가능)
        Debug.Log("=== 생성된 맵 레이아웃 ===\n" + sb.ToString());
    }
    
    // TileType을 심볼로 변환
    string GetSymbolForTileType(TileType type)
    {
        // MapData의 symbolMappings를 역으로 사용
        switch (type)
        {
            case TileType.Wall: return "#";
            case TileType.TreeBorder: return "T";
            case TileType.Grass: return ".";
            case TileType.Stone: return "=";
            case TileType.Path: return "-";
            case TileType.House1: return "H";
            case TileType.House2: return "h";
            case TileType.Shop: return "S";
            case TileType.Guild: return "G";
            case TileType.ChiefHouse: return "C";
            case TileType.PlayerHouse: return "P";
            case TileType.Door: return "D";
            case TileType.Water: return "~";
            case TileType.Plaza: return "*";
            case TileType.Fountain: return "@";
            case TileType.QuestBoard: return "Q";
            default: return " ";
        }
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
        // 간단한 직선 경로 (실제로는 A* 알고리즘 구현 권장)
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
    
    // 디버그용 기즈모
    void OnDrawGizmosSelected()
    {
        if (settings == null || generatedMap == null) return;
        
        // 생성된 맵의 건물 위치 표시
        foreach (var kvp in generatedMap)
        {
            if (IsBuilding(kvp.Value))
            {
                Gizmos.color = GetColorForTileType(kvp.Value);
                Vector3 worldPos = new Vector3(kvp.Key.x + 0.5f, kvp.Key.y + 0.5f, 0);
                Gizmos.DrawCube(worldPos, Vector3.one * 0.8f);
            }
        }
    }
    
    bool IsBuilding(TileType type)
    {
        return type == TileType.PlayerHouse || type == TileType.House1 || 
               type == TileType.House2 || type == TileType.Shop || 
               type == TileType.Guild || type == TileType.ChiefHouse;
    }
    
    Color GetColorForTileType(TileType type)
    {
        switch (type)
        {
            case TileType.PlayerHouse: return Color.blue;
            case TileType.Shop: return Color.yellow;
            case TileType.Guild: return Color.magenta;
            case TileType.ChiefHouse: return Color.red;
            default: return Color.gray;
        }
    }
}