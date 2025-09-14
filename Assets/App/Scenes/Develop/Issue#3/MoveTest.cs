
using UnityEngine;
using UnityEngine.InputSystem;
// Rigidbody必須
[RequireComponent(typeof(Rigidbody))]


public class MoveTest : MonoBehaviour
{
    private Rigidbody rb;


    private bool _isInputLocked = false;



    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    // FixedUpdateで物理演算に合わせて移動
    void FixedUpdate()
    {
        Vector3 move = Vector3.zero;
    float speed = 15.0f; // 移動速度（調整可）
        if (!_isInputLocked && Keyboard.current.rightArrowKey.isPressed)
        {
            move += new Vector3(1, 0, 0);
        }
        if (!_isInputLocked && Keyboard.current.leftArrowKey.isPressed)
        {
            move += new Vector3(-1, 0, 0);
        }
        if (!_isInputLocked && Keyboard.current.upArrowKey.isPressed)
        {
            move += new Vector3(0, 1, 0);
        }
        if (!_isInputLocked && Keyboard.current.downArrowKey.isPressed)
        {
            move += new Vector3(0, -1, 0);
        }
        if (move != Vector3.zero)
        {
            move = move.normalized * speed * Time.fixedDeltaTime;
            rb.MovePosition(rb.position + move);
        }
    }
}
