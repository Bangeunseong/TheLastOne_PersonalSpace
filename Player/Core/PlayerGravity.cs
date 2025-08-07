using UnityEngine;

namespace _1.Scripts.Entity.Scripts.Player.Core
{
    public class PlayerGravity : MonoBehaviour
    {
        [field: Header("Gravity Settings")]
        [field: SerializeField] public float Gravity { get; private set; } = -9.81f;
        [field: SerializeField] public float GroundedOffset { get; private set; } = -0.24f;
        [field: SerializeField] public float GroundedRadius { get; private set; } = 0.3f;
        [field: SerializeField] public LayerMask GroundLayers { get; private set; }
        [field: SerializeField] public bool IsGrounded { get; private set; } = true;
        
        private float verticalVelocity;
        
        public Vector3 ExtraMovement => Vector3.up * verticalVelocity;

        private void Update()
        {
            CheckCharacterIsGrounded();
            if (IsGrounded && verticalVelocity < 0f) verticalVelocity = -2f;
            else verticalVelocity += Gravity * Time.unscaledDeltaTime;
        }

        private void CheckCharacterIsGrounded()
        {
            Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - GroundedOffset,
                transform.position.z);
            IsGrounded = Physics.CheckSphere(spherePosition, GroundedRadius, GroundLayers,
                QueryTriggerInteraction.Ignore);
        }
        
        public void Jump(float jumpForce)
        {
            verticalVelocity = Mathf.Sqrt(jumpForce * -2f * Gravity);
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position - transform.up * GroundedOffset, GroundedRadius);
        }
    }
}