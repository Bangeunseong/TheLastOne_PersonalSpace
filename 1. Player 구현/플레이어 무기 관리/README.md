# 🧐 Weapon 및 WeaponParts 구현방식

<br/>

# 📁 폴더 구조

```
Weapon/                        # Weapon 관련 코드 모음
├── Common/                    # 모든 무기 클래스의 추상클래스 BaseWeapon.cs 및 Interactable 한 DummyWeapon.cs
├── Grenade/                   # Emp 수류탄 및 유탄기 관련 코드 모음
├── Guns/                      # Rifle, Pistol, SniperRifle과 같은 (주 무기, 보조 무기) 코드 모음 
├── Hack/                      # HackGun 및 S.O. 코드 모음
├── Melee/                     # Melee 관련 코드 모음
└── WeaponDetails/             # WeaponParts 관련 코드 모음
```

<br/>

# 💻 코드 샘플 및 주석

### 1. 무기 선언 및 관련 로직
- 가지고 있는 무기의 현재 탄약 수 및 탄창 속 탄약 수를 SaveFile이 존재할 시 이를 기준으로 Initialize
- 없을 시 기존 프리팹으로 선언된 방식으로 Initialize
- 무기의 기본 스탯은 S.O.를 통해 선언되는 방식
- 무기의 스탯 변화는 `UpdateStatValue` 메소드를 호출하여 선언된 프로퍼티 값을 업데이트 하여 기존 S.O. 값은 건드리지 않고 변경.
- [Original Code](https://github.com/Bangeunseong/TheLastOne_PersonalSpace/tree/master/Scripts/Weapon/Scripts)

```csharp
namespace _1.Scripts.Weapon.Scripts.Common
{
    public enum WeaponType
    {
        Punch,
        Rifle,
        Pistol,
        SniperRifle,
        GrenadeLauncher,
        HackGun,
    }
    
    public abstract class BaseWeapon : MonoBehaviour, IShootable
    {
        [Header("Owner")] 
        [SerializeField] protected GameObject owner;
        
        [Header("Components")] 
        [SerializeField] protected ParticleSystem muzzleFlashParticle;
        [SerializeField] protected LightCurves lightCurves;
        
        [field: Header("Hittable Layer")]
        [field: SerializeField] public LayerMask HittableLayer { get; protected set; }
        
        [field: Header("Weapon Parts")]
        [field: SerializeField] public SerializedDictionary<int, WeaponPart> WeaponParts { get; private set; }
        [field: SerializeField] public SerializedDictionary<PartType, int> EquippedWeaponParts { get; private set; } = new();
        [field: SerializeField] public SerializedDictionary<int, bool> EquipableWeaponParts { get; private set; } = new();
        
        // Fields
        [CanBeNull] protected Player player;
        // private Enemy enemy;
        protected bool isOwnedByPlayer;
        
        public bool TryCollectWeaponPart(int id)
        {
            if (!EquipableWeaponParts.TryGetValue(id, out bool value)) return false;
            if (value) return false;
            EquipableWeaponParts[id] = true;
            return true;
        }

        public bool TryEquipWeaponPart(PartType type, int id)
        {
            if (!WeaponParts.TryGetValue(id, out WeaponPart part)) return false;
            if (!EquipableWeaponParts.TryGetValue(id, out bool isEquipable)) return false;
            
            if (EquippedWeaponParts.TryGetValue(type, out int oldId))
            {
                if (WeaponParts.TryGetValue(oldId, out WeaponPart oldPart)) oldPart.OnUnWear();
            }
            
            part.OnWear();
            EquippedWeaponParts[type] = id;
            return true;
        }

        public bool TryUnequipWeaponPart(PartType type, int id)
        {
            if (!WeaponParts.TryGetValue(id, out WeaponPart part)) return false;
            if (!EquippedWeaponParts.TryGetValue(type, out int currentPartId)) return false;
            if (!currentPartId.Equals(id)) return false;
            
            part.OnUnWear();
            EquippedWeaponParts.Remove(type);
            return true;
        }

        public abstract bool TryForgeWeapon();
        public abstract void Initialize(GameObject ownerObj, DataTransferObject dto = null);
        public abstract bool OnShoot();
        public abstract bool OnRefillAmmo(int ammo);
        public abstract void UpdateStatValues(WeaponPart data, bool isWorn = true);
    }
}
```

### 2. 무기 파츠 관련 로직
- 각 무기 프리팹 안에 따로 선언되어 착용 및 탈착을 할 때 Instantiate, Destroy 방식이 아닌 Mesh를 키고 끄는 방식으로 작성
- 착용을 할 때 Mesh를 키거나 IronSight와 같이 총에 기본으로 달린 것은 Rotation 값을 수정하여 표현
- 착용 및 탈착을 할 때 무기의 스탯도 같이 업데이트
- [Original Code](https://github.com/Bangeunseong/TheLastOne_PersonalSpace/tree/master/Scripts/Weapon/Scripts/WeaponDetails)
```csharp
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
```