using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Spawns food prefabs around the player using SidewalkSpawnPoints (same points as NPCSpawner).
/// Food that wanders out of range is returned to the pool.
/// </summary>
public class FoodSpawner : MonoBehaviour
{
    [Header("Food Definitions")]
    public GameObject[] foodPrefabs;

    [Header("Limits")]
    [Tooltip("Maximum total food items alive at once.")]
    public int maxFood = 30;

    [Header("Spawn Range")]
    [Tooltip("Minimum distance from player to spawn food.")]
    public float spawnRangeMin = 10f;
    [Tooltip("Food beyond this distance from the player is despawned.")]
    public float despawnRange = 45f;
    [Tooltip("How many spawn attempts per interval.")]
    public int spawnAttemptsPerTick = 3;

    [Header("Timing")]
    public float spawnInterval = 2f;

    // Pool: maps prefab -> inactive instances
    private Dictionary<GameObject, Queue<GameObject>> pool = new Dictionary<GameObject, Queue<GameObject>>();
    private List<GameObject> active = new List<GameObject>();

    private Transform player;
    private float spawnTimer;

    void Start()
    {
        player = GameObject.FindWithTag("Player")?.transform;

        foreach (var prefab in foodPrefabs)
        {
            if (prefab == null) continue;
            if (!pool.ContainsKey(prefab))
                pool[prefab] = new Queue<GameObject>();
        }
    }

    void Update()
    {
        if (player == null) return;

        spawnTimer += Time.deltaTime;
        if (spawnTimer >= spawnInterval)
        {
            spawnTimer = 0f;
            TryPopulate();
        }

        DespawnDistant();
    }

    // -------------------------------------------------------------------------
    // Spawning
    // -------------------------------------------------------------------------

    void TryPopulate()
    {
        if (foodPrefabs == null || foodPrefabs.Length == 0) return;

        active.RemoveAll(o => o == null);

        if (active.Count >= maxFood) return;

        for (int i = 0; i < spawnAttemptsPerTick; i++)
        {
            if (active.Count >= maxFood) break;

            if (!TryGetSpawnPosition(out Vector3 spawnPos)) continue;

            GameObject prefab = foodPrefabs[Random.Range(0, foodPrefabs.Length)];
            if (prefab == null) continue;

            Quaternion rot = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            GameObject food = GetFromPool(prefab, spawnPos, rot);
            active.Add(food);
        }
    }

    bool TryGetSpawnPosition(out Vector3 pos)
    {
        if (SidewalkSpawnPoint.All.Count > 0)
        {
            var candidates = new List<SidewalkSpawnPoint>();
            foreach (var sp in SidewalkSpawnPoint.All)
            {
                if (sp == null) continue;
                float d = Vector3.Distance(sp.transform.position, player.position);
                if (d >= spawnRangeMin && d <= despawnRange * 0.8f)
                    candidates.Add(sp);
            }

            if (candidates.Count > 0)
            {
                pos = candidates[Random.Range(0, candidates.Count)].transform.position;
                return true;
            }

            pos = Vector3.zero;
            return false;
        }

        // Fallback: random raycast when no sidewalk points exist.
        float angle = Random.Range(0f, Mathf.PI * 2f);
        float dist  = Random.Range(spawnRangeMin, despawnRange * 0.8f);
        Vector3 candidate = player.position + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * dist;

        if (Physics.Raycast(candidate + Vector3.up * 5f, Vector3.down, out RaycastHit hit, 20f))
        {
            pos = hit.point;
            return true;
        }

        pos = Vector3.zero;
        return false;
    }

    // -------------------------------------------------------------------------
    // Despawning
    // -------------------------------------------------------------------------

    void DespawnDistant()
    {
        for (int i = active.Count - 1; i >= 0; i--)
        {
            GameObject food = active[i];
            if (food == null) { active.RemoveAt(i); continue; }

            if (Vector3.Distance(food.transform.position, player.position) > despawnRange)
            {
                ReturnToPool(food);
                active.RemoveAt(i);
            }
        }
    }

    // -------------------------------------------------------------------------
    // Pool helpers
    // -------------------------------------------------------------------------

    GameObject GetFromPool(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (pool.ContainsKey(prefab) && pool[prefab].Count > 0)
        {
            GameObject instance = pool[prefab].Dequeue();
            instance.transform.SetPositionAndRotation(position, rotation);
            instance.SetActive(true);
            return instance;
        }
        return Instantiate(prefab, position, rotation, transform);
    }

    void ReturnToPool(GameObject instance)
    {
        // Find which prefab this belongs to by matching name (strip "(Clone)")
        string baseName = instance.name.Replace("(Clone)", "").Trim();
        foreach (var prefab in foodPrefabs)
        {
            if (prefab != null && prefab.name == baseName)
            {
                instance.SetActive(false);
                if (!pool.ContainsKey(prefab))
                    pool[prefab] = new Queue<GameObject>();
                pool[prefab].Enqueue(instance);
                return;
            }
        }
        // Fallback: just destroy if we can't match the prefab
        Destroy(instance);
    }

    // -------------------------------------------------------------------------
    // Editor helpers
    // -------------------------------------------------------------------------

    void OnDrawGizmos()
    {
        if (player == null) return;
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
        Gizmos.DrawWireSphere(player.position, spawnRangeMin);
        Gizmos.color = new Color(1f, 0f, 0f, 0.15f);
        Gizmos.DrawWireSphere(player.position, despawnRange);
    }
}
