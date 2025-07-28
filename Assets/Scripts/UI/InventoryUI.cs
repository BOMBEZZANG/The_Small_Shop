using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class InventoryUI : MonoBehaviour
{
    public GameObject inventoryPanel;
    public Transform contentTransform;
    public GameObject slotPrefab;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.I))
        {
            UIManager.instance.ToggleInventoryUI();
        }
    }

    public void Open()
    {
        inventoryPanel.SetActive(true);
    }

    public void Close()
    {
        inventoryPanel.SetActive(false);
    }

    // Dictionary<MaterialData, int>를 받아서 표시
    public void UpdateDisplay(Dictionary<MaterialData, int> items)
    {
        // 기존 슬롯들 삭제
        foreach (Transform child in contentTransform)
        {
            Destroy(child.gameObject);
        }

        // Dictionary를 순회하며 슬롯 생성
        foreach (var item in items)
        {
            GameObject newSlot = Instantiate(slotPrefab, contentTransform);
            InventorySlot slot = newSlot.GetComponent<InventorySlot>();
            
            if (slot != null)
            {
                // MaterialData와 수량 전달
                slot.SetItem(item.Key, item.Value);
            }
        }
    }
}