using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;
using Random = UnityEngine.Random;

public class TargetManager : MonoBehaviour
{
    [Header("Target Settings")]
    public GameObject targetPrefab;

    public Transform playerTarget;
    public int maxTargets = 10;
    public float spawnRadius = 15f;
    
    public TMP_Text scoreText;

    [Header("Rotate Target to Player")] public bool rotateTarget = false;
    
    private int score = 0;

    void Start()
    {
        UpdateScoreText();
        SpawnInitialTargets();
    }

    private void FixedUpdate()
    {
        Target[] targets = FindObjectsOfType<Target>();
        if (targets.Length < maxTargets)
        {
            SpawnTarget();
            
        }
    }

    void SpawnInitialTargets()
    {
        for (int i = 0; i < maxTargets; i++)
        {
            SpawnTarget();
        }
    }
    
    void SpawnTarget()
    {
        if (targetPrefab == null) return;
        
        Vector2 randomCircle = Random.insideUnitCircle.normalized * spawnRadius;
        Vector3 spawnPosition = transform.position + new Vector3(randomCircle.x, 0f, randomCircle.y);
        
        GameObject newTarget = Instantiate(targetPrefab, spawnPosition, Quaternion.identity);
        if(rotateTarget) newTarget.transform.LookAt(playerTarget);

    }
    
    public void AddScore(int pointsToAdd)
    {
        score += pointsToAdd;
        UpdateScoreText();
    }
    
    void UpdateScoreText()
    {
        if (scoreText != null)
        {
            scoreText.text = "Score: " + score;
        }
    }
}