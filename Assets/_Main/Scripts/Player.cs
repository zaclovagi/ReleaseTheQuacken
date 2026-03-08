using UnityEngine;

public class Player : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float rotateSpeed = 90f;
    public float jumpForce = 4.5f;
    public float growPercent = 10f;

    private Rigidbody rb;
    private Collider col;
    private bool isGrounded;
    private float baseMoveSpeed;
    private float baseJumpForce;
    private float baseScale;

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

        if (Input.GetKeyDown(KeyCode.T))
        {
            transform.localScale *= 1f + growPercent / 100f;
            float sizeRatio = transform.localScale.x / baseScale;
            moveSpeed = baseMoveSpeed * sizeRatio;
            jumpForce = baseJumpForce * Mathf.Sqrt(sizeRatio);
        }
    }

}
