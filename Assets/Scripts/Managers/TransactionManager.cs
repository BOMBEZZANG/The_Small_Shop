using UnityEngine;
using System.Collections.Generic;

public class TransactionManager : MonoBehaviour
{
    public static TransactionManager instance = null;
    
    [Header("Transaction Settings")]
    [SerializeField] private bool enableTransactionLog = true;
    [SerializeField] private int maxTransactionHistory = 100;
    
    // Transaction history
    private Queue<TransactionRecord> transactionHistory = new Queue<TransactionRecord>();
    
    // Events
    public static event System.Action<MaterialData, int, int> OnItemSold; // item, quantity, goldEarned
    public static event System.Action<MaterialData, int, int> OnItemBought; // item, quantity, goldSpent
    public static event System.Action<int> OnGoldChanged; // newGoldAmount
    public static event System.Action<TransactionRecord> OnTransactionCompleted;
    public static event System.Action<string> OnTransactionFailed;
    
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
    
    // ===== Selling (Player to Shop) =====
    
    public bool SellItem(MaterialData item, int quantity)
    {
        if (item == null || quantity <= 0)
        {
            OnTransactionFailed?.Invoke("잘못된 아이템 또는 수량입니다.");
            return false;
        }
        
        // Validate shop state
        if (!ShopManager.instance.IsShopOpen)
        {
            OnTransactionFailed?.Invoke("상점이 열려있지 않습니다.");
            return false;
        }
        
        var currentShop = ShopManager.instance.CurrentShop;
        if (!currentShop.allowSelling)
        {
            OnTransactionFailed?.Invoke("이 상점에서는 판매할 수 없습니다.");
            return false;
        }
        
        // Check if shop accepts this item
        if (!currentShop.CanShopBuyItem(item))
        {
            OnTransactionFailed?.Invoke($"{currentShop.shopName}에서는 {item.materialName}을(를) 취급하지 않습니다.");
            return false;
        }
        
        // Check if player has enough items
        if (InventoryManager.instance == null)
        {
            OnTransactionFailed?.Invoke("인벤토리 시스템이 없습니다.");
            return false;
        }
        
        int playerQuantity = InventoryManager.instance.GetItemQuantity(item);
        if (playerQuantity < quantity)
        {
            OnTransactionFailed?.Invoke($"{item.materialName}이(가) 부족합니다. (보유: {playerQuantity}개)");
            return false;
        }
        
        // Calculate price
        int sellPrice = ShopManager.instance.GetSellPrice(item, quantity);
        if (sellPrice <= 0)
        {
            OnTransactionFailed?.Invoke("가격을 계산할 수 없습니다.");
            return false;
        }
        
        // Execute transaction
        if (InventoryManager.instance.RemoveItem(item, quantity))
        {
            // Add gold to player
            if (GoldManager.instance != null)
            {
                GoldManager.instance.AddGold(sellPrice);
            }
            
            // Record transaction
            var transaction = new TransactionRecord
            {
                transactionType = TransactionType.Sell,
                item = item,
                quantity = quantity,
                pricePerItem = sellPrice / quantity,
                totalPrice = sellPrice,
                shopName = currentShop.shopName,
                timestamp = System.DateTime.Now
            };
            
            RecordTransaction(transaction);
            
            // Fire events
            OnItemSold?.Invoke(item, quantity, sellPrice);
            OnGoldChanged?.Invoke(GoldManager.instance?.GetGold() ?? 0);
            OnTransactionCompleted?.Invoke(transaction);
            
            Debug.Log($"Sold {quantity}x {item.materialName} for {sellPrice} gold");
            return true;
        }
        else
        {
            OnTransactionFailed?.Invoke("아이템 제거에 실패했습니다.");
            return false;
        }
    }
    
