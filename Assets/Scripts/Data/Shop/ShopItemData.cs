using UnityEngine;

[CreateAssetMenu(fileName = "ShopItem", menuName = "Game Data/Shop/Shop Item")]
public class ShopItemData : ScriptableObject
{
    [Header("Item Reference")]
    public MaterialData materialData; // Reference to the actual item
    
    [Header("Shop Availability")]
    public bool isAvailableForBuying = false;  // Can shop sell this to player
    public bool isAvailableForSelling = true;  // Can player sell this to shop
    
    [Header("Stock Settings (for buying)")]
    public bool hasUnlimitedStock = true; // Unlimited stock for core items
    [SerializeField] private int maxStock = 10; // Only used if hasUnlimitedStock = false
    [SerializeField] private int currentStock = 10; // Current available stock
    
    [Header("Refresh Settings")]
    [Tooltip("How often stock refreshes (in hours). 0 = never refreshes")]
    public float stockRefreshHours = 0f; // 0 for core items, 168 for weekly refresh
    [SerializeField] private float lastRefreshTime = 0f;
    
    [Header("Price Overrides")]
    [Tooltip("Override base buy price. 0 = use calculated price")]
    public int customBuyPrice = 0;
    
    [Tooltip("Override base sell price. 0 = use calculated price")]
    public int customSellPrice = 0;
    
    [Header("Special Settings")]
    [Tooltip("Item appears only after certain conditions are met")]
    public bool requiresUnlock = false;
    
    [Tooltip("Minimum player level to access this item")]
    public int requiredPlayerLevel = 0;
    
    [Tooltip("Special pricing - rare items might have different multipliers")]
    [Range(0.1f, 5f)]
    public float priceMultiplier = 1f;
    
    // Properties
    public int MaxStock => hasUnlimitedStock ? int.MaxValue : maxStock;
    public int CurrentStock => hasUnlimitedStock ? int.MaxValue : currentStock;
    public bool IsInStock => hasUnlimitedStock || currentStock > 0;
    public bool NeedsRefresh => stockRefreshHours > 0 && 
                               (Time.time - lastRefreshTime) >= (stockRefreshHours * 3600f);
    
    // Get effective buy price
    public int GetBuyPrice()
    {
        if (customBuyPrice > 0)
            return customBuyPrice;
            
        return PriceCalculator.CalculateBuyPrice(materialData) * Mathf.RoundToInt(priceMultiplier);
    }
    
    // Get effective sell price
    public int GetSellPrice(int quantity = 1)
    {
        if (customSellPrice > 0)
            return customSellPrice * quantity;
            
        return PriceCalculator.CalculateSellPrice(materialData, quantity) * Mathf.RoundToInt(priceMultiplier);
    }
    
    // Check if item is available for purchase
    public bool CanBuy(int quantity = 1)
    {
        if (!isAvailableForBuying) return false;
        if (requiresUnlock && !IsUnlocked()) return false;
        if (!IsInStock) return false;
        if (!hasUnlimitedStock && currentStock < quantity) return false;
        
        // Check if player has enough gold
        int totalCost = GetBuyPrice() * quantity;
        return GoldManager.instance != null && GoldManager.instance.HasEnoughGold(totalCost);
    }
    
    // Check if item can be sold
    public bool CanSell(int quantity = 1)
    {
        if (!isAvailableForSelling) return false;
        
        // Check if player has enough items
        if (InventoryManager.instance != null)
        {
            return InventoryManager.instance.GetItemQuantity(materialData) >= quantity;
        }
        
        return false;
    }
    
    // Consume stock when buying (only for limited stock items)
    public bool ConsumeStock(int quantity)
    {
        if (hasUnlimitedStock) return true;
        
        if (currentStock >= quantity)
        {
            currentStock -= quantity;
            return true;
        }
        
        return false;
    }
    
    // Refresh stock
    public void RefreshStock()
    {
        if (stockRefreshHours > 0)
        {
            currentStock = maxStock;
            lastRefreshTime = Time.time;
            Debug.Log($"Refreshed stock for {materialData.materialName}: {currentStock}/{maxStock}");
        }
    }
    
    // Check if item is unlocked (placeholder for future unlock system)
    private bool IsUnlocked()
    {
        if (!requiresUnlock) return true;
        
        // Check player level
        if (PlayerDataManager.instance != null)
        {
            return PlayerDataManager.instance.GetLevel() >= requiredPlayerLevel;
        }
        
        // Add other unlock conditions here (quests, achievements, etc.)
        return true;
    }
    
    // Get stock status text for UI
    public string GetStockStatusText()
    {
        if (hasUnlimitedStock)
            return "재고 충분";
            
        if (currentStock <= 0)
            return "품절";
            
        return $"재고: {currentStock}/{maxStock}";
    }
    
    // Runtime initialization
    private void OnEnable()
    {
        // Initialize stock if this is the first time
        if (lastRefreshTime == 0f)
        {
            currentStock = maxStock;
            lastRefreshTime = Time.time;
        }
        
        // Auto-refresh if needed
        if (NeedsRefresh)
        {
            RefreshStock();
        }
    }
}