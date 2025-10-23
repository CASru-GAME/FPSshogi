using UnityEngine;
using UnityEngine.InputSystem;

namespace App.Main.Player
{
    public class Player : MonoBehaviour
    {
        private GameObject player;
        private float moveSpeed = 5f;
        private Vector2 moveVelocity = Vector2.zero;
        private Vector3 moveInput = Vector3.zero;
        private Rigidbody rb;

        public void Start()
        {
            Initialize();
        }

        public void Initialize()
        {
            player = gameObject;
            rb = player.GetComponent<Rigidbody>();
            player.GetComponent<PlayerInput>().onActionTriggered += SetMoveVelocity;
        }

        public void FixedUpdate()
        {
            moveInput = new Vector3(moveVelocity.x * moveSpeed, rb.linearVelocity.y, moveVelocity.y * moveSpeed);
            rb.linearVelocity = moveInput;
        }

        public void SetMoveVelocity(InputAction.CallbackContext context)
        {
            if (context.action.name != "Move") return;
            moveVelocity = context.ReadValue<Vector2>();
            Debug.Log("Move Velocity: " + moveVelocity);
        }
    }
}
