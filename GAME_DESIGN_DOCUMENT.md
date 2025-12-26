# Tower Offense - Neues Spielkonzept
## Game Design Document

---

## Executive Summary

**Genre:** Competitive RTS/Tower Defense Hybrid
**Perspektive:** 3D (Third-Person/Isometric)
**Kern-Gameplay:** Zwei Armeen treffen aufeinander und versuchen, mit Einheiten und Türmen die gegnerische Basis zu zerstören
**Progression:** Level-basiert mit Weltkarte, Fame-System für Upgrades

---

## 1. Kernkonzept

### Spielmodus: Live Battle (PvP-Stil gegen KI)

- **Symmetrisches Gameplay**: Beide Seiten (Spieler & KI) haben identische Möglichkeiten
- **Gold-Wirtschaft**: Gold zum Kaufen von Einheiten, Türmen, Fallen und Hindernissen
- **Zivilisationen-System**: Verschiedene Zivilisationen mit einzigartigen Einheiten
- **Baumeister-System**: Gebäude werden durch Baumeister errichtet
- **Siegbedingung**: Zerstörung der gegnerischen Basis

---

## 2. Zivilisationen-System

### Konzept
- Jede Karte hat **2 festgelegte Zivilisationen** (eine für Spieler, eine für KI)
- Jede Zivilisation hat **einzigartige Einheiten, Türme, und Helden**
- Vor jedem Level wählt der Spieler:
  - **5 Einheiten-Typen** aus der Zivilisation (sein "Deck")
  - **1 Held** aus der Zivilisation

### Beispiel-Zivilisationen (Initial)
1. **Königreich (Kingdom)**
   - Einheiten: Schwertkämpfer, Bogenschützen, Ritter, Priester, Katapult
   - Türme: Wachturm, Ballista, Magierturm
   - Held: König Artus

2. **Orks (Horde)**
   - Einheiten: Krieger, Wolfssreiter, Schamane, Troll, Rammbock
   - Türme: Speerwerfer, Flammenwerfer, Totemturm
   - Held: Kriegshäuptling Grok

3. **Untote (Undead)**
   - Einheiten: Skelett, Zombie, Geist, Nekromant, Knochengolem
   - Türme: Seelenturm, Giftturm, Knochenspieß
   - Held: Lich König

---

## 3. Gold-Wirtschaft

### Gold-Quellen
1. **Start-Gold**: Jedes Level beginnt mit X Gold (je nach Schwierigkeit)
2. **Passive Einnahmen**: Kleine Gold-Generierung über Zeit (optional)
3. **Kill-Belohnungen**:
   - Jede getötete feindliche Einheit → Gold
   - Jeder zerstörte feindliche Turm → Gold
   - Bonus für Combo-Kills (mehrere Einheiten schnell hintereinander)

### Gold-Ausgaben
- **Einheiten spawnen** (aus gewähltem Deck)
- **Türme bauen** (Baustelle + Baumeister)
- **Fallen platzieren**
- **Hindernisse errichten**

---

## 4. Baumeister & Konstruktions-System

### Konzept
Türme, Fallen und Hindernisse werden nicht sofort gebaut, sondern durchlaufen einen Bauprozess:

### Bauprozess
1. **Bauplatz auswählen** (auf "Build Tiles")
2. **Gold sofort abziehen**
3. **Baustelle wird platziert** (sichtbares Gerüst/Fundament)
4. **Baumeister müssen zur Baustelle laufen**
5. **Nach X Baumeistern (z.B. 3) → Gebäude fertiggestellt**

### Baumeister-Einheit
- **Baumeister spawnen automatisch** von der Basis (oder können gekauft werden)
- Gehen automatisch zur nächsten offenen Baustelle
- Können von Feinden getötet werden (unterbricht Bauprozess)

### Zerstörbare Baustellen
- **Baustellen haben HP** und können angegriffen werden
- Wenn Baustelle zerstört wird → Gold-Verlust (Teilrückerstattung?)
- Fertige Türme können ebenfalls zerstört werden

---

## 5. Spielablauf (Level)

