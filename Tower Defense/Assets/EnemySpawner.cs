using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class EnemySpawner : MonoBehaviour
{
    [System.Serializable]
    public class SubWaveEnemy
    {
        public GameObject enemyPrefab;
        public int count = 1;
        public float spawnRate = 1f;
        public bool spawnAllAtOnce = false;
    }

    [System.Serializable]
    public class SubWave
    {
        public string subWaveName = "SubWave";
        public List<SubWaveEnemy> enemies = new List<SubWaveEnemy>();
    }

    [System.Serializable]
    public class Wave
    {
        public string waveName = "Wave";
        public List<SubWave> subWaves = new List<SubWave>();
    }

    [Header("Waves")]
    public List<Wave> waves = new List<Wave>();

    [Header("Spawner Settings")]
    public Transform spawnPoint;
    public GameManager gameManager;
    public float timeBetweenWaves = 5f;

    [Header("Level Settings")]
    public string nextLevelSceneName; // Next scene after all waves

    private int currentWaveIndex = 0;
    private bool isSpawning = false;

    void Start()
    {
        if (gameManager == null)
            gameManager = FindObjectOfType<GameManager>();

        StartCoroutine(SpawnAllWaves());
    }

    public void ResetWaves()
    {
        StopAllCoroutines();
        currentWaveIndex = 0;

        // Destroy all existing enemies
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (var e in enemies)
            Destroy(e);

        StartCoroutine(SpawnAllWaves());
    }

    IEnumerator SpawnAllWaves()
    {
        yield return new WaitForSeconds(2f);

        while (currentWaveIndex < waves.Count)
        {
            if (gameManager != null && gameManager.isGameOver)
            {
                yield break; // Stop spawning if game is over
            }

            Wave wave = waves[currentWaveIndex];

            if (gameManager != null)
                gameManager.UpdateWaveUI(currentWaveIndex + 1);

            Debug.Log("Starting " + wave.waveName);

            yield return StartCoroutine(SpawnWave(wave));

            Debug.Log(wave.waveName + " complete!");
            yield return new WaitForSeconds(timeBetweenWaves);

            currentWaveIndex++;
        }

        Debug.Log("All waves complete!");

        // Notify GameManager that level is complete
        if (gameManager != null)
            gameManager.LevelComplete();
    }

    IEnumerator SpawnWave(Wave wave)
    {
        isSpawning = true;

        foreach (SubWave subWave in wave.subWaves)
        {
            if (gameManager != null && gameManager.isGameOver)
            {
                yield break; // Stop spawning if game is over
            }

            Debug.Log("Starting subwave: " + subWave.subWaveName);

            foreach (SubWaveEnemy subEnemy in subWave.enemies)
            {
                if (subEnemy.enemyPrefab == null)
                {
                    Debug.LogWarning("Enemy prefab missing in subwave: " + subWave.subWaveName);
                    continue;
                }

                if (subEnemy.spawnAllAtOnce)
                {
                    for (int i = 0; i < subEnemy.count; i++)
                    {
                        Instantiate(subEnemy.enemyPrefab, spawnPoint.position, spawnPoint.rotation);
                    }
                }
                else
                {
                    for (int i = 0; i < subEnemy.count; i++)
                    {
                        Instantiate(subEnemy.enemyPrefab, spawnPoint.position, spawnPoint.rotation);
                        yield return new WaitForSeconds(1f / subEnemy.spawnRate);
                    }
                }
            }

            // Wait until all enemies are destroyed before next subwave
            yield return new WaitUntil(() =>
                GameObject.FindGameObjectsWithTag("Enemy").Length == 0 ||
                (gameManager != null && gameManager.isGameOver)
            );

            Debug.Log("Subwave complete: " + subWave.subWaveName);
        }

        isSpawning = false;
    }
}
