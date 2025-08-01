using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopItemSlot : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image itemIcon;
    [SerializeField] private TextMeshProUGUI itemNameText;
    [SerializeField] private TextMeshProUGUI itemDescriptionText;
    [SerializeField] private TextMeshProUGUI priceText;
    [SerializeField] private TextMeshProUGUI quantityText;
    [SerializeField] private TextMeshProUGUI stockText;
    
    [Header("Quantity Controls")]
    [SerializeField] private Button decreaseButton;
    [SerializeField] private Button increaseButton;
    [SerializeField] private Button maxButton; // Set to max available
    [SerializeField] private TMP_InputField quantityInput;
    [SerializeField] private Slider quantitySlider;
    
    [Header("Transaction")]
    [SerializeField] private Button transactionButton;
    [SerializeField] private TextMeshProUGUI transactionButtonText;
    
    [Header("Visual States")]
    [SerializeField] private GameObject unavailableOverlay;
    [SerializeField] private Color availableColor = Color.white;
    [SerializeField] private Color unavailableColor = Color.gray;
    [SerializeField] private Color outOfStockColor = Color.red;
    
    [Header("Price Display")]
    [SerializeField] private GameObject bulkPricePanel;
    [SerializeField] private TextMeshProUGUI bulkBonusText;
    [SerializeField] private TextMeshProUGUI pricePerUnitText;
    
    // Data
    private MaterialData materialData;
    private ShopItemData shopItemData; // For buying
    private ShopData currentShop;
    private bool isBuyMode = true;
    private int currentQuantity = 0;
    private int maxAvailableQuantity = 0;
    
    // Events
    public System.Action<MaterialData, int> OnQuantityChanged;
    public System.Action<MaterialData, int> OnTransactionRequested;
    
    // Properties
    public MaterialData MaterialData => materialData;
    public int CurrentQuantity => currentQuantity;
    public bool IsBuyMode => isBuyMode;
    public int MaxAvailableQuantity => maxAvailableQuantity;
    
    void Awake()
    {
        SetupUI();
    }
    
    private void SetupUI()
    {
        // Setup quantity controls
        if (decreaseButton != null)
            decreaseButton.onClick.AddListener(() => ChangeQuantity(-1));
            
        if (increaseButton != null)
            increaseButton.onClick.AddListener(() => ChangeQuantity(1));
            
        if (maxButton != null)
            maxButton.onClick.AddListener(SetToMaxQuantity);
            
        if (transactionButton != null)
            transactionButton.onClick.AddListener(RequestTransaction);
        
        // Setup input field
        if (quantityInput != null)
        {
            quantityInput.onValueChanged.AddListener(OnQuantityInputChanged);
            quantityInput.contentType = TMP_InputField.ContentType.IntegerNumber;
        }
        
        // Setup slider
        if (quantitySlider != null)
        {
            quantitySlider.onValueChanged.AddListener(OnQuantitySliderChanged);
            quantitySlider.wholeNumbers = true;
        }
    }
    
    // ===== Setup Methods =====
    
    public void SetupForBuying(ShopItemData shopItem, ShopData shop)
    {
        shopItemData = shopItem;
        materialData = shopItem.materialData;
        currentShop = shop;
        isBuyMode = true;
        
        // Set max quantity
        if (shopItem.hasUnlimitedStock)
        {
            // Limit by player's gold
            int maxAffordable = CalculateMaxAffordableQuantity();
            maxAvailableQuantity = maxAffordable;
        }
        else
        {
            // Limit by both stock and gold
            int maxAffordable = CalculateMaxAffordableQuantity();
            maxAvailableQuantity = Mathf.Min(shopItem.CurrentStock, maxAffordable);
        }
        
        SetupUI();
        UpdateDisplay();
        
        Debug.Log($"Setup buy slot for {materialData.materialName} (max: {maxAvailableQuantity})");
    }
    
    public void SetupForSelling(MaterialData item, ShopData shop, int playerQuantity)
    {
        materialData = item;
        shopItemData = null;
        currentShop = shop;
        isBuyMode = false;
        maxAvailableQuantity = playerQuantity;
        
        SetupUI();
        UpdateDisplay();
        
        Debug.Log($"Setup sell slot for {materialData.materialName} (player has: {playerQuantity})");
    }
    
    // ===== Display Updates =====
    
    private void UpdateDisplay()
    {
        if (materialData == null) return;
        
        // Update item info
        UpdateItemInfo();
        
        // Update price info
        UpdatePriceInfo();
        
        // Update quantity info
        UpdateQuantityInfo();
        
        // Update availability state
        UpdateAvailabilityState();
        
        // Update transaction button
        UpdateTransactionButton();
        
        // Update bulk pricing
        UpdateBulkPricing();
    }
    
    private void UpdateItemInfo()
    {
        // Item icon
        if (itemIcon != null && materialData.materialIcon != null)
        {
            itemIcon.sprite = materialData.materialIcon;
            itemIcon.color = Color.white;
        }
        
        // Item name
        if (itemNameText != null)
        {
            itemNameText.text = materialData.materialName;
        }
        
        // Item description
        if (itemDescriptionText != null)
        {
            itemDescriptionText.text = materialData.materialDescription;
        }
    }
    
    private void UpdatePriceInfo()
    {
        if (priceText == null) return;
        
        if (currentQuantity <= 0)
        {
            priceText.text = isBuyMode ? "구매 가격: -" : "판매 가격: -";
            return;
        }
        
        int totalPrice = 0;
        int pricePerUnit = 0;
        
        if (isBuyMode && shopItemData != null)
        {
            totalPrice = ShopManager.instance.GetBuyPrice(shopItemData, currentQuantity);
            pricePerUnit = totalPrice / currentQuantity;
        }
        else if (!isBuyMode)
        {
            totalPrice = ShopManager.instance.GetSellPrice(materialData, currentQuantity);
            pricePerUnit = totalPrice / currentQuantity;
        }
        
        priceText.text = $"{(isBuyMode ? "구매" : "판매")} 가격: {totalPrice:N0}골드";
        
        // Update price per unit
        if (pricePerUnitText != null)
        {
            pricePerUnitText.text = $"개당: {pricePerUnit:N0}골드";
        }
    }
    
    private void UpdateQuantityInfo()
    {
        // Update quantity text
        if (quantityText != null)
        {
            quantityText.text = currentQuantity.ToString();
        }
        
        // Update stock text
        if (stockText != null)
        {
            if (isBuyMode && shopItemData != null)
            {
                stockText.text = shopItemData.GetStockStatusText();
            }
            else if (!isBuyMode)
            {
                stockText.text = $"보유: {maxAvailableQuantity}개";
            }
        }
        
        // Update input field
        if (quantityInput != null && !quantityInput.isFocused)
        {
            quantityInput.text = currentQuantity.ToString();
        }
        
        // Update slider
        if (quantitySlider != null)
        {
            quantitySlider.maxValue = maxAvailableQuantity;
            quantitySlider.value = currentQuantity;
        }
    }
    
    private void UpdateAvailabilityState()
    {
        bool isAvailable = IsAvailable();
        bool hasStock = maxAvailableQuantity > 0;
        
        // Update visual state
        Color targetColor = availableColor;
        if (!isAvailable)
            targetColor = unavailableColor;
        else if (!hasStock)
            targetColor = outOfStockColor;
        
        // Apply color to item icon
        if (itemIcon != null)
        {
            itemIcon.color = targetColor;
        }
        
        // Show/hide unavailable overlay
        if (unavailableOverlay != null)
        {
            unavailableOverlay.SetActive(!isAvailable || !hasStock);
        }
        
        // Enable/disable quantity controls
        SetControlsInteractable(isAvailable && hasStock);
    }
    
    private void UpdateTransactionButton()
    {
        if (transactionButton == null) return;
        
        bool canTransact = CanTransact();
        transactionButton.interactable = canTransact;
        
        // Update button text
        if (transactionButtonText != null)
        {
            if (isBuyMode)
            {
                transactionButtonText.text = currentQuantity > 0 ? "구매" : "선택";
            }
            else
            {
                transactionButtonText.text = currentQuantity > 0 ? "판매" : "선택";
            }
        }
    }
    
    private void UpdateBulkPricing()
    {
        if (bulkPricePanel == null || bulkBonusText == null) return;
        
        // Show bulk bonus for selling
        if (!isBuyMode && currentQuantity > 0)
        {
            float bonusPercent = PriceCalculator.GetBulkBonusPercentage(currentQuantity);
            
            if (bonusPercent > 0)
            {
                bulkPricePanel.SetActive(true);
                bulkBonusText.text = $"대량 판매 보너스: +{bonusPercent:F0}%";
            }
            else
            {
                bulkPricePanel.SetActive(false);
            }
        }
        else
        {
            bulkPricePanel.SetActive(false);
        }
    }
    
    // ===== Quantity Management =====
    
    public void SetQuantity(int quantity)
    {
        int newQuantity = Mathf.Clamp(quantity, 0, maxAvailableQuantity);
        
        if (newQuantity != currentQuantity)
        {
            currentQuantity = newQuantity;
            UpdateDisplay();
            
            // Fire event
            OnQuantityChanged?.Invoke(materialData, currentQuantity);
        }
    }
    
    public void ChangeQuantity(int delta)
    {
        SetQuantity(currentQuantity + delta);
    }
    
    public void SetToMaxQuantity()
    {
        SetQuantity(maxAvailableQuantity);
    }
    
    private void OnQuantityInputChanged(string value)
    {
        if (int.TryParse(value, out int quantity))
        {
            SetQuantity(quantity);
        }
    }
    
    private void OnQuantitySliderChanged(float value)
    {
        SetQuantity(Mathf.RoundToInt(value));
    }
    
    // ===== Transaction =====
    
    private void RequestTransaction()
    {
        if (!CanTransact()) return;
        
        OnTransactionRequested?.Invoke(materialData, currentQuantity);
    }
    
    // ===== Utility Methods =====
    
    private bool IsAvailable()
    {
        if (materialData == null || currentShop == null) return false;
        
        if (isBuyMode)
        {
            return shopItemData != null && 
                   currentShop.CanShopSellItem(shopItemData) &&
                   shopItemData.IsInStock;
        }
        else
        {
            return currentShop.CanShopBuyItem(materialData);
        }
    }
    
    private bool CanTransact()
    {
        if (!IsAvailable() || currentQuantity <= 0) return false;
        
        if (isBuyMode)
        {
            // Check if player has enough gold
            int totalCost = ShopManager.instance.GetBuyPrice(shopItemData, currentQuantity);
            return GoldManager.instance != null && GoldManager.instance.HasEnoughGold(totalCost);
        }
        else
        {
            // Check if player has enough items
            int playerQuantity = InventoryManager.instance?.GetItemQuantity(materialData) ?? 0;
            return playerQuantity >= currentQuantity;
        }
    }
    
    private int CalculateMaxAffordableQuantity()
    {
        if (isBuyMode && shopItemData != null && GoldManager.instance != null)
        {
            int playerGold = GoldManager.instance.GetGold();
            int pricePerUnit = ShopManager.instance.GetBuyPrice(shopItemData, 1);
            
            if (pricePerUnit <= 0) return 0;
            
            return playerGold / pricePerUnit;
        }
        
        return 0;
    }
    
    private void SetControlsInteractable(bool interactable)
    {
        if (decreaseButton != null) decreaseButton.interactable = interactable;
        if (increaseButton != null) increaseButton.interactable = interactable;
        if (maxButton != null) maxButton.interactable = interactable;
        if (quantityInput != null) quantityInput.interactable = interactable;
        if (quantitySlider != null) quantitySlider.interactable = interactable;
    }
    
    // ===== Public Interface =====
    
    public void RefreshSlot()
    {
        // Recalculate max available quantity
        if (isBuyMode && shopItemData != null)
        {
            int maxAffordable = CalculateMaxAffordableQuantity();
            maxAvailableQuantity = shopItemData.hasUnlimitedStock ? 
                maxAffordable : 
                Mathf.Min(shopItemData.CurrentStock, maxAffordable);
        }
        else if (!isBuyMode)
        {
            maxAvailableQuantity = InventoryManager.instance?.GetItemQuantity(materialData) ?? 0;
        }
        
        // Clamp current quantity to new max
        if (currentQuantity > maxAvailableQuantity)
        {
            SetQuantity(maxAvailableQuantity);
        }
        else
        {
            UpdateDisplay();
        }
    }
    
    public void ResetQuantity()
    {
        SetQuantity(0);
    }
    
    // ===== Debug Methods =====
    
    [ContextMenu("Debug - Set Max Quantity")]
    public void DebugSetMaxQuantity()
    {
        SetToMaxQuantity();
    }
    
    [ContextMenu("Debug - Reset Quantity")]
    public void DebugResetQuantity()
    {
        ResetQuantity();
    }
    
    [ContextMenu("Debug - Print Slot Info")]
    public void DebugPrintSlotInfo()
    {
        Debug.Log($"=== Shop Item Slot Info ===\n" +
                 $"Item: {materialData?.materialName ?? "None"}\n" +
                 $"Mode: {(isBuyMode ? "Buy" : "Sell")}\n" +
                 $"Current Quantity: {currentQuantity}\n" +
                 $"Max Available: {maxAvailableQuantity}\n" +
                 $"Can Transact: {CanTransact()}\n" +
                 $"Is Available: {IsAvailable()}");
    }
}