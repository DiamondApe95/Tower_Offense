# Game Logic & UI Improvements

Diese Datei dokumentiert alle durchgeführten Verbesserungen an der Spiellogik und dem UI-Design.

## 1. Baumeister-Logik Verbesserungen

### Status: ✅ Bereits korrekt implementiert

Die Baumeister-Logik war bereits vollständig implementiert:

- **Automatisches Spawnen**: Baumeister spawnen automatisch beim Platzieren von Türmen/Fallen
- **Spawn-Intervall**: 2 Sekunden zwischen jedem Baumeister (konfigurierbar in `ConstructionManager.builderSpawnInterval`)
- **Verknüpfung mit Baustellen**: Baumeister sind fest mit ihren Baustellen verbunden via `AssignToSite()`
- **Automatischer Tod**: Baumeister sterben automatisch wenn die Baustelle zerstört wird (siehe `BuilderController:198-210`)
- **Prefab**: Verwendet `Unit_Builder.prefab` aus `Assets/_Project/Prefab/`

### Relevante Dateien:
- `Assets/_Project/Scripts/Gameplay/Construction/ConstructionManager.cs`
- `Assets/_Project/Scripts/Gameplay/Construction/BuilderController.cs`
- `Assets/_Project/Scripts/Gameplay/Construction/ConstructionSite.cs`
- `Assets/_Project/Prefab/Unit_Builder.prefab`

### Konfiguration:
Im `ConstructionManager`:
- `builderSpawnInterval` = 2.0s
- `towerBuilderCount` = 3
- `trapBuilderCount` = 1
- `builderPrefab` = Verweis auf `Unit_Builder.prefab`

---

## 2. GUI-Verbesserungen für Smartphone

### Status: ✅ Implementiert

Alle GUI-Elemente wurden für bessere Sichtbarkeit auf Smartphones vergrößert.

### Änderungen in `LiveBattleHUD.cs`:

#### Einheiten-Spawn-Buttons:
- **Button-Größe**: 80x100 → **140x160** (+75% größer)
- **Name-Schrift**: 12pt → **24pt** (2x größer)
- **Kosten-Schrift**: 14pt → **28pt** (2x größer)

### Betroffene Datei:
- `Assets/_Project/Scripts/UI/LiveBattle/LiveBattleHUD.cs:157-202`

### Vor/Nach Vergleich:
```csharp
// VORHER
rectTransform.sizeDelta = new Vector2(80, 100);
labelText.fontSize = 12;
costText.fontSize = 14;

// NACHHER
rectTransform.sizeDelta = new Vector2(140, 160);
labelText.fontSize = 24;
costText.fontSize = 28;
```

---

## 3. HUD Safe Area Handling

### Status: ✅ Implementiert

Neues `SafeAreaHandler` Script erstellt für automatische Anpassung an Geräte-Safe-Areas.

### Features:
- Automatische Erkennung von Device-Safe-Areas (Notches, abgerundete Ecken)
- Zusätzliche Margin-Unterstützung (konfigurierbar)
- Mindest-Margin für Geräte ohne Safe-Area-Einschränkungen
- Automatische Aktualisierung bei Orientierungswechsel

### Neue Datei:
- `Assets/_Project/Scripts/UI/SafeAreaHandler.cs`

### Verwendung:
1. `SafeAreaHandler` Komponente zum HUD Canvas hinzufügen
2. Konfigurieren:
   - `additionalMargin` = 20px (Standard)
   - `minimumMargin` = 30px (Standard)

### Konfiguration:
```csharp
[SerializeField] private float additionalMargin = 20f;  // Extra Abstand
[SerializeField] private float minimumMargin = 30f;     // Mindest-Abstand
```

---

## 4. Zielpriorisierung für Türme

### Status: ✅ Implementiert

Türme verwenden jetzt intelligente Zielpriorisierung mit Distanzberücksichtigung.

### Prioritäts-Reihenfolge:
1. **Einheiten** (Units) - Höchste Priorität
2. **Türme** (Towers) - Mittlere Priorität
3. **Basis** (Base) - Niedrigste Priorität

**Innerhalb jeder Kategorie**: Nächstes Ziel wird priorisiert (nach Distanz)

### Implementierung:
- `TowerController.AcquireTarget()` überarbeitet
- Neue Methoden hinzugefügt:
  - `FindEnemyUnits()` - Findet feindliche Einheiten
  - `FindEnemyTowers()` - Findet feindliche Türme
  - `FindEnemyBase()` - Findet feindliche Basis

### Betroffene Datei:
- `Assets/_Project/Scripts/Gameplay/Entities/TowerController.cs:144-293`

### Logik:
```csharp
// 1. Versuche feindliche Einheiten zu finden
Transform target = FindEnemyUnits();

// 2. Falls keine Einheiten, finde Türme
if (target == null)
    target = FindEnemyTowers();

// 3. Falls keine Türme, finde Basis
if (target == null)
    target = FindEnemyBase();
```

---

## 5. Prefab-Konfiguration

### Status: ℹ️ Dokumentiert

Das PrefabRegistry-System ist bereits vorhanden und muss nur korrekt konfiguriert werden.

### Verfügbare Prefabs in `Assets/_Project/Prefab/`:
- **Einheiten**:
  - `Unit_Archer.prefab`
  - `Unit_Slinger.prefab`
  - `Unit_Builder.prefab`

