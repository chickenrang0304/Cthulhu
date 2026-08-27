using UnityEngine;
using Yarn.Unity;

// 얀 스피너 대사 스크립트 안에서 바로 호출 가능한 씬 전환 커맨드 모음.
// 예: <<good_ending>>  또는  <<bad_ending>>
public class EndingSceneChanger : MonoBehaviour
{
    [Header("씬 이름")]
    [SerializeField] private string goodEndingSceneName = "GoodEndingScene";
    [SerializeField] private string badEndingSceneName = "BadEndingScene";

    // 얀 스크립트에서 <<good_ending>> 이라고 쓰면 이 함수가 호출된다.
    [YarnCommand("good_ending")]
    public void GoToGoodEnding()
    {
        // "SceneManager"만 쓰면 친구가 만든 동명의 SceneManager 클래스와 충돌하므로
        // 풀네임(UnityEngine.SceneManagement.SceneManager)으로 명시해서 유니티 내장 클래스를 정확히 지정
        UnityEngine.SceneManagement.SceneManager.LoadScene(goodEndingSceneName);
    }

    // 얀 스크립트에서 <<bad_ending>> 이라고 쓰면 이 함수가 호출된다.
    [YarnCommand("bad_ending")]
    public void GoToBadEnding()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(badEndingSceneName);
    }
}