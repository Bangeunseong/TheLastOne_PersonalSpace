using _1.Scripts.Interfaces.Weapon;
using _1.Scripts.Weapon.Scripts.Common;
using UnityEngine;

namespace _1.Scripts.Weapon.Scripts.WeaponDetails
{
    public class WeaponPart : MonoBehaviour, IWearable
    {
        [field: Header("WeaponPart Data")]
        [field: SerializeField] public WeaponPartData Data { get; private set; }
        [field: SerializeField] public bool IsWorn { get; private set; }
        
        [field: Header("Components")]
        [field: SerializeField] public BaseWeapon ParentWeapon { get; private set; }
        [field: SerializeField] public Transform Parent { get; private set; }
        [field: SerializeField] public Transform IronSight_A { get; private set; }
        [field: SerializeField] public Transform IronSight_B { get; private set; }
        
        private void Awake()
        {
            if (!ParentWeapon) ParentWeapon = GetComponentInParent<BaseWeapon>(true);
            if (!Parent) Parent = this.TryGetComponent<Transform>();
        }

        private void Reset()
        {
            if (!ParentWeapon) ParentWeapon = GetComponentInParent<BaseWeapon>(true);
            if (!Parent) Parent = this.TryGetComponent<Transform>();
        }

        public void OnWear()
        {
            if (IsWorn) return;
            if (IronSight_A && IronSight_B)
            {
                var rotationOfA = IronSight_A.localRotation.eulerAngles;
                var rotationOfB = IronSight_B.localRotation.eulerAngles;
                Service.Log($"{rotationOfA}, {rotationOfB}");
                IronSight_A.localRotation = Quaternion.Euler(rotationOfA.x, rotationOfA.y, rotationOfA.z - 90f);
                IronSight_B.localRotation = Quaternion.Euler(rotationOfB.x, rotationOfB.y, rotationOfB.z + 90f);
            }
            else Parent.gameObject.SetActive(true);

            ParentWeapon.UpdateStatValues(this);
            IsWorn = true;
        }

        public void OnUnWear()
        {
            if (!IsWorn) return;
            if (IronSight_A && IronSight_B)
            {
                var rotationZofA = IronSight_A.localRotation.z;
                var rotationZofB = IronSight_B.localRotation.z;
                IronSight_A.localRotation = Quaternion.Euler(IronSight_A.localRotation.x, IronSight_A.localRotation.y, rotationZofA + 90f);
                IronSight_B.localRotation = Quaternion.Euler(IronSight_B.localRotation.x, IronSight_B.localRotation.y, rotationZofB - 90f);
            }
            else
            {
                Parent.gameObject.SetActive(false);
            }
            
            ParentWeapon.UpdateStatValues(this, false);
            IsWorn = false;
        }
    }
}