using System.Collections.Generic;
using LeagueSharp;
using LeagueSharp.SDK.Core.UI.IMenu;
using LeagueSharp.SDK.Core.Wrappers;

namespace Pantheon
{
    internal class Variable
    {
        public const string CharName = "Pantheon";
        public static List<Spell> Spells = new List<Spell>();
        public static Spell Q;
        public static Spell W;
        public static Spell E;
        public static SpellSlot IgniteSlot;
        //public static SpellSlot SmiteSlot;
        public static Menu Config;
        public static Obj_AI_Hero Player = ObjectManager.Player;
        public static bool UsingE;
    }
}