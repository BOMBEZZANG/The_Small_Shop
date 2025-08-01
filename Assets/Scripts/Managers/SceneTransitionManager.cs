// SceneTransitionManager.cs
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransitionManager
{
    // 싱글톤 인스턴스
    private static SceneTransitionManager instance;
    public static SceneTransitionManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = new SceneTransitionManager();
            }
            return instance;
        }
    }

    // 다음 씬에서 플레이어가 나타날 위치를 임시 저장
    private Vector2 playerEntryPoint;

    // 씬 로드를 시작하는 함수
    public void LoadScene(string sceneName, Vector2 entryPoint)
    {
        this.playerEntryPoint = entryPoint;
        SceneManager.LoadScene(sceneName);
    }

    // 다음 씬에서 플레이어 위치를 설정하기 위해 호출할 함수
    public Vector2 GetEntryPoint()
    {
        return playerEntryPoint;
    }
}