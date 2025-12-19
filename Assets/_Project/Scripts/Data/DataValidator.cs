using UnityEngine;

namespace TowerOffense.Data
{
    public class DataValidator
    {
        public static void ValidateAll(JsonDatabase db)
        {
            if (db == null)
            {
                Debug.LogWarning("DataValidator received null JsonDatabase.");
                return;
            }

            ValidateUniqueIds("Unit", db.Units, unit => unit.id);
            ValidateUniqueIds("Spell", db.Spells, spell => spell.id);
            ValidateUniqueIds("Tower", db.Towers, tower => tower.id);
            ValidateUniqueIds("Trap", db.Traps, trap => trap.id);
            ValidateUniqueIds("Level", db.Levels, level => level.id);

            ValidateLevelReferences(db);
        }

        private static void ValidateUniqueIds<T>(string label, System.Collections.Generic.List<T> entries, System.Func<T, string> selector)
        {
            if (entries == null)
            {
                Debug.LogWarning($"{label} list was null.");
                return;
            }

            for (int index = 0; index < entries.Count; index++)
            {
                string id = selector(entries[index]);
                if (string.IsNullOrWhiteSpace(id))
                {
                    Debug.LogWarning($"{label} at index {index} has an empty id.");
                    continue;
                }

                for (int otherIndex = index + 1; otherIndex < entries.Count; otherIndex++)
                {
                    string otherId = selector(entries[otherIndex]);
                    if (id == otherId)
                    {
                        Debug.LogWarning($"{label} id '{id}' is duplicated at indices {index} and {otherIndex}.");
                    }
                }
            }
        }

        private static void ValidateLevelReferences(JsonDatabase db)
        {
            if (db.Levels == null)
            {
                Debug.LogWarning("Level list was null.");
                return;
            }

            for (int levelIndex = 0; levelIndex < db.Levels.Count; levelIndex++)
            {
                LevelDefinition level = db.Levels[levelIndex];
                if (level == null)
                {
                    Debug.LogWarning($"Level at index {levelIndex} is null.");
                    continue;
                }

                if (level.enemy_defenses != null)
                {
                    if (level.enemy_defenses.towers != null)
                    {
                        for (int towerIndex = 0; towerIndex < level.enemy_defenses.towers.Length; towerIndex++)
                        {
                            string towerId = level.enemy_defenses.towers[towerIndex].tower_id;
                            if (string.IsNullOrWhiteSpace(towerId))
                            {
                                Debug.LogWarning($"Level '{level.id}' tower placement at index {towerIndex} has empty tower_id.");
                            }
                            else if (db.FindTower(towerId) == null)
                            {
                                Debug.LogWarning($"Level '{level.id}' references missing tower_id '{towerId}'.");
                            }
                        }
                    }

                    if (level.enemy_defenses.traps != null)
                    {
                        for (int trapIndex = 0; trapIndex < level.enemy_defenses.traps.Length; trapIndex++)
                        {
                            string trapId = level.enemy_defenses.traps[trapIndex].trap_id;
                            if (string.IsNullOrWhiteSpace(trapId))
                            {
                                Debug.LogWarning($"Level '{level.id}' trap placement at index {trapIndex} has empty trap_id.");
                            }
                            else if (db.FindTrap(trapId) == null)
                            {
                                Debug.LogWarning($"Level '{level.id}' references missing trap_id '{trapId}'.");
                            }
                        }
                    }
                }

                if (level.player_rules != null)
                {
                    if (level.player_rules.starting_deck != null)
                    {
                        string[] unitCards = level.player_rules.starting_deck.unit_cards;
                        if (unitCards != null)
                        {
                            for (int unitIndex = 0; unitIndex < unitCards.Length; unitIndex++)
                            {
                                string unitId = unitCards[unitIndex];
                                if (string.IsNullOrWhiteSpace(unitId))
                                {
                                    Debug.LogWarning($"Level '{level.id}' unit_cards entry {unitIndex} is empty.");
                                }
                                else if (db.FindUnit(unitId) == null)
                                {
                                    Debug.LogWarning($"Level '{level.id}' references missing unit card '{unitId}'.");
                                }
                            }
                        }

                        string[] spellCards = level.player_rules.starting_deck.spell_cards;
                        if (spellCards != null)
                        {
                            for (int spellIndex = 0; spellIndex < spellCards.Length; spellIndex++)
                            {
                                string spellId = spellCards[spellIndex];
                                if (string.IsNullOrWhiteSpace(spellId))
                                {
                                    Debug.LogWarning($"Level '{level.id}' spell_cards entry {spellIndex} is empty.");
                                }
                                else if (db.FindSpell(spellId) == null)
                                {
                                    Debug.LogWarning($"Level '{level.id}' references missing spell card '{spellId}'.");
                                }
                            }
                        }
                    }

                    string[] heroPool = level.player_rules.hero_pool;
                    if (heroPool == null || heroPool.Length == 0)
                    {
                        Debug.LogWarning($"Level '{level.id}' has empty hero_pool.");
                    }
                    else
                    {
                        for (int heroIndex = 0; heroIndex < heroPool.Length; heroIndex++)
                        {
                            if (string.IsNullOrWhiteSpace(heroPool[heroIndex]))
                            {
                                Debug.LogWarning($"Level '{level.id}' hero_pool entry {heroIndex} is empty.");
                            }
                        }
                    }
                }
            }
        }
    }
}
