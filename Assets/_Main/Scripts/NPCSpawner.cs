using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Spawns and pools pedestrian and vehicle NPCs at designated spawn points.
/// NPCs that wander too far from their spawn point are returned to the pool
/// and re-spawned, keeping the city area always populated.
/// </summary>
public class NPCSpawner : MonoBehaviour
{
    [System.Serializable]
    public class SpawnEntry
    {
        public GameObject prefab;
        [Tooltip("Points where this NPC type can appear. At least one required.")]
        public Transform[] spawnPoints;
        [Tooltip("Max simultaneous instances of this prefab.")]
        public int maxCount = 5;
        [Tooltip("How far from its spawn point before an NPC is recycled.")]
        public float recycleDistance = 40f;
    }

    [Header("NPC Definitions")]
    public SpawnEntry[] pedestrians;

    [Header("Timing")]
    public float spawnInterval = 2f;

    // Pool: maps prefab -> inactive instances
    private Dictionary<GameObject, Queue<GameObject>> pool = new();
    // Active instances per SpawnEntry
    private Dictionary<SpawnEntry, List<GameObject>> active = new();

    private float spawnTimer;

    void Start()
    {
        // Pre-warm pools for all entries
        foreach (var entry in pedestrians) RegisterEntry(entry);
    }

    void Update()
    {
        spawnTimer += Time.deltaTime;
        if (spawnTimer < spawnInterval) return;
        spawnTimer = 0f;

        foreach (var entry in pedestrians) TryPopulate(entry);

        RecycleDistant();
    }

    // -------------------------------------------------------------------------
    // Setup
    // -------------------------------------------------------------------------

    void RegisterEntry(SpawnEntry entry)
    {
        if (entry.prefab == null) return;
        if (!pool.ContainsKey(entry.prefab))
            pool[entry.prefab] = new Queue<GameObject>();
        active[entry] = new List<GameObject>();
    }

    // -------------------------------------------------------------------------
    // Spawning
    // -------------------------------------------------------------------------

    void TryPopulate(SpawnEntry entry)
    {
        if (entry.prefab == null) return;
        if (entry.spawnPoints == null || entry.spawnPoints.Length == 0) return;
        if (active[entry].Count >= entry.maxCount) return;

        Transform spawnPoint = entry.spawnPoints[Random.Range(0, entry.spawnPoints.Length)];
        if (spawnPoint == null) return;

        GameObject npc = GetFromPool(entry.prefab, spawnPoint.position, spawnPoint.rotation);
        active[entry].Add(npc);
    }

    GameObject GetFromPool(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        Queue<GameObject> q = pool[prefab];

        GameObject instance;
        if (q.Count > 0)
        {
            instance = q.Dequeue();
            instance.transform.SetPositionAndRotation(position, rotation);
            instance.SetActive(true);
        }
        else
        {
            instance = Instantiate(prefab, position, rotation, transform);
        }

        return instance;
    }

    // -------------------------------------------------------------------------
    // Recycling
    // -------------------------------------------------------------------------

    void RecycleDistant()
    {
        foreach (var entry in pedestrians) RecycleEntry(entry);
    }

    void RecycleEntry(SpawnEntry entry)
    {
        if (entry.spawnPoints == null || entry.spawnPoints.Length == 0) return;

        List<GameObject> list = active[entry];
        for (int i = list.Count - 1; i >= 0; i--)
        {
            GameObject npc = list[i];
            if (npc == null) { list.RemoveAt(i); continue; }

            // Check distance from all spawn points; keep alive if near any of them
            float minDist = float.MaxValue;
            foreach (var sp in entry.spawnPoints)
            {
                if (sp == null) continue;
                float d = Vector3.Distance(npc.transform.position, sp.position);
                if (d < minDist) minDist = d;
            }

            if (minDist > entry.recycleDistance)
            {
                ReturnToPool(entry.prefab, npc);
                list.RemoveAt(i);
            }
        }
    }

    void ReturnToPool(GameObject prefab, GameObject instance)
    {
        instance.SetActive(false);
        pool[prefab].Enqueue(instance);
    }

    // -------------------------------------------------------------------------
    // Editor helpers
    // -------------------------------------------------------------------------

    void OnDrawGizmos()
    {
        DrawEntryGizmos(pedestrians, Color.green);
    }

    void DrawEntryGizmos(SpawnEntry[] entries, Color color)
    {
        if (entries == null) return;
        Gizmos.color = color;
        foreach (var entry in entries)
        {
            if (entry?.spawnPoints == null) continue;
            foreach (var sp in entry.spawnPoints)
            {
                if (sp != null)
                    Gizmos.DrawWireSphere(sp.position, 1f);
            }
        }
    }
}
