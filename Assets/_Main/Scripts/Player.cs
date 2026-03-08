using UnityEngine;

public class Player : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float rotateSpeed = 90f;
    public float jumpForce = 4.5f;
    public float sizePerPoint = 0.01f;
    public int baseMaxEdibleValue = 10;

    private Rigidbody rb;
    private Collider col;
    private bool isGrounded;
    private float baseMoveSpeed;
    private float baseJumpForce;
    private float baseScale;
    private int score;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
        baseMoveSpeed = moveSpeed;
        baseJumpForce = jumpForce;
        baseScale = transform.localScale.x;
    }

    void Update()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        transform.Rotate(0f, horizontal * rotateSpeed * Time.deltaTime, 0f);
        transform.Translate(0f, 0f, vertical * moveSpeed * Time.deltaTime);

        isGrounded = Physics.Raycast(col.bounds.center, Vector3.down, col.bounds.extents.y + 0.05f);

        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
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
        jumpForce = baseJumpForce * Mathf.Sqrt(sizeRatio);
    }
}
