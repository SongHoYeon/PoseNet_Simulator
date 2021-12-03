using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneController : MonoBehaviour
{
    public Scene scene;
    void Awake()
    {
        scene = SceneManager.GetActiveScene();
    }
    public void Move()
    {
        if (scene.name == "TestMode")
        {
            SceneManager.LoadScene("GameMode");
        }
        else
        {
            SceneManager.LoadScene("TestMode");
        }
    }
    public void MoveToGameScene()
    {
        SceneManager.LoadScene("GameMode");
    }
    public void MoveToTestScene()
    {
        SceneManager.LoadScene("TestMode");
    }
}
