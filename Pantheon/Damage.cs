using LeagueSharp;
using LeagueSharp.SDK.Core.Wrappers;

namespace Pantheon
{
    internal class Damage
    {
        public static double SpellQ(Obj_AI_Base target)
        {
            return Variable.Player.CalculateDamage(target, DamageType.Physical,
                new[] {65, 105, 145, 185, 225}[Variable.Q.Level - 1] + 1.4*Variable.Player.FlatPhysicalDamageMod);
        }

        public static double SpellW(Obj_AI_Base target)
        {
            return Variable.Player.CalculateDamage(target, DamageType.Magical,
                new[] {50, 75, 100, 125, 150}[Variable.W.Level - 1] + 1.0*Variable.Player.FlatMagicDamageMod);
        }

        public static double SpellE(Obj_AI_Base target)
        {
            return Variable.Player.CalculateDamage(target, DamageType.Physical,
                new[] {80, 140, 200, 260, 320}[Variable.W.Level - 1] + 3.6*Variable.Player.FlatPhysicalDamageMod);
        }

        public static double Ignite(Obj_AI_Hero target)
        {
            return Variable.Player.CalculateDamage(target, DamageType.True, 70 + ((Variable.Player.Level - 1)*20));
        }
    }
}