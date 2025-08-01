using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class ShopManager : MonoBehaviour
{
    public static ShopManager instance = null;
    
    [Header("Shop System Settings")]
    [SerializeField] private List<ShopData> availableShops = new List<ShopData>();
    [SerializeField] private float autoRefreshInterval = 3600f; // 1 hour in seconds
    
    // Current shop state
    private ShopData currentShop = null;
    private bool isShopOpen = false;
    
    // Events
    public static event System.Action<ShopData> OnShopOpened;
    public static event System.Action<ShopData> OnShopClosed;
    public static event System.Action<ShopData> OnShopInventoryRefreshed;
    public static event System.Action<string> OnShopError;
    
    // Properties
    public ShopData CurrentShop => currentShop;
    public bool IsShopOpen => isShopOpen;
    public List<ShopData> AvailableShops => availableShops;
    
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
            return;
        }
        
        InitializeShopSystem();
    }
    
    void Start()
    {
        // Start auto-refresh timer
        if (autoRefreshInterval > 0)
        {
            InvokeRepeating(nameof(RefreshAllShops), autoRefreshInterval, autoRefreshInterval);
        }
    }
    
    void InitializeShopSystem()
    {
        Debug.Log($"ShopManager initialized with {availableShops.Count} shops");
        
        // Initialize all shop inventories
        foreach (var shop in availableShops)
        {
            if (shop != null)
            {
                shop.RefreshShopInventory();
            }
        }
    }
    
    // ===== Shop Opening/Closing =====
    
    public bool OpenShop(ShopData shopData)
    {
        if (shopData == null)
        {
            OnShopError?.Invoke("상점 데이터가 없습니다.");
            return false;
        }
        
        if (isShopOpen)
        {
            CloseShop();
        }
        
        if (!shopData.IsShopOpen())
        {
            OnShopError?.Invoke($"{shopData.shopName}은(는) 현재 영업시간이 아닙니다.");
            return false;
        }
        
        currentShop = shopData;
        isShopOpen = true;
        
        // Refresh shop inventory before opening
        currentShop.RefreshShopInventory();
        
        Debug.Log($"Shop opened: {currentShop.shopName}");
        OnShopOpened?.Invoke(currentShop);
        
        return true;
    }
    
    public void CloseShop()
    {
        if (!isShopOpen || currentShop == null) return;
        
        var closingShop = currentShop;
        currentShop = null;
        isShopOpen = false;
        
        Debug.Log($"Shop closed: {closingShop.shopName}");
        OnShopClosed?.Invoke(closingShop);
    }
    
    // ===== Shop Data Access =====
    
    public List<ShopItemData> GetShopBuyItems()
    {
        if (!isShopOpen || currentShop == null) return new List<ShopItemData>();
        
        return currentShop.GetAvailableBuyItems();
    }
    
    public List<MaterialData> GetShopSellItems()
    {
        if (!isShopOpen || currentShop == null) return new List<MaterialData>();
        
        return currentShop.GetAvailableSellItems();
    }
    
    public List<MaterialData> GetPlayerSellableItems()
    {
        if (!isShopOpen || currentShop == null) return new List<MaterialData>();
        
        var playerItems = InventoryManager.instance?.GetInventoryItems();
        if (playerItems == null) return new List<MaterialData>();
        
        return playerItems
            .Where(item => item.Value > 0 && currentShop.CanShopBuyItem(item.Key))
            .Select(item => item.Key)
            .ToList();
    }
    
    // ===== Shop Categories =====
    
    public List<ShopCategoryData> GetShopCategories()
    {
        if (!isShopOpen || currentShop == null) return new List<ShopCategoryData>();
        
        return currentShop.supportedCategories
            .Where(cat => cat != null && cat.showInShopUI)
            .OrderBy(cat => cat.sortOrder)
            .ToList();
    }
    
    public List<MaterialData> GetItemsByCategory(ShopCategoryData category, bool forSelling = true)
    {
        if (category == null) return new List<MaterialData>();
        
        if (forSelling)
        {
            // Get player items that can be sold in this category
            return GetPlayerSellableItems()
                .Where(item => category.ContainsItem(item))
                .ToList();
        }
        else
        {
            // Get shop items that can be bought in this category
            return GetShopBuyItems()
                .Where(shopItem => category.ContainsItem(shopItem.materialData))
                .Select(shopItem => shopItem.materialData)
                .ToList();
        }
    }
    
    // ===== Shop Registration =====
    
    public void RegisterShop(ShopData shopData)
    {
        if (shopData != null && !availableShops.Contains(shopData))
        {
            availableShops.Add(shopData);
            shopData.RefreshShopInventory();
            Debug.Log($"Shop registered: {shopData.shopName}");
        }
    }
    
    public void UnregisterShop(ShopData shopData)
    {
        if (availableShops.Contains(shopData))
        {
            availableShops.Remove(shopData);
            
            // Close shop if it's currently open
            if (currentShop == shopData)
            {
                CloseShop();
            }
            
            Debug.Log($"Shop unregistered: {shopData.shopName}");
        }
    }
    
    public ShopData FindShopByName(string shopName)
    {
        return availableShops.FirstOrDefault(shop => 
            shop != null && shop.shopName.Equals(shopName, System.StringComparison.OrdinalIgnoreCase));
    }
    
    // ===== Shop Refresh System =====
    
    public void RefreshAllShops()
    {
        foreach (var shop in availableShops)
        {
            if (shop != null)
            {
                shop.RefreshShopInventory();
                OnShopInventoryRefreshed?.Invoke(shop);
            }
        }
        
        Debug.Log("All shop inventories refreshed");
    }
    
    public void RefreshCurrentShop()
    {
        if (isShopOpen && currentShop != null)
        {
            currentShop.RefreshShopInventory();
            OnShopInventoryRefreshed?.Invoke(currentShop);
            Debug.Log($"Refreshed shop: {currentShop.shopName}");
        }
    }
    
    // ===== Price Information =====
    
    public int GetSellPrice(MaterialData item, int quantity = 1)
    {
        if (!isShopOpen || currentShop == null || item == null) return 0;
        
        return PriceManager.instance != null ? 
            PriceManager.instance.GetSellPrice(item, quantity, currentShop) :
            currentShop.CalculateEffectiveSellPrice(item, quantity);
    }
    
    public int GetBuyPrice(ShopItemData shopItem, int quantity = 1)
    {
        if (!isShopOpen || currentShop == null || shopItem == null) return 0;
        
        return PriceManager.instance != null ?
            PriceManager.instance.GetBuyPrice(shopItem, quantity, currentShop) :
            currentShop.CalculateEffectiveBuyPrice(shopItem, quantity);
    }
    
    // ===== Validation =====
    
    public bool CanPlayerSellItem(MaterialData item, int quantity = 1)
    {
        if (!isShopOpen || currentShop == null || item == null) return false;
        
        // Check if shop accepts this item
        if (!currentShop.CanShopBuyItem(item)) return false;
        
        // Check if player has enough items
        if (InventoryManager.instance == null) return false;
        
        return InventoryManager.instance.GetItemQuantity(item) >= quantity;
    }
    
    public bool CanPlayerBuyItem(ShopItemData shopItem, int quantity = 1)
    {
        if (!isShopOpen || currentShop == null || shopItem == null) return false;
        
        // Check if shop can sell this item
        if (!currentShop.CanShopSellItem(shopItem)) return false;
        
        // Check if shop has enough stock
        if (!shopItem.hasUnlimitedStock && shopItem.CurrentStock < quantity) return false;
        
        // Check if player has enough gold
        int totalCost = GetBuyPrice(shopItem, quantity);
        if (GoldManager.instance == null) return false;
        
        return GoldManager.instance.HasEnoughGold(totalCost);
    }
    
    // ===== Debug Methods =====
    
    [ContextMenu("Debug - Open First Shop")]
    public void DebugOpenFirstShop()
    {
        if (availableShops.Count > 0)
        {
            OpenShop(availableShops[0]);
        }
    }
    
    [ContextMenu("Debug - Refresh All Shops")]
    public void DebugRefreshAllShops()
    {
        RefreshAllShops();
    }
    
    [ContextMenu("Debug - Print Shop Info")]
    public void DebugPrintShopInfo()
    {
        if (isShopOpen && currentShop != null)
        {
            Debug.Log(currentShop.GetShopDebugInfo());
        }
        else
        {
            Debug.Log("No shop is currently open");
        }
    }
    
    // ===== Save/Load Support (Future) =====
    
    public void SaveShopStates()
    {
        // Placeholder for save system integration
        // Save shop stock states, refresh times, etc.
    }
    
    public void LoadShopStates()
    {
        // Placeholder for save system integration
        // Load shop stock states, refresh times, etc.
    }
    
    void OnDestroy()
    {
        // Clean up
        if (instance == this)
        {
            SaveShopStates();
        }
    }
}