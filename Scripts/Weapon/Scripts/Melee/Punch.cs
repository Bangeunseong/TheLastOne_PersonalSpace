using _1.Scripts.Manager.Data;
using _1.Scripts.Weapon.Scripts.Common;
using _1.Scripts.Weapon.Scripts.WeaponDetails;
using UnityEngine;

namespace _1.Scripts.Weapon.Scripts.Melee
{
    public class Punch : BaseWeapon
    {
        public override void Initialize(GameObject ownerObj, DataTransferObject dto = null) { }
        public override bool OnShoot() { return false; }
        public override bool OnRefillAmmo(int ammo) { return false; }
        public override void UpdateStatValues(WeaponPart data, bool isWorn = true) { }
        public override bool TryForgeWeapon() { return false; }
    }
}