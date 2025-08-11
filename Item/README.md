# 🧐 Item 구현방식

<br/>

# 📁 폴더 구조

```
Item/                        # Item 관련 코드 모음
├── Common/                  # Item을 구현하기 위한 부모 클래스 및 Item S.O. 모음
└── Items/                   # 게임에서 쓰이는 아이템 관련 코드 모음
```

<br/>

# 💻 코드 샘플 및 주석

### 아이템 선언, 사용 및 보유 로직
- 아이템의 현재 보유 갯수 및 사용, 보유 로직은 추상클래스인 [BaseItem.cs](https://github.com/Bangeunseong/TheLastOne_PersonalSpace/blob/master/Item/Common/BaseItem.cs)을 상속받아 각 아이템 별로 오버라이딩 되어 구현됨.
- 항상 아이템은 [PlayerInventory.cs](https://github.com/Bangeunseong/TheLastOne_PersonalSpace/blob/master/Player/Core/PlayerInventory.cs)을 통해 선언되는데 이때 SaveFile(즉, DTO class)을 통해 현재 보유 상태를 선언.
- 각 아이템의 효과는 `OnUse` 메소드를 오버라이드 하여 적용.
- [Original Code](https://github.com/Bangeunseong/TheLastOne_PersonalSpace/tree/master/Item)
```
namespace _1.Scripts.Item.Common
{
    [Serializable] public class BaseItem : IItem
    {
        [field: Header("Item Data")]
        [field: SerializeField] public ItemData ItemData { get; protected set; }
        
        [field: Header("Current Item Stat.")]
        [field: SerializeField] public int CurrentItemCount { get; protected set; }

        public virtual void Initialize(CoreManager coreManager, DataTransferObject dto = null) { }

        public virtual bool OnUse(GameObject interactor) { return false; }
        
        public void OnConsume() => CurrentItemCount--;
        
        public virtual bool OnRefill(int value = 1)
        {
            if (CurrentItemCount >= ItemData.MaxStackCount) return false;
            CurrentItemCount = Mathf.Min(CurrentItemCount + value, ItemData.MaxStackCount);
            return true;
        }
    }
}

namespace _1.Scripts.Item.Items
{
    public class Medkit : BaseItem
    {
        public override void Initialize(CoreManager coreManager, DataTransferObject dto = null)
        {
            ItemData = coreManager.resourceManager.GetAsset<ItemData>("Medkit");
            if (dto != null) { CurrentItemCount = dto.Items[(int)ItemData.ItemType]; }
        }
        public override bool OnUse(GameObject interactor)
        {
            if (CurrentItemCount <= 0 || !interactor.TryGetComponent(out Player player)) return false;
            player.PlayerCondition.OnItemUsed(this);
            return true;
        }
    }
}
```

---