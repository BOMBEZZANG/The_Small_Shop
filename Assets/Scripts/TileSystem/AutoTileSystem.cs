using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

// 타일 자동 배치 규칙
[CreateAssetMenu(fileName = "TileRule", menuName = "Tilemap/Tile Rule")]
public class TileRule : ScriptableObject
{
    [System.Serializable]
    public class NeighborRule
    {
        public Vector2Int offset;
        public TileType requiredType = TileType.Empty;
        public bool mustMatch = true; // true: 같아야 함, false: 달라야 함
    }
    
    [System.Serializable]
    public class TileVariant
    {
        public TileBase tile;
        public List<NeighborRule> rules = new List<NeighborRule>();
        public int priority = 0; // 높을수록 우선순위 높음
    }
    
    [Header("Tile Configuration")]
    public TileType tileType;
    public TileBase defaultTile;
    
    [Header("Auto-Tiling Variants")]
    public List<TileVariant> variants = new List<TileVariant>();
    
    // 주변 타일 체크하여 적절한 타일 반환
    public TileBase GetTileVariant(Dictionary<Vector3Int, TileType> surroundingTiles, Vector3Int position)
    {
        // 우선순위 순으로 정렬
        variants.Sort((a, b) => b.priority.CompareTo(a.priority));
        
        foreach (var variant in variants)
        {
            bool allRulesMatch = true;
            
            foreach (var rule in variant.rules)
            {
                Vector3Int checkPos = position + new Vector3Int(rule.offset.x, rule.offset.y, 0);
                TileType neighborType = surroundingTiles.ContainsKey(checkPos) ? 
                    surroundingTiles[checkPos] : TileType.Empty;
                
                bool matches = (neighborType == rule.requiredType) || 
                              (rule.requiredType == TileType.Empty && neighborType == tileType);
                
                if (rule.mustMatch != matches)
                {
                    allRulesMatch = false;
                    break;
                }
            }
            
            if (allRulesMatch)
                return variant.tile;
        }
        
        return defaultTile;
    }
}

// 타일 자동 배치 시스템
public class AutoTileSystem : MonoBehaviour
{
    [Header("Tile Rules")]
    public List<TileRule> tileRules = new List<TileRule>();
    private Dictionary<TileType, TileRule> ruleDict = new Dictionary<TileType, TileRule>();
    
    [Header("References")]
    public MapBuilder mapBuilder;
    
    void Awake()
    {
        // 룰 딕셔너리 생성
        foreach (var rule in tileRules)
        {
            if (rule != null)
                ruleDict[rule.tileType] = rule;
        }
    }
    
    // 자동 타일링 적용
    public void ApplyAutoTiling(Dictionary<Vector3Int, TileType> mapLayout)
    {
        Dictionary<Vector3Int, TileBase> tilesToPlace = new Dictionary<Vector3Int, TileBase>();
        
        foreach (var kvp in mapLayout)
        {
            Vector3Int position = kvp.Key;
            TileType tileType = kvp.Value;
            
            if (ruleDict.ContainsKey(tileType))
            {
                TileBase selectedTile = ruleDict[tileType].GetTileVariant(mapLayout, position);
                if (selectedTile != null)
                    tilesToPlace[position] = selectedTile;
            }
            else
            {
                // 기본 타일 사용
                TileBase defaultTile = mapBuilder.mapData.tileAssets.GetTile(tileType);
                if (defaultTile != null)
                    tilesToPlace[position] = defaultTile;
            }
        }
        
        // 타일 배치
        foreach (var kvp in tilesToPlace)
        {
            Tilemap targetTilemap = mapBuilder.GetTilemapForTileType(mapLayout[kvp.Key]);
            targetTilemap.SetTile(kvp.Key, kvp.Value);
        }
    }
    
    // 9-슬라이스 타일용 헬퍼 메서드
    public static List<NeighborRule> Get9SliceRules(bool topLeft = false, bool top = false, 
        bool topRight = false, bool left = false, bool right = false, 
        bool bottomLeft = false, bool bottom = false, bool bottomRight = false)
    {
        var rules = new List<NeighborRule>();
        
        // 8방향 체크
        if (topLeft) rules.Add(new NeighborRule { offset = new Vector2Int(-1, 1), mustMatch = true });
        if (top) rules.Add(new NeighborRule { offset = new Vector2Int(0, 1), mustMatch = true });
        if (topRight) rules.Add(new NeighborRule { offset = new Vector2Int(1, 1), mustMatch = true });
        if (left) rules.Add(new NeighborRule { offset = new Vector2Int(-1, 0), mustMatch = true });
        if (right) rules.Add(new NeighborRule { offset = new Vector2Int(1, 0), mustMatch = true });
        if (bottomLeft) rules.Add(new NeighborRule { offset = new Vector2Int(-1, -1), mustMatch = true });
        if (bottom) rules.Add(new NeighborRule { offset = new Vector2Int(0, -1), mustMatch = true });
        if (bottomRight) rules.Add(new NeighborRule { offset = new Vector2Int(1, -1), mustMatch = true });
        
        return rules;
    }
}