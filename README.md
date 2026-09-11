# Pokemon Champions Utils (pcu)

A cross-platform CLI tool for competitive Pokemon Champions players. Look up Pokemon stats, check speed tiers, run damage calculations, build and validate teams, and access live battle usage statistics—all from your terminal.

**This project is still in development.** There are still remaining bugs and missing features, especially for damage calculations.

---

## Table of Contents

- [Requirements](#requirements)
- [Installation](#installation)
- [First Run](#first-run)
- [Configuration](#configuration)
- [Commands](#commands)
  - [update](#update)
  - [Pokemon lookup](#pokemon-lookup)
  - [Move / Item / Ability lookup](#move--item--ability-lookup)
  - [Stat lookup](#stat-lookup)
    - [Build-specific comparison](#build-specific-comparison)
  - [Damage calc](#damage-calc)
  - [Teams](#teams)
  - [Aliases](#aliases)
- [Formats](#formats)
- [Offline vs. Online Mode](#offline-vs-online-mode)
- [Development](#development)

---

## Requirements

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) or later
- Internet connection (for the first `update` run and for online usage stats)

---

## Installation

```bash
git clone https://github.com/carter-elgie/pokemon-champions-utils.git
cd pokemon-champions-utils
dotnet build -c Release
```

To run from anywhere, publish a self-contained binary:

```bash
dotnet publish src/PokemonChampions.Cli -c Release -r linux-x64 --self-contained -o publish/
# or for Windows:
dotnet publish src/PokemonChampions.Cli -c Release -r win-x64 --self-contained -o publish/
```

Add the `publish/` directory to your `PATH` and invoke with `pcu` (rename the binary if needed).

---

## First Run

Before using any lookup commands, populate the local database:

```bash
update
```

This downloads Pokemon, move, item, ability, and learnset data from Pokemon Showdown and stores it in:

- **Linux**: `~/.config/pkmn-champs/data.db`
- **Windows**: `%APPDATA%\pkmn-champs\data.db`

To also fetch live battle usage statistics (requires online mode to be enabled):

```bash
update --online
```

---

## Configuration

Set the active competitive format:

```bash
config --format gen9championsregma
```

Toggle between online and offline mode:

```bash
config --online    # enable live usage stats
config --offline   # use only locally cached data
```

---

## Commands

### update

```
update [--online]
```

Refreshes local data from Pokemon Showdown. With `--online`, also fetches live battle usage statistics from MunchStats and Smogon.

---

### Pokemon lookup

Typing a Pokemon's name shows its base stats, min/max stats at level 50, and available abilities. If you're in online mode and usage data is available from MunchStats, the following are also shown:

- **Moves**: top 5 most-used, with usage percentages
- **Teammates**: top 3 most common partners, with usage percentages

> **Note:** MunchStats does not currently publish item or ability usage data for the Champions format. Those sections will be added when a data source becomes available.

```
incineroar
charizard-mega-y
mega charizard y        # spaces work too — no quotes or hyphens required
```

If the Pokemon is on your current team, an indicator will highlight the team member variant and show its actual stats, moveset, item, and ability.

For full detailed usage stats (all moves and teammates ranked by frequency):

```
incineroar --detailed
```

---

### Move / Item / Ability lookup

```
flamethrower            # move description + type, category, power, accuracy
leftovers               # item description
intimidate              # ability description
```

If a name refers to more than one type of thing (e.g. "metronome" is both a move and an item), the tool will ask:

```
Did you mean 'metronome (move)' or 'metronome (item)'?
```

Specify with a parenthetical:

```
"metronome (move)"
"metronome (item)"
```

If a move, item, ability, or Pokemon is not legal in the current format, the tool will say so but still show the description.

---

### Stat lookup

Shows base, minimum, and maximum values for a specific stat, plus two sorted tier lists — one uninvested, one fully invested — placing the searched Pokemon alongside your current team. If the queried Pokemon is on your active team, their actual build stat is shown prominently above the tier lists.

```
incineroar speed
incineroar hp
flutter-mane special attack    # flexible stat name recognition
```

#### Modifiers

Modifiers can be appended to any stat lookup. Modifiers affect only the **queried Pokemon's** stat; team entries always show their own actual or estimated stats.

```
incineroar speed scarf            # with Choice Scarf
incineroar speed +1               # with a +1 speed boost
incineroar speed tailwind         # under Tailwind
chansey defense eviolite          # Eviolite Def (only applies if the Pokemon can evolve)
flutter-mane special-attack specs # with Choice Specs
```

Multi-word modifier names can be typed with a space between words or run together:

```
incineroar speed choice scarf     # same as "scarf"
incineroar attack choice band     # same as "band"
chansey defense assault vest      # same as "vest"
```

**Speed modifiers:**

| Modifier | Aliases | Effect |
|---|---|---|
| `scarf` / `choice scarf` | `choicescarf` | ×1.5 |
| `tailwind` | `tw` | ×2 |
| `paralysis` | `para` | ×0.5 |
| `chlorophyll` | — | ×2 (sun assumed) |
| `swift swim` | `swiftswim` | ×2 (rain assumed) |
| `sand rush` | `sandrush` | ×2 (sand assumed) |
| `slush rush` | `slushrush` | ×2 (snow assumed) |
| `unburden` | — | ×2 (item consumed assumed) |
| `surge surfer` | `surgesurfer` | ×2 (electric terrain assumed) |
| `quick feet` | `quickfeet` | ×1.5 (statused assumed) |
| `slow start` | `slowstart` | ×0.5 |
| `iron ball` | `ironball` | ×0.5 |
| `+N` / `-N` | — | stage boost (±1–6) |

**Attack modifiers:**

| Modifier | Aliases | Effect |
|---|---|---|
| `band` / `choice band` | `choiceband` | ×1.5 |
| `huge power` / `pure power` | `hugepower`, `purepower` | ×2 |
| `hustle` | — | ×1.5 |
| `gorilla tactics` | `gorillatactics` | ×1.5 |
| `guts` | — | ×1.5 (statused assumed) |
| `defeatist` | — | ×0.5 (low HP assumed) |
| `flower gift` | `flowergift` | ×1.5 (sun assumed) |
| `light ball` | `lightball` | ×2 (Pikachu family only) |
| `thick club` | `thickclub` | ×2 (Cubone/Marowak only) |

**Special Attack modifiers:**

| Modifier | Aliases | Effect |
|---|---|---|
| `specs` / `choice specs` | `choicespecs` | ×1.5 |
| `solar power` | `solarpower` | ×1.5 (sun assumed) |
| `plus` | — | ×1.5 (ally has Minus assumed) |
| `minus` | — | ×1.5 (ally has Plus assumed) |
| `hadron engine` | `hadronengine` | ×1.33 |
| `defeatist` | — | ×0.5 (low HP assumed) |
| `light ball` | `lightball` | ×2 (Pikachu family only) |

**Defense modifiers:**

| Modifier | Aliases | Effect |
|---|---|---|
| `fur coat` | `furcoat` | ×2 |
| `marvel scale` | `marvelscale` | ×1.5 (statused assumed) |
| `eviolite` | — | ×1.5 (non-fully-evolved Pokemon only) |

**Special Defense modifiers:**

| Modifier | Aliases | Effect |
|---|---|---|
| `vest` / `assault vest` | `assaultvest` | ×1.5 |
| `ice scales` | `icescales` | ×2 |
| `eviolite` | — | ×1.5 (non-fully-evolved Pokemon only) |

> **Ability modifiers**: the ability does not need to be the Pokemon's actual ability — specifying `chlorophyll` or `huge power` applies the modifier regardless.
>
> **Item modifiers**: `light ball`, `thick club`, and `eviolite` are silently ignored if the Pokemon cannot benefit from them (wrong species, or already fully evolved).
>
> **Condition modifiers**: for abilities or moves that require a field condition (e.g., Chlorophyll requires sun, Guts requires a status), the condition is always assumed to be met.

#### Build-specific comparison

Specify a nature, a stat point / EV count, or both to calculate a particular build and show a single sorted list instead of two:

```
incineroar speed jolly          # Jolly nature, 0 investment
incineroar speed jolly 16       # Jolly + 16 stat points (Pokemon Champions)
incineroar speed timid 252      # Timid + 252 EVs (standard Gen 9; values > 32 treated as EVs)
incineroar speed 32             # max stat points, neutral nature
```

- Values ≤ 32 are treated as **stat points** (Pokemon Champions, 1 SP = 8 EVs in the formula).
- Values > 32 are treated as raw **EVs** (standard Gen 9).
- Nature is assumed **neutral** for the queried stat if omitted; nature is always ignored for HP.
- Modifiers work the same way:

```
incineroar speed jolly 16 scarf       # Jolly 16 SP + Choice Scarf
incineroar speed timid +2             # Timid, uninvested, +2 boost
chansey defense bold 32 eviolite      # Bold max Def + Eviolite
flutter-mane special-attack timid specs  # Timid uninvested + Choice Specs
```

All team members appear in the comparison list, showing their actual invested stats when a build is recorded or uninvested estimates otherwise. The queried Pokemon's actual team build appears as a separate entry if it differs from the hypothetical build.

---

### Damage calc

The `>` and `<` operators determine which side is **your Pokemon** and which is the **opponent**:

- `>` form: the **left** side is your Pokemon (uses team build if on team), the **right** side is always the opponent.
- `<` form: the **left** side is your Pokemon (uses team build if on team), the **right** side is always the opponent.

**Outgoing damage** (your Pokemon attacks):

```
sneasler close-combat > incineroar
```

**Incoming damage** (opponent attacks your Pokemon):

```
incineroar < sneasler close-combat
```

**Stat stages** — `+N` on the attacker boosts their relevant **offensive** stat; `-N` on the defender lowers their relevant **defensive** stat:

- Physical moves: `+N` raises Attack / `-N` lowers Defense
- Special moves: `+N` raises Sp. Atk / `-N` lowers Sp. Def
- Body Press: `+N` raises Defense (used as the attacking stat) / `-N` lowers Defense

```
sneasler +1 close-combat > incineroar -1    # sneasler +1 Atk / incineroar -1 Def
flutter-mane moonblast > incineroar --weather sun
sneasler close-combat > incineroar --screens
```

Supported `--weather` values: `sun`, `rain`, `sand`, `snow` (or `hail`). `--terrain` values: `electric`, `grassy`, `psychic`, `misty`. `--screens` applies Reflect for Physical moves and Light Screen for Special moves (suppressed on critical hits). `--aurora-veil` applies to all move categories (also suppressed on critical hits).

**Battle modifier flags** — append any combination to a damage calc:

*Field conditions (apply to Power calculation):*

| Flag | Effect |
|---|---|
| `--gravity` | Gravity is in effect (boosts Grav Apple ×1.5; all Pokemon grounded for terrain purposes) |

*Attacker status conditions (apply to Power calculation):*

| Flag | Effect |
|---|---|
| `--burned` | Attacker is burned (Facade ×2 power; physical damage ×0.5 unless Guts or Facade) |
| `--paralyzed` | Attacker is paralyzed (Facade ×2 power) |
| `--poisoned` | Attacker is poisoned (Facade ×2 power) |

*Power modifiers (computed before the damage formula, chained with 4096-based rounding):*

| Flag | Effect |
|---|---|
| `--helping-hand` | Ally used Helping Hand this turn (move power ×1.5) |
| `--charge` | Attacker is under the Charge effect; Electric moves ×2 power |
| `--analytic` | Attacker has Analytic and the target moved first this turn (power ×1.3) |
| `--sheer-force` | Attacker has Sheer Force and the move has a secondary effect (power ×1.3) |
| `--ally-battery` | Ally has Battery; special moves ×1.3 power |
| `--ally-power-spot` | Ally has Power Spot; all moves ×1.3 power |
| `--ally-steely-spirit` | Ally has Steely Spirit; Steel-type moves ×1.5 power |

*Damage modifiers (applied after the formula):*

| Flag | Effect |
|---|---|
| `--crit` | Critical hit (damage ×1.5; screens and Aurora Veil suppressed) |
| `--parental-bond` | Second hit of Parental Bond (base damage ×0.25) |
| `--glaive-rush` | Defender used Glaive Rush last turn (damage received ×2) |
| `--friend-guard` | Ally has Friend Guard (damage received ×0.75) |
| `--aurora-veil` | Aurora Veil on defender's side (damage ×0.5; suppressed on crits) |
| `--metronome N` | Attacker holds Metronome item; N = consecutive turns using same move (×1.2 at N=1, up to ×2.0 at N=5+) |

**Form and ability flags:**

| Flag | Effect |
|---|---|
| `--atk-form <name>` | Override the attacker's form for this calc (e.g. `--atk-form charizard-mega-y`). Megas are auto-detected from the held item if on your team. |
| `--def-form <name>` | Override the defender's form for this calc (e.g. `--def-form aegislash-blade`). |
| `--ability <name>` | Set the ability assumed for the right-side (opponent) Pokemon. If omitted, the most common ability from usage stats is used (online mode), or Ability0 (offline mode). |

Examples:

```
sneasler close-combat > incineroar --crit --helping-hand
flutter-mane moonblast > incineroar --weather sun --screens
incineroar < sneasler close-combat --burned
dragonite extreme-speed > incineroar --friend-guard
ninetales dazzlinggleam > incineroar --terrain psychic

# Form changes
charizard flamethrower > incineroar --atk-form charizard-mega-y    # explicit mega form
charizard flamethrower > incineroar                                 # auto-detected if holding Charizardite Y
morpeko aura-wheel > incineroar --atk-form morpeko-hangry           # Dark-type Aura Wheel

# Unknown opponent with a specific ability
incineroar < iron-moth flamethrower --ability quark-drive
```

**How stats are resolved:**

- The **left-side** Pokemon is always yours. If it is on your active team (with a build recorded), their actual nature, stat points, IVs, item, and ability are used automatically. Otherwise, maximum offensive investment is assumed.
- The **right-side** Pokemon is always the opponent. Two scenarios are shown — minimum bulk (0 SP, hindering nature) and maximum bulk (32 SP, boosting nature). The tool never uses your team build for the right-side Pokemon.
- **Mega Evolution**: if your team member holds a Mega Stone, the tool automatically calculates using the mega form's base stats and ability. You can also force any form with `--atk-form` or `--def-form`.

**Type-changing moves and abilities handled automatically:**

| Source | Effect |
|---|---|
| Pixilate | Normal moves become Fairy-type and gain ×1.2 power |
| Refrigerate | Normal moves become Ice-type and gain ×1.2 power |
| Aerilate | Normal moves become Flying-type and gain ×1.2 power |
| Galvanize | Normal moves become Electric-type and gain ×1.2 power |
| Normalize | All moves become Normal-type |
| Mega Sol | Weather Ball is always Fire-type (and ×2 power) |
| Weather Ball | Type matches the current weather (Fire/Water/Rock/Ice); power ×2 in any weather |
| Aura Wheel | Electric-type for Morpeko; Dark-type for Morpeko-Hangry (use `--atk-form morpeko-hangry`) |
| Body Press | Uses the attacker's Defense stat as the attacking stat |

**Attacker ability bonuses applied automatically** (when the team member's ability matches):

| Ability | Effect |
|---|---|
| Adaptability | STAB ×2 instead of ×1.5 |
| Technician | Moves with ≤60 base power get ×1.5 power |
| Strong Jaw | Biting moves ×1.5 power |
| Iron Fist | Punching moves ×1.2 power |
| Tough Claws | Contact moves ×1.3 power |
| Punk Rock | Sound moves ×1.3 power |
| Mega Launcher | Pulse moves ×1.5 power |
| Sharpness | Slicing moves ×1.5 power |
| Analytic | Power ×1.3 when moving last (use `--analytic`) |
| Sand Force | Ground/Rock/Steel ×1.3 power in sandstorm |
| Sheer Force | ×1.3 power for moves with secondary effects (use `--sheer-force`) |
| Steely Spirit | Steel moves ×1.5 power (attacker's own Steely Spirit; use `--ally-steely-spirit` for ally's) |
| Guts | Attack ×1.5 when statused; burn penalty negated |
| Neuroforce | Super-effective damage ×1.25 |
| Sniper | Critical hit damage ×1.5 extra (stacks with base crit boost) |
| Tinted Lens | Not-very-effective damage ×2 |
| Pixilate / Refrigerate / Aerilate / Galvanize | Type conversion + ×1.2 power (auto-applied when attacker has the ability) |
| Mega Sol | Weather Ball always Fire-type + ×2 power |

**Defender ability bonuses applied automatically** (when the defender is a known team member or ability is specified):

| Ability | Effect |
|---|---|
| Multiscale / Shadow Shield | Damage ×0.5 when at full HP |
| Fluffy | Contact moves ×0.5; Fire-type moves ×2 |
| Punk Rock | Sound moves ×0.5 |
| Ice Scales | Special moves ×0.5 |
| Filter / Solid Rock / Prism Armor | Super-effective moves ×0.75 |
| Dry Skin | Fire-type move power ×1.25 (auto-applied from Power chain) |

**Item bonuses applied automatically**: Choice Band (physical ×1.5 Atk), Choice Specs (special ×1.5 SpA), Life Orb (damage ×1.3), Expert Belt (super-effective damage ×1.2), Muscle Band (physical power ×1.1), Wise Glasses (special power ×1.1), Punching Glove (punch move power ×1.1), type-enhancing items and Plates (matching-type power ×1.2), Metronome item (use `--metronome N`).

---

### Teams

List your teams for the current format:

```
teams
```

Create a new team (prompts for pokepaste input):

```
teams new
```

Import a specific team from a pokepaste file:

```
teams new --name "My Team" --file myteam.txt
```

Edit an existing team:

```
teams edit "My Team"
```

Set the active team (used for stat tiers, damage calcs, and team-member Pokemon lookups):

```
teams set "My Team"
```

Remove a team (asks for confirmation first):

```
teams remove "My Team"
```

---

### Aliases

Define a shorthand for any Pokemon, move, item, or ability:

```
alias "mcy" charizard-mega-y
alias "big tree" venusaur-mega
```

After setting an alias, you can use it anywhere a name is expected:

```
mcy speed
mcy flamethrower > incineroar
```

Remove an alias:

```
alias remove "mcy"
```

List all aliases:

```
alias list
```

---

## Formats

The tool is format-aware. All team validation, legality warnings, and usage statistics reflect the currently selected format.

Supported formats:

| Showdown ID | Display Name |
|---|---|
| `gen9championsregma` | [Champions] VGC 2026 Reg M-A |

To switch formats:

```
config --format gen9championsregma
```

---

## Offline vs. Online Mode

**Offline mode** (default):
- All static data (Pokemon stats, moves, items, abilities, learnsets) is available locally after the first `pcu update`.
- Battle usage statistics (top moves, items, EV spreads) are unavailable unless previously cached.

**Online mode**:
- Usage statistics are fetched live from MunchStats (for Champions format) and Smogon chaos stats (for EV spreads).
- A cached copy is saved locally, so a network connection is not required for every query once data has been fetched.

Toggle:

```bash
config --online
config --offline
```

---

## Development

### Project structure

```
src/
  PokemonChampions.Shared/     Enums, value types, string utilities
  PokemonChampions.Core/       Domain models, service interfaces, stat/damage calculation
  PokemonChampions.Data/       EF Core entities, AppDbContext, migrations
  PokemonChampions.Import/     HTTP data sources, Showdown JS parser, data importers
  PokemonChampions.Cli/        CLI entry point, commands, Spectre.Console renderers
tests/
  PokemonChampions.Core.Tests/   Unit tests for stat/damage calculation and legality
  PokemonChampions.Import.Tests/ Unit tests for parsers
```

### Running tests

```bash
dotnet test
```

### Adding a new format

1. Create a new class in `src/PokemonChampions.Core/Formats/Regulations/` implementing `IFormatDefinition`.
2. Register it in `src/PokemonChampions.Cli/ServiceRegistration.cs`:
   ```csharp
   registry.Register(new MyNewFormat());
   ```
3. That's it — all commands automatically respect the new format's legality rules once it's selected.

### Database migrations

After modifying entities in `PokemonChampions.Data/Entities/`, generate a migration:

```bash
dotnet ef migrations add MyMigration \
  --project src/PokemonChampions.Data \
  --startup-project src/PokemonChampions.Data
```

Migrations are applied automatically on startup.
