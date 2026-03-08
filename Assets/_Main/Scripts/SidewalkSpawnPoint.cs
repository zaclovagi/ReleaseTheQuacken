using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Marks a position on a sidewalk prefab as a valid NPC spawn point.
/// Add this component to child GameObjects on your sidewalk prefabs,
/// then assign them in the scene. NPCSpawner will only spawn pedestrians
/// at these locations when any are present.
/// </summary>
public class SidewalkSpawnPoint : MonoBehaviour
{
    /// <summary>All currently active spawn points across all sidewalk instances.</summary>
    public static readonly List<SidewalkSpawnPoint> All = new List<SidewalkSpawnPoint>();

    void OnEnable()  => All.Add(this);
    void OnDisable() => All.Remove(this);

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        Gizmos.color = new Color(0f, 1f, 0.4f, 0.8f);
        Gizmos.DrawSphere(transform.position, 0.25f);
        Gizmos.DrawLine(transform.position, transform.position + Vector3.up * 1f);
    }
#endif
}
