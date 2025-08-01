using UnityEngine;

public static class PriceCalculator
{
    [Header("Base Pricing Settings")]
    public static readonly float BaseBuyMultiplier = 1.2f;  // Shop sells at 120% of base value
    public static readonly float BaseSellMultiplier = 0.6f; // Shop buys at 60% of base value
    
    [Header("Bulk Sale Bonuses")]
    public static readonly int BulkThreshold1 = 10;   // First bulk bonus threshold
    public static readonly int BulkThreshold2 = 50;   // Second bulk bonus threshold
    public static readonly int BulkThreshold3 = 100;  // Third bulk bonus threshold
    
    public static readonly float BulkBonus1 = 0.05f; // 5% bonus for 10+ items
    public static readonly float BulkBonus2 = 0.10f; // 10% bonus for 50+ items  
    public static readonly float BulkBonus3 = 0.15f; // 15% bonus for 100+ items
    
    // Calculate price when buying FROM shop (player purchasing)
    public static int CalculateBuyPrice(MaterialData item, int quantity = 1)
    {
        if (item == null) return 0;
        
        float basePrice = item.baseValue * BaseBuyMultiplier; // Using MaterialData.baseValue
        float totalPrice = basePrice * quantity;
        
        // Apply any item-specific modifiers here
        // (future: reputation, shop specialization, etc.)
        
        return Mathf.RoundToInt(totalPrice);
    }
    
    // Calculate price when selling TO shop (player selling)
    public static int CalculateSellPrice(MaterialData item, int quantity = 1)
    {
        if (item == null) return 0;
        
        float basePrice = item.baseValue * BaseSellMultiplier;
        float totalPrice = basePrice * quantity;
        
        // Apply bulk sale bonuses
        float bulkMultiplier = CalculateBulkMultiplier(quantity);
        totalPrice *= bulkMultiplier;
        
        // Apply any item-specific modifiers here
        // (future: reputation, market fluctuation, etc.)
        
        return Mathf.RoundToInt(totalPrice);
    }
    
    // Calculate bulk sale multiplier
    private static float CalculateBulkMultiplier(int quantity)
    {
        if (quantity >= BulkThreshold3)
            return 1f + BulkBonus3;
        else if (quantity >= BulkThreshold2)
            return 1f + BulkBonus2;
        else if (quantity >= BulkThreshold1)
            return 1f + BulkBonus1;
        else
            return 1f; // No bonus
    }
    
    // Calculate sell price with category modifiers
    public static int CalculateSellPriceWithCategory(MaterialData item, int quantity, ShopCategoryData category)
    {
        if (item == null || category == null) 
            return CalculateSellPrice(item, quantity);
        
        float basePrice = CalculateSellPrice(item, quantity);
        return Mathf.RoundToInt(basePrice * category.sellPriceModifier);
    }
    
    // Calculate buy price with category modifiers  
    public static int CalculateBuyPriceWithCategory(MaterialData item, int quantity, ShopCategoryData category)
    {
        if (item == null || category == null) 
            return CalculateBuyPrice(item, quantity);
        
        float basePrice = CalculateBuyPrice(item, quantity);
        return Mathf.RoundToInt(basePrice * category.buyPriceModifier);
    }
    
    // Get bulk bonus percentage for UI display
    public static float GetBulkBonusPercentage(int quantity)
    {
        if (quantity >= BulkThreshold3)
            return BulkBonus3 * 100f;
        else if (quantity >= BulkThreshold2)
            return BulkBonus2 * 100f;
        else if (quantity >= BulkThreshold1)
            return BulkBonus1 * 100f;
        else
            return 0f;
    }
    
    // Get next bulk threshold for UI hints
    public static int GetNextBulkThreshold(int currentQuantity)
    {
        if (currentQuantity < BulkThreshold1)
            return BulkThreshold1;
        else if (currentQuantity < BulkThreshold2)
            return BulkThreshold2;
        else if (currentQuantity < BulkThreshold3)
            return BulkThreshold3;
        else
            return -1; // Max threshold reached
    }
    
    // Calculate total value of multiple different items (for bulk operations)
    public static int CalculateTotalSellValue(System.Collections.Generic.List<ItemQuantityPair> items)
    {
        int totalValue = 0;
        
        foreach (var itemPair in items)
        {
            totalValue += CalculateSellPrice(itemPair.item, itemPair.quantity);
        }
        
        return totalValue;
    }
    
    // Get price difference text for UI (show savings/markup)
    public static string GetPriceDifferenceText(MaterialData item, int quantity, bool isSelling)
    {
        if (item == null) return "";
        
        float baseValue = item.baseValue * quantity;
        float shopPrice = isSelling ? 
            CalculateSellPrice(item, quantity) : 
            CalculateBuyPrice(item, quantity);
        
        float difference = shopPrice - baseValue;
        float percentage = (difference / baseValue) * 100f;
        
        if (isSelling)
        {
            // When selling, negative difference means player gets less than base value
            if (difference < 0)
                return $"(-{Mathf.Abs(percentage):F0}%)";
            else
                return $"(+{percentage:F0}%)";
        }
        else
        {
            // When buying, positive difference means player pays more than base value
            if (difference > 0)
                return $"(+{percentage:F0}% 마크업)";
            else
                return $"(-{Mathf.Abs(percentage):F0}% 할인)";
        }
    }
    
    // Validate if transaction is economically sound
    public static bool IsValidTransaction(MaterialData item, int quantity, int offeredPrice, bool isSelling)
    {
        if (item == null || quantity <= 0) return false;
        
        int calculatedPrice = isSelling ? 
            CalculateSellPrice(item, quantity) : 
            CalculateBuyPrice(item, quantity);
        
        // Allow small variance for rounding
        return Mathf.Abs(offeredPrice - calculatedPrice) <= 1;
    }
}

// Helper class for multi-item calculations
[System.Serializable]
public class ItemQuantityPair
{
    public MaterialData item;
    public int quantity;
    
    public ItemQuantityPair(MaterialData item, int quantity)
    {
        this.item = item;
        this.quantity = quantity;
    }
}