### Phase 1: Vorbereitung
- Level-Auswahl auf Weltkarte
- **Zivilisation wird durch Karte vorgegeben**
- Spieler wählt:
  - 5 Einheiten-Typen
  - 1 Held

### Phase 2: Level Start
- Spieler und KI starten an eigener **Basis**
- Beide erhalten Start-Gold
- Einheiten spawnen an der Basis

### Phase 3: Live-Schlacht
- **Echtzeit-Gameplay** (keine rundenbasierte Strategie)
- Spieler kann jederzeit:
  - Einheiten kaufen und spawnen (an Basis)
  - Türme/Fallen bauen (auf Build Tiles)
  - Helden aktivieren (Cooldown beachten)
  - Spezialfähigkeiten einsetzen (Cooldown beachten)

- **KI spielt nach gleichen Regeln**:
  - Bekommt Gold für Kills
  - Baut Türme und spawnt Einheiten
  - Nutzt Helden und Fähigkeiten

### Phase 4: Sieg/Niederlage
- **Sieg**: Gegnerische Basis zerstört
- **Niederlage**: Eigene Basis zerstört
- **Belohnung**: Fame + ggf. neue Zivilisationen/Einheiten freischalten

---

## 6. Helden-System

### Held-Mechanik
- **1 Held pro Level** (vor Level ausgewählt)
- **Kostenlos spawnbar** (kein Gold)
- **Langer Cooldown** (z.B. 120 Sekunden)
- **Stärker als normale Einheiten**
- **Können sterben** → müssen neu gespawnt werden (nach Cooldown)

### Held-Progression
- Helden werden mit **Fame freigeschaltet**
- Helden können mit **Fame verbessert** werden (Level-Ups)
- Jeder Held hat **einzigartige Fähigkeiten**

---

## 7. Spezialfähigkeiten

### Konzept
- **Zivilisations-spezifische Fähigkeiten** (z.B. "Berserker-Wut", "Heiliger Segen")
- **Kostenlos** (kein Gold)
- **Cooldown-basiert** (z.B. 60–180 Sekunden)
- **Strategischer Einsatz** (Buffs, Debuffs, AoE-Schaden)

### Beispiele
- **Königreich**: "Göttlicher Schutz" (alle Einheiten +50% Rüstung für 10s)
- **Orks**: "Blutdurst" (alle Einheiten +30% Schaden für 15s)
- **Untote**: "Totenbeschwörung" (spawnt 5 Skelette kostenlos)

---

## 8. Fame & Progression

### Fame-System
- **Fame wird nach jedem abgeschlossenen Level vergeben**
- Fame-Menge abhängig von:
  - Sieg/Niederlage
  - Schwierigkeit
  - Performance (Bonus-Ziele erreicht)

### Fame-Verwendung
1. **Einheiten upgraden** (HP, Schaden, Geschwindigkeit)
2. **Helden freischalten**
3. **Helden upgraden**
4. **Neue Zivilisationen freischalten** (später)
5. **Spezialfähigkeiten upgraden** (kürzere Cooldowns, stärkere Effekte)

---

## 9. KI-Gegner

### KI-Verhalten
- **Spielt nach gleichen Regeln** wie Spieler (kein Cheating)
- **Gold-Wirtschaft**: Verdient Gold durch Kills
- **Strategie-Modi**:
  - **Aggressiv**: Spawnt viele Einheiten, wenig Verteidigung
  - **Defensiv**: Baut viele Türme, wenig Einheiten
  - **Ausgewogen**: Mix aus beidem

### Schwierigkeitsgrade
- **Leicht**: KI macht Fehler, reagiert langsam
- **Normal**: KI spielt kompetent
- **Schwer**: KI reagiert schnell, optimale Build-Order

---

## 10. Weltkarte & Level-Progression

### Weltkarte
- **Knoten-basierte Karte** (wie in Slay the Spire)
- Jeder Knoten = ein Level
- Fortschritt durch Siege
- **Verzweigungen** (Spieler wählt Pfad)

### Level-Typen
1. **Standard-Schlacht** (normale KI)
2. **Boss-Level** (stärkere KI, besondere Belohnungen)
3. **Herausforderungen** (z.B. "ohne Türme gewinnen")

