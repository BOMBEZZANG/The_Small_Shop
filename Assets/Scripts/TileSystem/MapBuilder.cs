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
    
    [Header("Quick Actions")]
    [Space(10)]
    [Button("Build Map")]
    public bool buildButton;
    
    [Button("Clear Map")]
    public bool clearButton;
    
    private Dictionary<Vector3Int, TileType> mapLayout = new Dictionary<Vector3Int, TileType>();
    private List<NPCSpawnPoint> npcSpawnPoints = new List<NPCSpawnPoint>();
    private List<string> validationErrors = new List<string>();
    
    [System.Serializable]
    public class NPCSpawnPoint
    {
        public Vector3Int position;
        public TileType npcType;
    }
    
    // Context Menu 방식
    [ContextMenu("Build Map From Data")]
    public void BuildMap()
    {
        Debug.Log("BuildMap 메서드 호출됨!");
        BuildMapInternal();
    }
    
    [ContextMenu("Clear Map")]
    public void ClearMap()
    {
        Debug.Log("ClearMap 메서드 호출됨!");
        ClearMapInternal();
    }
    
    // Inspector 버튼용 메서드
    [ContextMenu("Build Map (Button)")]
    public void BuildMapButton()
    {
        BuildMapInternal();
    }
    
    [ContextMenu("Clear Map (Button)")]
    public void ClearMapButton()
    {
        ClearMapInternal();
    }
    
    // 실제 구현
    private void BuildMapInternal()
    {
        if (mapData == null)
        {
            Debug.LogError("Map Data is missing!");
            return;
        }
        
        ClearMapInternal();
        
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
    
    private void ClearMapInternal()
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
        
        Debug.Log("Map cleared!");
    }
    
    // 나머지 코드는 동일...
    
    void ClearTilemap(Tilemap tilemap)
    {
        if (tilemap == null) return;
        
        tilemap.CompressBounds();
        BoundsInt bounds = tilemap.cellBounds;
        
        foreach (var position in bounds.allPositionsWithin)
        {
            tilemap.SetTile(position, null);
        }
        
        tilemap.CompressBounds();
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
                
                if (IsNPCMarker(tileType))
                {
                    npcSpawnPoints.Add(new NPCSpawnPoint { position = position, npcType = tileType });
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
        foreach (var kvp in mapLayout)
        {
            if (NeedsCollision(kvp.Value))
            {
                // TODO: 충돌 타일 배치
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
    
// MapBuilder.cs의 ValidateMap 메서드를 다음과 같이 수정합니다:

bool ValidateMap()
{
    validationErrors.Clear();
    
    // 플레이어 집 확인
    if (!mapLayout.Any(kvp => kvp.Value == TileType.PlayerHouse))
    {
        validationErrors.Add("No player house found!");
    }
    
    // 출구 확인 - 경계 근처(1칸 이내)도 허용
    bool hasExit = mapLayout.Any(kvp => kvp.Value == TileType.Door && 
                                      (kvp.Key.y <= 1 || kvp.Key.y >= mapData.height - 2 ||
                                       kvp.Key.x <= 1 || kvp.Key.x >= mapData.width - 2));
    if (!hasExit)
    {
        validationErrors.Add("No exit door found near map borders!");
    }
    
    // 추가 검증: 최소한 하나의 문이 있는지
    if (!mapLayout.Any(kvp => kvp.Value == TileType.Door))
    {
        validationErrors.Add("No doors found in the map!");
    }
    
    // 추가 검증: 상점이 있는지 (선택적)
    if (!mapLayout.Any(kvp => kvp.Value == TileType.Shop))
    {
        Debug.LogWarning("No shop found in the map (optional)");
    }
    
    foreach (var error in validationErrors)
    {
        Debug.LogError($"Map Validation: {error}");
    }
    
    return validationErrors.Count == 0;
}
    
    void OnDrawGizmos()
    {
        if (!showDebugGizmos || mapLayout == null || groundTilemap == null) return;
        
        Gizmos.color = new Color(1, 0, 0, 0.3f);
        foreach (var kvp in mapLayout)
        {
            if (NeedsCollision(kvp.Value))
            {
                Vector3 worldPos = groundTilemap.CellToWorld(kvp.Key) + new Vector3(0.5f, 0.5f, 0);
                Gizmos.DrawCube(worldPos, Vector3.one * 0.9f);
            }
        }
        
        Gizmos.color = Color.yellow;
        foreach (var spawn in npcSpawnPoints)
        {
            Vector3 worldPos = groundTilemap.CellToWorld(spawn.position) + new Vector3(0.5f, 0.5f, 0);
            Gizmos.DrawWireSphere(worldPos, 0.3f);
        }
    }
}

// Button Attribute (간단한 구현)
public class ButtonAttribute : PropertyAttribute
{
    public string MethodName { get; }
    
    public ButtonAttribute(string methodName)
    {
        MethodName = methodName;
    }
}