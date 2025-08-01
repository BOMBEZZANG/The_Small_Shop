using UnityEngine;
using System.Collections.Generic;

public class PriceManager : MonoBehaviour
{
    public static PriceManager instance = null;
    
    [Header("Dynamic Pricing Settings")]
    [SerializeField] private bool enableDynamicPricing = false; // Start with simple fixed pricing
    [SerializeField] private float reputationInfluence = 0.1f; // Future feature
    [SerializeField] private float demandInfluence = 0.05f; // Future feature
    
    [Header("Price History")]
    [SerializeField] private bool trackPriceHistory = true;
    [SerializeField] private int maxPriceHistoryEntries = 1000;
    
    // Price tracking
    private Dictionary<int, List<PriceHistoryEntry>> priceHistory = new Dictionary<int, List<PriceHistoryEntry>>();
    private Dictionary<int, float> itemDemand = new Dictionary<int, float>(); // Future: demand-based pricing
    
    // Events
    public static event System.Action<MaterialData, int, int> OnPriceCalculated; // item, quantity, price
    public static event System.Action<MaterialData, float> OnDemandChanged; // item, newDemand
    
    void Awake()
    {
        // Singleton pattern
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }
    
    // ===== Core Price Calculation =====
    
    public int GetSellPrice(MaterialData item, int quantity, ShopData shop)
    {
        if (item == null || shop == null) return 0;
        
        int basePrice;
        
        if (enableDynamicPricing)
        {
            basePrice = CalculateDynamicSellPrice(item, quantity, shop);
        }
        else
        {
            // Use simple fixed pricing from shop
            basePrice = shop.CalculateEffectiveSellPrice(item, quantity);
        }
        
        // Record price for history
        RecordPriceHistory(item, basePrice, quantity, TransactionType.Sell, shop.shopName);
        
        OnPriceCalculated?.Invoke(item, quantity, basePrice);
        return basePrice;
    }
    
    public int GetBuyPrice(ShopItemData shopItem, int quantity, ShopData shop)
    {
        if (shopItem?.materialData == null || shop == null) return 0;
        
        int basePrice;
        
        if (enableDynamicPricing)
        {
            basePrice = CalculateDynamicBuyPrice(shopItem, quantity, shop);
        }
        else
        {
            // Use simple fixed pricing from shop
            basePrice = shop.CalculateEffectiveBuyPrice(shopItem, quantity);
        }
        
        // Record price for history
        RecordPriceHistory(shopItem.materialData, basePrice, quantity, TransactionType.Buy, shop.shopName);
        
        OnPriceCalculated?.Invoke(shopItem.materialData, quantity, basePrice);
        return basePrice;
    }
    
    // ===== Dynamic Pricing (Future Feature) =====
    
    private int CalculateDynamicSellPrice(MaterialData item, int quantity, ShopData shop)
    {
        // Start with base shop price
        float price = shop.CalculateEffectiveSellPrice(item, quantity);
        
        // Apply reputation modifier (future)
        // price *= GetReputationModifier(shop);
        
        // Apply demand modifier (future)
        // price *= GetDemandModifier(item);
        
        // Apply time-based modifiers (future)
        // price *= GetTimeModifier();
        
        return Mathf.RoundToInt(price);
    }
    
    private int CalculateDynamicBuyPrice(ShopItemData shopItem, int quantity, ShopData shop)
    {
        // Start with base shop price
        float price = shop.CalculateEffectiveBuyPrice(shopItem, quantity);
        
        // Apply reputation modifier (future)
        // price *= GetReputationModifier(shop);
        
        // Apply stock scarcity modifier (future)
        // price *= GetScarcityModifier(shopItem);
        
        return Mathf.RoundToInt(price);
    }
    
    // ===== Price History Tracking =====
    
