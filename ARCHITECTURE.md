# Tower Offense - Neue Architektur
## Technical Architecture Document

---

## Übersicht

Dieses Dokument beschreibt die technische Architektur für das überarbeitete Tower Offense Spielkonzept.

---

## 1. Neue Ordnerstruktur

```
Assets/_Project/
  Scripts/
    Core/
      (bestehend: GameBootstrapper, ServiceLocator, EventBus, GameConfig, GameTime)
      + EntityRegistry.cs
      + SceneFlowController.cs

    Data/
      (bestehend: JsonDatabase, JsonLoader, DataValidator, etc.)
      + CivilizationDefinition.cs
      + AbilityDefinition.cs
      + CivilizationsJsonRoot.cs
      (ändern: UnitDefinition, TowerDefinition, HeroDefinition)

    Gameplay/
      Economy/
        + GoldManager.cs
        + GoldRewardSystem.cs
        + CostSystem.cs

      Construction/
        + ConstructionSite.cs
        + BuilderController.cs
        + BuildQueue.cs
        + ConstructionManager.cs

      Civilization/
        + CivilizationManager.cs
        + CivilizationSelector.cs
        + UnitDeck.cs

      Abilities/
        + AbilityManager.cs
        + AbilityCooldown.cs
        + AbilityEffect.cs

      Heroes/
        + HeroManager.cs
        + HeroSpawner.cs
        + HeroAbility.cs

      (bestehend behalten: PathManager, BaseController, SpawnController)
      (ändern: LevelController, WaveController → entfernen)

    AI/
      + AICommander.cs
      + AIStrategy.cs
      + AIBuildPlanner.cs
      + AIUnitSpawner.cs
      + AIGoldManager.cs
      (bestehend: UnitBrain, TowerBrain)

    Progression/
      + FameManager.cs
      + UpgradeSystem.cs
      + ProgressionDatabase.cs
      + UnlockSystem.cs

    Combat/
      (bestehend: HealthComponent, DamageSystem, etc. - keine Änderungen)

    UI/
      + GoldDisplay.cs
      + UnitSpawnBar.cs
      + HeroButton.cs
      + AbilityButton.cs
      + CivilizationSelectUI.cs
      + UnitDeckSelectUI.cs
      + FameShopUI.cs
      (ändern: LevelHUD, ResultScreenView)

    Saving/
      (bestehend: SaveManager, PlayerProgress)
      (ändern: PlayerProgress → neue Felder für Fame, Upgrades)
```

---

## 2. Daten-Strukturen (JSON)

### civilizations.json

```json
{
  "civilizations": [
    {
      "id": "kingdom",
      "name": "Königreich",
      "description": "Tapfere Ritter und mächtige Magier",
      "color": "#3498db",
      "availableUnits": ["knight", "archer", "priest", "catapult", "swordsman"],
      "availableTowers": ["guard_tower", "ballista", "mage_tower"],
      "availableHeroes": ["king_arthur", "paladin"],
      "specialAbility": "divine_protection",
      "unlockCost": 0
    },
    {
      "id": "horde",
      "name": "Orks",
      "description": "Wilde Krieger und brutale Belagerungswaffen",
      "color": "#e74c3c",
      "availableUnits": ["orc_warrior", "wolf_rider", "shaman", "troll", "ram"],
      "availableTowers": ["spear_thrower", "flame_tower", "totem"],
      "availableHeroes": ["warchief_grok"],
      "specialAbility": "bloodlust",
      "unlockCost": 5000
    }
  ]
}
```

### units.json (erweitert)

```json
{
  "units": [
    {
      "id": "knight",
      "name": "Ritter",
      "civilization": "kingdom",
      "goldCost": 100,
      "goldReward": 60,
      "baseHP": 200,
      "baseDamage": 25,
      "baseSpeed": 3.0,
      "attackRange": 1.5,
      "attackSpeed": 1.0,
      "armor": 5,
      "upgradeLevels": [
        { "level": 1, "fameCost": 0, "hpBonus": 0, "damageBonus": 0 },
        { "level": 2, "fameCost": 100, "hpBonus": 50, "damageBonus": 5 },
        { "level": 3, "fameCost": 300, "hpBonus": 100, "damageBonus": 10 }
      ],
      "prefabPath": "Prefabs/Units/Knight"
    }
  ]
}
```

