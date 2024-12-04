using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class LevelFinishUI : MonoBehaviour
{
    public static LevelFinishUI Instance { get; private set; }

    public TextMeshProUGUI levelCompleteText;
    public GameObject levelCompleteCanvas;
    public Transform head;
    public float spawnDistance = 2.0f;
    
    private bool isUIActive = false;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public void ShowLevelCompleteUI(uint levelNumber)
    {
        levelCompleteText.text = "Level " + levelNumber + " Completed!";
        isUIActive = true;
        levelCompleteCanvas.SetActive(true);
    }

    public void HideLevelCompleteUI()
    {
        isUIActive = false;
        levelCompleteCanvas.SetActive(false);
    }

    private void Update()
    {
        if (!isUIActive)
            return;

        levelCompleteCanvas.transform.position = head.position + new Vector3(head.forward.x, 0.0f, head.forward.z).normalized * spawnDistance;
        levelCompleteCanvas.transform.LookAt(new Vector3(head.position.x, levelCompleteCanvas.transform.position.y, head.position.z));
        levelCompleteCanvas.transform.forward *= -1.0f;
    }
}

