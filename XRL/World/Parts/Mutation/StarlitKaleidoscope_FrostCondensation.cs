using System;
using XRL.Messages;
using XRL.UI;

namespace XRL.World.Parts.Mutation {
    public class StarlitKaleidoscope_FrostCondensation : BaseMutation {
        public const string FrostCondensationCommand = "StarlitKaleidoscope_FrostCondensation";
        public const string FrostSlugItem = "StarlitKaleidoscope_Frost Slug";

        public Guid FrostCondensationActivatedAbilityID = Guid.Empty;

        public StarlitKaleidoscope_FrostCondensation() {
            DisplayName = "Frost Condensation";
            Type = "Mental";
        }

        public override bool WantEvent(int ID, int cascade) {
            return base.WantEvent(ID, cascade) ||
                   ID == CommandReloadEvent.ID ||
                   ID == CommandEvent.ID;
        }

        bool checkCanLoadAmmo(GameObject Weapon, out MagazineAmmoLoader loader) {
            var ammoLoader = Weapon.GetPart<MagazineAmmoLoader>();
            if (ammoLoader != null && (ammoLoader.AmmoPart == "AmmoSlug" || ammoLoader.AmmoPart == "AmmoArrow")) {
                if (IsMyActivatedAbilityToggledOn(FrostCondensationActivatedAbilityID)) {
                    loader = ammoLoader;
                    return true;
                }
            }
            loader = null;
            return false;
        }

        void reloadWeapon(CommandReloadEvent E, GameObject Weapon) {
            if (checkCanLoadAmmo(Weapon, out var ammoLoader)) {
                E.CheckedForReload.Add(ammoLoader);
                ammoLoader.Unload(E.Actor);
                
                // create the frost slug items
                var ammo = GameObject.Create(FrostSlugItem);
                ammo.Count = ammoLoader.MaxAmmo;

                // load the ammo into the weapon
                ammoLoader.Load(E.Actor, ammo, E.FromDialog);
                E.Reloaded.Add(ammoLoader);
                if (!E.ObjectsReloaded.Contains(ammoLoader.ParentObject))
                    E.ObjectsReloaded.Add(ammoLoader.ParentObject);
                E.EnergyCost(ammoLoader.ReloadEnergy);
            }
        }
        
        public override bool HandleEvent(CommandReloadEvent E) {
            if (E.Pass != 1 || !IsMyActivatedAbilityToggledOn(FrostCondensationActivatedAbilityID))
                return true;
            if (E.Weapon != null) reloadWeapon(E, E.Weapon);
            else {
                foreach (var obj in E.Actor.Body.GetEquippedObjects(obj => checkCanLoadAmmo(obj, out _))) {
                    reloadWeapon(E, obj);
                }
            }
            return true;
        }
        
        public override bool HandleEvent(CommandEvent E) {
            // TODO: New sound effects
            if (E.Command == FrostCondensationCommand) {
                if (IsMyActivatedAbilityToggledOn(FrostCondensationActivatedAbilityID)) {
                    PlayWorldSound("Sounds/Abilities/sfx_ability_mutation_lightManipulation_deactivate");
                    ToggleMyActivatedAbility(FrostCondensationActivatedAbilityID);
                } else {
                    if (IsMyActivatedAbilityCoolingDown(FrostCondensationActivatedAbilityID)) {
                        if (ParentObject.IsPlayer()) {
                            var cdText = GetMyActivatedAbilityCooldownDescription(FrostCondensationActivatedAbilityID);
                            var cooldownMessage = 
                                "You must wait {{C|" + cdText + "}} before you can enable frost condensation.";
                            if (Options.AbilityCooldownWarningAsMessage) MessageQueue.AddPlayerMessage(cooldownMessage);
                            else Popup.ShowFail(cooldownMessage);
                        }
                        return false;
                    }
                    PlayWorldSound("Sounds/Abilities/sfx_ability_mutation_lightManipulation_activate");
                    ToggleMyActivatedAbility(FrostCondensationActivatedAbilityID);
                }
            }
            return base.HandleEvent(E);
        }

        public override bool Mutate(GameObject GO, int Level) {
            FrostCondensationActivatedAbilityID = AddMyActivatedAbility(
                "Frost Condensation",
                FrostCondensationCommand,
                "Mental Mutations",
                Toggleable: true,
                DefaultToggleState: true,
                Icon: "\0"
            );
            return base.Mutate(GO, Level);
        }

        public override bool Unmutate(GameObject GO) {
            RemoveMyActivatedAbility(ref FrostCondensationActivatedAbilityID);
            return base.Unmutate(GO);
        }
    }
}