---

## 11. Technische Architektur (Unity)

### Neue/Geänderte Systeme

#### A) **CivilizationSystem**
- `CivilizationDefinition.cs` – Zivilisations-Daten (Einheiten, Türme, Helden)
- `CivilizationSelector.cs` – Auswahl vor Level
- `UnitDeck.cs` – 5 gewählte Einheiten + Held

#### B) **GoldEconomy**
- `GoldManager.cs` – Verwaltet Gold für Spieler & KI
- `GoldRewardSystem.cs` – Berechnet Belohnungen für Kills/Destructions
- `CostSystem.cs` – Prüft/zieht Kosten ab

#### C) **ConstructionSystem**
- `ConstructionSite.cs` – Baustelle (HP, benötigte Baumeister)
- `BuilderController.cs` – Baumeister-Einheit (navigiert zu Baustellen)
- `BuildQueue.cs` – Warteschlange für Baustellen
- `ConstructionManager.cs` – Koordiniert Bau-Prozess

#### D) **AIOpponent**
- `AICommander.cs` – KI-Hauptlogik (Entscheidungen)
- `AIStrategy.cs` – Strategische Planung (wann Einheiten/Türme)
- `AIBuildPlanner.cs` – Platziert Türme intelligent
- `AIUnitSpawner.cs` – Spawnt Einheiten basierend auf Gold

#### E) **HeroSystem**
- `HeroManager.cs` – Verwaltet Helden-Spawns & Cooldowns
- `HeroAbility.cs` – Helden-Fähigkeiten
- `HeroCooldown.cs` – Cooldown-Management

#### F) **FameSystem**
- `FameManager.cs` – Verwaltet Fame-Punkte
- `UpgradeSystem.cs` – Einheiten/Helden-Upgrades
- `ProgressionDatabase.cs` – Speichert Unlock-Status

#### G) **SpecialAbilities**
- `AbilityManager.cs` – Aktiviert Fähigkeiten
- `AbilityDefinition.cs` – Daten (Cooldown, Effekte)
- `AbilityCooldown.cs` – Cooldown-Tracking

---

## 12. UI-Anforderungen

### In-Game HUD
- **Gold-Anzeige** (aktuelles Gold)
- **Einheiten-Bar** (5 Einheiten-Buttons mit Kosten)
- **Held-Button** (mit Cooldown-Anzeige)
- **Spezialfähigkeit-Button** (mit Cooldown)
- **Basis-HP** (Spieler & KI)
- **Mini-Map**

### Pre-Level UI
- **Zivilisations-Info** (welche Zivilisation auf Karte)
- **Einheiten-Auswahl** (5 aus allen verfügbaren)
- **Helden-Auswahl** (1 Held)

### Post-Level UI
- **Fame-Belohnung**
- **Upgrade-Menü** (Fame ausgeben)
- **Level-Fortschritt**

---

## 13. Balancing-Überlegungen

### Gold-Werte (Beispiel)
- Start-Gold: 500
- Einfache Einheit: 50 Gold
- Mittlere Einheit: 100 Gold
- Schwere Einheit: 200 Gold
- Einfacher Turm: 150 Gold
- Fortgeschrittener Turm: 300 Gold

### Kill-Belohnungen
- Einfache Einheit: 25 Gold (50% Rückerstattung)
- Mittlere Einheit: 60 Gold
- Schwere Einheit: 120 Gold
- Turm zerstört: 100 Gold

### Baumeister
- Pro Turm benötigt: 3 Baumeister
- Bauzeit: ~5 Sekunden (bei 3 Baumeistern)

---

## 14. Implementierungs-Roadmap

### Phase 1: Kern-Systeme (MVP)
1. Gold-Wirtschaft
2. Basis-KI (einfache Strategie)
3. Baumeister-System (vereinfacht)
4. Held-System (1 Held)
5. Einfache Zivilisation (Königreich)

### Phase 2: Progression
1. Fame-System
2. Upgrade-System
3. Weltkarte
4. Speichern/Laden

