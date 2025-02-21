using System;
using XRL.Messages;
using XRL.Rules;
using XRL.UI;

namespace XRL.World.Parts.Mutation {
    public class StarlitKaleidoscope_FrostCondensation : BaseMutation {
        public const string FrostCondensationCommand = "StarlitKaleidoscope_FrostCondensation";
        public const string FrostSlugItem = "StarlitKaleidoscope_Frost Slug";
        public const string FrostArrowItem = "StarlitKaleidoscope_Frost Arrow";

        public Guid FrostCondensationActivatedAbilityID = Guid.Empty;

        public StarlitKaleidoscope_FrostCondensation() {
            DisplayName = "Frost Condensation";
            Type = "Mental";
        }

        public override string GetDescription() => "You gather frost from the air and condense it into projectiles.";
        
        public int ProjectilePenetrationBonus(int Level) => Level / 6;
        public string ArrowBaseDamage(int Level) => Level switch {
            1 or 2 => "1d4",
            3 or 4 => "1d3+1",
            _ => $"1d4+{(Level-1)/2-1}",
        };
        public string SlugColdDamage(int Level) => Level switch {
            1 => "1d2",
            2 => "1d3",
            3 => "1d4",
            4 => "1d5",
            _ => $"{(Level-1)/2}d3{(Level % 2 == 0 ? "+1" : "")}",
        };
        public string ArrowColdDamage(int Level) => Level switch {
            1 => "1d3",
            2 => "1d5",
            3 => "1d7",
            _ => $"{Level/2}d5{(Level % 2 == 0 ? "+1" : "")}",
        };
        public string ProjectileTemperatureChange(int Level) => $"-{Level}d5";
        
        public override string GetLevelText(int Level) {
            return "You may load frost slugs and frost bullets into weapons when you reload them. " +
                   "They deal extra cold damage, and chill enemies they hit. This cannot freeze enemies.\n" +
                   "\n" +
                   "Bonus penetration: {{rules|+" + ProjectilePenetrationBonus(Level) + "}}\n" +
                   "Slug bonus damage: {{rules|" + SlugColdDamage(Level) + "}}\n" +
                   "Arrow base damage: {{rules|" + ArrowBaseDamage(Level) + "}}\n" +
                   "Arrow bonus damage: {{rules|" + ArrowColdDamage(Level) + "}}\n" +
                   "Temperature reduction: {{rules|" + ProjectileTemperatureChange(Level) + "}} degrees\n";
        }
        
        public override void CollectStats(Templates.StatCollector stats, int Level) {
            stats.Set("PenetrationBonus", "+" + ProjectilePenetrationBonus(Level), !stats.mode.Contains("ability"));
            stats.Set("SlugColdDamage", SlugColdDamage(Level), !stats.mode.Contains("ability"));
            stats.Set("ArrowBaseDamage", ArrowBaseDamage(Level), !stats.mode.Contains("ability"));
            stats.Set("ArrowColdDamage", ArrowColdDamage(Level), !stats.mode.Contains("ability"));
            stats.Set("TemperatureChange", $"{ProjectileTemperatureChange(Level)} degrees", !stats.mode.Contains("ability"));
        }

        public override bool WantEvent(int ID, int cascade) {
            return base.WantEvent(ID, cascade) ||
                   ID == CommandReloadEvent.ID ||
                   ID == CommandEvent.ID ||
                   ID == BeforeAbilityManagerOpenEvent.ID;
        }

        public override bool HandleEvent(BeforeAbilityManagerOpenEvent E) {
            this.DescribeMyActivatedAbility(FrostCondensationActivatedAbilityID, CollectStats);
            return base.HandleEvent(E);
        }

        bool checkCanLoadAmmo(GameObject Weapon, out MagazineAmmoLoader loader) {
            if (
                Weapon.TryGetPart<MagazineAmmoLoader>(out var ammoLoader) && 
                ammoLoader.AmmoPart is "AmmoSlug" or "AmmoArrow" &&
                IsMyActivatedAbilityToggledOn(FrostCondensationActivatedAbilityID)
            ) {
                loader = ammoLoader;
                return true;
            }
            loader = null;
            return false;
        }

        void reloadWeapon(CommandReloadEvent E, GameObject Weapon) {
            if (checkCanLoadAmmo(Weapon, out var ammoLoader)) {
                var isSlug = ammoLoader.AmmoPart == "AmmoSlug";

                E.CheckedForReload.Add(ammoLoader);
                ammoLoader.Unload(E.Actor);

                // create the frost slug items
                var ammo = GameObject.Create(isSlug ? FrostSlugItem : FrostArrowItem);
                ammo.Count = ammoLoader.MaxAmmo;
                var overrideStats = ammo.GetPart<StarlitKaleidoscope_OverrideWeaponProjectile>();
                if (overrideStats == null) throw new Exception("OverrideWeaponProjectile part is missing?");
                overrideStats.OverrideStats = true;

                // inherit base weapon stats, if possible
                overrideStats.BasePenetration = isSlug ? 3 : 0;
                overrideStats.BaseDamage = isSlug ? "1d4" : ArrowBaseDamage(Level);
                if (ammoLoader.ProjectileObject != null) {
                    var defaultProjectile = GameObject.Create(ammoLoader.ProjectileObject, Context: "Projectile");
                    if (defaultProjectile.TryGetPart<Projectile>(out var projectilePart)) {
                        overrideStats.BasePenetration = projectilePart.BasePenetration;
                        overrideStats.BaseDamage = projectilePart.BaseDamage;
                        if (!projectilePart.Attributes.IsNullOrEmpty())
                            overrideStats.Attributes += "," + projectilePart.Attributes;
                    }
                }

                // adjust based on mutation level
                overrideStats.BasePenetration += ProjectilePenetrationBonus(Level);
                overrideStats.FrostCondensationTemperatureChange = ProjectileTemperatureChange(Level);
                overrideStats.FrostCondensationBonusColdDamage = isSlug ? SlugColdDamage(Level) : ArrowColdDamage(Level);

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