- **Türme**:
  - `Tower_Basic.prefab`
  - `Tower_Archery.prefab`
  - `Tower_Crossbow.prefab`
  - `Tower_Slinger.prefab`

- **Basen**:
  - `Prefab_Roman_Base.prefab`
  - `Prefab_Enemy_Base.prefab`

- **Projektile**:
  - `Projectile.prefab`
  - `Projectile_Arrow.prefab`
  - `Projectile_Pilum.prefab`

### Konfiguration im Unity Editor:

1. **PrefabRegistry GameObject finden** (meist in der Hauptszene)
2. **Prefabs manuell zuweisen** in der `entries` Liste:
   - ID: `unit_archer` → Prefab: `Unit_Archer`
   - ID: `unit_slinger` → Prefab: `Unit_Slinger`
   - ID: `unit_builder` → Prefab: `Unit_Builder`
   - ID: `tower_basic` → Prefab: `Tower_Basic`
   - ID: `tower_archery` → Prefab: `Tower_Archery`
   - ID: `tower_crossbow` → Prefab: `Tower_Crossbow`
   - ID: `tower_slinger` → Prefab: `Tower_Slinger`
   - ID: `base_roman` → Prefab: `Prefab_Roman_Base`
   - ID: `base_enemy` → Prefab: `Prefab_Enemy_Base`

3. **Oder: Auto-Register verwenden**:
   - Im Inspector auf PrefabRegistry → Rechtsklick → "Auto-Register From Prefab Folder"
   - Dies scannt automatisch den Prefab-Ordner

### Relevante Datei:
- `Assets/_Project/Scripts/Data/PrefabRegistry.cs`

### Wichtig für AI und Spieler:
Beide Teams (Spieler und AI) können ihre Türme/Einheiten auswählen über:
- **Decks**: PlayerDeck definiert verfügbare Einheiten
- **Build-System**: BuildManager verwendet PrefabRegistry für Tower-Bau
- **JSON-Definitionen**: JsonDatabase enthält Stats und Konfiguration

---

## 6. Zusammenfassung der Änderungen

### Geänderte Dateien:
1. ✅ `Assets/_Project/Scripts/UI/LiveBattle/LiveBattleHUD.cs` - GUI-Skalierung
2. ✅ `Assets/_Project/Scripts/Gameplay/Entities/TowerController.cs` - Zielpriorisierung
3. ✅ `Assets/_Project/Scripts/UI/SafeAreaHandler.cs` - **NEU** - Safe Area Support

### Neue Features:
- ✅ Größere Buttons und Schrift für Smartphone
- ✅ Safe Area Handling für Geräte mit Notches
- ✅ Intelligente Zielpriorisierung (Units > Towers > Base)
- ✅ Distanz-basierte Auswahl innerhalb jeder Priorität

### Baumeister-System:
- ✅ Bereits korrekt implementiert
- ✅ Verwendet `Unit_Builder.prefab`
- ✅ 2 Sekunden Spawn-Intervall
- ✅ Automatischer Tod bei Baustellen-Zerstörung

---

## 7. Nächste Schritte für Unity Editor

### Erforderliche Konfiguration:

1. **SafeAreaHandler hinzufügen**:
   - Öffne die LiveBattle-Szene
   - Finde das HUD Canvas
   - Füge `SafeAreaHandler` Komponente hinzu
   - Setze `additionalMargin` = 20
   - Setze `minimumMargin` = 30

2. **PrefabRegistry konfigurieren**:
   - Finde PrefabRegistry GameObject in der Szene
   - Entweder manuell Prefabs zuweisen (siehe Sektion 5)
   - Oder Auto-Register verwenden (Rechtsklick → "Auto-Register From Prefab Folder")

3. **ConstructionManager prüfen**:
   - Stelle sicher dass `builderPrefab` auf `Unit_Builder.prefab` verweist
   - Prüfe Builder-Spawn-Points (sollten auf Player/Enemy Base zeigen)

4. **Testen**:
   - Teste auf einem Smartphone oder Simulator
   - Prüfe ob UI-Elemente gut sichtbar sind
   - Prüfe ob Safe Areas korrekt berücksichtigt werden
   - Teste Baumeister-Spawning und Lebenszyklus
   - Teste Tower-Targeting-Priorisierung

---

## 8. Technische Details

### GUI-Skalierungs-Strategie:
- Alle UI-Elemente 75-100% vergrößert
- Schriftgrößen verdoppelt (12→24, 14→28)
- Empfohlene minimale Touch-Target-Größe: 44x44pt (iOS), 48x48dp (Android)
- Unsere Buttons: 140x160 ≈ 70x80pt @ 2x DPI ✅

### Safe Area Implementation:
- Verwendet `Screen.safeArea` API
- Konvertiert zu normalisierten Anchor-Koordinaten
- Update bei Orientierungswechsel
- Mindest-Margin für ältere Geräte

### Targeting-Algorithmus:
```
foreach priority_level (Units, Towers, Base):
    candidates = find_all_in_range(priority_level)
    if candidates.any():
        return nearest(candidates)
return null
```

### Performance:
- Target-Scan: 4x pro Sekunde (scanTimer = 0.25s)
- UI-Update: 10x pro Sekunde (REFRESH_INTERVAL = 0.1s)
- EntityRegistry-Cache für effiziente Lookups

---

## Ende der Dokumentation

Alle angeforderten Verbesserungen wurden implementiert und dokumentiert.
