using UnityEngine;

public class Food : MonoBehaviour
{
    public int foodValue = 10;
    public GameObject eatVFXPrefab;

    void OnDestroy()
    {
        if (eatVFXPrefab != null)
            Instantiate(eatVFXPrefab, transform.position, transform.rotation);
    }
}
