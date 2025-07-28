using System.Collections.Generic;
using UnityEngine;



public class InventoryUI : MonoBehaviour
{
    // 1. 유니티 에디터에서 실제 인벤토리 UI 패널을 연결할 변수
    public GameObject inventoryPanel;

    public Transform contentTransform; // 슬롯들이 생성될 부모 위치 (Content 오브젝트)
    public GameObject slotPrefab;      // 아이템 슬롯 원본 (SlotPrefab 프리팹)

    void Update()
    {
        // 2. 매 프레임 'I' 키가 눌렸는지 확인한다.
        if (Input.GetKeyDown(KeyCode.I))
        {

            UIManager.instance.ToggleInventoryUI();
        }
    }

    // 4. Presenter(UIManager)가 호출할 수 있는 창 열기 기능
    public void Open()
    {
        inventoryPanel.SetActive(true);
    }

    // 5. Presenter(UIManager)가 호출할 수 있는 창 닫기 기능
    public void Close()
    {
        inventoryPanel.SetActive(false);
    }

    public void UpdateDisplay(List<MaterialData> items)
    {
        // 1. 기존 슬롯들 삭제
        foreach (Transform child in contentTransform)
        {
            Destroy(child.gameObject);
        }

        // 2. 아이템 개수를 세기 위한 Dictionary 생성 (MaterialData를 Key로 사용)
        var itemSummary = new Dictionary<MaterialData, int>();
        foreach (var itemData in items)
        {
            // 이미 요약 목록에 아이템이 있으면 개수만 1 증가
            if (itemSummary.ContainsKey(itemData))
            {
                itemSummary[itemData]++;
            }
            // 목록에 없는 새로운 아이템이면 새로 추가하고 개수는 1로 설정
            else
            {
                itemSummary.Add(itemData, 1);
            }
        }

        // 3. 요약된 아이템 목록을 바탕으로 실제 UI 슬롯을 생성하고 데이터 연결
       foreach (var summaryEntry in itemSummary)
{
    GameObject newSlot = Instantiate(slotPrefab, contentTransform);
    
    // InventorySlot 컴포넌트 가져오기
    InventorySlot slot = newSlot.GetComponent<InventorySlot>();
    if (slot != null)
    {
        slot.SetItem(summaryEntry.Key, summaryEntry.Value);
    }
}
    }
}