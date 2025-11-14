using UnityEngine;             // Core Unity functionality
using System.Collections;       // For IEnumerator and coroutines
using System.Collections.Generic; // For List<T>
using UnityEngine.SceneManagement; // For scene loading

public class EnemySpawner : MonoBehaviour
{
    // Data class for individual enemy in a subwave
    [System.Serializable]
    public class SubWaveEnemy
    {
        public GameObject enemyPrefab;   // Prefab to spawn
        public int count = 1;            // Number of enemies to spawn
        public float spawnRate = 1f;     // Number of enemies per second
        public bool spawnAllAtOnce = false; // If true, spawn all at once
    }

    // Data class for a subwave
    [System.Serializable]
    public class SubWave
    {
        public string subWaveName = "SubWave"; // Name of the subwave
        public List<SubWaveEnemy> enemies = new List<SubWaveEnemy>(); // Enemies in the subwave
    }

    // Data class for a wave
    [System.Serializable]
    public class Wave
    {
        public string waveName = "Wave";        // Name of the wave
        public List<SubWave> subWaves = new List<SubWave>(); // Subwaves in the wave
    }

    [Header("Waves")]
    public List<Wave> waves = new List<Wave>(); // All waves in the level

    [Header("Spawner Settings")]
    public Transform spawnPoint;       // Where enemies spawn
    public GameManager gameManager;    // Reference to GameManager
    public float timeBetweenWaves = 5f; // Delay between waves

    [Header("Level Settings")]
    public string nextLevelSceneName;  // Next scene after all waves

    private int currentWaveIndex = 0;  // Track current wave index
    private bool isSpawning = false;   // Flag if currently spawning

    void Start()
    {
        // If no GameManager assigned, find one in scene
        if (gameManager == null)
            gameManager = FindObjectOfType<GameManager>();

        StartCoroutine(SpawnAllWaves()); // Start spawning waves
    }

    // Reset all waves and start over (used when game restarts)
    public void ResetWaves()
    {
        StopAllCoroutines(); // Stop any ongoing spawning
        currentWaveIndex = 0;

        // Destroy all existing enemies
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (var e in enemies)
            Destroy(e);

        StartCoroutine(SpawnAllWaves()); // Start waves again
    }

    // Coroutine to spawn all waves sequentially
    IEnumerator SpawnAllWaves()
    {
        yield return new WaitForSeconds(2f); // Initial delay before first wave

        while (currentWaveIndex < waves.Count)
        {
            // Stop if game is over
            if (gameManager != null && gameManager.isGameOver)
                yield break;

            Wave wave = waves[currentWaveIndex]; // Get current wave

            // Update UI with current wave number
            if (gameManager != null)
                gameManager.UpdateWaveUI(currentWaveIndex + 1);

            Debug.Log("Starting " + wave.waveName); // Log start

            yield return StartCoroutine(SpawnWave(wave)); // Spawn all subwaves

            Debug.Log(wave.waveName + " complete!"); // Log completion
            yield return new WaitForSeconds(timeBetweenWaves); // Wait before next wave

            currentWaveIndex++; // Move to next wave
        }

        Debug.Log("All waves complete!"); // All waves done

        // Notify GameManager that level is complete
        if (gameManager != null)
            gameManager.LevelComplete();
    }

    // Coroutine to spawn all subwaves and enemies in a wave
    IEnumerator SpawnWave(Wave wave)
    {
        isSpawning = true; // Mark spawning in progress

        foreach (SubWave subWave in wave.subWaves)
        {
            // Stop spawning if game is over
            if (gameManager != null && gameManager.isGameOver)
                yield break;

            Debug.Log("Starting subwave: " + subWave.subWaveName);

            foreach (SubWaveEnemy subEnemy in subWave.enemies)
            {
                if (subEnemy.enemyPrefab == null)
                {
                    Debug.LogWarning("Enemy prefab missing in subwave: " + subWave.subWaveName);
                    continue;
                }

                if (subEnemy.spawnAllAtOnce) // Spawn all at once
                {
                    for (int i = 0; i < subEnemy.count; i++)
                        Instantiate(subEnemy.enemyPrefab, spawnPoint.position, spawnPoint.rotation);
                }
                else // Spawn over time
                {
                    for (int i = 0; i < subEnemy.count; i++)
                    {
                        Instantiate(subEnemy.enemyPrefab, spawnPoint.position, spawnPoint.rotation);
                        yield return new WaitForSeconds(1f / subEnemy.spawnRate); // Delay between spawns
                    }
                }
            }

            // Wait until all enemies are destroyed or game is over before next subwave
            yield return new WaitUntil(() =>
                GameObject.FindGameObjectsWithTag("Enemy").Length == 0 ||
                (gameManager != null && gameManager.isGameOver)
            );

            Debug.Log("Subwave complete: " + subWave.subWaveName); // Log completion
        }

        isSpawning = false; // Done spawning this wave
    }
}
