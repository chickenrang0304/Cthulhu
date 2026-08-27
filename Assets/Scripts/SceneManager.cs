using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using Yarn.Unity;


public class SceneManager : MonoBehaviour
{
    public void ChangeStartScene()
    {
               UnityEngine.SceneManagement.SceneManager.LoadScene("StartScene");
    }

    public void ChangeCouenselSelectScene()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("CounselSelectScene");
    }
    public void ChangeCounselScene()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("CounselScene");
    }

    public void ChangeBadEnding()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("DefeatScene");
    }
}   