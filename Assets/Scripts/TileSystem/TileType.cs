using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

// 타일 타입 정의
public enum TileType
{
    // 지형
    Empty,          // 빈 공간
    Grass,          // 잔디
    Path,           // 길
    Stone,          // 돌바닥
    Water,          // 물
    
    // 경계
    Wall,           // 벽
    TreeBorder,     // 나무 경계
    
    // 건물
    PlayerHouse,    // 플레이어 집
    House1,         // 일반 집 1
    House2,         // 일반 집 2
    Shop,           // 상점
    Guild,          // 길드
    ChiefHouse,     // 촌장 집
    Door,           // 문
    
    // 자연
    Tree1,          // 나무 1
    Tree2,          // 나무 2
    Flower1,        // 꽃 1
    Flower2,        // 꽃 2
    Flower3,        // 꽃 3
    Crops,          // 작물
    
    // 특수
    Fountain,       // 분수대
    FountainCenter, // 분수대 중앙
    QuestBoard,     // 퀘스트 게시판
    Plaza,          // 광장 특수 타일
    
    // NPC 위치 마커 (타일이 아닌 마커)
    NPC_Merchant,   // 상인 NPC
    NPC_Chief,      // 촌장 NPC
    NPC_Farmer,     // 농부 NPC
    NPC_Child,      // 아이 NPC
}

// 타일 에셋 딕셔너리
[System.Serializable]
public class TileAssetDictionary
{
    [Header("Ground Tiles")]
    public TileBase grassTile;
    public TileBase pathTile;
    public TileBase stoneTile;
    public TileBase plazaTile;
    public TileBase waterTile;
    
    [Header("Building Tiles")]
    public TileBase wallTile;
    public TileBase playerHouseTile;
    public TileBase house1Tile;
    public TileBase house2Tile;
    public TileBase shopTile;
    public TileBase guildTile;
    public TileBase chiefHouseTile;
    public TileBase doorTile;
    
    [Header("Nature Tiles")]
    public TileBase tree1Tile;
    public TileBase tree2Tile;
    public TileBase flower1Tile;
    public TileBase flower2Tile;
    public TileBase flower3Tile;
    public TileBase cropsTile;
    
    [Header("Special Tiles")]
    public TileBase fountainTile;
    public TileBase fountainCenterTile;
    public TileBase questBoardTile;
    
    public TileBase GetTile(TileType type)
    {
        switch (type)
        {
            case TileType.Grass: return grassTile;
            case TileType.Path: return pathTile;
            case TileType.Stone: return stoneTile;
            case TileType.Plaza: return plazaTile;
            case TileType.Water: return waterTile;
            case TileType.Wall: return wallTile;
            case TileType.TreeBorder: return tree1Tile; // 임시로 tree1 사용
            case TileType.PlayerHouse: return playerHouseTile;
            case TileType.House1: return house1Tile;
            case TileType.House2: return house2Tile;
            case TileType.Shop: return shopTile;
            case TileType.Guild: return guildTile;
            case TileType.ChiefHouse: return chiefHouseTile;
            case TileType.Door: return doorTile;
            case TileType.Tree1: return tree1Tile;
            case TileType.Tree2: return tree2Tile;
            case TileType.Flower1: return flower1Tile;
            case TileType.Flower2: return flower2Tile;
            case TileType.Flower3: return flower3Tile;
            case TileType.Crops: return cropsTile;
            case TileType.Fountain: return fountainTile;
            case TileType.FountainCenter: return fountainCenterTile;
            case TileType.QuestBoard: return questBoardTile;
            default: return null;
        }
    }
}