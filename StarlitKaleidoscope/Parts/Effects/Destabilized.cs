using System;
using System.Collections.Generic;
using ConsoleLib.Console;
using StarlitKaleidoscope.Common;
using StarlitKaleidoscope.Mutations;
using XRL;
using XRL.Rules;
using XRL.World;
using XRL.World.Parts;

namespace StarlitKaleidoscope.Parts.Effects {
    [Serializable]
    public class Destabilized : Effect, ITierInitialized {
        const int GlitchEffectDuration = 6;
        const int GlitchEffectRandomness = 3;
        const int GlitchEffectRange = 2;
        const int ParticleChance = 20;

        public Destabilized() {
            DisplayName = "{{Y|destabilized}}";
            Duration = 10;
        }

        public Destabilized(int duration) : this() {
            Duration = duration;
        }

        public void Initialize(int Tier) {
            Duration = Stat.Random(10, 30);
        }

        public override string GetDetails() => "Target will drop additional items on death.";

        // not marked as TYPE_NEGATIVE so it isn't removed. also cus it doesn't really debuff
        public override int GetEffectType() => TYPE_GENERAL;

        public override bool UseStandardDurationCountdown() => true;

        public override bool WantEvent(int ID, int cascade) {
            return base.WantEvent(ID, cascade) ||
                   ID == BeforeRenderEvent.ID ||
                   ID == BeforeDeathRemovalEvent.ID;
        }

        public override bool HandleEvent(BeforeDeathRemovalEvent E) {
            if (!Object.IsNowhere() && E.Dying == Object) {
                foreach (var drop in StaticBurstDrop.CreateDrops(Object)) {
                    Object.CurrentCell.AddObject(drop);
                }
            }
            return base.HandleEvent(E);
        }

        [Serializable]
        public struct GlitchEffectColor {
            public readonly char Foreground, TileForeground, Background, TileBackground, Detail;
            public GlitchEffectColor(
                char foreground, char tileForeground, char background, char tileBackground, char detail
            ) {
                Foreground = foreground;
                TileForeground = tileForeground;
                Background = background;
                TileBackground = tileBackground;
                Detail = detail;
            }
            public static GlitchEffectColor NewRandom() {
                var foreground = Crayons.GetRandomColorAll()[0];
                var tileForeground = Crayons.GetRandomColorAll()[0];
                var background = Crayons.GetRandomColorAll()[0];
                var tileBackground = Crayons.GetRandomColorAll()[0];
                var detail = Crayons.GetRandomColorAll()[0];
                return new GlitchEffectColor(foreground, tileForeground, background, tileBackground, detail);
            }
        }

        [Serializable]
        public class GlitchEffectParams {
            public readonly GlitchEffectColor ColorMain, Glitch0, Glitch1;
            public int ParticleDuration;
            public int GlitchDuration, GlitchColor;

            public GlitchEffectParams() {
                ColorMain = GlitchEffectColor.NewRandom();
                Glitch0 = GlitchEffectColor.NewRandom();
                Glitch1 = GlitchEffectColor.NewRandom();
                ParticleDuration = GlitchEffectDuration +
                                   Stat.RandomCosmetic(-GlitchEffectRandomness, GlitchEffectRandomness);
            }

            internal void updateTimer() {
                if (ParticleDuration == 0) return;
                ParticleDuration--;
            }

            void updateRender() {
                if (ParticleDuration == 0) return;
                if (GlitchDuration > 0) {
                    GlitchDuration--;
                } else if (Stat.Chance(2)) {
                    GlitchDuration = Stat.RandomCosmetic(10, 20);
                    GlitchColor = Stat.RandomCosmetic(0, 1);
                }
            }
            GlitchEffectColor effectColor() {
                if (GlitchDuration > 0)
                    return GlitchColor == 0 ? Glitch0 : Glitch1;
                return ColorMain;
            }

            public void applyToBuffer(ScreenBuffer buffer, Loc loc, GameObject source) {
                updateRender();

                var bufferCell = buffer.get(loc.x, loc.y);
                if (bufferCell == null) return;
                if (!source.CurrentZone.GetVisibility(loc.x, loc.y)) return;

                var color = effectColor();
                bufferCell.Foreground = The.Color[color.Foreground];
                bufferCell.TileForeground = The.Color[color.TileForeground];
                bufferCell.Background = The.Color[color.Background];
                bufferCell.TileBackground = The.Color[color.TileBackground];
                bufferCell.Detail = The.Color[color.Detail];
            }
        }

        String currentZoneID;
        Dictionary<Loc, GlitchEffectParams> locations = new();
        public GlitchEffectParams UnitEffectParams = new();

        void zoneUpdate() {
            if (currentZoneID != Object.CurrentZone.ZoneID) {
                currentZoneID = Object.CurrentZone.ZoneID;
                locations.Clear();
                for (int i = 0; i < 10; i++)
                    updateParticles();
            }
        }

        void updateParticles() {
            // update particles
            foreach (var entry in locations) {
                entry.Value.updateTimer();
            }
            locations.RemoveAll(x => x.Value.ParticleDuration <= 0);

            // create a new particle
            if (!Stat.Chance(ParticleChance)) return;

            var loc = new Loc(Object.CurrentCell.X, Object.CurrentCell.Y);
            for (int i = 0; i < GlitchEffectRange; i++) {
                var xAdd = Stat.RandomCosmetic(-1, 1);
                var yAdd = Stat.RandomCosmetic(-1, 1);
                loc = loc.Add(xAdd, yAdd);
            }
            if (Object.CurrentZone.IsValidLoc(loc.x, loc.y) && !locations.ContainsKey(loc))
                locations.Add(loc, new GlitchEffectParams());
        }


        public override bool FinalRender(RenderEvent E, bool bAlt) {
            zoneUpdate();
            if (currentZoneID == TheGame.ZoneManager.ActiveZone.ZoneID)
                E.WantsToPaint = true;
            return true;
        }

        // :D
        public override void OnPaint(ScreenBuffer buffer) {
            if (currentZoneID != TheGame.ZoneManager.ActiveZone.ZoneID)
                return;
            updateParticles();

            var loc = new Loc(Object.CurrentCell.X, Object.CurrentCell.Y);
            foreach (var entry in locations) {
                if (loc == entry.Key) continue;
                entry.Value.applyToBuffer(buffer, entry.Key, Object);
            }
            UnitEffectParams.applyToBuffer(buffer, loc, Object);

            base.OnPaint(buffer);
        }

        public override bool Apply(GameObject obj) {
            if (!obj.HasPart<Brain>()) return false; // exclude inanimate objects, as a procaution
            if (obj.TryGetEffect<Destabilized>(out var existingEffect)) {
                existingEffect.Duration += Duration;
                return false;
            }
            return true;
        }
    }
}