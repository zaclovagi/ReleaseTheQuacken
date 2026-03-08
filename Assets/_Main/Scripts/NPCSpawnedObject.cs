using UnityEngine;

/// <summary>
/// Attached automatically by NPCSpawner to every live NPC.
/// Removes the NPC from the global list and its spawner when destroyed.
/// </summary>
public class NPCSpawnedObject : MonoBehaviour
{
    public GameObject Prefab { get; private set; }

    private NPCSpawner spawner;
    private GameObject trackedObject;

    public void Init(NPCSpawner spawner, GameObject prefab, GameObject trackedObject)
    {
        this.spawner = spawner;
        this.Prefab = prefab;
        this.trackedObject = trackedObject;
    }

    void OnDestroy()
    {
        if (spawner != null)
            spawner.NotifyDestroyed(Prefab, trackedObject);
        else
            NPCSpawner.AllNPCs.Remove(trackedObject);
    }
}
