using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance; 
    public string[] sceneNames = { "Illusion 1", "Illusion 2" }; 
    private int currentSceneIndex = 0;
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void LoadScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }
    
    public void LoadNextScene()
    {
        currentSceneIndex++;
        if (currentSceneIndex >= sceneNames.Length)
        {
            currentSceneIndex = 0;
        }
        SceneManager.LoadScene(sceneNames[currentSceneIndex]);
    }

    public void LoadPreviousLevel()
    {
        currentSceneIndex--;
        if (currentSceneIndex < 0)
        {
            currentSceneIndex = sceneNames.Length - 1; 
        }
        StartCoroutine(LoadLevelWithTransition(sceneNames[currentSceneIndex]));
    }

    public void LoadSpecificLevel(string sceneName)
    {
        for (int i = 0; i < sceneNames.Length; i++)
        {
            if (sceneNames[i] == sceneName)
            {
                currentSceneIndex = i;
                break;
            }
        }
        StartCoroutine(LoadLevelWithTransition(sceneName));
    }

    private IEnumerator LoadLevelWithTransition(string sceneName)
    {
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        while (!asyncLoad.isDone)
        {
            yield return null;
        }
    }
}