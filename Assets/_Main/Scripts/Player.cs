using UnityEngine;

public class Player : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float rotateSpeed = 150f;
    public float sizePerPoint = 0.001f;
    public float speedIncreaseRate = 1f;
    public int baseMaxEdibleValue = 10;
    public GameObject quackPrefab;
    public AudioClip[] quackSounds;

    public float scaleSmoothing = 5f;

    private Collider col;
    private AudioSource audioSource;
    private float baseMoveSpeed;
    private float baseScale;
    private float targetScale;
    private int score;
    private readonly float quackCooldown = 1f;
    private float lastQuackTime = -Mathf.Infinity;

    void Start()
    {
        col = GetComponent<Collider>();
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.spatialBlend = 0f;
        baseMoveSpeed = moveSpeed;
        baseScale = transform.localScale.x;
        targetScale = baseScale;
    }

    void Update()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        float currentScale = Mathf.Lerp(transform.localScale.x, targetScale, scaleSmoothing * Time.deltaTime);
        transform.localScale = Vector3.one * currentScale;

        transform.Rotate(0f, horizontal * rotateSpeed * Time.deltaTime, 0f);
        transform.Translate(0f, 0f, vertical * moveSpeed * Time.deltaTime);

        if (Input.GetKeyDown(KeyCode.Space) && quackPrefab != null && Time.time >= lastQuackTime + quackCooldown)
        {
            GameObject quack = Instantiate(quackPrefab, transform.position, transform.rotation);
            quack.transform.localScale *= transform.localScale.x / baseScale;
            lastQuackTime = Time.time;
            if (quackSounds != null && quackSounds.Length > 0)
                audioSource.PlayOneShot(quackSounds[Random.Range(0, quackSounds.Length)]);
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
        targetScale = baseScale * sizeRatio;
        moveSpeed = baseMoveSpeed * (1f + (sizeRatio - 1f) * speedIncreaseRate);
    }
}
