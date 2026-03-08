using UnityEngine;
using System;

public class Player : MonoBehaviour
{
    public static event Action<Vector3, float> OnQuack;

    public float moveSpeed = 5f;
    public float rotateSpeed = 90f;
    public float sizePerPoint = 0.01f;
    public int baseMaxEdibleValue = 10;
    public GameObject quackPrefab;
    public float quackPanicRadius = 10f;
    public Animator animator;

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

        if (animator != null)
        {
            float moveValue = Mathf.Approximately(vertical, 0f) ? 0f : Mathf.Sign(vertical);
            animator.SetFloat("Move", moveValue);
        }

        if (Input.GetKeyDown(KeyCode.Space) && quackPrefab != null && Time.time >= lastQuackTime + quackCooldown)
        {
            GameObject quack = Instantiate(quackPrefab, transform.position, transform.rotation);
            quack.transform.localScale *= transform.localScale.x / baseScale;
            lastQuackTime = Time.time;

            if (animator != null)
                animator.SetTrigger("Attack");

            float sizeRatio = transform.localScale.x / baseScale;
            OnQuack?.Invoke(transform.position, quackPanicRadius * sizeRatio);

            TryEatInFront();
        }
    }

    void TryEatInFront()
    {
        float radius = col.bounds.extents.x;
        float distance = col.bounds.extents.z + 2f;
        RaycastHit[] hits = Physics.SphereCastAll(transform.position, radius, transform.forward, distance);
        foreach (RaycastHit hit in hits)
        {
            // Skip anything behind or to the side
            Vector3 toHit = (hit.collider.bounds.center - transform.position).normalized;
            if (Vector3.Dot(toHit, transform.forward) <= 0f)
                continue;

            Food food = hit.collider.GetComponent<Food>();
            if (food != null && CanEat(food))
            {
                AddScore(food.foodValue);
                Destroy(hit.collider.gameObject);
            }
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
        moveSpeed = baseMoveSpeed / sizeRatio;
    }
}
