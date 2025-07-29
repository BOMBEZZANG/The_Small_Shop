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
        public string variantName;  // 디버그용 이름
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
    
    // 에디터용 헬퍼 메서드 - 기본 9-슬라이스 타일 설정
    [ContextMenu("Setup 9-Slice Tiles")]
    private void Setup9SliceTiles()
    {
        variants.Clear();
        
        // 중앙 (모든 면이 같은 타일)
        variants.Add(new TileVariant
        {
            variantName = "Center",
            priority = 1,
            rules = new List<NeighborRule>
            {
                new NeighborRule { offset = new Vector2Int(0, 1), mustMatch = true },
                new NeighborRule { offset = new Vector2Int(0, -1), mustMatch = true },
                new NeighborRule { offset = new Vector2Int(1, 0), mustMatch = true },
                new NeighborRule { offset = new Vector2Int(-1, 0), mustMatch = true }
            }
        });
        
        // 모서리들
        variants.Add(new TileVariant
        {
            variantName = "Top Left Corner",
            priority = 2,
            rules = new List<NeighborRule>
            {
                new NeighborRule { offset = new Vector2Int(0, 1), mustMatch = false },
                new NeighborRule { offset = new Vector2Int(-1, 0), mustMatch = false },
                new NeighborRule { offset = new Vector2Int(1, 0), mustMatch = true },
                new NeighborRule { offset = new Vector2Int(0, -1), mustMatch = true }
            }
        });
        
        // 가장자리들
        variants.Add(new TileVariant
        {
            variantName = "Top Edge",
            priority = 2,
            rules = new List<NeighborRule>
            {
                new NeighborRule { offset = new Vector2Int(0, 1), mustMatch = false },
                new NeighborRule { offset = new Vector2Int(0, -1), mustMatch = true },
                new NeighborRule { offset = new Vector2Int(1, 0), mustMatch = true },
                new NeighborRule { offset = new Vector2Int(-1, 0), mustMatch = true }
            }
        });
        
        Debug.Log($"{name}: 9-슬라이스 타일 설정 완료!");
    }
}