using UnityEngine;
using System.Collections.Generic;
public class UIManager : MonoBehaviour
{
    // 1. 싱글톤 패턴을 위한 instance 변수
    public static UIManager instance = null;

    // 2. 제어할 View(InventoryUI)를 연결할 변수
    public InventoryUI inventoryUI;

    // 3. 인벤토리 창이 현재 열려있는지 상태를 기억할 변수
    private bool isInventoryOpen = false;

    void Awake()
    {
        // 싱글톤 인스턴스 설정
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    // 4. InventoryUI가 호출할 토글 기능 함수
    public void ToggleInventoryUI()
    {
        // 5. 현재 상태를 반대로 뒤집는다 (false -> true, true -> false)
        isInventoryOpen = !isInventoryOpen;

        // 6. 뒤집힌 상태에 따라 View에게 열거나 닫으라고 명령한다.
        if (isInventoryOpen)
        {
            inventoryUI.Open();

            List<MaterialData> items = InventoryManager.instance.GetInventoryItems();

            // (임시) 받아온 아이템 개수를 콘솔에 출력하여 확인
            Debug.Log("인벤토리 아이템 " + items.Count + "개를 받아왔습니다.");

                        inventoryUI.UpdateDisplay(items);

        }
        else
        {
            inventoryUI.Close();
        }
    }
}