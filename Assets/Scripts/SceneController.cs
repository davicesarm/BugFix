using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneController : MonoBehaviour
{
    [SerializeField]
    private string sceneName = "MainScene";

    public void ChangeScene()
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogWarning("SceneController: sceneName não foi definido.");
            return;
        }

        SceneManager.LoadScene(sceneName);
    }
}
