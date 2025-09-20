using UnityEngine;

namespace App.Main.Player
{
    public class Player : MonoBehaviour
    {
        private GameObject player;
        private float moveSpeed = 5f;
        private Vector2 moveVelocity = Vector2.zero;
        private Vector3 moveInput = Vector3.zero;
        private Rigidbody rb;
        void Start()
        {
            player = gameObject;
            rb = player.GetComponent<Rigidbody>();
        }
        void FixedUpdate()
        {
            moveInput = new Vector3(moveVelocity.x * moveSpeed, rb.linearVelocity.y, moveVelocity.y * moveSpeed);
            rb.linearVelocity = moveInput;
        }

        public void SetMoveVelocity(Vector2 velocity)
        {
            moveVelocity = velocity;
        }
    }
}
