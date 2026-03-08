using UnityEngine;

public class Player : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float rotateSpeed = 90f;
    public float sizePerPoint = 0.01f;
    public int baseMaxEdibleValue = 10;
    public GameObject quackPrefab;

    private Collider col;
    private float baseMoveSpeed;
    private float baseScale;
    private int score;
    private readonly float quackCooldown = 1f;
    private float lastQuackTime = -Mathf.Infinity;

    void Start()
    {
        col = GetComponent<Collider>();
        baseMoveSpeed = moveSpeed;
        baseScale = transform.localScale.x;
    }

    void Update()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        transform.Rotate(0f, horizontal * rotateSpeed * Time.deltaTime, 0f);
        transform.Translate(0f, 0f, vertical * moveSpeed * Time.deltaTime);

        if (Input.GetKeyDown(KeyCode.Space) && quackPrefab != null && Time.time >= lastQuackTime + quackCooldown)
        {
            Instantiate(quackPrefab, transform.position, transform.rotation);
            lastQuackTime = Time.time;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Food"))
        {
            Food food = other.GetComponent<Food>();
            if (food == null || !CanEat(food))
                return;

            AddScore(food.foodValue);
            Destroy(other.gameObject);
        }
    }

    public void AddScore(int value)
    {
        score += value;
        UpdateSize();
        Debug.Log($"Score: {score}");
    }

    bool CanEat(Food food)
    {
        float sizeRatio = 1f + score * sizePerPoint;
        return food.foodValue <= baseMaxEdibleValue * sizeRatio;
    }

    void UpdateSize()
    {
        float sizeRatio = 1f + score * sizePerPoint;
        transform.localScale = Vector3.one * baseScale * sizeRatio;
        moveSpeed = baseMoveSpeed * sizeRatio;
    }
}
