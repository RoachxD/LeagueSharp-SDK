using System.Threading;
using LeagueSharp;
using LeagueSharp.SDK.Core;
using LeagueSharp.SDK.Core.Events;
using LeagueSharp.SDK.Core.Extensions;
using LeagueSharp.SDK.Core.UI.IMenu;
using LeagueSharp.SDK.Core.UI.IMenu.Values;
using LeagueSharp.SDK.Core.Utils;
using SharpDX;

namespace Advanced_Turn_Around
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            Load.OnLoad += delegate
            {
                var onGameLoadThread = new Thread(Game_OnGameLoad);
                onGameLoadThread.Start();
            };
        }

        private static void Game_OnGameLoad()
        {
            Internal.AddChampions();

            Variable.Config = new Menu("ATA", "Roach's Advanced Turn Around#", true);

            Variable.Config.Add(new MenuBool("Enabled", "Enable the Script", true));

            var Champions = Variable.Config.Add(new Menu("CAS", "Champions and Spells"));
            foreach (var champ in Variable.ExistingChampions)
            {
                var Champion = Champions.Add(new Menu(champ.CharName, champ.CharName + "'s Spells to Avoid"));
                Champion.Add(new MenuBool(champ.Slot.ToString(), champ.SpellName, true));
            }

            Variable.Config.Attach();

            Game.PrintChat(
                "<font color=\"#FF440A\">Advanced Turn Around# -</font> <font color=\"#FFFFFF\">Loaded</font>");

            Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;
        }

        private static void Obj_AI_Base_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            var enabled = Variable.Config["Enabled"].GetValue<MenuBool>().Value;
            if (!enabled || !Variable.Player.IsTargetable || (sender == null || sender.Team == Variable.Player.Team))
            {
                return;
            }

            foreach (var champ in Variable.ExistingChampions)
            {
                var champion = Variable.Config["CAS"][champ.CharName];
                if (champion == null || champion[champ.Slot.ToString()] == null || !champion[champ.Slot.ToString()].GetValue<MenuBool>().Value)
                {
                    continue;
                }

                if (champ.Slot != (sender as Obj_AI_Hero).GetSpellSlot(args.SData.Name) ||
                    (!(Variable.Player.Distance(sender.Position) <= champ.Range) && args.Target != Variable.Player))
                {
                    continue;
                }

                var vector =
                    new Vector3(
                        Variable.Player.Position.X +
                        ((sender.Position.X - Variable.Player.Position.X)*(Internal.MoveTo(champ.Movement))/
                         Variable.Player.Distance(sender.Position)),
                        Variable.Player.Position.Y +
                        ((sender.Position.Y - Variable.Player.Position.Y)*(Internal.MoveTo(champ.Movement))/
                         Variable.Player.Distance(sender.Position)), 0);
                Variable.Player.IssueOrder(GameObjectOrder.MoveTo, vector);
                Orbwalker.Movement = false;
                DelayAction.Add((int) (champ.CastTime + 0.1)*1000, () => Orbwalker.Movement = true);
            }
        }
    }
}