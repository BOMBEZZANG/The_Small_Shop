// SceneEntryController.cs (수정된 버전)
using UnityEngine;

public class SceneEntryController : MonoBehaviour
{
    void Start()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            // SceneTransitionManager에 저장된 진입점 위치를 가져옵니다.
            Vector2 entryPoint = SceneTransitionManager.Instance.GetEntryPoint();
            player.transform.position = entryPoint;

            Debug.Log($"플레이어 위치를 새 씬의 진입점 {entryPoint} (으)로 재설정했습니다.");
        }
        else
        {
            Debug.LogError("플레이어를 찾을 수 없어 위치를 재설정할 수 없습니다!");
        }
    }
}