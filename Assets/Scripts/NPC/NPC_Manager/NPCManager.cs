using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class NPCManager : MonoBehaviour
{
    // 싱글톤
    public static NPCManager instance = null;
    
    [Header("NPC Management")]
    [SerializeField] private List<NPCController> allNPCs = new List<NPCController>();
    [SerializeField] private Dictionary<int, NPCController> npcDictionary = new Dictionary<int, NPCController>();
    
    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = true;
    
    void Awake()
    {
        // 싱글톤 설정
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
    }
    
    void Start()
    {
        // 씬의 모든 NPC 찾기
        RefreshNPCList();
        
        // 이벤트 구독
        NPCController.OnNPCStateChanged += OnNPCStateChanged;
    }
    
    void OnDestroy()
    {
        // 이벤트 구독 해제
        NPCController.OnNPCStateChanged -= OnNPCStateChanged;
    }
    
    // ===== NPC 목록 관리 =====
    public void RefreshNPCList()
    {
        allNPCs.Clear();
        npcDictionary.Clear();
        
        // 씬의 모든 NPC 찾기
        NPCController[] npcs = FindObjectsOfType<NPCController>();
        
        foreach (var npc in npcs)
        {
            RegisterNPC(npc);
        }
        
        Debug.Log($"NPCManager: {allNPCs.Count}명의 NPC를 찾았습니다.");
    }
    
    // ===== NPC 등록/해제 =====
    public void RegisterNPC(NPCController npc)
    {
        if (npc == null || allNPCs.Contains(npc)) return;
        
        allNPCs.Add(npc);
        
        // ID로도 관리
        if (npc.GetNPCData() != null)
        {
            int npcID = npc.GetNPCData().npcID;
            if (!npcDictionary.ContainsKey(npcID))
            {
                npcDictionary[npcID] = npc;
            }
            else
            {
                Debug.LogWarning($"NPCManager: 중복된 NPC ID {npcID}!");
            }
        }
    }
    
    public void UnregisterNPC(NPCController npc)
    {
        if (npc == null) return;
        
        allNPCs.Remove(npc);
        
        if (npc.GetNPCData() != null)
        {
            npcDictionary.Remove(npc.GetNPCData().npcID);
        }
    }
    
    // ===== NPC 찾기 =====
    public NPCController GetNPCByID(int npcID)
    {
        return npcDictionary.ContainsKey(npcID) ? npcDictionary[npcID] : null;
    }
    
    public NPCController GetNPCByName(string npcName)
    {
        return allNPCs.FirstOrDefault(npc => 
            npc.GetNPCData() != null && 
            npc.GetNPCData().npcName == npcName
        );
    }
    
    public List<NPCController> GetNPCsByType(NPCType type)
    {
        return allNPCs.Where(npc => 
            npc.GetNPCData() != null && 
            npc.GetNPCData().npcType == type
        ).ToList();
    }
    
    public List<NPCController> GetNearbyNPCs(Vector3 position, float radius)
    {
        return allNPCs.Where(npc => 
            Vector3.Distance(npc.transform.position, position) <= radius
        ).ToList();
    }
    
    // ===== NPC 상태 관리 =====
    public void SetAllNPCsState(NPCState state)
    {
        foreach (var npc in allNPCs)
        {
            npc.ChangeState(state);
        }
    }
    
    public void DisableAllNPCs()
    {
        SetAllNPCsState(NPCState.Disabled);
    }
    
    public void EnableAllNPCs()
    {
        SetAllNPCsState(NPCState.Idle);
    }
    
    // ===== 이벤트 처리 =====
    private void OnNPCStateChanged(NPCController npc, NPCState newState)
    {
        if (showDebugInfo)
        {
            Debug.Log($"NPCManager: {npc.GetNPCData()?.npcName ?? "Unknown"} → {newState}");
        }
    }
    
    // ===== 특수 기능 =====
    
    // 모든 NPC와의 대화 강제 종료
    public void ForceEndAllDialogues()
    {
        if (DialogueManager.instance != null && DialogueManager.instance.IsInDialogue())
        {
            DialogueManager.instance.ForceEndDialogue();
        }
    }
    
    // 특정 조건의 NPC 활성화 (방문형 NPC용)
    public void CheckVisitorNPCConditions()
    {
        var visitorNPCs = GetNPCsByType(NPCType.Visitor);
        
        foreach (var npc in visitorNPCs)
        {
            // TODO: 방문 조건 체크 및 활성화
        }
    }
    
    // ===== 디버그 정보 =====
    public void LogNPCStatus()
    {
        Debug.Log("=== NPC 상태 ===");
        foreach (var npc in allNPCs)
        {
            var data = npc.GetNPCData();
            Debug.Log($"{data?.npcName ?? "Unknown"}: {npc.GetCurrentState()}, " +
                     $"Player in range: {npc.IsPlayerInRange()}");
        }
    }
    
    // ===== 유틸리티 =====
    public int GetTotalNPCCount() => allNPCs.Count;
    public int GetActiveNPCCount() => allNPCs.Count(npc => npc.gameObject.activeInHierarchy);
    public List<NPCController> GetAllNPCs() => new List<NPCController>(allNPCs);
}