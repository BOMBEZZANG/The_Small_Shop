using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

public class ShopUI : MonoBehaviour
{
    [Header("Header References")]
    [SerializeField] private GameObject shopPanel;
    [SerializeField] private TextMeshProUGUI shopNameText;
    [SerializeField] private TextMeshProUGUI shopKeeperText;
    [SerializeField] private Button closeButton;
    
    [Header("Tab References")]
    [SerializeField] private Button buyTabButton;
    [SerializeField] private Button sellTabButton;
    [SerializeField] private Color tabActiveColor = Color.white;
    [SerializeField] private Color tabInactiveColor = Color.gray;
    
    [Header("Content References")]
    [SerializeField] private ShopCategoryUI categoryUI;
    [SerializeField] private Transform itemListContainer;
    [SerializeField] private ShopItemSlot itemSlotPrefab;
    [SerializeField] private GameObject emptyListPanel;
    [SerializeField] private TextMeshProUGUI emptyListText;
    [SerializeField] private string emptyBuyMessage = "현재 구매 가능한 아이템이 없습니다";
    [SerializeField] private string emptySellMessage = "판매 가능한 아이템이 없습니다";
    
    [Header("Player Info")]
    [SerializeField] private TextMeshProUGUI playerGoldText;
    [SerializeField] private TextMeshProUGUI inventoryCapacityText;
    
    [Header("Transaction")]
    [SerializeField] private Button actionButton;
    [SerializeField] private TextMeshProUGUI actionButtonText;
    [SerializeField] private TransactionConfirmationUI confirmationUI;
    
    [Header("Settings")]
    [SerializeField] private bool enableAnimations = true;
    [SerializeField] private float animationDuration = 0.3f;
    [SerializeField] private bool showItemTooltips = true;
    
    [Header("Panel References")]
    [SerializeField] private GameObject buyPanel;
    [SerializeField] private GameObject sellPanel;
    
    [Header("Audio")]
    [SerializeField] private AudioClip openShopSound;
    [SerializeField] private AudioClip closeShopSound;
    [SerializeField] private AudioClip tabSwitchSound;
    
    // Current state
    private ShopData currentShop = null;
    private ShopTab currentTab = ShopTab.Buy;
    private ShopCategoryData selectedCategory = null;
    
    // Item slots
    private List<ShopItemSlot> itemSlots = new List<ShopItemSlot>();
    
    // Selection tracking
    private Dictionary<MaterialData, int> selectedBuyItems = new Dictionary<MaterialData, int>();
    private Dictionary<MaterialData, int> selectedSellItems = new Dictionary<MaterialData, int>();
    
    // Components
    private AudioSource audioSource;
    
    // Properties
    public bool IsOpen => shopPanel != null && shopPanel.activeInHierarchy;
    public ShopTab CurrentTab => currentTab;
    public ShopData CurrentShop => currentShop;
    
    void Awake()
    {
        // Get components
        audioSource = GetComponent<AudioSource>();
        
        // Validate references
        ValidateReferences();
        
        // Setup UI
        SetupUI();
    }
    
    void Start()
    {
        // Subscribe to events
        SubscribeToEvents();
        
        // Close shop initially
        CloseShop();
    }
    
    void Update()
    {
        // Update player gold display
        UpdatePlayerGoldDisplay();
        
        // Handle escape key to close shop
        if (IsOpen && Input.GetKeyDown(KeyCode.Escape))
        {
            CloseShop();
        }
    }
    
    // ===== Setup =====
    
    private void ValidateReferences()
    {
        if (shopPanel == null) Debug.LogError("ShopUI: shopPanel is not assigned!");
        if (itemSlotPrefab == null) Debug.LogError("ShopUI: itemSlotPrefab is not assigned!");
        if (confirmationUI == null) Debug.LogWarning("ShopUI: confirmationUI is not assigned!");
    }
    
    private void SetupUI()
    {
        // Setup buttons
        if (closeButton != null)
            closeButton.onClick.AddListener(CloseShop);
            
        if (buyTabButton != null)
            buyTabButton.onClick.AddListener(() => SwitchToTab(ShopTab.Buy));
            
        if (sellTabButton != null)
            sellTabButton.onClick.AddListener(() => SwitchToTab(ShopTab.Sell));
            
        if (actionButton != null)
            actionButton.onClick.AddListener(OnActionButtonClicked);
        
        // Setup category UI
        if (categoryUI != null)
        {
            categoryUI.OnCategorySelected += OnCategorySelected;
            categoryUI.OnShowAllSelected += OnShowAllCategoriesSelected;
        }
        
        // Initially show buy tab
        SwitchToTab(ShopTab.Buy);
    }
    
