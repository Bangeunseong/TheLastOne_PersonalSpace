using _1.Scripts.Entity.Scripts.Player.Core;
using _1.Scripts.Item.Common;
using _1.Scripts.Manager.Core;
using _1.Scripts.Manager.Data;
using UnityEngine;

namespace _1.Scripts.Item.Items
{
    public class NanoAmple : BaseItem
    {
        public override void Initialize(CoreManager coreManager, DataTransferObject dto = null)
        {
            ItemData = coreManager.resourceManager.GetAsset<ItemData>("NanoAmple");
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