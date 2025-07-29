using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

// 개선된 심볼 매핑 시스템
[System.Serializable]
public class TileSymbolMapping
{
    public string symbol;
    public TileType tileType;
    public Color debugColor = Color.white;
}

[CreateAssetMenu(fileName = "MapData", menuName = "Tilemap/Map Data")]
public class MapData : ScriptableObject
{
    [Header("Map Settings")]
    public string mapName = "First Village";
    public int width = 50;
    public int height = 50;
    
    [Header("Tile References")]
    public TileAssetDictionary tileAssets = new TileAssetDictionary();
    
    [Header("Symbol Mappings")]
    public List<TileSymbolMapping> symbolMappings = new List<TileSymbolMapping>
    {
        // 단순 ASCII 기반 매핑
        new TileSymbolMapping { symbol = "#", tileType = TileType.Wall },
        new TileSymbolMapping { symbol = "T", tileType = TileType.TreeBorder },
        new TileSymbolMapping { symbol = ".", tileType = TileType.Grass },
        new TileSymbolMapping { symbol = "=", tileType = TileType.Stone },
        new TileSymbolMapping { symbol = "-", tileType = TileType.Path },
        new TileSymbolMapping { symbol = "H", tileType = TileType.House1 },
        new TileSymbolMapping { symbol = "h", tileType = TileType.House2 },
        new TileSymbolMapping { symbol = "S", tileType = TileType.Shop },
        new TileSymbolMapping { symbol = "G", tileType = TileType.Guild },
        new TileSymbolMapping { symbol = "C", tileType = TileType.ChiefHouse },
        new TileSymbolMapping { symbol = "P", tileType = TileType.PlayerHouse },
        new TileSymbolMapping { symbol = "D", tileType = TileType.Door },
        new TileSymbolMapping { symbol = "c", tileType = TileType.Crops },
        new TileSymbolMapping { symbol = "f", tileType = TileType.Flower1 },
        new TileSymbolMapping { symbol = "F", tileType = TileType.Flower2 },
        new TileSymbolMapping { symbol = "Y", tileType = TileType.Flower3 },
        new TileSymbolMapping { symbol = "~", tileType = TileType.Water },
        new TileSymbolMapping { symbol = "*", tileType = TileType.Plaza },
        new TileSymbolMapping { symbol = "+", tileType = TileType.Stone },
        new TileSymbolMapping { symbol = "@", tileType = TileType.Fountain },
        new TileSymbolMapping { symbol = "Q", tileType = TileType.QuestBoard },
        new TileSymbolMapping { symbol = " ", tileType = TileType.Empty },
        
        // NPC 마커
        new TileSymbolMapping { symbol = "1", tileType = TileType.NPC_Merchant },
        new TileSymbolMapping { symbol = "2", tileType = TileType.NPC_Chief },
        new TileSymbolMapping { symbol = "3", tileType = TileType.NPC_Farmer },
        new TileSymbolMapping { symbol = "4", tileType = TileType.NPC_Child }
    };
    
    [Header("Map Layout")]
    [TextArea(20, 50)]
    public string mapLayoutText;
    
    // 심볼을 TileType으로 변환
    public TileType GetTileTypeFromSymbol(string symbol)
    {
        var mapping = symbolMappings.Find(m => m.symbol == symbol);
        return mapping != null ? mapping.tileType : TileType.Empty;
    }
}