// SceneTransitionDoor.cs
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransitionDoor : InteractableObject
{
    [Header("씬 전환 설정")]
    [Tooltip("로드할 씬의 이름입니다. Build Settings에 추가되어 있어야 합니다.")]
    public string targetSceneName;

    [Tooltip("새 씬에서 플레이어가 나타날 위치입니다.")]
    public Vector2 entryPointPosition;

    // 플레이어가 문과 상호작용할 때 호출됩니다.
    public override void StartInteraction(PlayerController player)
    {
        base.StartInteraction(player);

        // 전환 시작
        Debug.Log($"{targetSceneName} 씬으로 전환을 시작합니다...");
        SceneTransitionManager.Instance.LoadScene(targetSceneName, entryPointPosition);
    }

    // 이 문은 대기 시간이 필요 없으므로 CanInteract를 간단하게 오버라이드합니다.
    public override bool CanInteract(PlayerController player)
    {
        return true;
    }
}