    // ===== Shop Management =====
    
    public void OpenShop(ShopData shopData)
    {
        if (shopData == null)
        {
            Debug.LogError("Cannot open shop: shopData is null");
            return;
        }
        
        currentShop = shopData;
        
        // Update shop info display
        UpdateShopInfo();
        
        // Update category filtering
        if (categoryUI != null)
        {
            categoryUI.SetupCategories(shopData.supportedCategories);
        }
        
        // Refresh current tab content
        RefreshCurrentTab();
        
        // Show UI
        shopPanel.SetActive(true);
        
        // Play sound
        PlaySound(openShopSound);
        
        Debug.Log($"Opened shop UI: {shopData.shopName}");
    }
    
    public void CloseShop()
    {
        if (!IsOpen) return;
        
        // Clear selections
        ClearSelections();
        
        // Close shop through manager
        if (ShopManager.instance != null && ShopManager.instance.IsShopOpen)
        {
            ShopManager.instance.CloseShop();
        }
        
        // Hide UI
        shopPanel.SetActive(false);
        
        // Play sound
        PlaySound(closeShopSound);
        
        currentShop = null;
        
        Debug.Log("Closed shop UI");
    }
    
    // ===== Tab Management =====
    
    public void SwitchToTab(ShopTab tab)
    {
        if (currentTab == tab) return;
        
        currentTab = tab;
        
        // Update tab visuals
        UpdateTabVisuals();
        
        // Show/hide panels
        if (buyPanel != null) buyPanel.SetActive(tab == ShopTab.Buy);
        if (sellPanel != null) sellPanel.SetActive(tab == ShopTab.Sell);
        
        // Refresh content
        RefreshCurrentTab();
        
        // Play sound
        PlaySound(tabSwitchSound);
        
        Debug.Log($"Switched to {tab} tab");
    }
    
    private void UpdateTabVisuals()
    {
        // Update tab button colors
        if (buyTabButton != null)
        {
            var buyColors = buyTabButton.colors;
            buyColors.normalColor = currentTab == ShopTab.Buy ? tabActiveColor : tabInactiveColor;
            buyTabButton.colors = buyColors;
        }
        
        if (sellTabButton != null)
        {
            var sellColors = sellTabButton.colors;
            sellColors.normalColor = currentTab == ShopTab.Sell ? tabActiveColor : tabInactiveColor;
            sellTabButton.colors = sellColors;
        }
        
        // Update action button text
        if (actionButtonText != null)
        {
            actionButtonText.text = currentTab == ShopTab.Buy ? "구매하기" : "판매하기";
        }
    }
    
    // ===== Content Management =====
    
    private void RefreshCurrentTab()
    {
        switch (currentTab)
        {
            case ShopTab.Buy:
                RefreshBuyItems();
                break;
            case ShopTab.Sell:
                RefreshSellItems();
                break;
        }
        
        UpdateTotalDisplays();
        UpdateEmptyListDisplay();
    }
    
    private void RefreshBuyItems()
    {
        if (currentShop == null || itemListContainer == null) return;
        
        // Clear existing slots
        ClearItemSlots();
        
        // Get available items
        var availableItems = ShopManager.instance?.GetShopBuyItems() ?? new List<ShopItemData>();
        
        // Filter by category if selected
        if (selectedCategory != null)
        {
            availableItems = availableItems.Where(item => 
                selectedCategory.ContainsItem(item.materialData)).ToList();
        }
        
        // Create item slots
        foreach (var shopItem in availableItems)
        {
            CreateBuyItemSlot(shopItem);
        }
        
        // Update empty display
        if (emptyListText != null)
        {
            emptyListText.text = emptyBuyMessage;
        }
        
        Debug.Log($"Refreshed buy items: {availableItems.Count} items");
    }
    
