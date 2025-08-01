using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[CreateAssetMenu(fileName = "ShopData", menuName = "Game Data/Shop/Shop Data")]
public class ShopData : ScriptableObject
{
    [Header("Shop Information")]
    public string shopName = "일반 상점";
    public string shopDescription = "다양한 물품을 사고팔 수 있는 상점입니다.";
    public Sprite shopIcon;
    
    [Header("Shop Type & Behavior")]
    public ShopType shopType = ShopType.Universal;
    public bool isAlwaysOpen = true;
    public bool allowBuying = true;  // Can player buy items from this shop
    public bool allowSelling = true; // Can player sell items to this shop
    
    [Header("Available Categories")]
    [Tooltip("Categories this shop deals with. Empty list = accepts all categories")]
    public List<ShopCategoryData> supportedCategories = new List<ShopCategoryData>();
    
    [Header("Shop Inventory (Items available for buying)")]
    [Tooltip("Items this shop sells to players. For universal shop, leave empty to accept all")]
    public List<ShopItemData> shopInventory = new List<ShopItemData>();
    
    [Header("Buying Settings (Shop selling to player)")]
    [Range(0.5f, 3f)]
    public float buyPriceMultiplier = 1.2f; // Shop sells at 120% of base price
    
    [Header("Selling Settings (Player selling to shop)")]
    [Range(0.1f, 1f)]
    public float sellPriceMultiplier = 0.6f; // Shop buys at 60% of base price
    
    [Header("Special Modifiers")]
    [Tooltip("Items this shop specializes in (better prices)")]
    public List<MaterialData> specializedItems = new List<MaterialData>();
    
    [Range(0f, 0.5f)]
    [Tooltip("Bonus percentage for specialized items")]
    public float specializationBonus = 0.1f; // 10% better prices
    
    [Header("Operating Hours (Future Feature)")]
    public bool hasOperatingHours = false;
    public int openHour = 8;  // 8 AM
    public int closeHour = 20; // 8 PM
    
    [Header("Shop Keeper")]
    public string shopKeeperName = "상점 주인";
    public Sprite shopKeeperPortrait;
    
    // Get all items that this shop can sell to the player
    public List<ShopItemData> GetAvailableBuyItems()
    {
        return shopInventory.Where(item => 
            item != null && 
            item.isAvailableForBuying && 
            item.IsInStock &&
            (!item.requiresUnlock || IsItemUnlockedForPlayer(item))
        ).ToList();
    }
    
    // Get all items that this shop will buy from the player
    public List<MaterialData> GetAvailableSellItems()
    {
        List<MaterialData> availableItems = new List<MaterialData>();
        
        if (!allowSelling) return availableItems;
        
        // Get all items from inventory that belong to supported categories
        var playerItems = InventoryManager.instance?.GetInventoryItems();
        if (playerItems == null) return availableItems;
        
        foreach (var playerItem in playerItems)
        {
            if (playerItem.Value > 0 && CanShopBuyItem(playerItem.Key))
            {
                availableItems.Add(playerItem.Key);
            }
        }
        
        return availableItems;
    }
    
    // Check if shop can buy this item from player  
    public bool CanShopBuyItem(MaterialData item)
    {
        if (!allowSelling || item == null) return false;
        
        // If no categories specified, accept all items
        if (supportedCategories.Count == 0) return true;
        
        // Check if item belongs to any supported category
        return supportedCategories.Any(category => category.ContainsItem(item));
    }
    
    // Check if shop can sell this item to player
    public bool CanShopSellItem(ShopItemData shopItem)
    {
        if (!allowBuying || shopItem == null) return false;
        
        return shopItem.isAvailableForBuying && 
               shopItem.IsInStock &&
               (!shopItem.requiresUnlock || IsItemUnlockedForPlayer(shopItem));
    }
    
    // Calculate effective sell price (player selling to shop)
    public int CalculateEffectiveSellPrice(MaterialData item, int quantity)
    {
        if (item == null) return 0;
        
        float basePrice = PriceCalculator.CalculateSellPrice(item, quantity);
        float finalPrice = basePrice * sellPriceMultiplier;
        
        // Apply specialization bonus
        if (specializedItems.Contains(item))
        {
            finalPrice *= (1f + specializationBonus);
        }
        
        // Apply category modifier if applicable
        var category = GetItemCategory(item);
        if (category != null)
        {
            finalPrice *= category.sellPriceModifier;
        }
        
        return Mathf.RoundToInt(finalPrice);
    }
    