### Phase 3: Content-Erweiterung
1. Weitere Zivilisationen (Orks, Untote)
2. Mehr Helden
3. Spezialfähigkeiten
4. Boss-Level

### Phase 4: Polishing
1. VFX/SFX
2. UI-Verbesserungen
3. Balancing
4. Tutorials

---

## 15. Was bleibt aus altem Konzept?

### Behalten
- **Level-basierte Progression** mit Weltkarte
- **Daten-getriebenes Design** (JSON)
- **3D-Gameplay**
- **PathManager, HealthComponent, DamageSystem** (Basis-Combat)
- **Prefab-System**

### Entfernen/Ändern
- **Karten-Deck-Mechanik** → Ersetzt durch direkte Einheiten-Auswahl
- **Wellen-System** → Ersetzt durch kontinuierliches Spawning
- **Planning-Phase** → Ersetzt durch Live-Gameplay
- **Offense/Defense Modi** → Vorerst nur ein Modus (Live Battle)

---

## 16. Erweiterte Spielmechaniken

### 16.1 Level-Start Countdown
- **5-Sekunden-Timer** erscheint zu Beginn jedes Levels
- Timer wird groß und zentral auf dem Bildschirm angezeigt
- Bei **0** verschwindet der Timer und das Gameplay wird freigeschaltet
- Während des Countdowns: Keine Aktionen möglich (Bauen, Spawnen, etc.)

### 16.2 Erweitertes Baumeister-System

#### Automatisches Spawnen
- **Bei Turm-Baustelle**: 3 Baumeister spawnen automatisch nacheinander (2 Sekunden Abstand)
- **Bei Fallen-Baustelle**: 1 Baumeister spawnt automatisch
- Baumeister spawnen an der eigenen Basis und laufen zur Baustelle

#### Fortschrittsanzeige
- Jede Baustelle zeigt Fortschritt: **"0/3"** (aktuelle/benötigte Baumeister)
- Aktualisiert sich in Echtzeit wenn Baumeister ankommen
- Visuelles Feedback bei Fertigstellung

### 16.3 Fallen-System

#### Platzierung
- Fallen können auf **jedem Path Tile** platziert werden
- Path Tiles sind die Wege, auf denen Einheiten laufen
- Fallen werden als Baustellen erstellt (1 Baumeister benötigt)

#### Fallen-Typen
- **Stachelfalle**: Verursacht Schaden bei Kontakt
- **Verlangsamungsfalle**: Reduziert Bewegungsgeschwindigkeit
- **Explosionfalle**: AoE-Schaden, zerstört sich selbst nach Aktivierung
- **Giftfalle**: Vergiftet Einheiten über Zeit

### 16.4 Platzierungsregeln

#### Türme
- Nur auf **Build Tiles** (spezielle Bauplätze)
- **Nicht** wo bereits eine Baustelle oder ein Turm existiert
- Kosten werden sofort abgezogen

#### Fallen
- Nur auf **Path Tiles** (Einheitenpfade)
- Können mehrere Fallen auf dem gleichen Pfad existieren
- Günstigere Kosten als Türme

### 16.5 Angriffs-Prioritäten für Einheiten

Einheiten folgen dieser Prioritätenliste:
1. **Feindliche Einheiten** (höchste Priorität) - wenn in Reichweite
2. **Türme** - wenn keine Einheiten in Reichweite und von Turm angegriffen
3. **Baustellen** - wenn in Reichweite (niedriger als Türme)
4. **Gegnerische Basis** (niedrigste Priorität) - Standard-Ziel

#### Verhalten
- **Ohne Feinde in Nähe**: Einheiten laufen Richtung gegnerische Basis
- **Von Turm angegriffen**: Einheiten greifen den Turm an (wenn erreichbar)
- **Einheiten in Nähe**: Sofortiger Kampf, pausieren Bewegung

### 16.6 Zerstörbare Baustellen und Türme

#### Baustellen
- **Niedrige HP** (sehr anfällig)
- Bei Zerstörung: Gegner erhält **Gold-Belohnung**
- Keine Teilrückerstattung für den Besitzer

