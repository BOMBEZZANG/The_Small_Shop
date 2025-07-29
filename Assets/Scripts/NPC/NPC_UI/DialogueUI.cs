using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class DialogueUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TextMeshProUGUI speakerNameText;
    [SerializeField] private TextMeshProUGUI dialogueText;
    [SerializeField] private Image speakerPortrait;
    [SerializeField] private GameObject portraitFrame;
    
    [Header("Choice UI")]
    [SerializeField] private GameObject choiceContainer;
    [SerializeField] private GameObject choiceButtonPrefab;
    [SerializeField] private Transform choiceButtonParent;
    
    [Header("Indicators")]
    [SerializeField] private GameObject continueIndicator;
    [SerializeField] private float indicatorBlinkSpeed = 1f;
    
    [Header("Animation")]
    [SerializeField] private float panelFadeSpeed = 5f;
    [SerializeField] private AnimationCurve showCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private AnimationCurve hideCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);
    
    // 상태
    private bool isShowingChoices = false;
    private List<GameObject> activeChoiceButtons = new List<GameObject>();
    private CanvasGroup dialogueCanvasGroup;
    private Coroutine blinkCoroutine;
    
    void Awake()
    {
        // CanvasGroup 확인/추가
        dialogueCanvasGroup = dialoguePanel.GetComponent<CanvasGroup>();
        if (dialogueCanvasGroup == null)
        {
            dialogueCanvasGroup = dialoguePanel.AddComponent<CanvasGroup>();
        }
        
        // 초기 상태
        dialoguePanel.SetActive(false);
        choiceContainer.SetActive(false);
        continueIndicator.SetActive(false);
    }
    
    void Start()
    {
        // 이벤트 구독
        DialogueManager.OnLineDisplayed += OnLineDisplayed;
    }
    
    void OnDestroy()
    {
        // 이벤트 구독 해제
        DialogueManager.OnLineDisplayed -= OnLineDisplayed;
    }
    
    // ===== 대화창 표시 =====
    public void ShowDialogue()
    {
        dialoguePanel.SetActive(true);
        StartCoroutine(FadePanel(true));
    }
    
    // ===== 대화창 숨기기 =====
    public void HideDialogue()
    {
        HideChoices();
        StartCoroutine(FadePanel(false));
    }
    
    // ===== 패널 페이드 효과 =====
    private IEnumerator FadePanel(bool show)
    {
        float elapsed = 0f;
        float duration = 1f / panelFadeSpeed;
        AnimationCurve curve = show ? showCurve : hideCurve;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            dialogueCanvasGroup.alpha = curve.Evaluate(t);
            yield return null;
        }
        
        dialogueCanvasGroup.alpha = show ? 1f : 0f;
        
        if (!show)
        {
            dialoguePanel.SetActive(false);
        }
    }
    
    // ===== 대사 표시 =====
    public void DisplayLine(DialogueLine line)
    {
        // 화자 이름
        if (speakerNameText != null)
        {
            speakerNameText.text = line.speakerName;
            speakerNameText.color = line.isPlayerLine ? Color.cyan : Color.white;
        }
        
        // 초상화
        if (speakerPortrait != null && portraitFrame != null)
        {
            if (line.speakerPortrait != null)
            {
                speakerPortrait.sprite = line.speakerPortrait;
                portraitFrame.SetActive(true);
            }
            else
            {
                portraitFrame.SetActive(false);
            }
        }
        
        // 대사 텍스트 (타이핑 효과를 위해 초기화)
        if (dialogueText != null)
        {
            dialogueText.text = "";
        }
        
        // 음성 재생 (있을 경우)
        if (line.voiceClip != null)
        {
            // TODO: 오디오 재생
        }
        
        // Continue 표시 숨기기 (타이핑 완료 후 표시)
        ShowContinueIndicator(false);
    }
    
    // ===== 텍스트 업데이트 (타이핑 효과용) =====
    public void UpdateLineText(string text)
    {
        if (dialogueText != null)
        {
            dialogueText.text = text;
        }
    }
    
    // ===== 대사 표시 완료 시 =====
    private void OnLineDisplayed(DialogueLine line)
    {
        // Continue 표시
        ShowContinueIndicator(true);
    }
    
    // ===== Continue 표시 =====
    private void ShowContinueIndicator(bool show)
    {
        if (continueIndicator == null) return;
        
        continueIndicator.SetActive(show);
        
        if (show)
        {
            // 깜빡임 효과
            if (blinkCoroutine != null)
            {
                StopCoroutine(blinkCoroutine);
            }
            blinkCoroutine = StartCoroutine(BlinkIndicator());
        }
        else
        {
            if (blinkCoroutine != null)
            {
                StopCoroutine(blinkCoroutine);
                blinkCoroutine = null;
            }
        }
    }
    
    // ===== 깜빡임 효과 =====
    private IEnumerator BlinkIndicator()
    {
        CanvasGroup indicatorGroup = continueIndicator.GetComponent<CanvasGroup>();
        if (indicatorGroup == null)
        {
            indicatorGroup = continueIndicator.AddComponent<CanvasGroup>();
        }
        
        while (true)
        {
            float alpha = Mathf.PingPong(Time.time * indicatorBlinkSpeed, 1f);
            indicatorGroup.alpha = alpha;
            yield return null;
        }
    }
    
    // ===== 선택지 표시 =====
    public void ShowChoices(DialogueChoice[] choices)
    {
        isShowingChoices = true;
        choiceContainer.SetActive(true);
        ShowContinueIndicator(false);
        
        // 기존 버튼 정리
        ClearChoiceButtons();
        
        // 새 버튼 생성
        for (int i = 0; i < choices.Length; i++)
        {
            CreateChoiceButton(choices[i], i);
        }
        
        // 첫 번째 버튼 선택 (키보드/컨트롤러 지원)
        if (activeChoiceButtons.Count > 0)
        {
            var firstButton = activeChoiceButtons[0].GetComponent<Button>();
            if (firstButton != null)
            {
                firstButton.Select();
            }
        }
    }
    
    // ===== 선택지 버튼 생성 =====
    private void CreateChoiceButton(DialogueChoice choice, int index)
    {
        GameObject buttonObj = Instantiate(choiceButtonPrefab, choiceButtonParent);
        
        // 텍스트 설정
        TextMeshProUGUI buttonText = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
        if (buttonText != null)
        {
            buttonText.text = choice.choiceText;
        }
        
        // 버튼 이벤트 설정
        Button button = buttonObj.GetComponent<Button>();
        if (button != null)
        {
            int choiceIndex = index; // 클로저를 위한 로컬 변수
            button.onClick.AddListener(() => OnChoiceSelected(choiceIndex));
        }
        
        // 애니메이션 효과
        StartCoroutine(AnimateChoiceButton(buttonObj, index));
        
        activeChoiceButtons.Add(buttonObj);
    }
    
    // ===== 선택지 버튼 애니메이션 =====
    private IEnumerator AnimateChoiceButton(GameObject button, int index)
    {
        RectTransform rect = button.GetComponent<RectTransform>();
        if (rect == null) yield break;
        
        // 초기 위치 (오른쪽에서 시작)
        Vector3 startPos = rect.localPosition;
        startPos.x += 300f;
        rect.localPosition = startPos;
        
        // 원래 위치로 슬라이드
        float elapsed = 0f;
        float duration = 0.3f;
        float delay = index * 0.1f; // 순차적 등장
        
        yield return new WaitForSeconds(delay);
        
        Vector3 endPos = startPos;
        endPos.x -= 300f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            rect.localPosition = Vector3.Lerp(startPos, endPos, showCurve.Evaluate(t));
            yield return null;
        }
    }
    
    // ===== 선택지 선택 =====
    private void OnChoiceSelected(int index)
    {
        // 선택 효과음 재생
        // TODO: AudioManager.instance.PlaySound("UI_Select");
        
        // DialogueManager에 선택 전달
        DialogueManager.instance.SelectChoice(index);
        
        // 선택지 숨기기
        HideChoices();
    }
    
    // ===== 선택지 숨기기 =====
    private void HideChoices()
    {
        isShowingChoices = false;
        choiceContainer.SetActive(false);
        ClearChoiceButtons();
    }
    
    // ===== 선택지 버튼 정리 =====
    private void ClearChoiceButtons()
    {
        foreach (var button in activeChoiceButtons)
        {
            if (button != null)
            {
                Destroy(button);
            }
        }
        activeChoiceButtons.Clear();
    }
    
    // ===== 상태 확인 =====
    public bool IsShowingChoices() => isShowingChoices;
    
    // ===== UI 설정 메서드 =====
    public void SetDialogueSpeed(float speed)
    {
        // DialogueManager의 타이핑 속도 조절
        if (DialogueManager.instance != null)
        {
            // TODO: DialogueManager에 SetTypingSpeed 메서드 추가 필요
        }
    }
    
    public void SetDialogueOpacity(float alpha)
    {
        if (dialogueCanvasGroup != null)
        {
            dialogueCanvasGroup.alpha = alpha;
        }
    }
}