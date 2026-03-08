using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Player : MonoBehaviour
{
    public static event Action<Vector3, float> OnQuack;

    public float moveSpeed = 5f;
    public float rotateSpeed = 150f;
    public float sizePerPoint = 0.001f;
    public float speedIncreaseRate = 1f;
    public int baseMaxEdibleValue = 10;
    public GameObject quackPrefab;
    public float quackPanicRadius = 10f;
    public Animator animator;

    public AudioClip[] quackSounds;

    public float scaleSmoothing = 5f;
    public GameObject explosionPrefab;
    public Game game;

    [Header("Quack Meter")]
    public float quackMeterMax = 3f;
    public int pressesToFill = 5;
    public float quackMeterDecayRate = 10f;

    public float QuackMeterNormalized => quackMeterMax > 0f ? quackMeter / quackMeterMax : 0f;

    private Collider col;
    private AudioSource audioSource;
    private float baseMoveSpeed;
    private float baseScale;
    private float targetScale;
    private int score;
    private float quackMeter;
    private bool _won;

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
        if (_won)
        {
            if (Input.GetKeyDown(KeyCode.Space))
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            return;
        }

        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        float currentScale = Mathf.Lerp(transform.localScale.x, targetScale, scaleSmoothing * Time.deltaTime);
        transform.localScale = Vector3.one * currentScale;

        transform.Rotate(0f, horizontal * rotateSpeed * Time.deltaTime, 0f);
        transform.Translate(0f, 0f, vertical * moveSpeed * Time.deltaTime);

        if (animator != null)
        {
            float moveValue = Mathf.Approximately(vertical, 0f) ? 0f : Mathf.Sign(vertical);
            animator.SetFloat("Move", moveValue);
        }

        if (quackMeter > 0f)
            quackMeter = Mathf.Max(0f, quackMeter - quackMeterDecayRate * Time.deltaTime);

        if (Input.GetKeyDown(KeyCode.Space) && quackPrefab != null)
        {
            GameObject quack = Instantiate(quackPrefab, transform.position, transform.rotation);
            quack.transform.localScale *= transform.localScale.x / baseScale;

            if (animator != null)
                animator.SetTrigger("Attack");

            float sizeRatio = transform.localScale.x / baseScale;
            OnQuack?.Invoke(transform.position, quackPanicRadius * sizeRatio);
            if (quackSounds != null && quackSounds.Length > 0)
            {
                AudioClip clip = quackSounds[UnityEngine.Random.Range(0, quackSounds.Length)];
                audioSource.PlayOneShot(clip);
            }
            TryEatInFront();

            quackMeter += quackMeterMax / pressesToFill;
            if (quackMeter >= quackMeterMax)
                Explode();
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
                AddScore(food.foodValue / 2);
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

        if (!_won && targetScale >= 1000f)
        {
            _won = true;
            game.Win();
        }
    }

    public void TakeDamage(int amount)
    {
        score = Mathf.Max(0, score - amount);
        UpdateSize();
        Debug.Log($"Player hit! Score: {score}");
    }

    void Explode()
    {
        if (explosionPrefab != null)
            Instantiate(explosionPrefab, transform.position, Quaternion.identity);

        score = 0;
        quackMeter = 0f;
        targetScale = baseScale;
        moveSpeed = baseMoveSpeed;
        Debug.Log("EXPLODED! Size reset.");
    }
}
