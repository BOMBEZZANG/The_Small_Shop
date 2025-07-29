using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;
using System.Linq;

public class MapBuilder : MonoBehaviour
{
    [Header("Tilemap References")]
    public Tilemap groundTilemap;
    public Tilemap buildingTilemap;
    public Tilemap decorationTilemap;
    public Tilemap collisionTilemap;
    
    [Header("Map Data")]
    public MapData mapData;
    
    [Header("Auto-Tiling")]
    public AutoTileSystem autoTileSystem;
    public bool useAutoTiling = true;
    
    [Header("NPC Prefabs")]
    public GameObject npcMerchantPrefab;
    public GameObject npcChiefPrefab;
    public GameObject npcFarmerPrefab;
    public GameObject npcChildPrefab;
    
    [Header("Map Validation")]
    public bool validateOnBuild = true;
    public bool showDebugGizmos = true;
    
    private Dictionary<Vector3Int, TileType> mapLayout = new Dictionary<Vector3Int, TileType>();
    private List<NPCSpawnPoint> npcSpawnPoints = new List<NPCSpawnPoint>();
    private List<string> validationErrors = new List<string>();
    
    [System.Serializable]
    public class NPCSpawnPoint
    {
        public Vector3Int position;
        public TileType npcType;
    }
    
    [ContextMenu("Build Map From Data")]
    public void BuildMap()
    {
        if (mapData == null)
        {
            Debug.LogError("Map Data is missing!");
            return;
        }
        
        ClearMap();
        
        if (ParseMapLayout())
        {
            if (validateOnBuild && !ValidateMap())
            {
                Debug.LogError("Map validation failed! Check console for errors.");
                return;
            }
            
            if (useAutoTiling && autoTileSystem != null)
            {
                autoTileSystem.ApplyAutoTiling(mapLayout);
            }
            else
            {
                PlaceTiles();
            }
            
            PlaceCollisionTiles();
            SpawnNPCs();
            
            Debug.Log($"Map '{mapData.mapName}' built successfully!");
        }
    }
    
    [ContextMenu("Clear Map")]
    public void ClearMap()
    {
        ClearTilemap(groundTilemap);
        ClearTilemap(buildingTilemap);
        ClearTilemap(decorationTilemap);
        ClearTilemap(collisionTilemap);
        
        // NPCs 제거
        GameObject[] npcs = GameObject.FindGameObjectsWithTag("NPC");
        foreach (var npc in npcs)
        {
            DestroyImmediate(npc);
        }
    }
    
    void ClearTilemap(Tilemap tilemap)
    {
        if (tilemap == null) return;
        
        tilemap.CompressBounds();
        BoundsInt bounds = tilemap.cellBounds;
        TileBase[] emptyTiles = new TileBase[bounds.size.x * bounds.size.y * bounds.size.z];
        tilemap.SetTiles(bounds, emptyTiles);
    }
    
    bool ParseMapLayout()
    {
        mapLayout.Clear();
        npcSpawnPoints.Clear();
        
        string[] lines = mapData.mapLayoutText.Split('\n');
        
        for (int y = 0; y < lines.Length && y < mapData.height; y++)
        {
            string line = lines[y];
            
            for (int x = 0; x < line.Length && x < mapData.width; x++)
            {
                string symbol = line[x].ToString();
                TileType tileType = mapData.GetTileTypeFromSymbol(symbol);
                
                Vector3Int position = new Vector3Int(x, mapData.height - y - 1, 0);
                mapLayout[position] = tileType;
                
                // NPC 위치 저장
                if (IsNPCMarker(tileType))
                {
                    npcSpawnPoints.Add(new NPCSpawnPoint { position = position, npcType = tileType });
                    // NPC 위치는 잔디로 변경
                    mapLayout[position] = TileType.Grass;
                }
            }
        }
        
        return mapLayout.Count > 0;
    }
    
    void PlaceTiles()
    {
        foreach (var kvp in mapLayout)
        {
            Vector3Int position = kvp.Key;
            TileType tileType = kvp.Value;
            
            TileBase tile = mapData.tileAssets.GetTile(tileType);
            if (tile != null)
            {
                Tilemap targetTilemap = GetTilemapForTileType(tileType);
                targetTilemap.SetTile(position, tile);
            }
        }
    }
    
    void PlaceCollisionTiles()
    {
        // Collision 타일맵용 투명 타일 생성
        foreach (var kvp in mapLayout)
        {
            if (NeedsCollision(kvp.Value))
            {
                // Tilemaps Extras 패키지의 RuleTile이나 커스텀 투명 타일 사용
                // collisionTilemap.SetTile(kvp.Key, transparentCollisionTile);
            }
        }
    }
    
    public Tilemap GetTilemapForTileType(TileType type)
    {
        switch (type)
        {
            case TileType.Grass:
            case TileType.Path:
            case TileType.Stone:
            case TileType.Plaza:
            case TileType.Water:
                return groundTilemap;
                
            case TileType.Wall:
            case TileType.PlayerHouse:
            case TileType.House1:
            case TileType.House2:
            case TileType.Shop:
            case TileType.Guild:
            case TileType.ChiefHouse:
            case TileType.Door:
                return buildingTilemap;
                
            default:
                return decorationTilemap;
        }
    }
    
