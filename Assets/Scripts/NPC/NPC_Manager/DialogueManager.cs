using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class DialogueManager : MonoBehaviour
{
    // 싱글톤
    public static DialogueManager instance = null;
    
    // 이벤트
    public static event Action<DialogueData> OnDialogueStarted;
    public static event Action OnDialogueEnded;
    public static event Action<DialogueLine> OnLineDisplayed;
    public static event Action<DialogueChoice[]> OnChoicesPresented;
    
    // 상태
    private bool isInDialogue = false;
    private DialogueData currentDialogue;
    private int currentLineIndex = 0;
    private NPCData currentNPC;
    private Transform currentNPCTransform;
    
    // 대화 기록 (선택적)
    private List<int> completedDialogues = new List<int>();
    private Dictionary<string, int> npcRelationships = new Dictionary<string, int>();
    
    // 설정
    [Header("Dialogue Settings")]
    [SerializeField] private float typingSpeed = 0.05f;
    [SerializeField] private bool autoAdvance = false;
    [SerializeField] private float autoAdvanceDelay = 3f;
    
    // 코루틴 참조
    private Coroutine typingCoroutine;
    private Coroutine autoAdvanceCoroutine;
    
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
        }
    }

    // ===== 대화 시작 (Transform 포함) =====
    public void StartDialogue(DialogueData dialogue, NPCData npc, Transform npcTransform)
    {
        currentNPCTransform = npcTransform;
        StartDialogue(dialogue, npc);
    }
    
    // ===== 대화 시작 =====
    [Obsolete]
    public void StartDialogue(DialogueData dialogue, NPCData npc = null)
    {
        if (dialogue == null)
        {
            Debug.LogError("DialogueManager: DialogueData is null!");
            return;
        }
        
        if (isInDialogue)
        {
            Debug.LogWarning("DialogueManager: Already in dialogue!");
            return;
        }
        
        // 대화 시작 조건 체크
        if (!dialogue.CanStartDialogue())
        {
            Debug.Log($"DialogueManager: Cannot start dialogue {dialogue.dialogueName} - conditions not met");
            return;
        }
        
        // 상태 설정
        isInDialogue = true;
        currentDialogue = dialogue;
        currentNPC = npc;
        currentLineIndex = 0;
        
        // 플레이어 이동 정지
        PlayerController player = FindObjectOfType<PlayerController>();
        if (player != null)
        {
            player.SetMovementEnabled(false);
        }
        
        // UI 표시
        if (UIManager.instance != null && UIManager.instance.dialogueUI != null)
        {
            UIManager.instance.dialogueUI.ShowDialogue();
        }
        
        // 이벤트 발생
        OnDialogueStarted?.Invoke(dialogue);
        
        // 첫 대사 표시
        DisplayCurrentLine();
        
        Debug.Log($"DialogueManager: Started dialogue '{dialogue.dialogueName}'");
    }

    // ===== 현재 대사 표시 =====
    [Obsolete]
    private void DisplayCurrentLine()
    {
        if (currentLineIndex >= currentDialogue.dialogueLines.Length)
        {
            // 대화 끝에 도달
            CheckForChoices();
            return;
        }
        
        DialogueLine currentLine = currentDialogue.dialogueLines[currentLineIndex];
        
        // UI에 표시
        if (UIManager.instance?.dialogueUI != null)
        {
            UIManager.instance.dialogueUI.DisplayLine(currentLine);
        }
        
        // 이벤트 발생
        OnLineDisplayed?.Invoke(currentLine);
        
        // 타이핑 효과
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
        }
        typingCoroutine = StartCoroutine(TypeLine(currentLine.text));
        
        // 자동 진행 설정
        if (autoAdvance && currentLine.displayTime > 0)
        {
            if (autoAdvanceCoroutine != null)
            {
                StopCoroutine(autoAdvanceCoroutine);
            }
            autoAdvanceCoroutine = StartCoroutine(AutoAdvance(currentLine.displayTime));
        }
    }
    
    // ===== 타이핑 효과 =====
    private IEnumerator TypeLine(string fullText)
    {
        if (UIManager.instance?.dialogueUI == null) yield break;
        
        string displayedText = "";
        foreach (char letter in fullText)
        {
            displayedText += letter;
            UIManager.instance.dialogueUI.UpdateLineText(displayedText);
            yield return new WaitForSeconds(typingSpeed);
        }
    }
    
    // ===== 자동 진행 =====
    private IEnumerator AutoAdvance(float delay)
    {
        yield return new WaitForSeconds(delay);
        AdvanceDialogue();
    }

    // ===== 대화 진행 =====
    [Obsolete]
    public void AdvanceDialogue()
    {
        // 타이핑 중이면 즉시 완료
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            if (currentLineIndex < currentDialogue.dialogueLines.Length)
            {
                UIManager.instance?.dialogueUI?.UpdateLineText(
                    currentDialogue.dialogueLines[currentLineIndex].text
                );
            }
            typingCoroutine = null;
            return;
        }
        
        // 다음 대사로
        currentLineIndex++;
        
        if (currentLineIndex < currentDialogue.dialogueLines.Length)
        {
            DisplayCurrentLine();
        }
        else
        {
            CheckForChoices();
        }
    }

    // ===== 선택지 확인 =====
    [Obsolete]
    private void CheckForChoices()
    {
        if (currentDialogue.hasChoices && currentDialogue.choices != null && currentDialogue.choices.Length > 0)
        {
            // 조건에 맞는 선택지만 필터링
            List<DialogueChoice> validChoices = new List<DialogueChoice>();
            
            foreach (var choice in currentDialogue.choices)
            {
                bool isValid = true;
                
                // 선택지 조건 체크
                if (choice.conditions != null)
                {
                    foreach (var condition in choice.conditions)
                    {
                        if (!condition.CheckCondition())
                        {
                            isValid = false;
                            break;
                        }
                    }
                }
                
                if (isValid)
                {
                    validChoices.Add(choice);
                }
            }
            
            if (validChoices.Count > 0)
            {
                // 선택지 표시
                UIManager.instance?.dialogueUI?.ShowChoices(validChoices.ToArray());
                OnChoicesPresented?.Invoke(validChoices.ToArray());
            }
            else
            {
                // 유효한 선택지가 없으면 대화 종료
                EndDialogue();
            }
        }
        else
        {
            // 선택지가 없으면 대화 종료
            EndDialogue();
        }
    }
    
    // ===== 선택지 선택 =====
    public void SelectChoice(int choiceIndex)
    {
        if (!currentDialogue.hasChoices || choiceIndex >= currentDialogue.choices.Length)
        {
            Debug.LogError($"DialogueManager: Invalid choice index {choiceIndex}");
            return;
        }
        
        DialogueChoice selectedChoice = currentDialogue.choices[choiceIndex];
        
        // 선택 효과 적용
        if (selectedChoice.effects != null)
        {
            foreach (var effect in selectedChoice.effects)
            {
                ApplyDialogueEffect(effect);
            }
        }
        
        // 다음 대화로 진행
        if (selectedChoice.nextDialogueID >= 0)
        {
            // TODO: 다음 대화 ID로 새 대화 시작
            Debug.Log($"DialogueManager: Would start dialogue with ID {selectedChoice.nextDialogueID}");
            EndDialogue(); // 임시로 종료
        }
        else
        {
            EndDialogue();
        }
    }
    
    // ===== 대화 효과 적용 =====
    private void ApplyDialogueEffect(DialogueEffect effect)
    {
        switch (effect.type)
        {
            case DialogueEffect.EffectType.GiveGold:
                GoldManager.instance.AddGold(effect.value);
                Debug.Log($"DialogueManager: Gave {effect.value} gold");
                break;
                
            case DialogueEffect.EffectType.GiveExp:
                PlayerDataManager.instance.AddExp(effect.value);
                Debug.Log($"DialogueManager: Gave {effect.value} exp");
                break;
                
            case DialogueEffect.EffectType.GiveItem:
                // TODO: 아이템 지급 구현
                Debug.Log($"DialogueManager: Would give item {effect.parameterKey} x{effect.value}");
                break;
                
            case DialogueEffect.EffectType.StartQuest:
                // TODO: 퀘스트 시작 구현
                Debug.Log($"DialogueManager: Would start quest {effect.parameterKey}");
                break;
                
            case DialogueEffect.EffectType.ChangeNPCRelationship:
                ChangeNPCRelationship(effect.parameterKey, effect.value);
                break;
                
            default:
                Debug.LogWarning($"DialogueManager: Unhandled effect type {effect.type}");
                break;
        }
    }
    
    // ===== NPC 관계도 변경 =====
    private void ChangeNPCRelationship(string npcName, int change)
    {
        if (!npcRelationships.ContainsKey(npcName))
        {
            npcRelationships[npcName] = 0;
        }
        
        npcRelationships[npcName] += change;
        Debug.Log($"DialogueManager: {npcName} relationship changed by {change} (now: {npcRelationships[npcName]})");
    }

    // ===== 대화 종료 =====
    [Obsolete]
    public void EndDialogue()
    {
        if (!isInDialogue) return;
        
        // 대화 기록에 추가
        if (!currentDialogue.isRepeatable && !completedDialogues.Contains(currentDialogue.dialogueID))
        {
            completedDialogues.Add(currentDialogue.dialogueID);
        }
        
        // 코루틴 정리
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }
        
        if (autoAdvanceCoroutine != null)
        {
            StopCoroutine(autoAdvanceCoroutine);
            autoAdvanceCoroutine = null;
        }
        
        // UI 숨기기
        UIManager.instance?.dialogueUI?.HideDialogue();
        
        // 플레이어 이동 재개
        PlayerController player = FindObjectOfType<PlayerController>();
        if (player != null)
        {
            player.SetMovementEnabled(true);
        }
        
        // 상태 초기화
        isInDialogue = false;
        currentDialogue = null;
        currentNPC = null;
        currentNPCTransform = null;
        currentLineIndex = 0;
        
        // 이벤트 발생
        OnDialogueEnded?.Invoke();
        
        Debug.Log("DialogueManager: Dialogue ended");
    }

    // ===== 강제 종료 =====
    [Obsolete]
    public void ForceEndDialogue()
    {
        if (isInDialogue)
        {
            EndDialogue();
        }
    }
    
    // ===== 상태 확인 =====
    public bool IsInDialogue() => isInDialogue;
    public DialogueData GetCurrentDialogue() => currentDialogue;
    public NPCData GetCurrentNPC() => currentNPC;
    public Transform GetCurrentNPCTransform() => currentNPCTransform;
    
    // ===== 대화 기록 확인 =====
    public bool HasCompletedDialogue(int dialogueID)
    {
        return completedDialogues.Contains(dialogueID);
    }
    
    public int GetNPCRelationship(string npcName)
    {
        return npcRelationships.ContainsKey(npcName) ? npcRelationships[npcName] : 0;
    }

    // ===== 입력 처리 =====
    [Obsolete]
    void Update()
    {
        if (!isInDialogue) return;
        
        // 대화 진행 입력 (Space, Enter, 마우스 클릭)
        if (Input.GetKeyDown(KeyCode.Space) || 
            Input.GetKeyDown(KeyCode.Return) || 
            Input.GetMouseButtonDown(0))
        {
            // 선택지가 표시 중이 아닐 때만
            if (!UIManager.instance?.dialogueUI?.IsShowingChoices() ?? true)
            {
                AdvanceDialogue();
            }
        }
        
        // ESC로 대화 강제 종료 (선택적)
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ForceEndDialogue();
        }
    }
}