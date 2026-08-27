using UnityEngine;
using UnityEngine.InputSystem;

public class SoundManager : MonoBehaviour
{
    [SerializeField] private AudioSource normalBGM;
    [SerializeField] private AudioSource trueFormBGM;

    private bool isTrueForm = false;

    void Start()
    {
        // 처음에는 노말 BGM만 재생
        normalBGM.Play();
        trueFormBGM.Stop();
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame)
        {
            ToggleBGM();
        }
    }

    void ToggleBGM()
    {
        if (!isTrueForm)
        {
            // 노말 BGM → 현재 위치에서 일시정지
            normalBGM.Pause();

            // 본모습 BGM → 멈춰있던 위치에서 재생
            trueFormBGM.UnPause();

            // 본모습 BGM을 한 번도 재생한 적 없다면 처음부터 재생
            if (!trueFormBGM.isPlaying && trueFormBGM.time == 0)
            {
                trueFormBGM.Play();
            }

            isTrueForm = true;
        }
        else
        {
            // 본모습 BGM → 현재 위치에서 일시정지
            trueFormBGM.Pause();

            // 노말 BGM → 멈춰있던 위치에서 재생
            normalBGM.UnPause();

            isTrueForm = false;
        }
    }
}