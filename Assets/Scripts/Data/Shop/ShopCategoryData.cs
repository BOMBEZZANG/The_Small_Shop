using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "ShopCategory", menuName = "Game Data/Shop/Shop Category")]
public class ShopCategoryData : ScriptableObject
{
    [Header("Category Info")]
    public string categoryName;
    public string categoryDescription;
    public Sprite categoryIcon;
    public Color categoryColor = Color.white;
    
    [Header("Category Settings")]
    public bool isAvailableForBuying = true;  // Can player buy items from this category
    public bool isAvailableForSelling = true; // Can player sell items from this category
    
    [Header("Price Modifiers")]
    [Tooltip("Multiplier applied to base buy prices for this category")]
    [Range(0.1f, 2f)]
    public float buyPriceModifier = 1f;
    
    [Tooltip("Multiplier applied to base sell prices for this category")]
    [Range(0.1f, 2f)]
    public float sellPriceModifier = 1f;
    
    [Header("Item Classification")]
    [Tooltip("Materials that belong to this category")]
    public List<MaterialData> categoryItems = new List<MaterialData>();
    
    [Header("Display Settings")]
    public int sortOrder = 0; // Lower numbers appear first
    public bool showInShopUI = true;
    
    // Check if a material belongs to this category
    public bool ContainsItem(MaterialData item)
    {
        return categoryItems.Contains(item);
    }
    
    // Get all items in this category that the player owns
    public List<MaterialData> GetOwnedItems()
    {
        List<MaterialData> ownedItems = new List<MaterialData>();
        
        foreach (var item in categoryItems)
        {
            if (InventoryManager.instance != null && 
                InventoryManager.instance.GetItemQuantity(item) > 0)
            {
                ownedItems.Add(item);
            }
        }
        
        return ownedItems;
    }
    
    // Get total value of all items in this category that player owns
    public int GetTotalOwnedValue()
    {
        int totalValue = 0;
        
        foreach (var item in categoryItems)
        {
            if (InventoryManager.instance != null)
            {
                int quantity = InventoryManager.instance.GetItemQuantity(item);
                if (quantity > 0)
                {
                    totalValue += PriceCalculator.CalculateSellPrice(item, quantity);
                }
            }
        }
        
        return totalValue;
    }
}

// Enum for common material categories
public enum MaterialCategory
{
    RawMaterials,    // Wood, Stone, Scrap
    ProcessedGoods,  // Ingots, Planks, Components
    Tools,           // Hammers, Axes, etc.
    ConsumableItems, // Food, Potions, Fuel
    Rare,            // Special or rare materials
    Blueprints,      // Crafting recipes
    UpgradeKits      // Equipment upgrades
}