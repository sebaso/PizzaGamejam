using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public enum GameState
    {
        WaitingToStart,  
        Countdown,       
        Playing,         
        DayComplete,     
        GameOver,        
        Victory          
    }

    public GameState currentState = GameState.WaitingToStart;
    public bool IsGameActive => currentState == GameState.Playing;

    public int currentDay = 1;
    public int maxDays = 3;
    
    public int[] enemiesPerDay = { 3, 5, 7 };
    
    public int maxConcurrentEnemies = 3;
    
    private int enemiesToSpawnThisDay;
    private int enemiesSpawnedThisDay;
    private int enemiesDefeatedThisDay;

    public GameObject enemyPrefab;
    public GameObject enemyHam;
    public GameObject enemyPineapple;
    public GameObject enemyTomato;
    public GameObject player;

    public List<Transform> spawnPoints;
    public float spawnDelay = 2f;
    
    private List<GameObject> activeEnemies = new List<GameObject>();
    private Coroutine spawnCoroutine;

    public CountdownUI countdownUI;
    public GameObject arena;

    public int totalEnemiesKilled = 0;
    public int score = 0;
    public GameObject gameOverUI;
    public GameObject victoryUI;

    public System.Action OnDayStart;
    public System.Action<int> OnDayComplete;
    public System.Action OnGameOver;
    public System.Action OnVictory;


    
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        SetState(GameState.WaitingToStart);
    }

    void Update()
    {
        if(player.activeSelf == false) PlayerDied();
        if (currentState == GameState.WaitingToStart)
        {
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
            {
                StartGame();
            }
        }

        if (currentState == GameState.Playing)
        {
            CheckDayCompletion();
        }

        if (currentState == GameState.DayComplete)
        {
        }

        if (currentState == GameState.GameOver)
        {
            if(Input.GetKeyDown(KeyCode.Space))
            {
                ToMenu();
            }
            gameOverUI.SetActive(true);
        }

        if (currentState == GameState.Victory)
        {
            if(Input.GetKeyDown(KeyCode.Space))
            {
                ToMenu();
            }
            victoryUI.SetActive(true);
        }
    }
    
    void SetState(GameState newState)
    {
        GameState previousState = currentState;
        currentState = newState;

        Debug.Log($"GameManager: {previousState} → {newState}");

        switch (newState)
        {
            case GameState.WaitingToStart:
                currentDay = 1;
                totalEnemiesKilled = 0;
                score = 0;
                ClearAllEnemies();
                break;

            case GameState.Countdown:
                StartCoroutine(CountdownRoutine());
                break;

            case GameState.Playing:
                StartDay();
                break;

            case GameState.DayComplete:
                StartCoroutine(DayCompleteRoutine());
                break;

            case GameState.GameOver:
                OnGameOver?.Invoke();
                break;

            case GameState.Victory:
                OnVictory?.Invoke();
                break;
        }
    }
    
    public void StartGame()
    {
        if (currentState != GameState.WaitingToStart) return;
        
        currentDay = 1;
        SetState(GameState.Countdown);
    }

    IEnumerator CountdownRoutine()
    {
        if (countdownUI != null)
        {
            countdownUI.gameObject.SetActive(true);
            if (arena != null) arena.SetActive(true);

            bool countdownComplete = false;
            countdownUI.OnCountdownComplete = () => countdownComplete = true;
            countdownUI.StartCountdown();
            while (!countdownComplete)
            {
                yield return null;
            }
        }
        else
        {
            yield return new WaitForSeconds(1f);
        }

        SetState(GameState.Playing);
    }

    void StartDay()
    {
        Debug.Log($"Day {currentDay} Starting!");

        int dayIndex = Mathf.Clamp(currentDay - 1, 0, enemiesPerDay.Length - 1);
        enemiesToSpawnThisDay = enemiesPerDay[dayIndex];
        enemiesSpawnedThisDay = 0;
        enemiesDefeatedThisDay = 0;
        OnDayStart?.Invoke();
        if (spawnCoroutine != null) StopCoroutine(spawnCoroutine);
        spawnCoroutine = StartCoroutine(SpawnEnemiesRoutine());
    }

    void CheckDayCompletion()
    {
        if (enemiesDefeatedThisDay >= enemiesToSpawnThisDay && activeEnemies.Count == 0)
        {
            SetState(GameState.DayComplete);
        }
    }

    IEnumerator DayCompleteRoutine()
    {
        Debug.Log($"Day {currentDay} Complete!");
        
        OnDayComplete?.Invoke(currentDay);

        if (spawnCoroutine != null) StopCoroutine(spawnCoroutine);

        yield return new WaitForSeconds(2f);

        if (currentDay >= maxDays)
        {
            SetState(GameState.Victory);
        }
        else
        {
            currentDay++;
            SetState(GameState.Countdown);
        }
    }

    public void PlayerDied()
    {
        currentState = GameState.GameOver;

        Debug.Log("Player has been defeated!");
        
        if (spawnCoroutine != null) StopCoroutine(spawnCoroutine);
        ClearAllEnemies();
        
    }
    public void ToMenu(){
        SceneManager.LoadScene("MenuPrincipal");
    }

    public void RestartGame()
    {
        ClearAllEnemies();
        SetState(GameState.WaitingToStart);
    }
    
    IEnumerator SpawnEnemiesRoutine()
    {
        yield return new WaitForSeconds(1f);

        while (enemiesSpawnedThisDay < enemiesToSpawnThisDay)
        {
            while (activeEnemies.Count >= maxConcurrentEnemies)
            {
                yield return new WaitForSeconds(0.5f);
            }

            SpawnEnemy();
            enemiesSpawnedThisDay++;

            yield return new WaitForSeconds(spawnDelay);
        }
    }

    void SpawnEnemy()
    {
        if (spawnPoints == null || spawnPoints.Count == 0)
        {
            Debug.LogWarning("No spawn points assigned!");
            return;
        }

        Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Count)];


        GameObject prefabToSpawn = GetRandomEnemyPrefab();
        
        if (prefabToSpawn == null)
        {
            Debug.LogWarning("No enemy prefab assigned!");
            return;
        }
        GameObject enemy = Instantiate(prefabToSpawn, spawnPoint.position, spawnPoint.rotation);
        RegisterEnemy(enemy);
    }

    GameObject GetRandomEnemyPrefab()
    {
        List<GameObject> availablePrefabs = new List<GameObject>();
        
        if (enemyPrefab != null) availablePrefabs.Add(enemyPrefab);
        if (enemyHam != null) availablePrefabs.Add(enemyHam);
        if (enemyPineapple != null) availablePrefabs.Add(enemyPineapple);
        if (enemyTomato != null) availablePrefabs.Add(enemyTomato);

        if (availablePrefabs.Count == 0) return null;

        return availablePrefabs[Random.Range(0, availablePrefabs.Count)];
    }

    public void RegisterEnemy(GameObject enemy)
    {
        if (!activeEnemies.Contains(enemy))
        {
            activeEnemies.Add(enemy);
        }
    }


    public void EnemyDefeated(GameObject enemy)
    {
        if (activeEnemies.Contains(enemy))
        {
            activeEnemies.Remove(enemy);
            enemiesDefeatedThisDay++;
            totalEnemiesKilled++;
            score += 100;
        }
    }

    void ClearAllEnemies()
    {
        foreach (GameObject enemy in activeEnemies)
        {
            if (enemy != null)
            {
                Destroy(enemy);
            }
        }
        activeEnemies.Clear();
    }

    public float GetDayProgress()
    {
        if (enemiesToSpawnThisDay <= 0) return 0f;
        return (float)enemiesDefeatedThisDay / enemiesToSpawnThisDay;
    }


    public int GetEnemiesRemaining()
    {
        return enemiesToSpawnThisDay - enemiesDefeatedThisDay;
    }

    public bool IsPlaying()
    {
        return currentState == GameState.Playing;
    }
}
