using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "MapGenerationSettings", menuName = "Tilemap/Map Generation Settings")]
public class MapGenerationSettings : ScriptableObject
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