using UnityEngine;
using Yarn.Unity;
using System.Collections;

public class EndingSceneChanger : MonoBehaviour
{
    [Header("씬 이름")]
    [SerializeField] private string goodEndingSceneName = "GoodEndingScene";
    [SerializeField] private string badEndingSceneName = "BadEndingScene";

    [YarnCommand("good_ending")]
    public void GoToGoodEnding()
    {
        StartCoroutine(LoadSceneAfterDelay(goodEndingSceneName));
    }

    [YarnCommand("bad_ending")]
    public void GoToBadEnding()
    {
        StartCoroutine(LoadSceneAfterDelay(badEndingSceneName));
    }

    private IEnumerator LoadSceneAfterDelay(string sceneName)
    {
        // LinePresenter의 Fade Duration(0.1)보다 살짝 길게 잡아서
        // 페이드 애니메이션이 완전히 끝난 다음에 씬을 전환
        yield return new WaitForSeconds(0.3f);
        UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
    }
}