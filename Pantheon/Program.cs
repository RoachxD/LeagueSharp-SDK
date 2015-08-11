using System;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using LeagueSharp;
using LeagueSharp.SDK.Core;
using LeagueSharp.SDK.Core.Enumerations;
using LeagueSharp.SDK.Core.Events;
using LeagueSharp.SDK.Core.Extensions;
using LeagueSharp.SDK.Core.UI;
using LeagueSharp.SDK.Core.UI.IMenu.Values;
using LeagueSharp.SDK.Core.Utils;
using LeagueSharp.SDK.Core.Wrappers;
using SharpDX;
using Menu = LeagueSharp.SDK.Core.UI.IMenu.Menu;

namespace Pantheon
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            Load.OnLoad += delegate
            {
                var onGameLoad = new Thread(Game_OnGameLoad);
                onGameLoad.Start();
            };
        }

        private static void Game_OnGameLoad()
        {
            if (Variable.Player.CharData.BaseSkinName != Variable.CharName)
            {
                return;
            }

            Variable.Q = new Spell(SpellSlot.Q, 600);
            Variable.W = new Spell(SpellSlot.W, 600);
            Variable.E = new Spell(SpellSlot.E, 700);

            Variable.IgniteSlot = Variable.Player.GetSpellSlot("summonerdot");
            //Internal.SetSmiteSlot();

            Variable.Spells.Add(Variable.Q);
            Variable.Spells.Add(Variable.W);
            Variable.Spells.Add(Variable.E);

            Bootstrap.Init(new string[] {});

            Variable.Config = new Menu("Roach's " + Variable.CharName + "#", "Roach's " + Variable.CharName + "#", true);

            var ComboMenu = Variable.Config.Add(new Menu("Combo", "Combo Settings"));
            ComboMenu.Add(new MenuList<string>("ComboMode", "Combo Mode",
                new[] {"Normal (Q-W-E with No Restrictions)", "Ganking (W-E-Q - Will not E until target immovable)"}));
            ComboMenu.Add(new MenuKeyBind("ComboSwitch", "Switch mode Key", Keys.T, KeyBindType.Press));
            ComboMenu.Add(new MenuBool("ComboItems", "Use Items with Burst", true));
            /*if (Variable.SmiteSlot != SpellSlot.Unknown)
            {
                ComboMenu.Add(new MenuBool("AutoSmite", "Use Smite on Target if QWE Available", true));
            }*/

            if (Variable.IgniteSlot != SpellSlot.Unknown)
            {
                ComboMenu.Add(new MenuList<string>("AutoIgnite", "Use Ignite with Burst", new[] {"Burst", "KS"}));
            }

            var HarassMenu = Variable.Config.Add(new Menu("Harass", "Harass Settings"));
            HarassMenu.Add(new MenuList<string>("HarassMode", "Harass Mode: ", new[] {"Q", "W+E"}));
            HarassMenu.Add(new MenuKeyBind("AutoQ", "Auto-Q when Target in Range", Keys.Z, KeyBindType.Toggle));
            HarassMenu.Add(new MenuBool("AutoQTurret", "Don't Auto-Q if in enemy Turret Range", true));
            HarassMenu.Add(new MenuSlider("HarassMana", "Min. Mana Percent: ", 50));

            var FarmMenu = Variable.Config.Add(new Menu("Farm", "Farming Settings"));
            FarmMenu.Add(new MenuBool("FarmQ", "Farm with Spear Shot (Q)", true));
            FarmMenu.Add(new MenuBool("FarmW", "Farm with Aegis of Zeonia (W)", true));
            FarmMenu.Add(new MenuSlider("FarmMana", "Min. Mana Percent: ", 50));

            var JungleMenu = Variable.Config.Add(new Menu("Jungle", "Jungle Clear Settings"));
            JungleMenu.Add(new MenuBool("JungleQ", "Farm with Spear Shot (Q)", true));
            JungleMenu.Add(new MenuBool("JungleW", "Farm with Aegis of Zeonia (W)", true));
            JungleMenu.Add(new MenuBool("JungleE", "Farm with Heartseeker Strike (E)", true));

            var DrawingMenu = Variable.Config.Add(new Menu("Drawing", "Draw Settings"));
            var ColorMenu = DrawingMenu.Add(new Menu("Color", "Color Settings"));
            ColorMenu.Add(new MenuColor("TColor", "Change Target Color", Color.Wheat));
            ColorMenu.Add(new MenuColor("QColor", "Change Q Range Color", Color.Red));
            ColorMenu.Add(new MenuColor("WColor", "Change W Range Color", Color.DarkRed));
            ColorMenu.Add(new MenuColor("EColor", "Change E Range Color", Color.Blue));

            DrawingMenu.Add(new MenuBool("NoDrawings", "Disable All Range Draws"));
            DrawingMenu.Add(new MenuBool("Target", "Draw Circle on Target", true));
            DrawingMenu.Add(new MenuBool("DrawQ", "Draw Spear Shot (Q) Range", true));
            DrawingMenu.Add(new MenuBool("DrawW", "Draw Aegis of Zeonia (W) Range", true));
            DrawingMenu.Add(new MenuBool("DrawE", "Draw Heartseeker Strike (E) Range", true));
            DrawingMenu.Add(new MenuBool("CurrentComboMode", "Draw Current Combo Mode", true));

            var MiscMenu = Variable.Config.Add(new Menu("Misc", "Misc Settings"));
            MiscMenu.Add(new MenuBool("StopChannel", "Interrupt Channeling Spells", true));
            Variable.Config.Attach();

            DamageIndicator.DamageToUnit = Internal.ComboDamage;
            DamageIndicator.Enabled = true;

            Drawing.OnDraw += Drawing_OnDraw;
            Game.OnUpdate += Game_OnGameUpdate;
            InterruptableSpell.OnInterruptableTarget += InterruptableSpell_OnInterruptableTarget;
            Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;

            Game.PrintChat("<font color=\"#D2444A\">Pantheon# -</font> <font color=\"#FFFFFF\">Loaded</font>");
        }

        private static void Obj_AI_Base_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (!sender.IsMe)
            {
                return;
            }

            if (!args.SData.Name.ToLower().Equals("pantheone"))
            {
                return;
            }

            Orbwalker.Attack = false;
            Orbwalker.Movement = false;
            Variable.UsingE = true;

            DelayAction.Add(750, delegate
            {
                Orbwalker.Attack = true;
                Orbwalker.Movement = true;
                Variable.UsingE = false;
            });
        }

        private static void Game_OnGameUpdate(EventArgs args)
        {
            if (Variable.Player.IsDead)
            {
                return;
            }

            Internal.ComboModeSwitch();

            var target = TargetSelector.GetTarget(Variable.Q.Range);
            var comboKey = Orbwalker.ActiveMode == OrbwalkerMode.Orbwalk;
            var harassKey = Orbwalker.ActiveMode == OrbwalkerMode.Hybrid;
            var farmKey = Orbwalker.ActiveMode == OrbwalkerMode.LaneClear;
            var jungleClearKey = Orbwalker.ActiveMode == OrbwalkerMode.LaneClear;

            if (comboKey && target != null)
            {
                Internal.Combo(target);
            }
            else
            {
                if (harassKey && target != null)
                {
                    Internal.Harass(target);
                }

                if (farmKey)
                {
                    Internal.Farm();
                }

                if (jungleClearKey)
                {
                    Internal.JungleClear();
                }

                var autoQ = Variable.Config["Harass"]["AutoQ"].GetValue<MenuKeyBind>().Active;
                if (!autoQ || target == null)
                {
                    return;
                }

                var autoQTurret = Variable.Config["Harass"]["AutoQTurret"].GetValue<MenuBool>().Value;
                if (autoQTurret
                    ? !Internal.UnderTurret(Variable.Player.Position, true)
                    : Internal.UnderTurret(Variable.Player.Position, true) &&
                      Variable.Player.Distance(target) <= Variable.Q.Range &&
                      Variable.Q.IsReady())
                {
                    Variable.Q.CastOnUnit(target);
                }
            }
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            var noDrawings = Variable.Config["Drawing"]["NoDrawings"].GetValue<MenuBool>().Value;
            if (noDrawings)
            {
                return;
            }

            foreach (
                var spell in
                    Variable.Spells.Where(
                        spell => Variable.Config["Drawing"]["Draw" + spell.Slot].GetValue<MenuBool>().Value)
                )
            {
                var menuColor = Variable.Config["Drawing"]["Color"][spell.Slot + "Color"].GetValue<MenuColor>().Color;
                var color = System.Drawing.Color.FromArgb(menuColor.A, menuColor.R, menuColor.G, menuColor.B);
                Drawing.DrawCircle(Variable.Player.Position, spell.Range, color);
            }

            var target = TargetSelector.GetTarget(Variable.Q.Range);
            var targetDraw = Variable.Config["Drawing"]["Target"].GetValue<MenuBool>().Value;
            if (targetDraw && target != null)
            {
                var menuColor = Variable.Config["Drawing"]["Color"]["TColor"].GetValue<MenuColor>().Color;
                var color = System.Drawing.Color.FromArgb(menuColor.A, menuColor.R, menuColor.G, menuColor.B);
                Drawing.DrawCircle(target.Position, target.BoundingRadius, color);
            }

            var currentComboMode = Variable.Config["Drawing"]["CurrentComboMode"].GetValue<MenuBool>().Value;
            if (!currentComboMode)
            {
                return;
            }

            var worldToScreen = Drawing.WorldToScreen(Variable.Player.Position);
            var comboMode = Variable.Config["Combo"]["ComboMode"].GetValue<MenuList<string>>().Index;
            switch (comboMode)
            {
                case 0:
                    Drawing.DrawText(worldToScreen[0] - 130, worldToScreen[1], System.Drawing.Color.White,
                        "Normal (Q-W-E with No Restrictions)");
                    break;
                case 1:
                    Drawing.DrawText(worldToScreen[0] - 175, worldToScreen[1], System.Drawing.Color.White,
                        "Ganking (W-E-Q - Will not E until target immovable)");
                    break;
            }
        }

        private static void InterruptableSpell_OnInterruptableTarget(object sender,
            InterruptableSpell.InterruptableTargetEventArgs interruptableTargetEventArgs)
        {
            var stopChannel = Variable.Config["Misc"]["StopChannel"].GetValue<MenuBool>().Value;
            if (!stopChannel)
            {
                return;
            }

            var unit = interruptableTargetEventArgs.Sender;
            if (!(Variable.Player.Distance(unit) <= Variable.W.Range) || !Variable.W.IsReady())
            {
                return;
            }

            Variable.W.CastOnUnit(unit);
        }
    }
}