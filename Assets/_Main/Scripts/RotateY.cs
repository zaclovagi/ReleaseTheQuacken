using UnityEngine;

public class RotateY : MonoBehaviour
{
    public float speed = 45f;

    void Update()
    {
        transform.Rotate(0f, speed * Time.deltaTime, 0f);
    }
}