### towers.json (erweitert)

```json
{
  "towers": [
    {
      "id": "guard_tower",
      "name": "Wachturm",
      "civilization": "kingdom",
      "goldCost": 150,
      "goldReward": 100,
      "constructionTime": 5.0,
      "requiredBuilders": 3,
      "baseHP": 500,
      "baseDamage": 30,
      "attackRange": 10.0,
      "attackSpeed": 1.5,
      "projectileType": "arrow",
      "upgradeLevels": [
        { "level": 1, "fameCost": 0, "hpBonus": 0, "damageBonus": 0, "rangeBonus": 0 },
        { "level": 2, "fameCost": 200, "hpBonus": 100, "damageBonus": 10, "rangeBonus": 2.0 }
      ],
      "prefabPath": "Prefabs/Towers/GuardTower"
    }
  ]
}
```

### heroes.json (erweitert)

```json
{
  "heroes": [
    {
      "id": "king_arthur",
      "name": "König Artus",
      "civilization": "kingdom",
      "unlockCost": 1000,
      "spawnCooldown": 120.0,
      "baseHP": 500,
      "baseDamage": 50,
      "baseSpeed": 4.0,
      "attackRange": 2.0,
      "attackSpeed": 0.8,
      "armor": 10,
      "ability": {
        "id": "holy_strike",
        "cooldown": 30.0,
        "effects": [
          { "type": "damage_aoe", "value": 100, "radius": 5.0 }
        ]
      },
      "upgradeLevels": [
        { "level": 1, "fameCost": 0, "hpBonus": 0, "damageBonus": 0 },
        { "level": 2, "fameCost": 500, "hpBonus": 100, "damageBonus": 10 },
        { "level": 3, "fameCost": 1500, "hpBonus": 250, "damageBonus": 25 }
      ],
      "prefabPath": "Prefabs/Heroes/KingArthur"
    }
  ]
}
```

### abilities.json

```json
{
  "abilities": [
    {
      "id": "divine_protection",
      "name": "Göttlicher Schutz",
      "civilization": "kingdom",
      "description": "Alle Einheiten erhalten +50% Rüstung für 10 Sekunden",
      "cooldown": 90.0,
      "effects": [
        {
          "type": "buff_armor",
          "target": "all_friendly_units",
          "value": 0.5,
          "duration": 10.0
        }
      ],
      "vfxPrefab": "Prefabs/VFX/DivineProtection",
      "sfx": "divine_protection_cast"
    },
    {
      "id": "bloodlust",
      "name": "Blutdurst",
      "civilization": "horde",
      "description": "Alle Einheiten erhalten +30% Schaden für 15 Sekunden",
      "cooldown": 90.0,
      "effects": [
        {
          "type": "buff_damage",
          "target": "all_friendly_units",
          "value": 0.3,
          "duration": 15.0
        }
      ],
      "vfxPrefab": "Prefabs/VFX/Bloodlust",
      "sfx": "bloodlust_cast"
    }
  ]
}
```

### levels.json (erweitert)

```json
{
  "levels": [
    {
      "id": "level_01",
      "name": "Erste Schlacht",
      "description": "Zerstöre die feindliche Basis",
      "scenePath": "Scenes/Levels/Level01",
      "playerCivilization": "kingdom",
      "enemyCivilization": "horde",
      "startGold": 500,
      "aiDifficulty": "easy",
      "aiStrategy": "balanced",
      "fameReward": {
        "victory": 100,
        "defeat": 20,
        "bonus": [
          { "condition": "win_under_5_min", "fame": 50 },
          { "condition": "no_tower_lost", "fame": 30 }
        ]
      },
      "unlockRequirement": null
    },
    {
      "id": "level_02",
      "name": "Orks Angriff",
      "scenePath": "Scenes/Levels/Level02",
      "playerCivilization": "kingdom",
      "enemyCivilization": "horde",
      "startGold": 600,
      "aiDifficulty": "normal",
      "aiStrategy": "aggressive",
      "fameReward": {
        "victory": 150,
        "defeat": 30
      },
      "unlockRequirement": "level_01"
    }
  ]
}
```

