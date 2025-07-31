using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "WorkstationData", menuName = "Game Data/Workstation/Workstation Data")]
public class WorkstationData : ScriptableObject
{
    [Header("Basic Info")]
    public string workstationName;
    public string description;
    public Sprite icon;
    
    [Header("Workstation Settings")]
    public WorkstationType workstationType;
    public float defaultProcessTime = 5f; // 기본 처리 시간 (초)
    
    [Header("Durability")]
    public bool hasDurability = true;
    public int maxDurability = 100;
    public int durabilityLossPerUse = 5;
    
    [Header("Visual & Audio")]
    public GameObject processingEffect; // 작동 중 이펙트
    public AudioClip startSound;
    public AudioClip processingSound;
    public AudioClip completeSound;
    
    [Header("UI")]
    public string idlePrompt = "상호작용하기";
    public string processingPrompt = "처리 중...";
    public string completePrompt = "완료! 수거하기";
    public string brokenPrompt = "수리가 필요합니다";
}

public enum WorkstationType
{
    Decomposer,     // 분해기
    CraftingTable,  // 제작대
    Furnace,        // 용광로
    CookingStation  // 요리대
}