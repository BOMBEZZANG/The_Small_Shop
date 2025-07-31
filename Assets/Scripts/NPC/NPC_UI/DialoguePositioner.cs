using UnityEngine;

public class DialoguePositioner : MonoBehaviour
{
    [Header("Position Settings")]
    [SerializeField] private Vector3 npcDialogueOffset = new Vector3(0, 2.5f, 0);
    [SerializeField] private Vector3 playerDialogueOffset = new Vector3(0, 2f, 0);
    [SerializeField] private bool followSpeaker = true;
    
    [Header("References")]
    [SerializeField] private RectTransform dialoguePanel;
    [SerializeField] private Camera mainCamera;
    
    private Transform currentSpeaker;
    private Vector3 currentOffset;
    
    void Awake()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }
        
        if (dialoguePanel == null && transform.GetChild(0) != null)
        {
            dialoguePanel = transform.GetChild(0).GetComponent<RectTransform>();
        }
    }
    
    void Start()
    {
        // Subscribe to dialogue events
        DialogueManager.OnLineDisplayed += OnLineDisplayed;
        DialogueManager.OnDialogueEnded += OnDialogueEnded;
    }
    
    void OnDestroy()
    {
        // Unsubscribe from events
        DialogueManager.OnLineDisplayed -= OnLineDisplayed;
        DialogueManager.OnDialogueEnded -= OnDialogueEnded;
    }
    
    void LateUpdate()
    {
        if (followSpeaker && currentSpeaker != null && dialoguePanel != null)
        {
            UpdateDialoguePosition();
        }
    }
    
    private void OnLineDisplayed(DialogueLine line)
    {
        // Determine who is speaking
        bool isPlayerSpeaking = line.isPlayerLine || line.speakerName == "Player" || line.speakerName == "플레이어";
        
        if (isPlayerSpeaking)
        {
            // Position above player
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                SetSpeaker(player.transform, playerDialogueOffset);
            }
        }
        else
        {
            // Position above NPC
            Transform npcTransform = DialogueManager.instance.GetCurrentNPCTransform();
            if (npcTransform != null)
            {
                SetSpeaker(npcTransform, npcDialogueOffset);
            }
            else
            {
                // Fallback to finding active NPC
                NPCController activeNPC = GetActiveNPC();
                if (activeNPC != null)
                {
                    SetSpeaker(activeNPC.transform, npcDialogueOffset);
                }
            }
        }
    }
    
    private void OnDialogueEnded()
    {
        currentSpeaker = null;
    }
    
    private void SetSpeaker(Transform speaker, Vector3 offset)
    {
        currentSpeaker = speaker;
        currentOffset = offset;
        UpdateDialoguePosition();
    }
    
    private void UpdateDialoguePosition()
    {
        if (mainCamera == null || currentSpeaker == null || dialoguePanel == null) return;
        
        // Calculate world position above speaker
        Vector3 worldPosition = currentSpeaker.position + currentOffset;
        
        // Convert to screen position
        Vector3 screenPosition = mainCamera.WorldToScreenPoint(worldPosition);
        
        // Check if position is in front of camera
        if (screenPosition.z > 0)
        {
            // Set dialogue panel position
            dialoguePanel.position = screenPosition;
        }
    }
    
    private NPCController GetActiveNPC()
    {
        // Find the NPC currently in dialogue
        NPCController[] allNPCs = FindObjectsOfType<NPCController>();
        
        foreach (var npc in allNPCs)
        {
            if (npc.GetCurrentState() == NPCState.Talking)
            {
                return npc;
            }
        }
        
        return null;
    }
    
    // Public method to manually set speaker
    public void SetDialogueSpeaker(Transform speaker, bool isPlayer = false)
    {
        Vector3 offset = isPlayer ? playerDialogueOffset : npcDialogueOffset;
        SetSpeaker(speaker, offset);
    }
}