    private void RefreshSellItems()
    {
        if (currentShop == null || itemListContainer == null) return;
        
        // Clear existing slots
        ClearItemSlots();
        
        // Get sellable items
        var sellableItems = ShopManager.instance?.GetPlayerSellableItems() ?? new List<MaterialData>();
        
        // Filter by category if selected
        if (selectedCategory != null)
        {
            sellableItems = sellableItems.Where(item => 
                selectedCategory.ContainsItem(item)).ToList();
        }
        
        // Create item slots
        foreach (var item in sellableItems)
        {
            CreateSellItemSlot(item);
        }
        
        // Update empty display
        if (emptyListText != null)
        {
            emptyListText.text = emptySellMessage;
        }
        
        Debug.Log($"Refreshed sell items: {sellableItems.Count} items");
    }
    
    // ===== Item Slot Creation =====
    
    private void CreateBuyItemSlot(ShopItemData shopItem)
    {
        if (itemSlotPrefab == null || itemListContainer == null) return;
        
        var slotObj = Instantiate(itemSlotPrefab, itemListContainer);
        var slot = slotObj.GetComponent<ShopItemSlot>();
        
        if (slot != null)
        {
            slot.SetupForBuying(shopItem, currentShop);
            slot.OnQuantityChanged += OnBuyQuantityChanged;
            slot.OnTransactionRequested += OnBuyTransactionRequested;
            
            itemSlots.Add(slot);
        }
    }
    
    private void CreateSellItemSlot(MaterialData item)
    {
        if (itemSlotPrefab == null || itemListContainer == null) return;
        
        var slotObj = Instantiate(itemSlotPrefab, itemListContainer);
        var slot = slotObj.GetComponent<ShopItemSlot>();
        
        if (slot != null)
        {
            int playerQuantity = InventoryManager.instance?.GetItemQuantity(item) ?? 0;
            slot.SetupForSelling(item, currentShop, playerQuantity);
            slot.OnQuantityChanged += OnSellQuantityChanged;
            slot.OnTransactionRequested += OnSellTransactionRequested;
            
            itemSlots.Add(slot);
        }
    }
    
    private void ClearItemSlots()
    {
        foreach (var slot in itemSlots)
        {
            if (slot != null)
            {
                slot.OnQuantityChanged -= OnBuyQuantityChanged;
                slot.OnQuantityChanged -= OnSellQuantityChanged;
                slot.OnTransactionRequested -= OnBuyTransactionRequested;
                slot.OnTransactionRequested -= OnSellTransactionRequested;
                
                Destroy(slot.gameObject);
            }
        }
        
        itemSlots.Clear();
    }
    
    // ===== Item Selection =====
    
    private void OnBuyQuantityChanged(MaterialData item, int quantity)
    {
        if (quantity > 0)
        {
            selectedBuyItems[item] = quantity;
        }
        else
        {
            selectedBuyItems.Remove(item);
        }
        
        UpdateTotalDisplays();
    }
    
    private void OnSellQuantityChanged(MaterialData item, int quantity)
    {
        if (quantity > 0)
        {
            selectedSellItems[item] = quantity;
        }
        else
        {
            selectedSellItems.Remove(item);
        }
        
        UpdateTotalDisplays();
    }
    
    // ===== Transaction Handling =====
    
    private void OnBuyTransactionRequested(MaterialData item, int quantity)
    {
        if (confirmationUI != null)
        {
            var shopItem = GetShopItemData(item);
            if (shopItem != null)
            {
                int totalCost = ShopManager.instance.GetBuyPrice(shopItem, quantity);
                confirmationUI.ShowBuyConfirmation(item, quantity, totalCost, 
                    () => ExecuteBuyTransaction(shopItem, quantity));
            }
        }
        else
        {
            var shopItem = GetShopItemData(item);
            if (shopItem != null)
            {
                ExecuteBuyTransaction(shopItem, quantity);
            }
        }
    }
    
    private void OnSellTransactionRequested(MaterialData item, int quantity)
    {
        if (confirmationUI != null)
        {
            int totalPrice = ShopManager.instance.GetSellPrice(item, quantity);
            confirmationUI.ShowSellConfirmation(item, quantity, totalPrice,
                () => ExecuteSellTransaction(item, quantity));
        }
        else
        {
            ExecuteSellTransaction(item, quantity);
        }
    }
    
    private void ExecuteBuyTransaction(ShopItemData shopItem, int quantity)
    {
        if (TransactionManager.instance != null)
        {
            bool success = TransactionManager.instance.BuyItem(shopItem, quantity);
            
            if (success)
            {
                RefreshCurrentTab();
                ClearSelections();
            }
        }
    }
    
