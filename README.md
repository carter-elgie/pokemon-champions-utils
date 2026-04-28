# Pokemon Champions Utils (pcu)

A cross-platform CLI tool for competitive Pokemon Champions players. Look up Pokemon stats, check speed tiers, run damage calculations, build and validate teams, and access live battle usage statistics — all from your terminal.

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

Shows base, minimum, and maximum values for a specific stat, plus two sorted tier lists — one uninvested, one fully invested — placing the searched Pokemon alongside your current team.

```
incineroar speed
incineroar hp
flutter-mane special attack    # flexible stat name recognition
```

Modifiers can be appended to either form of the stat lookup:

```
incineroar speed scarf         # with Choice Scarf
incineroar speed +1            # with a +1 speed boost
incineroar speed tailwind      # under Tailwind
```

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
incineroar speed jolly 16 scarf    # Jolly 16 SP + Choice Scarf
incineroar speed timid +2          # Timid, uninvested, +2 boost
```

All six current team members appear in the comparison list (showing their actual invested stats when a build is recorded, or uninvested estimates otherwise).

---

### Damage calc

**Outgoing damage** (your Pokemon attacks):

```
sneasler close-combat > incineroar
```

**Incoming damage** (opponent attacks your Pokemon):

```
incineroar < sneasler close-combat
```

Modifiers:

```
sneasler +1 close-combat > incineroar -1
flutter-mane moonblast > incineroar --weather sun
sneasler close-combat > incineroar --screens
```

Your team member's actual item, ability, EVs, and IVs are automatically used without you needing to specify them. The opponent's stats are calculated at minimum bulk, maximum bulk, and (in online mode) the most common EV spread.

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
