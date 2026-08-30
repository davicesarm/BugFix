using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Exitbutton : MonoBehaviour
{
    [SerializeField]
    private string mainSceneName = "MainScene";

    public void ExitApp()
    {
        Application.Quit();
    }

    public void BackToMainScene()
    {
        if (string.IsNullOrWhiteSpace(mainSceneName))
        {
            Debug.LogWarning("Exitbutton: mainSceneName não foi definido.");
            return;
        }

        SceneManager.LoadScene(mainSceneName);
    }
}
