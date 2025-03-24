using System;
using StarlitKaleidoscope.Common;
using StarlitKaleidoscope.Mutations;
using StarlitKaleidoscope.Parts.Effects;
using XRL;
using XRL.Rules;
using XRL.UI;
using XRL.World;
using XRL.World.Capabilities;
using XRL.World.Parts;
using XRL.World.Parts.Mutation;

namespace StarlitKaleidoscope.Parts.Mutations {
    public class StaticBurst : BaseMutation {
        public const string StaticBurstCommand = "StarlitKaleidoscope_StaticBurst";

        public Guid StaticBurstActivatedAbilityID = Guid.Empty;

        public StaticBurst() {
            DisplayName = "Static Rupture";
            Type = "Mental";
        }

        public override string GetDescription() =>
            "You rupture the fabric of existence, drowning your foes in the static beyond.";

        public int StatusCount(int Level) => 2 + Level / 6;
        public string BurstDamage(int Level) => $"{Level}d3";
        public int Cooldown(int Level) => 100;
        public int DestabilizedDuration(int Level) => 5 + Level * 5;

        public override string GetLevelText(int Level) {
            return
                $"You create a burst of static, damaging your target and inflicting {StatusCount(Level)} random negative statuses.\n" +
                "\n" +
                "Burst Damage: {{rules|" + BurstDamage(Level) + "}}\n" +
                "Range: sight\n" +
                $"Cooldown: {Cooldown(Level)}\n" +
                "Destabilizes the target for {{rules|" + DestabilizedDuration(Level) + "}} rounds, causing them to drop " +
                "special loot based on their level.\n" +
                "+400 reputation with {{w|highly entropic beings}}\n";
        }

        public override void CollectStats(Templates.StatCollector stats, int Level) {
            stats.Set("StatusCount", StatusCount(Level), !stats.mode.Contains("ability"));
            stats.Set("BurstDamage", BurstDamage(Level), !stats.mode.Contains("ability"));
            stats.Set("Cooldown", Cooldown(Level), !stats.mode.Contains("ability"));
            stats.Set("DestabilizedDuration", DestabilizedDuration(Level), !stats.mode.Contains("ability"));
        }

        public override bool WantEvent(int ID, int cascade) {
            return base.WantEvent(ID, cascade) ||
                   ID == CommandEvent.ID ||
                   ID == BeforeAbilityManagerOpenEvent.ID;
        }

        public override bool HandleEvent(BeforeAbilityManagerOpenEvent E) {
            DescribeMyActivatedAbility(StaticBurstActivatedAbilityID, CollectStats);
            return base.HandleEvent(E);
        }

        public bool Cast(CommandEvent E) {
            Cell targetCell = PickDestinationCell(80, IgnoreLOS: true, Label: "Static Rupture", Snap: true);
            if (targetCell == null) return false;

            // find the target of the ability
            GameObject target = targetCell.GetCombatTarget(ParentObject, true, Phase: Phase.PHASE_INSENSITIVE);
            if (!target.HasPart<Brain>())
                target = null;
            if (target == null)
                target = targetCell.GetFirstObjectWithPart("Brain");
            if (target == ParentObject && target.IsPlayer() &&
                Popup.ShowYesNo("Are you sure you want to attack yourself?") == DialogResult.No)
                return false;
            if (target == null) {
                if (ParentObject.IsPlayer()) Popup.ShowFail("There's no animate target there.");
                return false;
            }
            
            // bookkeeping
            if (!this.CheckRealityDistortion(targetCell, E))
                return false;
            UseEnergy(1000, "Mental Mutation StaticBurst");
            CooldownMyActivatedAbility(StaticBurstActivatedAbilityID, Cooldown(Level));

            // Apply the Static Rupture effect.
            StaticBurstApplyEffects.ApplyEffects(ParentObject, target, StatusCount(Level), 1 + ParentObject.Level / 6);
            int damage = BurstDamage(Level).Roll();
            target.ApplyEffect(new Destabilized(DestabilizedDuration(Level)), ParentObject);
            target.TakeDamage(ref damage, "Cosmic", Attacker: ParentObject, Message: "from %t blast of warm static!");

            // VFX
            if (target.InActiveZone()) {
                float speed = 3.0f / Stat.RandomCosmetic(2, 20);
                int life = Stat.RandomCosmetic(5, 15);
                const float particleCount = 100;
                for (int index = 0; index < particleCount; ++index) {
                    The.ParticleManager.Add("@", targetCell.X, targetCell.Y,
                        (float) Math.Sin(index * (Math.PI * 2) / particleCount) * speed,
                        (float) Math.Cos(index * (Math.PI * 2) / particleCount) * speed, life);
                }
            }

            return true;
        }

        public override bool HandleEvent(CommandEvent E) {
            if (E.Command == StaticBurstCommand) {
                if (OnWorldMap)
                    return ParentObject.Fail("You cannot do that on the world map.");
                if (!Cast(E))
                    return false;
            }
            return base.HandleEvent(E);
        }

        public override bool Mutate(GameObject GO, int Level) {
            StaticBurstActivatedAbilityID = AddMyActivatedAbility(
                "Static Rupture",
                StaticBurstCommand,
                "Mental Mutations",
                Icon: "\x15"
            );
            return base.Mutate(GO, Level);
        }

        public override bool Unmutate(GameObject GO) {
            RemoveMyActivatedAbility(ref StaticBurstActivatedAbilityID);
            return base.Unmutate(GO);
        }
    }
}