#### Türme
- **Höhere HP** als Baustellen
- Bei Zerstörung: Gegner erhält **Gold-Belohnung** (wie bei Kills)
- Können repariert werden (zukünftiges Feature)

---

## 17. Bau-Kategorien System

Der Spieler hat Zugriff auf **drei Bau-Kategorien**:

### 17.1 Einheiten
- Spawnen an der eigenen Basis
- **Spawn-Zeit**: Je nach Einheit variabel (z.B. 2-10 Sekunden Cooldown)
- Kosten: Gold
- Keine Baumeister erforderlich

### 17.2 Türme
- Platzierung auf Build Tiles
- **Baustellen-Mechanik**: 3 Baumeister erforderlich
- Kosten: Gold (sofort abgezogen)
- Bauzeit: ~5-10 Sekunden

### 17.3 Fallen
- Platzierung auf Path Tiles
- **Baustellen-Mechanik**: 1 Baumeister erforderlich
- Günstigere Kosten als Türme
- Bauzeit: ~3 Sekunden

---

## 18. Menü-System

### 18.1 Hauptmenü
- **Hintergrundbild** (thematisch passend zur Zivilisation/Epoche)
- Buttons:
  - **Spielen** → Weltkarte
  - **Einstellungen** → Settings Panel
  - **Credits** → Credits Panel
  - **Beenden** → Spiel schließen

### 18.2 Weltkarte (World Map)
- **Übersichtskarte** mit allen verfügbaren Levels
- **Hintergrundbild** der Spielwelt

#### Level-Status Icons
| Status | Icon | Beschreibung |
|--------|------|--------------|
| Gesperrt | 🔒 | Level noch nicht freigeschaltet |
| Freigeschaltet | ⚔️ | Level spielbar, noch nicht abgeschlossen |
| Abgeschlossen | ✓ | Level erfolgreich abgeschlossen |
| Perfekt | ⭐ | Level mit perfektem Ergebnis abgeschlossen |

#### Freischaltung
- Erstes Level immer freigeschaltet
- Weitere Level durch Abschluss vorheriger Level freigeschaltet
- Optional: Bonus-Level durch besondere Leistungen

### 18.3 Fame Shop
- **Zugang** über Weltkarte
- **Upgrade-Kategorien**:
  - Einheiten verbessern (HP, Schaden, Geschwindigkeit)
  - Helden freischalten und verbessern
  - Türme verbessern
  - Fallen verbessern
  - Spezialfähigkeiten verbessern

### 18.4 Level-Gameplay
1. **Countdown Timer** (5 Sekunden)
2. **Aktive Schlacht** (Live Battle)
3. **Sieg/Niederlage Anzeige**

### 18.5 Ergebnis-Bildschirm
- **Sieg** oder **Niederlage** Anzeige
- **Fame-Belohnung** (verdiente Fame-Punkte)
- **Statistiken** (Kills, Einheiten gespawnt, Zeit)
- **Buttons**:
  - **Zur Weltkarte** → Zurück zur Level-Auswahl
  - **Level wiederholen** → Gleiches Level neu starten
  - **Nächstes Level** (nur bei Sieg und freigeschaltet)

---

## 19. Zivilisationen-System (Erweiterung)

### Aktueller Stand
- **Rom vs. Rom**: Anfangs kämpfen beide Seiten als Römer
- Ermöglicht balanciertes Testing

### Zukünftige Erweiterung
- Jedes Level definiert:
  - **Spieler-Zivilisation** (z.B. Rom)
  - **Gegner-Zivilisation** (z.B. Gallier)
- Unterschiedliche Einheiten und Spielstile pro Zivilisation

---

## 20. Nächste Schritte

1. **Neue JSON-Strukturen** für Zivilisationen, Gold-Kosten, Fame-Werte
2. **Core-Scripts implementieren** (GoldManager, AICommander, ConstructionSystem)
3. **UI anpassen** (Gold, Einheiten-Bar, Cooldowns)
4. **Test-Level erstellen** mit 1 Zivilisation
5. **Balancing-Tests** durchführen

---

**Dieses Design ermöglicht ein dynamisches, strategisches Spiel mit Tiefe und Wiederspielwert.**