    // Bulk sell multiple items
    public bool SellMultipleItems(List<ItemQuantityPair> itemsToSell)
    {
        if (itemsToSell == null || itemsToSell.Count == 0)
        {
            OnTransactionFailed?.Invoke("판매할 아이템이 없습니다.");
            return false;
        }
        
        // Validate all items first
        int totalValue = 0;
        foreach (var itemPair in itemsToSell)
        {
            if (!ShopManager.instance.CanPlayerSellItem(itemPair.item, itemPair.quantity))
            {
                OnTransactionFailed?.Invoke($"{itemPair.item.materialName} 판매에 실패했습니다.");
                return false;
            }
            totalValue += ShopManager.instance.GetSellPrice(itemPair.item, itemPair.quantity);
        }
        
        // Execute all transactions
        List<TransactionRecord> transactions = new List<TransactionRecord>();
        int totalGoldEarned = 0;
        
        foreach (var itemPair in itemsToSell)
        {
            int sellPrice = ShopManager.instance.GetSellPrice(itemPair.item, itemPair.quantity);
            
            if (InventoryManager.instance.RemoveItem(itemPair.item, itemPair.quantity))
            {
                totalGoldEarned += sellPrice;
                
                var transaction = new TransactionRecord
                {
                    transactionType = TransactionType.Sell,
                    item = itemPair.item,
                    quantity = itemPair.quantity,
                    pricePerItem = sellPrice / itemPair.quantity,
                    totalPrice = sellPrice,
                    shopName = ShopManager.instance.CurrentShop.shopName,
                    timestamp = System.DateTime.Now
                };
                
                transactions.Add(transaction);
                OnItemSold?.Invoke(itemPair.item, itemPair.quantity, sellPrice);
            }
        }
        
        // Add total gold
        if (GoldManager.instance != null && totalGoldEarned > 0)
        {
            GoldManager.instance.AddGold(totalGoldEarned);
        }
        
        // Record all transactions
        foreach (var transaction in transactions)
        {
            RecordTransaction(transaction);
            OnTransactionCompleted?.Invoke(transaction);
        }
        
        OnGoldChanged?.Invoke(GoldManager.instance?.GetGold() ?? 0);
        
        Debug.Log($"Bulk sell completed: {transactions.Count} items, {totalGoldEarned} gold earned");
        return true;
    }
    
    // ===== Buying (Shop to Player) =====
    
    public bool BuyItem(ShopItemData shopItem, int quantity)
    {
        if (shopItem?.materialData == null || quantity <= 0)
        {
            OnTransactionFailed?.Invoke("잘못된 아이템 또는 수량입니다.");
            return false;
        }
        
        // Validate shop state
        if (!ShopManager.instance.IsShopOpen)
        {
            OnTransactionFailed?.Invoke("상점이 열려있지 않습니다.");
            return false;
        }
        
        var currentShop = ShopManager.instance.CurrentShop;
        if (!currentShop.allowBuying)
        {
            OnTransactionFailed?.Invoke("이 상점에서는 구매할 수 없습니다.");
            return false;
        }
        
        // Check if shop can sell this item
        if (!currentShop.CanShopSellItem(shopItem))
        {
            OnTransactionFailed?.Invoke($"{shopItem.materialData.materialName}을(를) 구매할 수 없습니다.");
            return false;
        }
        
        // Check stock
        if (!shopItem.hasUnlimitedStock && shopItem.CurrentStock < quantity)
        {
            OnTransactionFailed?.Invoke($"재고가 부족합니다. (재고: {shopItem.CurrentStock}개)");
            return false;
        }
        
        // Calculate price
        int buyPrice = ShopManager.instance.GetBuyPrice(shopItem, quantity);
        if (buyPrice <= 0)
        {
            OnTransactionFailed?.Invoke("가격을 계산할 수 없습니다.");
            return false;
        }
        
        // Check if player has enough gold
        if (GoldManager.instance == null)
        {
            OnTransactionFailed?.Invoke("골드 시스템이 없습니다.");
            return false;
        }
        
        if (!GoldManager.instance.HasEnoughGold(buyPrice))
        {
            OnTransactionFailed?.Invoke($"골드가 부족합니다. (필요: {buyPrice}, 보유: {GoldManager.instance.GetGold()})");
            return false;
        }
        
        // Execute transaction
        if (GoldManager.instance.SpendGold(buyPrice))
        {
            // Add item to player inventory
            if (InventoryManager.instance != null)
            {
                InventoryManager.instance.AddItem(shopItem.materialData, quantity);
            }
            
            // Consume shop stock
            shopItem.ConsumeStock(quantity);
            
            // Record transaction
            var transaction = new TransactionRecord
            {
                transactionType = TransactionType.Buy,
                item = shopItem.materialData,
                quantity = quantity,
                pricePerItem = buyPrice / quantity,
                totalPrice = buyPrice,
                shopName = currentShop.shopName,
                timestamp = System.DateTime.Now
            };
            
            RecordTransaction(transaction);
            
            // Fire events
            OnItemBought?.Invoke(shopItem.materialData, quantity, buyPrice);
            OnGoldChanged?.Invoke(GoldManager.instance.GetGold());
            OnTransactionCompleted?.Invoke(transaction);
            
            Debug.Log($"Bought {quantity}x {shopItem.materialData.materialName} for {buyPrice} gold");
            return true;
        }
        else
        {
            OnTransactionFailed?.Invoke("골드 지불에 실패했습니다.");
            return false;
        }
    }
    
    // ===== Quick Actions =====
    
    public bool SellAllOfItem(MaterialData item)
    {
        if (InventoryManager.instance == null) return false;
        
        int quantity = InventoryManager.instance.GetItemQuantity(item);
        if (quantity <= 0) return false;
        
        return SellItem(item, quantity);
    }
    