    private void ExecuteSellTransaction(MaterialData item, int quantity)
    {
        if (TransactionManager.instance != null)
        {
            bool success = TransactionManager.instance.SellItem(item, quantity);
            
            if (success)
            {
                RefreshCurrentTab();
                ClearSelections();
            }
        }
    }
    
    // ===== Action Button =====
    
    private void OnActionButtonClicked()
    {
        if (currentTab == ShopTab.Buy)
        {
            OnBuySelectedItems();
        }
        else
        {
            OnSellSelectedItems();
        }
    }
    
    private void OnBuySelectedItems()
    {
        if (selectedBuyItems.Count == 0)
        {
            if (UIManager.instance != null)
            {
                UIManager.instance.ShowNotification("구매할 아이템을 선택해주세요.", 2f);
            }
            return;
        }
        
        // Process each selected item
        foreach (var kvp in selectedBuyItems)
        {
            var shopItem = GetShopItemData(kvp.Key);
            if (shopItem != null)
            {
                ExecuteBuyTransaction(shopItem, kvp.Value);
            }
        }
    }
    
    private void OnSellSelectedItems()
    {
        if (selectedSellItems.Count == 0)
        {
            if (UIManager.instance != null)
            {
                UIManager.instance.ShowNotification("판매할 아이템을 선택해주세요.", 2f);
            }
            return;
        }
        
        // Process each selected item
        foreach (var kvp in selectedSellItems)
        {
            ExecuteSellTransaction(kvp.Key, kvp.Value);
        }
    }
    
    // ===== Bulk Operations =====
    
    private void OnSellAllClicked()
    {
        var sellableItems = new List<ItemQuantityPair>();
        
        foreach (var slot in itemSlots)
        {
            if (slot != null && slot.MaterialData != null)
            {
                int playerQuantity = InventoryManager.instance?.GetItemQuantity(slot.MaterialData) ?? 0;
                if (playerQuantity > 0)
                {
                    sellableItems.Add(new ItemQuantityPair(slot.MaterialData, playerQuantity));
                }
            }
        }
        
        if (sellableItems.Count == 0)
        {
            if (UIManager.instance != null)
            {
                UIManager.instance.ShowNotification("판매할 아이템이 없습니다.", 2f);
            }
            return;
        }
        
        if (confirmationUI != null)
        {
            int totalValue = PriceCalculator.CalculateTotalSellValue(sellableItems);
            confirmationUI.ShowBulkSellConfirmation(sellableItems, totalValue,
                () => ExecuteBulkSellTransaction(sellableItems));
        }
        else
        {
            ExecuteBulkSellTransaction(sellableItems);
        }
    }
    
    private void ExecuteBulkSellTransaction(List<ItemQuantityPair> items)
    {
        if (TransactionManager.instance != null)
        {
            bool success = TransactionManager.instance.SellMultipleItems(items);
            
            if (success)
            {
                RefreshCurrentTab();
                ClearSelections();
            }
        }
    }
    
    // ===== Category Filtering =====
    
    private void OnCategorySelected(ShopCategoryData category)
    {
        selectedCategory = category;
        RefreshCurrentTab();
        
        Debug.Log($"Selected category: {category?.categoryName ?? "None"}");
    }
    
    private void OnShowAllCategoriesSelected()
    {
        selectedCategory = null;
        RefreshCurrentTab();
        
        Debug.Log("Showing all categories");
    }
    
    // ===== UI Updates =====
    
    private void UpdateShopInfo()
    {
        if (currentShop == null) return;
        
        if (shopNameText != null)
            shopNameText.text = currentShop.shopName;
            
        if (shopKeeperText != null)
            shopKeeperText.text = currentShop.shopKeeperName;
    }
    
    private void UpdatePlayerGoldDisplay()
    {
        if (playerGoldText != null && GoldManager.instance != null)
        {
            playerGoldText.text = $"골드: {GoldManager.instance.GetGold():N0}";
        }
        
        if (inventoryCapacityText != null && InventoryManager.instance != null)
        {
            // TODO: Update when inventory has capacity system
            inventoryCapacityText.text = "";
        }
    }
    
