// PersistentObject.cs (수정된 최종 버전)
using UnityEngine;

/// <summary>
/// 이 컴포넌트가 붙은 게임 오브젝트와 그 모든 자식 오브젝트들이
/// 씬이 전환될 때 파괴되지 않도록 합니다.
/// static 변수를 사용하여 오직 첫 번째 인스턴스만 살아남도록 보장합니다.
/// </summary>
public class PersistentObject : MonoBehaviour
{
    // 이 스크립트의 인스턴스가 이미 존재하는지 확인하는 static 변수
    private static bool hasInstance = false;

    void Awake()
    {
        // 이 스크립트의 인스턴스가 이미 씬에 존재한다면 (즉, 내가 원본이 아니라면)
        if (hasInstance)
        {
            // 이 게임 오브젝트(복제본)를 즉시 파괴합니다.
            Destroy(gameObject);
            Debug.Log("중복 PersistentObject(복제본)를 파괴했습니다.");
        }
        // 이 스크립트의 인스턴스가 아직 없다면 (즉, 내가 최초의 원본이라면)
        else
        {
            // 이제 인스턴스가 존재한다고 표시합니다.
            hasInstance = true;

            // 이 게임 오브젝트(원본)가 씬 전환 시 파괴되지 않도록 설정합니다.
            DontDestroyOnLoad(gameObject);
            Debug.Log("원본 PersistentObject를 파괴되지 않도록 설정했습니다.");
        }
    }
}