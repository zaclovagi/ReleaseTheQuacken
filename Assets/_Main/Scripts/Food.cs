using UnityEngine;

public class Food : MonoBehaviour
{
    public int foodValue = 10;
    public GameObject eatVFXPrefab;

    void OnDestroy()
    {
        if (eatVFXPrefab != null && Application.isPlaying)
            Instantiate(eatVFXPrefab, transform.position, transform.rotation);
    }
}
