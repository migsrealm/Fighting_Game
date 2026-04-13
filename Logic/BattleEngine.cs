using GoonWarfareX.Models;

namespace GoonWarfareX.Logic;

public static class BattleEngine
{
    public static float GetDamage(Character attacker, Character defender)
    {
        float damage = attacker.AttackPower * (1 - defender.DefenseGrade);
        
        // UDD Elemental Advantage Logic
        if (attacker.ElementType == Element.Water && defender.ElementType == Element.Fire)
            damage *= 1.5f;

        return damage;
    }
}