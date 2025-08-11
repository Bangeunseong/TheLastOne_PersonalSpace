using UnityEngine;

namespace _1.Scripts.Entity.Scripts.Player.Core
{
    public enum GroundType
    {
        Steel,
        Grass,
    }
    
    public class PlayerGravity : MonoBehaviour
    {
        [field: Header("Current GroundType")]
        [field: SerializeField] public GroundType CurrentGroundType { get; private set; } = GroundType.Steel;
        
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

            if (!IsGrounded) return;
            if (!Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, 0.5f, GroundLayers)) return;
            if (hit.collider.CompareTag("Steel")) CurrentGroundType = GroundType.Steel;
            else if (hit.collider.CompareTag("Grass")) CurrentGroundType = GroundType.Grass;
            else CurrentGroundType = GroundType.Steel;
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