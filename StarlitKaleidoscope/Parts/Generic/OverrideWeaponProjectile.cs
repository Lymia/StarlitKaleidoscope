using System;
using XRL.World;

namespace StarlitKaleidoscope.Parts.Generic {
    // While not ideal, this part serves entirely as a marker for modified code in MagazineAmmoLoader
    [Serializable]
    public class OverrideWeaponProjectile : IPart {
        public bool OverrideStats = false;
        public int BasePenetration = 1;
        public string BaseDamage = "1d4";

        public string Attributes = "";

        public string FrostCondensationTemperatureChange = null;
        public string FrostCondensationBonusColdDamage = null;
    }
}