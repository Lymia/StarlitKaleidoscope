using System;

namespace XRL.World.Parts.Mutation {
    public class StarlitKaleidoscope_FrostCondensation : BaseMutation {
        public const string FrostCondensationCommand = "StarlitKaleidoscope_FrostCondensation";
        
        public Guid FrostCondensationActivatedAbilityID = Guid.Empty;

        public StarlitKaleidoscope_FrostCondensation() {
            DisplayName = "Frost Condensation";
            Type = "Mental";
        }

        public override bool WantEvent(int ID, int cascade) {
            return base.WantEvent(ID, cascade) ||
                   ID == CommandReloadEvent.ID;
        }

        public override bool HandleEvent(CommandReloadEvent E) {
            if (E.Pass == 1 && E.Weapon != null && E.Weapon.HasPart<MagazineAmmoLoader>()) {
                // TODO: implementation
            }
            return true;
        }
        
        public override bool Mutate(GameObject GO, int Level)
        {
            FrostCondensationActivatedAbilityID = AddMyActivatedAbility(
                "Frost Condensation",
                FrostCondensationCommand,
                "Mental Mutations",
                Toggleable: true,
                Icon: "\0"
            );
            return base.Mutate(GO, Level);
        }

        public override bool Unmutate(GameObject GO)
        {
            RemoveMyActivatedAbility(ref FrostCondensationActivatedAbilityID);
            return base.Unmutate(GO);
        }
    }
}