using System;
using System.Linq;
using LeagueSharp;
using LeagueSharp.SDK.Core;
using LeagueSharp.SDK.Core.Extensions;
using LeagueSharp.SDK.Core.UI.IMenu.Values;
using LeagueSharp.SDK.Core.Wrappers;
using SharpDX;

namespace Pantheon
{
    internal class Internal
    {
        private static int _lastTick;

        public static void Combo(Obj_AI_Base target)
        {
            if (target == null)
            {
                return;
            }

            if (Variable.UsingE)
            {
                return;
            }

            var comboMode = Variable.Config["Combo"]["ComboMode"].GetValue<MenuList<string>>().Index;
            if (comboMode == 0)
            {
                if (Variable.Q.IsReady())
                {
                    Variable.Q.CastOnUnit(target);
                }

                if (Variable.W.IsReady())
                {
                    Variable.W.CastOnUnit(target);
                }

                if (Variable.E.IsReady() && !Variable.W.IsReady())
                {
                    Variable.E.Cast(target);
                }
            }
            else
            {
                if (Variable.Q.IsReady() && !Variable.W.IsReady())
                {
                    Variable.Q.CastOnUnit(target);
                }

                if (Variable.W.IsReady())
                {
                    Variable.W.CastOnUnit(target);
                }

                if (Variable.E.IsReady() && !target.CanMove)
                {
                    Variable.E.Cast(target);
                }
            }

            var comboItems = Variable.Config["Combo"]["ComboItems"].GetValue<MenuBool>().Value;
            if (comboItems)
            {
                UseItems(target);
            }

            /*var autoSmite = Variable.Config["Combo"]["AutoSmite"].GetValue<MenuBool>().Value;
            if (autoSmite)
            {
                if (Variable.SmiteSlot != SpellSlot.Unknown &&
                    Variable.Player.Spellbook.CanUseSpell(Variable.SmiteSlot) == SpellState.Ready)
                {
                    if (Variable.Q.IsReady() && Variable.W.IsReady() && Variable.E.IsReady())
                    {
                        Variable.Player.Spellbook.CastSpell(Variable.SmiteSlot, target);
                    }
                }
            }*/

            if (Variable.IgniteSlot == SpellSlot.Unknown ||
                Variable.Player.Spellbook.CanUseSpell(Variable.IgniteSlot) != SpellState.Ready)
            {
                return;
            }

            var autoIgnite = Variable.Config["Combo"]["AutoIgnite"].GetValue<MenuList<string>>().Index;
            if (autoIgnite == 1)
            {
                if (Variable.Player.GetSpellDamage(target, Variable.IgniteSlot) >= target.Health)
                {
                    Variable.Player.Spellbook.CastSpell(Variable.IgniteSlot, target);
                }
            }
            else
            {
                Variable.Player.Spellbook.CastSpell(Variable.IgniteSlot, target);
            }
        }

        public static void Harass(Obj_AI_Base target)
        {
            if (target == null)
            {
                return;
            }

            if (Variable.UsingE)
            {
                return;
            }

            var mana = Variable.Player.MaxMana*
                       (Variable.Config["Harass"]["HarassMana"].GetValue<MenuSlider>().Value/100.0);
            if (!(Variable.Player.Mana > mana))
            {
                return;
            }

            var menuItem = Variable.Config["Harass"]["HarassMode"].GetValue<MenuList<string>>().Index;
            switch (menuItem)
            {
                case 0:
                    if (Variable.Q.IsReady())
                    {
                        Variable.Q.CastOnUnit(target);
                    }
                    break;
                case 1:
                    if (Variable.W.IsReady())
                    {
                        Variable.W.CastOnUnit(target);
                    }

                    if (!Variable.W.IsReady() && Variable.E.IsReady())
                    {
                        Variable.E.Cast(target);
                    }
                    break;
            }
        }

        public static void Farm()
        {
            if (Variable.UsingE)
            {
                return;
            }

            var minions = GameObjects.EnemyMinions.Where(minion => minion.Distance(Variable.Player) <= Variable.Q.Range);
            var mana = Variable.Player.MaxMana*
                       (Variable.Config["Farm"]["FarmMana"].GetValue<MenuSlider>().Value/100.0);
            if (!(Variable.Player.Mana > mana))
            {
                return;
            }

            var farmQ = Variable.Config["Farm"]["FarmQ"].GetValue<MenuBool>().Value;
            var objAiMinions = minions as Obj_AI_Minion[] ?? minions.ToArray();
            if (farmQ && Variable.Q.IsReady())
            {
                foreach (
                    var minion in
                        objAiMinions.Where(unit => unit.Health <= Damage.SpellQ(unit))
                    )
                {
                    Variable.Q.CastOnUnit(minion);
                    return;
                }
            }

            var farmW = Variable.Config["Farm"]["FarmW"].GetValue<MenuBool>().Value;
            if (!farmW || !Variable.W.IsReady())
            {
                return;
            }

            foreach (
                var minion in
                    objAiMinions.Where(unit => unit.Health <= Damage.SpellW(unit)))
            {
                Variable.W.CastOnUnit(minion);
                return;
            }
        }