    bool NeedsCollision(TileType type)
    {
        switch (type)
        {
            case TileType.Wall:
            case TileType.TreeBorder:
            case TileType.PlayerHouse:
            case TileType.House1:
            case TileType.House2:
            case TileType.Shop:
            case TileType.Guild:
            case TileType.ChiefHouse:
            case TileType.Fountain:
            case TileType.Water:
            case TileType.Tree1:
            case TileType.Tree2:
                return true;
            default:
                return false;
        }
    }
    
    bool IsNPCMarker(TileType type)
    {
        return type >= TileType.NPC_Merchant && type <= TileType.NPC_Child;
    }
    
    void SpawnNPCs()
    {
        foreach (var spawnPoint in npcSpawnPoints)
        {
            GameObject npcPrefab = GetNPCPrefab(spawnPoint.npcType);
            if (npcPrefab != null)
            {
                Vector3 worldPosition = groundTilemap.CellToWorld(spawnPoint.position) + new Vector3(0.5f, 0.5f, 0);
                GameObject npc = Instantiate(npcPrefab, worldPosition, Quaternion.identity);
                npc.name = $"NPC_{spawnPoint.npcType}_{spawnPoint.position}";
            }
        }
    }
    
    GameObject GetNPCPrefab(TileType npcType)
    {
        switch (npcType)
        {
            case TileType.NPC_Merchant: return npcMerchantPrefab;
            case TileType.NPC_Chief: return npcChiefPrefab;
            case TileType.NPC_Farmer: return npcFarmerPrefab;
            case TileType.NPC_Child: return npcChildPrefab;
            default: return null;
        }
    }
    
    // 맵 검증 시스템
    bool ValidateMap()
    {
        validationErrors.Clear();
        
        // 1. 플레이어 시작 위치 확인
        if (!mapLayout.Any(kvp => kvp.Value == TileType.PlayerHouse))
        {
            validationErrors.Add("No player house found!");
        }
        
        // 2. 출구 확인
        bool hasExit = mapLayout.Any(kvp => kvp.Value == TileType.Door && 
                                           (kvp.Key.y == 0 || kvp.Key.y == mapData.height - 1 ||
                                            kvp.Key.x == 0 || kvp.Key.x == mapData.width - 1));
        if (!hasExit)
        {
            validationErrors.Add("No exit door found on map borders!");
        }
        
        // 3. 접근성 확인 (간단한 flood fill)
        CheckAccessibility();
        
        // 4. 필수 건물 확인
        CheckRequiredBuildings();
        
        // 에러 출력
        foreach (var error in validationErrors)
        {
            Debug.LogError($"Map Validation: {error}");
        }
        
        return validationErrors.Count == 0;
    }
    
    void CheckAccessibility()
    {
        // 플레이어 집에서 시작하여 도달 가능한 모든 타일 확인
        var playerHouse = mapLayout.FirstOrDefault(kvp => kvp.Value == TileType.PlayerHouse);
        if (playerHouse.Key == null) return;
        
        HashSet<Vector3Int> visited = new HashSet<Vector3Int>();
        Queue<Vector3Int> toVisit = new Queue<Vector3Int>();
        toVisit.Enqueue(playerHouse.Key);
        
        Vector3Int[] directions = {
            Vector3Int.up, Vector3Int.down, Vector3Int.left, Vector3Int.right
        };
        
        while (toVisit.Count > 0)
        {
            var current = toVisit.Dequeue();
            if (visited.Contains(current)) continue;
            
            visited.Add(current);
            
            foreach (var dir in directions)
            {
                var next = current + dir;
                if (mapLayout.ContainsKey(next) && !NeedsCollision(mapLayout[next]) && !visited.Contains(next))
                {
                    toVisit.Enqueue(next);
                }
            }
        }
        
        // 중요 건물들이 접근 가능한지 확인
        var importantBuildings = mapLayout.Where(kvp => 
            kvp.Value == TileType.Shop || 
            kvp.Value == TileType.Guild || 
            kvp.Value == TileType.ChiefHouse);
        
        foreach (var building in importantBuildings)
        {
            if (!visited.Contains(building.Key))
            {
                validationErrors.Add($"{building.Value} at {building.Key} is not accessible!");
            }
        }
    }
    
    void CheckRequiredBuildings()
    {
        TileType[] requiredTypes = {
            TileType.PlayerHouse,
            TileType.Shop,
            TileType.ChiefHouse
        };
        
        foreach (var required in requiredTypes)
        {
            if (!mapLayout.Any(kvp => kvp.Value == required))
            {
                validationErrors.Add($"Required building {required} is missing!");
            }
        }
    }
    
    // 디버그 표시
    void OnDrawGizmos()
    {
        if (!showDebugGizmos || mapLayout == null || groundTilemap == null) return;
        
        // 충돌 영역 표시
        Gizmos.color = new Color(1, 0, 0, 0.3f);
        foreach (var kvp in mapLayout)
        {
            if (NeedsCollision(kvp.Value))
            {
                Vector3 worldPos = groundTilemap.CellToWorld(kvp.Key) + new Vector3(0.5f, 0.5f, 0);
                Gizmos.DrawCube(worldPos, Vector3.one * 0.9f);
            }
        }
        
        // NPC 스폰 위치 표시
        Gizmos.color = Color.yellow;
        foreach (var spawn in npcSpawnPoints)
        {
            Vector3 worldPos = groundTilemap.CellToWorld(spawn.position) + new Vector3(0.5f, 0.5f, 0);
            Gizmos.DrawWireSphere(worldPos, 0.3f);
        }
    }
}