---

## 3. Kern-Klassen (Neue)

### GoldManager.cs

```csharp
public class GoldManager : MonoBehaviour
{
    public int CurrentGold { get; private set; }
    public event Action<int> OnGoldChanged;

    public void Initialize(int startGold);
    public bool CanAfford(int cost);
    public bool SpendGold(int amount);
    public void AddGold(int amount);
    public void RewardKill(string unitId);
    public void RewardDestruction(string towerId);
}
```

### GoldRewardSystem.cs

```csharp
public class GoldRewardSystem
{
    private JsonDatabase database;

    public int CalculateUnitReward(string unitId);
    public int CalculateTowerReward(string towerId);
    public int CalculateComboBonus(int killCount, float timeWindow);
}
```

### ConstructionSite.cs

```csharp
public class ConstructionSite : MonoBehaviour
{
    public string TowerID { get; private set; }
    public int RequiredBuilders { get; private set; }
    public int CurrentBuilders { get; private set; }
    public float ConstructionProgress { get; private set; }

    public void AddBuilder();
    public void OnBuilderArrived();
    public void CompleteConstruction();
    public void TakeDamage(float damage);
}
```

### BuilderController.cs

```csharp
public class BuilderController : MonoBehaviour
{
    private ConstructionSite targetSite;

    public void AssignToSite(ConstructionSite site);
    public void MoveToSite();
    public void OnReachedSite();
}
```

### ConstructionManager.cs

```csharp
public class ConstructionManager : MonoBehaviour
{
    private List<ConstructionSite> activeSites = new();
    private Queue<BuilderController> availableBuilders = new();

    public ConstructionSite PlaceTower(string towerId, Vector3 position);
    public void SpawnBuilder();
    public void AssignBuilders();
}
```

### AICommander.cs

```csharp
public class AICommander : MonoBehaviour
{
    public enum Difficulty { Easy, Normal, Hard }
    public enum Strategy { Aggressive, Defensive, Balanced }

    private GoldManager goldManager;
    private AIStrategy strategy;
    private AIBuildPlanner buildPlanner;
    private AIUnitSpawner unitSpawner;

    public void Initialize(Difficulty difficulty, Strategy strategyType);
    public void Update(); // Entscheidungen treffen
}
```

### AIStrategy.cs

```csharp
public class AIStrategy
{
    public virtual void DecideActions(AICommander commander);
    protected bool ShouldSpawnUnit();
    protected bool ShouldBuildTower();
    protected string SelectUnitToSpawn();
    protected string SelectTowerToBuild();
}

public class AggressiveStrategy : AIStrategy { }
public class DefensiveStrategy : AIStrategy { }
public class BalancedStrategy : AIStrategy { }
```

### CivilizationManager.cs

```csharp
public class CivilizationManager
{
    private Dictionary<string, CivilizationDefinition> civilizations;

    public CivilizationDefinition GetCivilization(string id);
    public List<UnitDefinition> GetAvailableUnits(string civId);
    public List<TowerDefinition> GetAvailableTowers(string civId);
    public List<HeroDefinition> GetAvailableHeroes(string civId);
}
```

### UnitDeck.cs

```csharp
public class UnitDeck
{
    public string CivilizationID { get; private set; }
    public List<string> SelectedUnits { get; private set; } // Max 5
    public string SelectedHero { get; private set; }

    public void SetCivilization(string civId);
    public bool AddUnit(string unitId);
    public void RemoveUnit(string unitId);
    public void SetHero(string heroId);
    public bool IsValid(); // 5 Units + 1 Hero
}
```

### HeroManager.cs

