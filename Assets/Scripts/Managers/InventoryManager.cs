using System;
using System.Collections.Generic;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    // 1. 자기 자신과 똑같은 타입의 static 변수를 만든다. 이 변수가 유일한 '총사령관'을 담을 공간이다.
    public static InventoryManager instance = null;

    // Awake()는 Start()보다 먼저 호출되는 유니티 생명주기 함수이다.
    void Awake()
    {
        // 2. 만약 아직 총사령관(instance)이 임명되지 않았다면,
        if (instance == null)
        {
            // 자기 자신을 총사령관으로 임명한다.
            instance = this;
        }
        // 3. 만약 총사령관이 이미 존재하는데 또 다른 자신(가짜)이 생성되려고 한다면,
        else if (instance != this)
        {
            // 가짜인 자신을 파괴한다.
            Destroy(gameObject);
        }
    }

    public List<MaterialData> inventory = new List<MaterialData>();

    public void AddItem(MaterialData item)
    {
        inventory.Add(item);
        Debug.Log(instance.name + ": " + item.materialName + "을(를) 획득했습니다!");
    }

        public List<MaterialData> GetInventoryItems()
    {
        return inventory;
    }

    internal static void AddItem(object itemToAdd)
    {
        throw new NotImplementedException();
    }
}