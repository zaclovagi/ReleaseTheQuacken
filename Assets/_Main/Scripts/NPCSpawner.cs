using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Spawns pedestrian NPCs in a ring around the player as they explore.
/// NPCs too far from the player are returned to the pool.
/// AllNPCs is a global list of every currently live NPC.
/// </summary>
public class NPCSpawner : MonoBehaviour
{
    // Global list of every currently live NPC.
    public static List<GameObject> AllNPCs { get; private set; } = new List<GameObject>();

    [Header("NPC Definitions")]
    public GameObject[] pedestrianPrefabs;

    [Header("Limits")]
    [Tooltip("Maximum total NPCs alive at once across all types.")]
    public int maxNPCs = 20;

    [Header("Spawn Range")]
    [Tooltip("Minimum distance from player to spawn NPCs.")]
    public float spawnRangeMin = 20f;
    [Tooltip("NPCs beyond this distance from the player are despawned.")]
    public float despawnRange = 40f;
    [Tooltip("How many spawn attempts per interval.")]
    public int spawnAttemptsPerTick = 3;

    [Header("Timing")]
    public float spawnInterval = 1.5f;

    // Pool: maps prefab -> inactive instances
    private Dictionary<GameObject, Queue<GameObject>> pool = new Dictionary<GameObject, Queue<GameObject>>();
    // All active instances
    private List<GameObject> active = new List<GameObject>();

    private Transform player;
    private float spawnTimer;

    void Start()
    {
        player = GameObject.FindWithTag("Player")?.transform;

        foreach (var prefab in pedestrianPrefabs)
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
        if (pedestrianPrefabs == null || pedestrianPrefabs.Length == 0) return;

        active.RemoveAll(o => o == null);

        if (AllNPCs.Count >= maxNPCs) return;

        for (int i = 0; i < spawnAttemptsPerTick; i++)
        {
            if (AllNPCs.Count >= maxNPCs) break;

            if (!TryGetSpawnPosition(out Vector3 spawnPos)) continue;

            GameObject prefab = pedestrianPrefabs[Random.Range(0, pedestrianPrefabs.Length)];
            if (prefab == null) continue;

            Quaternion rot = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            GameObject npc = GetFromPool(prefab, spawnPos, rot);

            active.Add(npc);
            AllNPCs.Add(npc);

            NPCSpawnedObject tracker = npc.GetComponent<NPCSpawnedObject>();
            if (tracker == null)
                tracker = npc.AddComponent<NPCSpawnedObject>();
            tracker.Init(this, prefab, npc);
        }
    }

    bool TryGetSpawnPosition(out Vector3 pos)
    {
        // When sidewalk spawn points are present, only spawn there.
        if (SidewalkSpawnPoint.All.Count > 0)
        {
            // Collect points within the spawn ring around the player.
            var candidates = new System.Collections.Generic.List<SidewalkSpawnPoint>();
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

            // No sidewalk points are in range — don't fall back to random positions.
            pos = Vector3.zero;
            return false;
        }

        // Fallback: random raycast (used before any sidewalk points are placed in the scene).
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
            GameObject npc = active[i];
            if (npc == null) { active.RemoveAt(i); continue; }

            if (Vector3.Distance(npc.transform.position, player.position) > despawnRange)
            {
                AllNPCs.Remove(npc);
                NPCSpawnedObject tracker = npc.GetComponent<NPCSpawnedObject>();
                ReturnToPool(tracker != null ? tracker.Prefab : npc, npc);
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

    void ReturnToPool(GameObject prefab, GameObject instance)
    {
        instance.SetActive(false);
        if (!pool.ContainsKey(prefab))
            pool[prefab] = new Queue<GameObject>();
        pool[prefab].Enqueue(instance);
    }

    public void NotifyDestroyed(GameObject prefab, GameObject npc)
    {
        active.Remove(npc);
        AllNPCs.Remove(npc);
    }

    // -------------------------------------------------------------------------
    // Editor helpers
    // -------------------------------------------------------------------------

    void OnDrawGizmos()
    {
        if (player == null) return;
        Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
        Gizmos.DrawWireSphere(player.position, spawnRangeMin);
        Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
        Gizmos.DrawWireSphere(player.position, despawnRange);
    }
}
