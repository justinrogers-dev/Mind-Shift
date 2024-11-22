using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.SceneManagement;
using LevelManagement;

public class GameManager : MonoBehaviour
{
    // Singleton instance of GameManager
    public static GameManager instance;

    [Header("Level References")]
    public IdaMovement player;
    public List<Transform> pivots;
    public List<PathCondition> pathConditions = new List<PathCondition>();
    
    [Header("Scene Management")]
    public Loader sceneLoader;
    public string nextLevelName = "Illusion 2";
    public float transitionDelay = 1f;
    public bool isFinalLevel = false;

    private void Awake()
    {
        // Set up the singleton instance
        instance = this;
        
        // Find the loader if not assigned
        if (sceneLoader == null)
        {
            sceneLoader = FindObjectOfType<Loader>();
        }
    }

    void Update()
    {
        // Check and validate all path conditions
        ProcessPathValidation();

        // Skip further updates if the player is walking
        if (player.walking)
            return;

        // Handle player input for rotation or reset
        HandlePlayerInput();
    }

    private void ProcessPathValidation()
    {
        // Validate all path conditions
        foreach (PathCondition rule in pathConditions)
        {
            int validConditions = 0;

            // Check if all conditions are met for a given path rule
            for (int i = 0; i < rule.conditions.Count; i++)
            {
                if (rule.conditions[i].conditionObject.eulerAngles == rule.conditions[i].eulerAngle)
                {
                    validConditions++;
                }
            }

            // Activate or deactivate paths based on the conditions
            foreach (SinglePath path in rule.paths)
            {
                bool isValid = false;
                if (validConditions == rule.conditions.Count)
                {
                    isValid = true;
                }
                path.block.possiblePaths[path.index].active = isValid;
            }
        }
    }

    private void HandlePlayerInput()
    {
        // Check for rotation input (left or right arrow keys)
        if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.LeftArrow))
        {
            int direction;

            // Determine rotation direction based on the key pressed
            if (Input.GetKey(KeyCode.RightArrow))
            {
                direction = 1;
            }
            else
            {
                direction = -1;
            }

            // Complete any ongoing rotation and start a new one
            pivots[0].DOComplete();
            pivots[0].DORotate(new Vector3(0, 90 * direction, 0), .6f, RotateMode.WorldAxisAdd).SetEase(Ease.OutBack);
        }

        // Check for reset input (R key)
        if (Input.GetKeyDown(KeyCode.R))
        {
            ResetLevel();
        }
    }

    private void ResetLevel()
    {
        // Reload the current scene
        SceneManager.LoadSceneAsync(SceneManager.GetActiveScene().name);
    }

    public void RotateRightPivot()
    {
        // Complete any ongoing rotation and start a new one
        pivots[1].DOComplete();
        pivots[1].DORotate(new Vector3(0, 0, 90), .6f).SetEase(Ease.OutBack);
    }

    public void OnFinalButtonReached()
    {
        if (isFinalLevel)
        {
            // Handle final level completion
            StartCoroutine(LoadMainMenu());
        }
        else
        {
            // Load the next level
            StartCoroutine(LoadNextLevel());
        }
    }

    private IEnumerator LoadNextLevel()
    {
        yield return new WaitForSeconds(transitionDelay);

        if (sceneLoader != null)
        {
            sceneLoader.LoadScene(nextLevelName);
        }
        else
        {
            SceneManager.LoadSceneAsync(nextLevelName);
        }
    }

    private IEnumerator LoadMainMenu()
    {
        yield return new WaitForSeconds(transitionDelay);

        if (sceneLoader != null)
        {
            sceneLoader.LoadScene("Main Menu");
        }
        else
        {
            SceneManager.LoadSceneAsync("Main Menu");
        }
    }
}