        public static void JungleClear()
        {
            if (Variable.UsingE)
            {
                return;
            }

            var mob = (GameObjects.JungleLegendary.FirstOrDefault(j => j.IsValidTarget(Variable.Q.Range)) ??
                       GameObjects.JungleSmall.FirstOrDefault(
                           j =>
                               j.IsValidTarget(Variable.Q.Range) && j.Name.Contains("Mini") &&
                               j.Name.Contains("SRU_Razorbeak")) ??
                       GameObjects.JungleLarge.FirstOrDefault(j => j.IsValidTarget(Variable.Q.Range))) ??
                      GameObjects.JungleSmall.FirstOrDefault(j => j.IsValidTarget(Variable.Q.Range));

            if (mob == null)
            {
                return;
            }

            var jungleQ = Variable.Config["Jungle"]["JungleQ"].GetValue<MenuBool>().Value;
            if (jungleQ && Variable.Q.IsReady())
            {
                Variable.Q.CastOnUnit(mob);
            }

            var jungleW = Variable.Config["Jungle"]["JungleW"].GetValue<MenuBool>().Value;
            if (jungleW && Variable.W.IsReady())
            {
                Variable.W.CastOnUnit(mob);
            }

            var jungleE = Variable.Config["Jungle"]["JungleE"].GetValue<MenuBool>().Value;
            if (jungleE && Variable.E.IsReady())
            {
                Variable.E.Cast(mob);
            }
        }

        public static float ComboDamage(Obj_AI_Base target)
        {
            var dmg = 0d;
            if (Variable.Q.IsReady())
            {
                dmg += Damage.SpellQ(target);
            }

            if (Variable.W.IsReady())
            {
                dmg += Damage.SpellW(target);
            }

            if (Variable.E.IsReady())
            {
                dmg += Damage.SpellE(target);
            }

            if (Variable.IgniteSlot != SpellSlot.Unknown &&
                Variable.Player.Spellbook.CanUseSpell(Variable.IgniteSlot) == SpellState.Ready)
            {
                dmg += Damage.Ignite(target as Obj_AI_Hero);
            }

            return (float) dmg;
        }

        public static void UseItems(Obj_AI_Base target)
        {
            if (target == null)
            {
                return;
            }

            if (Variable.UsingE)
            {
                return;
            }

            short[] targetedItems = {3188, 3153, 3144, 3128, 3146, 3184};
            short[] nonTargetedItems = {3180, 3131, 3074, 3077, 3142};

            foreach (var itemId in targetedItems.Where(itemId => Items.HasItem(itemId) && Items.CanUseItem(itemId)))
            {
                Items.UseItem(itemId, target);
            }

            foreach (var itemId in nonTargetedItems.Where(itemId => Items.HasItem(itemId) && Items.CanUseItem(itemId)))
            {
                Items.UseItem(itemId);
            }
        }

        /*public static string SmiteType()
        {
            int[] redSmite = {3715, 3718, 3717, 3716, 3714};
            int[] blueSmite = {3706, 3710, 3709, 3708, 3707};

            return blueSmite.Any(itemId => Items.HasItem(itemId))
                ? "s5_summonersmiteplayerganker"
                : (redSmite.Any(itemId => Items.HasItem(itemId)) ? "s5_summonersmiteduel" : "summonersmite");
        }

        public static void SetSmiteSlot()
        {
            foreach (
                var spell in
                    ObjectManager.Player.Spellbook.Spells.Where(
                        spell => string.Equals(spell.Name, SmiteType(), StringComparison.CurrentCultureIgnoreCase)))
            {
                Variable.SmiteSlot = spell.Slot;
                break;
            }
        }*/

        public static void ComboModeSwitch()
        {
            var comboMode = Variable.Config["Combo"]["ComboMode"].GetValue<MenuList<string>>().Index;
            var lastTime = Environment.TickCount - _lastTick;
            var comboSwitch = Variable.Config["Combo"]["ComboSwitch"].GetValue<MenuKeyBind>().Active;
            if (!comboSwitch || lastTime <= Game.Ping)
            {
                return;
            }

            switch (comboMode)
            {
                case 0:
                    Variable.Config["Combo"]["ComboMode"].GetValue<MenuList<string>>().Index = 1;
                    _lastTick = Environment.TickCount + 300;
                    break;
                case 1:
                    Variable.Config["Combo"]["ComboMode"].GetValue<MenuList<string>>().Index = 0;
                    _lastTick = Environment.TickCount + 300;
                    break;
            }
        }

        public static bool UnderTurret(Vector3 position, bool enemyTurretsOnly)
        {
            return
                ObjectManager.Get<Obj_AI_Turret>().Any(turret => turret.IsValidTarget(950, enemyTurretsOnly, position));
        }
    }
}