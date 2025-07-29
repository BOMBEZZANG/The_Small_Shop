using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

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
    public static List<TileRule.NeighborRule> Get9SliceRules(bool topLeft = false, bool top = false, 
        bool topRight = false, bool left = false, bool right = false, 
        bool bottomLeft = false, bool bottom = false, bool bottomRight = false)
    {
        var rules = new List<TileRule.NeighborRule>();
        
        // 8방향 체크
        if (topLeft) rules.Add(new TileRule.NeighborRule { offset = new Vector2Int(-1, 1), mustMatch = true });
        if (top) rules.Add(new TileRule.NeighborRule { offset = new Vector2Int(0, 1), mustMatch = true });
        if (topRight) rules.Add(new TileRule.NeighborRule { offset = new Vector2Int(1, 1), mustMatch = true });
        if (left) rules.Add(new TileRule.NeighborRule { offset = new Vector2Int(-1, 0), mustMatch = true });
        if (right) rules.Add(new TileRule.NeighborRule { offset = new Vector2Int(1, 0), mustMatch = true });
        if (bottomLeft) rules.Add(new TileRule.NeighborRule { offset = new Vector2Int(-1, -1), mustMatch = true });
        if (bottom) rules.Add(new TileRule.NeighborRule { offset = new Vector2Int(0, -1), mustMatch = true });
        if (bottomRight) rules.Add(new TileRule.NeighborRule { offset = new Vector2Int(1, -1), mustMatch = true });
        
        return rules;
    }
    
    // Rule 타일 쉽게 생성하기 위한 헬퍼 메서드들
    public static TileRule.NeighborRule[] GetCornerRules(bool isOuterCorner)
    {
        if (isOuterCorner)
        {
            // 외부 모서리 (2면이 비어있음)
            return new TileRule.NeighborRule[]
            {
                new TileRule.NeighborRule { offset = new Vector2Int(-1, 0), mustMatch = false },
                new TileRule.NeighborRule { offset = new Vector2Int(0, -1), mustMatch = false },
                new TileRule.NeighborRule { offset = new Vector2Int(1, 0), mustMatch = true },
                new TileRule.NeighborRule { offset = new Vector2Int(0, 1), mustMatch = true }
            };
        }
        else
        {
            // 내부 모서리 (3면이 채워짐)
            return new TileRule.NeighborRule[]
            {
                new TileRule.NeighborRule { offset = new Vector2Int(-1, 0), mustMatch = true },
                new TileRule.NeighborRule { offset = new Vector2Int(0, -1), mustMatch = true },
                new TileRule.NeighborRule { offset = new Vector2Int(1, 0), mustMatch = true },
                new TileRule.NeighborRule { offset = new Vector2Int(0, 1), mustMatch = true },
                new TileRule.NeighborRule { offset = new Vector2Int(-1, -1), mustMatch = false }
            };
        }
    }
}