    // Calculate effective buy price (shop selling to player)
    public int CalculateEffectiveBuyPrice(ShopItemData shopItem, int quantity = 1)
    {
        if (shopItem?.materialData == null) return 0;
        
        // Use custom price if set
        if (shopItem.customBuyPrice > 0)
        {
            return shopItem.customBuyPrice * quantity;
        }
        
        float basePrice = PriceCalculator.CalculateBuyPrice(shopItem.materialData, quantity);
        float finalPrice = basePrice * buyPriceMultiplier;
        
        // Apply specialization bonus (discount for specialized items)
        if (specializedItems.Contains(shopItem.materialData))
        {
            finalPrice *= (1f - specializationBonus); // Discount instead of markup
        }
        
        // Apply shop item specific multiplier
        finalPrice *= shopItem.priceMultiplier;
        
        return Mathf.RoundToInt(finalPrice);
    }
    
    // Get category that contains this item
    private ShopCategoryData GetItemCategory(MaterialData item)
    {
        return supportedCategories.FirstOrDefault(category => category.ContainsItem(item));
    }
    
    // Check if item is unlocked for current player (placeholder)
    private bool IsItemUnlockedForPlayer(ShopItemData shopItem)
    {
        if (!shopItem.requiresUnlock) return true;
        
        // Check player level
        if (PlayerDataManager.instance != null)
        {
            return PlayerDataManager.instance.GetLevel() >= shopItem.requiredPlayerLevel;
        }
        
        return true;
    }
    
    // Check if shop is currently open
    public bool IsShopOpen()
    {
        if (isAlwaysOpen) return true;
        if (!hasOperatingHours) return true;
        
        // Get current time (placeholder - implement actual time system)
        int currentHour = System.DateTime.Now.Hour;
        
        if (openHour <= closeHour)
        {
            // Normal hours (e.g., 8 AM to 8 PM)
            return currentHour >= openHour && currentHour < closeHour;
        }
        else
        {
            // Overnight hours (e.g., 10 PM to 6 AM)
            return currentHour >= openHour || currentHour < closeHour;
        }
    }
    
    // Get shop status text for UI
    public string GetShopStatusText()
    {
        if (!IsShopOpen())
        {
            return $"영업 종료 (영업시간: {openHour}시 - {closeHour}시)";
        }
        
        List<string> status = new List<string>();
        
        if (allowBuying && shopInventory.Count > 0)
            status.Add("구매 가능");
            
        if (allowSelling)
            status.Add("판매 가능");
            
        return string.Join(" • ", status);
    }
    
    // Refresh all shop items that need refreshing
    public void RefreshShopInventory()
    {
        foreach (var item in shopInventory)
        {
            if (item != null && item.NeedsRefresh)
            {
                item.RefreshStock();
            }
        }
        
        Debug.Log($"Shop '{shopName}' inventory refreshed");
    }
    
    // Get shop statistics for debugging
    public string GetShopDebugInfo()
    {
        int buyableItems = GetAvailableBuyItems().Count;
        int sellableItems = GetAvailableSellItems().Count;
        
        return $"Shop: {shopName}\n" +
               $"Type: {shopType}\n" +
               $"Buyable Items: {buyableItems}\n" +
               $"Sellable Items: {sellableItems}\n" +
               $"Categories: {supportedCategories.Count}\n" +
               $"Specialized Items: {specializedItems.Count}\n" +
               $"Status: {(IsShopOpen() ? "Open" : "Closed")}";
    }
}

// Shop types for future expansion
public enum ShopType
{
    Universal,      // Buys and sells everything (current implementation)
    MaterialBuyer,  // Only buys materials from player
    ToolShop,       // Specializes in tools and equipment
    RareGoods,      // Deals in rare/special items
    Consumables,    // Food, potions, consumable items
    Blueprints      // Crafting recipes and upgrades
}