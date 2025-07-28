// 새 스크립트: InventorySlot.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;  // TextMeshPro 네임스페이스

public class InventorySlot : MonoBehaviour
{
    public Image itemIcon;      // 인스펙터에서 직접 연결
    public TextMeshProUGUI  itemCountText;  // 인스펙터에서 직접 연결
    
    public void SetItem(MaterialData itemData, int count)
    {
        if (itemIcon != null && itemData.materialIcon != null)
        {
            itemIcon.sprite = itemData.materialIcon;
            itemIcon.enabled = true; // 아이콘 활성화
        }
        
        if (itemCountText != null)
        {
            itemCountText.text = itemData.materialName + " x " + count;
        }
    }
}