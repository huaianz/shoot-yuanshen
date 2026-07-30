public static class RoleStatsCalculator
{
    public static (float attack, float defense, float moveSpeed, float maxHealth, float maxArmor)
        CalculateFinalStats(Character character, WeaponItem weapon)
    {
        float attack = character.characterATK;
        float defense = character.characterDEF;
        float moveSpeed = 5f;
        float maxHealth = character.characterHP;
        float maxArmor = 0f;

        if (weapon != null)
        {
            var template = InventoryManager.INSTANCE.weaponData?.GetWeaponByID(weapon.itemID);
            if (template != null)
            {
                attack += template.weaponATK;
            }
        }

        return (attack, defense, moveSpeed, maxHealth, maxArmor);
    }
}