```csharp
public class HeroManager : MonoBehaviour
{
    private string heroId;
    private float cooldown;
    private float lastSpawnTime;
    private GameObject currentHero;

    public void Initialize(string heroId);
    public bool CanSpawn();
    public void SpawnHero();
    public void OnHeroDied();
    public float GetCooldownRemaining();
}
```

### AbilityManager.cs

```csharp
public class AbilityManager : MonoBehaviour
{
    private string abilityId;
    private float cooldown;
    private float lastUseTime;

    public void Initialize(string abilityId);
    public bool CanUse();
    public void UseAbility();
    public void ApplyEffects(AbilityDefinition ability);
    public float GetCooldownRemaining();
}
```

### FameManager.cs

```csharp
public class FameManager
{
    public int TotalFame { get; private set; }
    public event Action<int> OnFameChanged;

    public void AddFame(int amount);
    public bool SpendFame(int amount);
    public void RewardLevelComplete(string levelId, bool victory);
}
```

### UpgradeSystem.cs

```csharp
public class UpgradeSystem
{
    private Dictionary<string, int> unitLevels; // unitId -> level
    private Dictionary<string, int> heroLevels;

    public int GetUnitLevel(string unitId);
    public bool UpgradeUnit(string unitId);
    public int GetUpgradeCost(string unitId, int currentLevel);
    public void ApplyUpgrades(UnitController unit);
}
```

---

## 4. Erweiterte Klassen (Änderungen)

### LevelController.cs (massiv geändert)

**Alt:** Verwaltete Waves und Planning-Phase
**Neu:** Verwaltet Live-Schlacht

```csharp
public class LevelController : MonoBehaviour
{
    private GoldManager playerGold;
    private GoldManager aiGold;
    private BaseController playerBase;
    private BaseController aiBase;
    private AICommander aiCommander;
    private HeroManager playerHeroManager;
    private AbilityManager playerAbilityManager;

    public void StartLevel(LevelDefinition levelDef);
    public void OnBaseDestroyed(BaseController destroyedBase);
    public void EndLevel(bool playerVictory);
}
```

### UnitDefinition.cs (erweitert)

```csharp
[System.Serializable]
public class UnitDefinition
{
    public string id;
    public string name;
    public string civilization; // NEU
    public int goldCost; // NEU
    public int goldReward; // NEU
    public float baseHP;
    public float baseDamage;
    public float baseSpeed;
    public float attackRange;
    public float attackSpeed;
    public float armor;
    public List<UpgradeLevel> upgradeLevels; // NEU
    public string prefabPath;
}

[System.Serializable]
public class UpgradeLevel
{
    public int level;
    public int fameCost;
    public float hpBonus;
    public float damageBonus;
    public float speedBonus;
}
```

### BaseController.cs (leicht geändert)

```csharp
public class BaseController : MonoBehaviour
{
    public enum Team { Player, AI }
    public Team team;

    public float MaxHP { get; private set; }
    public float CurrentHP { get; private set; }

    public void TakeDamage(float damage);
    public void OnDestroyed(); // triggert LevelController.OnBaseDestroyed
}
```

---

## 5. Event-System (EventBus erweitern)

```csharp
public static class GameEvents
{
    // Gold
    public static event Action<int> OnGoldChanged;
    public static event Action<string, int> OnGoldSpent;
    public static event Action<string, int> OnGoldEarned;

    // Combat
    public static event Action<string> OnUnitKilled;
    public static event Action<string> OnTowerDestroyed;

    // Construction
    public static event Action<ConstructionSite> OnConstructionStarted;
    public static event Action<ConstructionSite> OnConstructionCompleted;
    public static event Action<ConstructionSite> OnConstructionCancelled;

    // Heroes & Abilities
    public static event Action<string> OnHeroSpawned;
    public static event Action<string> OnAbilityUsed;

    // Level
    public static event Action<bool> OnLevelEnded;
    public static event Action<int> OnFameAwarded;
}
```

---

## 6. Service-Locator Updates