    private void UpdateTotalDisplays()
    {
        // Update totals based on selected items
        int total = currentTab == ShopTab.Buy ? CalculateBuyTotal() : CalculateSellTotal();
        
        if (actionButtonText != null)
        {
            string prefix = currentTab == ShopTab.Buy ? "구매" : "판매";
            if (total > 0)
            {
                actionButtonText.text = $"{prefix}하기 ({total:N0} 골드)";
            }
            else
            {
                actionButtonText.text = $"{prefix}하기";
            }
        }
    }
    
    private void UpdateEmptyListDisplay()
    {
        bool hasItems = itemSlots.Count > 0;
        
        if (emptyListPanel != null)
        {
            emptyListPanel.SetActive(!hasItems);
        }
        
        if (actionButton != null)
        {
            actionButton.interactable = hasItems;
        }
    }
    
    private int CalculateBuyTotal()
    {
        int total = 0;
        
        foreach (var kvp in selectedBuyItems)
        {
            var shopItem = GetShopItemData(kvp.Key);
            if (shopItem != null)
            {
                total += ShopManager.instance.GetBuyPrice(shopItem, kvp.Value);
            }
        }
        
        return total;
    }
    
    private int CalculateSellTotal()
    {
        int total = 0;
        
        foreach (var kvp in selectedSellItems)
        {
            total += ShopManager.instance.GetSellPrice(kvp.Key, kvp.Value);
        }
        
        return total;
    }
    
    // ===== Utility =====
    
    private ShopItemData GetShopItemData(MaterialData materialData)
    {
        var buyItems = ShopManager.instance?.GetShopBuyItems();
        return buyItems?.FirstOrDefault(item => item.materialData == materialData);
    }
    
    private void ClearSelections()
    {
        selectedBuyItems.Clear();
        selectedSellItems.Clear();
        UpdateTotalDisplays();
    }
    
    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }
    
    // ===== Event Management =====
    
    private void SubscribeToEvents()
    {
        ShopManager.OnShopOpened += OnShopManagerOpened;
        ShopManager.OnShopClosed += OnShopManagerClosed;
        ShopManager.OnShopInventoryRefreshed += OnShopInventoryRefreshed;
        
        TransactionManager.OnItemBought += OnTransactionCompleted;
        TransactionManager.OnItemSold += OnTransactionCompleted;
        TransactionManager.OnTransactionFailed += OnTransactionFailed;
    }
    
    private void UnsubscribeFromEvents()
    {
        ShopManager.OnShopOpened -= OnShopManagerOpened;
        ShopManager.OnShopClosed -= OnShopManagerClosed;
        ShopManager.OnShopInventoryRefreshed -= OnShopInventoryRefreshed;
        
        TransactionManager.OnItemBought -= OnTransactionCompleted;
        TransactionManager.OnItemSold -= OnTransactionCompleted;
        TransactionManager.OnTransactionFailed -= OnTransactionFailed;
    }
    
    private void OnShopManagerOpened(ShopData shopData)
    {
        if (!IsOpen)
        {
            OpenShop(shopData);
        }
    }
    
    private void OnShopManagerClosed(ShopData shopData)
    {
        if (IsOpen && currentShop == shopData)
        {
            CloseShop();
        }
    }
    
    private void OnShopInventoryRefreshed(ShopData shopData)
    {
        if (IsOpen && currentShop == shopData)
        {
            RefreshCurrentTab();
        }
    }
    
    private void OnTransactionCompleted(MaterialData item, int quantity, int price)
    {
        // Refresh the current tab to show updated quantities
        RefreshCurrentTab();
    }
    
    private void OnTransactionFailed(string error)
    {
        if (UIManager.instance != null)
        {
            UIManager.instance.ShowNotification($"거래 실패: {error}", 3f);
        }
    }
    
    // ===== Public Interface =====
    
    public void SetTab(ShopTab tab)
    {
        SwitchToTab(tab);
    }
    
    public void RefreshUI()
    {
        RefreshCurrentTab();
        UpdateTotalDisplays();
    }
    
    // ===== Debug Methods =====
    
    [ContextMenu("Debug - Refresh Current Tab")]
    public void DebugRefreshCurrentTab()
    {
        RefreshCurrentTab();
    }
    
    [ContextMenu("Debug - Clear Selections")]
    public void DebugClearSelections()
    {
        ClearSelections();
    }
    
    // ===== Cleanup =====
    
    void OnDestroy()
    {
        UnsubscribeFromEvents();
        
        // Clean up item slots
        ClearItemSlots();
    }
}

// Shop tab enum
public enum ShopTab
{
    Buy,
    Sell
}