    public bool SellAllItems()
    {
        if (InventoryManager.instance == null) return false;
        
        var playerItems = InventoryManager.instance.GetInventoryItems();
        var sellableItems = new List<ItemQuantityPair>();
        
        foreach (var playerItem in playerItems)
        {
            if (ShopManager.instance.CanPlayerSellItem(playerItem.Key, playerItem.Value))
            {
                sellableItems.Add(new ItemQuantityPair(playerItem.Key, playerItem.Value));
            }
        }
        
        if (sellableItems.Count == 0)
        {
            OnTransactionFailed?.Invoke("판매 가능한 아이템이 없습니다.");
            return false;
        }
        
        return SellMultipleItems(sellableItems);
    }
    
    // ===== Transaction History =====
    
    private void RecordTransaction(TransactionRecord transaction)
    {
        if (!enableTransactionLog) return;
        
        transactionHistory.Enqueue(transaction);
        
        // Maintain max history size
        while (transactionHistory.Count > maxTransactionHistory)
        {
            transactionHistory.Dequeue();
        }
    }
    
    public List<TransactionRecord> GetTransactionHistory(int maxRecords = 50)
    {
        var records = new List<TransactionRecord>(transactionHistory);
        
        // Return most recent transactions first
        records.Reverse();
        
        if (records.Count > maxRecords)
        {
            records = records.GetRange(0, maxRecords);
        }
        
        return records;
    }
    
    public List<TransactionRecord> GetTransactionsByType(TransactionType type, int maxRecords = 50)
    {
        var allRecords = GetTransactionHistory();
        var filteredRecords = new List<TransactionRecord>();
        
        foreach (var record in allRecords)
        {
            if (record.transactionType == type)
            {
                filteredRecords.Add(record);
                if (filteredRecords.Count >= maxRecords) break;
            }
        }
        
        return filteredRecords;
    }
    
    public void ClearTransactionHistory()
    {
        transactionHistory.Clear();
        Debug.Log("Transaction history cleared");
    }
    
    // ===== Statistics =====
    
    public TransactionStats GetTransactionStats()
    {
        var stats = new TransactionStats();
        
        foreach (var transaction in transactionHistory)
        {
            if (transaction.transactionType == TransactionType.Buy)
            {
                stats.totalGoldSpent += transaction.totalPrice;
                stats.totalItemsBought += transaction.quantity;
                stats.buyTransactionCount++;
            }
            else if (transaction.transactionType == TransactionType.Sell)
            {
                stats.totalGoldEarned += transaction.totalPrice;
                stats.totalItemsSold += transaction.quantity;
                stats.sellTransactionCount++;
            }
        }
        
        return stats;
    }
    
    // ===== Debug Methods =====
    
    [ContextMenu("Debug - Print Transaction History")]
    public void DebugPrintTransactionHistory()
    {
        var history = GetTransactionHistory(10);
        Debug.Log($"=== Last {history.Count} Transactions ===");
        
        foreach (var transaction in history)
        {
            Debug.Log($"{transaction.timestamp:HH:mm:ss} - {transaction.transactionType} - " +
                     $"{transaction.quantity}x {transaction.item.materialName} - " +
                     $"{transaction.totalPrice} gold at {transaction.shopName}");
        }
    }
    
    [ContextMenu("Debug - Print Transaction Stats")]
    public void DebugPrintTransactionStats()
    {
        var stats = GetTransactionStats();
        Debug.Log($"=== Transaction Statistics ===\n" +
                 $"Bought: {stats.totalItemsBought} items, spent {stats.totalGoldSpent} gold\n" +
                 $"Sold: {stats.totalItemsSold} items, earned {stats.totalGoldEarned} gold\n" +
                 $"Net Gold: {stats.totalGoldEarned - stats.totalGoldSpent}\n" +
                 $"Transactions: {stats.buyTransactionCount} buys, {stats.sellTransactionCount} sells");
    }
}

// Transaction record for history tracking
[System.Serializable]
public class TransactionRecord
{
    public TransactionType transactionType;
    public MaterialData item;
    public int quantity;
    public int pricePerItem;
    public int totalPrice;
    public string shopName;
    public System.DateTime timestamp;
}

public enum TransactionType
{
    Buy,    // Player buying from shop
    Sell    // Player selling to shop
}

// Statistics class
[System.Serializable]
public class TransactionStats
{
    public int totalGoldSpent = 0;
    public int totalGoldEarned = 0;
    public int totalItemsBought = 0;
    public int totalItemsSold = 0;
    public int buyTransactionCount = 0;
    public int sellTransactionCount = 0;
}