using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager instance = null;
    public static event System.Action OnInventoryChanged;
    
    // Dictionary로 아이템ID와 수량 관리
    private Dictionary<int, int> itemQuantities = new Dictionary<int, int>();
    // 빠른 접근을 위한 MaterialData 캐시
    private Dictionary<int, MaterialData> itemDataCache = new Dictionary<int, MaterialData>();
    
    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }
    
    public void AddItem(MaterialData item, int amount = 1)
    {
        if (item == null) return;
        
        // 아이템이 이미 있으면 수량 증가
        if (itemQuantities.ContainsKey(item.materialID))
        {
            itemQuantities[item.materialID] += amount;
            Debug.Log($"{item.materialName} {amount}개 추가 (총 {itemQuantities[item.materialID]}개)");
        }
        else
        {
            // 새 아이템 추가
            itemQuantities[item.materialID] = amount;
            itemDataCache[item.materialID] = item;
            Debug.Log($"{item.materialName} {amount}개 획득!");
        }
        
        OnInventoryChanged?.Invoke();
    }
    
    public bool RemoveItem(MaterialData item, int amount = 1)
    {
        if (item == null || !itemQuantities.ContainsKey(item.materialID))
            return false;
        
        if (itemQuantities[item.materialID] >= amount)
        {
            itemQuantities[item.materialID] -= amount;
            
            // 수량이 0이 되면 완전히 제거
            if (itemQuantities[item.materialID] == 0)
            {
                itemQuantities.Remove(item.materialID);
                itemDataCache.Remove(item.materialID);
            }
            
            OnInventoryChanged?.Invoke();
            return true;
        }
        
        Debug.Log($"{item.materialName}이(가) 부족합니다!");
        return false;
    }
    
    // UI를 위한 Dictionary 반환 (MaterialData와 수량)
    public Dictionary<MaterialData, int> GetInventoryItems()
    {
        var result = new Dictionary<MaterialData, int>();
        
        foreach (var kvp in itemQuantities)
        {
            if (itemDataCache.ContainsKey(kvp.Key))
            {
                result[itemDataCache[kvp.Key]] = kvp.Value;
            }
        }
        
        return result;
    }
    
    // 특정 아이템의 수량 확인
    public int GetItemQuantity(MaterialData item)
    {
        if (item == null || !itemQuantities.ContainsKey(item.materialID))
            return 0;
            
        return itemQuantities[item.materialID];
    }
}