    private void RecordPriceHistory(MaterialData item, int price, int quantity, TransactionType type, string shopName)
    {
        if (!trackPriceHistory || item == null) return;
        
        int itemId = item.materialID;
        
        if (!priceHistory.ContainsKey(itemId))
        {
            priceHistory[itemId] = new List<PriceHistoryEntry>();
        }
        
        var entry = new PriceHistoryEntry
        {
            price = price,
            quantity = quantity,
            pricePerUnit = price / quantity,
            transactionType = type,
            shopName = shopName,
            timestamp = System.DateTime.Now
        };
        
        priceHistory[itemId].Add(entry);
        
        // Maintain max history size
        if (priceHistory[itemId].Count > maxPriceHistoryEntries)
        {
            priceHistory[itemId].RemoveAt(0);
        }
    }
    
    public List<PriceHistoryEntry> GetPriceHistory(MaterialData item, int maxEntries = 50)
    {
        if (item == null || !priceHistory.ContainsKey(item.materialID))
            return new List<PriceHistoryEntry>();
        
        var history = priceHistory[item.materialID];
        int startIndex = Mathf.Max(0, history.Count - maxEntries);
        
        return history.GetRange(startIndex, history.Count - startIndex);
    }
    
    public float GetAverageSellPrice(MaterialData item, int days = 7)
    {
        if (item == null) return 0f;
        
        var cutoffTime = System.DateTime.Now.AddDays(-days);
        var history = GetPriceHistory(item);
        
        float totalPrice = 0f;
        int count = 0;
        
        foreach (var entry in history)
        {
            if (entry.timestamp >= cutoffTime && entry.transactionType == TransactionType.Sell)
            {
                totalPrice += entry.pricePerUnit;
                count++;
            }
        }
        
        return count > 0 ? totalPrice / count : 0f;
    }
    
    public float GetAverageBuyPrice(MaterialData item, int days = 7)
    {
        if (item == null) return 0f;
        
        var cutoffTime = System.DateTime.Now.AddDays(-days);
        var history = GetPriceHistory(item);
        
        float totalPrice = 0f;
        int count = 0;
        
        foreach (var entry in history)
        {
            if (entry.timestamp >= cutoffTime && entry.transactionType == TransactionType.Buy)
            {
                totalPrice += entry.pricePerUnit;
                count++;
            }
        }
        
        return count > 0 ? totalPrice / count : 0f;
    }
    
    // ===== Price Comparison & Analysis =====
    
    public PriceComparison ComparePrices(MaterialData item, int quantity = 1)
    {
        if (item == null) return null;
        
        var comparison = new PriceComparison
        {
            item = item,
            quantity = quantity,
            baseValue = item.baseValue * quantity // MaterialData base value
        };
        
        if (ShopManager.instance.IsShopOpen)
        {
            var currentShop = ShopManager.instance.CurrentShop;
            comparison.currentSellPrice = GetSellPrice(item, quantity, currentShop);
            comparison.currentShopName = currentShop.shopName;
        }
        
        // Calculate averages
        comparison.averageSellPrice = Mathf.RoundToInt(GetAverageSellPrice(item) * quantity);
        comparison.averageBuyPrice = Mathf.RoundToInt(GetAverageBuyPrice(item) * quantity);
        
        // Calculate profit margins
        if (comparison.baseValue > 0)
        {
            comparison.sellMarginPercent = ((float)comparison.currentSellPrice / comparison.baseValue - 1f) * 100f;
        }
        
        return comparison;
    }
    
    // ===== Bulk Pricing Analysis =====
    
    public Dictionary<int, int> CalculateBulkPricing(MaterialData item, List<int> quantities)
    {
        var pricing = new Dictionary<int, int>();
        
        if (item == null || !ShopManager.instance.IsShopOpen) return pricing;
        
        var currentShop = ShopManager.instance.CurrentShop;
        
        foreach (int quantity in quantities)
        {
            if (quantity > 0)
            {
                int price = GetSellPrice(item, quantity, currentShop);
                pricing[quantity] = price;
            }
        }
        
        return pricing;
    }
    