```csharp
public class ServiceLocator
{
    public static JsonDatabase Database { get; private set; }
    public static GoldManager PlayerGold { get; private set; }
    public static GoldManager AIGold { get; private set; }
    public static CivilizationManager CivilizationManager { get; private set; }
    public static FameManager FameManager { get; private set; }
    public static UpgradeSystem UpgradeSystem { get; private set; }
    public static ConstructionManager ConstructionManager { get; private set; }
    public static HeroManager HeroManager { get; private set; }
    public static AbilityManager AbilityManager { get; private set; }
}
```

---

## 7. Spieler-Input-Handling

### UnitSpawnInput.cs

```csharp
public class UnitSpawnInput : MonoBehaviour
{
    private UnitDeck playerDeck;
    private GoldManager playerGold;

    public void OnUnitButtonClicked(int slotIndex); // 0-4
    public void TrySpawnUnit(string unitId);
}
```

### TowerPlacementInput.cs

```csharp
public class TowerPlacementInput : MonoBehaviour
{
    private ConstructionManager constructionManager;
    private GoldManager playerGold;

    public void EnterPlacementMode(string towerId);
    public void OnTileClicked(Vector3 position);
    public void PlaceTower();
}
```

---

## 8. Migrations-Strategie

### Was bleibt unverändert?
- **Combat-System** (HealthComponent, DamageSystem, TargetingSystem)
- **Projectile-System**
- **PathManager** (Units folgen weiterhin Pfaden zur Base)
- **Audio-System**
- **Saving-Infrastruktur** (SaveManager, JsonSaveSerializer)

### Was wird entfernt?
- **DeckManager** (Karten-System)
- **HandManager** (Hand von Karten)
- **CardPlayResolver**
- **WaveController** (Wellen-basiertes Spawning)
- **LevelStateMachine** (Planning/Attack-Phasen)

### Was wird ersetzt?
- WaveController → **Kontinuierliches Spawning per Klick**
- DeckManager → **UnitDeck** (5 Units auswählen)
- Planning Phase → **Live Battle** (Echtzeit)

---

## 9. Implementierungs-Reihenfolge

### Sprint 1: Gold & Economy
1. GoldManager.cs
2. GoldRewardSystem.cs
3. CostSystem.cs
4. UI: GoldDisplay.cs
5. UnitDefinition erweitern (goldCost, goldReward)
6. TowerDefinition erweitern

### Sprint 2: Construction System
1. ConstructionSite.cs
2. BuilderController.cs
3. ConstructionManager.cs
4. BuildQueue.cs
5. UI: Baustellen-Anzeige

### Sprint 3: AI Opponent
1. AICommander.cs
2. AIStrategy.cs (Aggressive, Defensive, Balanced)
3. AIBuildPlanner.cs
4. AIUnitSpawner.cs
5. AIGoldManager Integration

### Sprint 4: Civilization System
1. CivilizationDefinition.cs
2. CivilizationManager.cs
3. UnitDeck.cs
4. CivilizationSelector.cs
5. JSON: civilizations.json
6. UI: CivilizationSelectUI, UnitDeckSelectUI

### Sprint 5: Heroes & Abilities
1. HeroManager.cs
2. HeroDefinition erweitern
3. AbilityManager.cs
4. AbilityDefinition.cs
5. JSON: abilities.json
6. UI: HeroButton, AbilityButton

### Sprint 6: Fame & Progression
1. FameManager.cs
2. UpgradeSystem.cs
3. ProgressionDatabase.cs
4. PlayerProgress erweitern
5. UI: FameShopUI

### Sprint 7: Level Flow Refactor
1. LevelController.cs neu schreiben
2. Remove: WaveController, LevelStateMachine
3. BaseController anpassen
4. LevelDefinition erweitern
5. UI: LevelHUD anpassen

### Sprint 8: Testing & Balancing
1. Test-Level erstellen
2. Balancing-Werte anpassen
3. Bug-Fixes
4. Performance-Optimierung

---

**Dieses Architektur-Dokument dient als Blueprint für die Implementierung des neuen Spielkonzepts.**