    public string GetBulkPricingText(MaterialData item)
    {
        if (item == null) return "";
        
        var quantities = new List<int> { 1, 10, 50, 100 };
        var pricing = CalculateBulkPricing(item, quantities);
        
        var lines = new List<string>();
        
        foreach (var kvp in pricing)
        {
            int quantity = kvp.Key;
            int totalPrice = kvp.Value;
            int pricePerUnit = totalPrice / quantity;
            
            float bonusPercent = PriceCalculator.GetBulkBonusPercentage(quantity);
            string bonusText = bonusPercent > 0 ? $" (+{bonusPercent:F0}%)" : "";
            
            lines.Add($"{quantity}개: {totalPrice}골드 (개당 {pricePerUnit}골드){bonusText}");
        }
        
        return string.Join("\n", lines);
    }
    
    // ===== Market Trends (Future Feature) =====
    
    public MarketTrend GetMarketTrend(MaterialData item, int days = 7)
    {
        if (item == null) return MarketTrend.Stable;
        
        var recentHistory = GetPriceHistory(item, days * 10); // Rough estimate
        if (recentHistory.Count < 2) return MarketTrend.Stable;
        
        // Simple trend analysis based on recent prices
        var recentPrices = new List<float>();
        var cutoffTime = System.DateTime.Now.AddDays(-days);
        
        foreach (var entry in recentHistory)
        {
            if (entry.timestamp >= cutoffTime && entry.transactionType == TransactionType.Sell)
            {
                recentPrices.Add(entry.pricePerUnit);
            }
        }
        
        if (recentPrices.Count < 2) return MarketTrend.Stable;
        
        float firstHalf = 0f, secondHalf = 0f;
        int halfPoint = recentPrices.Count / 2;
        
        for (int i = 0; i < halfPoint; i++)
            firstHalf += recentPrices[i];
        for (int i = halfPoint; i < recentPrices.Count; i++)
            secondHalf += recentPrices[i];
        
        firstHalf /= halfPoint;
        secondHalf /= (recentPrices.Count - halfPoint);
        
        float changePercent = (secondHalf / firstHalf - 1f) * 100f;
        
        if (changePercent > 10f) return MarketTrend.Rising;
        if (changePercent < -10f) return MarketTrend.Falling;
        return MarketTrend.Stable;
    }
    
    // ===== Settings & Configuration =====
    
    public void EnableDynamicPricing(bool enable)
    {
        enableDynamicPricing = enable;
        Debug.Log($"Dynamic pricing {(enable ? "enabled" : "disabled")}");
    }
    
    public void ClearPriceHistory()
    {
        priceHistory.Clear();
        Debug.Log("Price history cleared");
    }
    
    // ===== Debug Methods =====
    
    [ContextMenu("Debug - Print Price History")]
    public void DebugPrintPriceHistory()
    {
        if (ShopManager.instance.IsShopOpen)
        {
            var sellableItems = ShopManager.instance.GetPlayerSellableItems();
            
            foreach (var item in sellableItems)
            {
                var history = GetPriceHistory(item, 5);
                if (history.Count > 0)
                {
                    Debug.Log($"=== {item.materialName} Price History ===");
                    foreach (var entry in history)
                    {
                        Debug.Log($"{entry.timestamp:MM/dd HH:mm} - {entry.transactionType} - " +
                                 $"{entry.quantity}x @ {entry.pricePerUnit} each = {entry.price} total");
                    }
                }
            }
        }
    }
    
    [ContextMenu("Debug - Toggle Dynamic Pricing")]
    public void DebugToggleDynamicPricing()
    {
        EnableDynamicPricing(!enableDynamicPricing);
    }
}

// Price history entry
[System.Serializable]
public class PriceHistoryEntry
{
    public int price;
    public int quantity;
    public float pricePerUnit;
    public TransactionType transactionType;
    public string shopName;
    public System.DateTime timestamp;
}

// Price comparison data
[System.Serializable]
public class PriceComparison
{
    public MaterialData item;
    public int quantity;
    public int baseValue;
    public int currentSellPrice;
    public int averageSellPrice;
    public int averageBuyPrice;
    public string currentShopName;
    public float sellMarginPercent;
}

// Market trend enum
public enum MarketTrend
{
    Falling,    // Prices decreasing
    Stable,     // Prices stable
    Rising      // Prices increasing
}