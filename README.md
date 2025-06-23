# Simulturn

## Introduction

> Built for thinkers, not grinders. Simulturn rewards clarity, foresight, and adaptation—not memorization or micromanagement.

- WEGO Turn Based Strategy Game
- Construct buildings, train units, scout, adapt, fight win
- Multiple time formats
- Sophisticated Replay capabilities
- Hex Grid
- API Handles Game logic - Independent Clients
- Destroy all opponent buildings
- Highly customizable gameplay through complex game settings

## Pre Game Setup

- Game settings are defined
- Instead of selecting a race as in other strategy games, each players selects a set of features (see the feature section)
- A map compiled of multiple hexagon tiles is generated
- Each hexagon has no or a limited number of harvestable resources and a terrain type
- Each player starts with a headquarter building and 3 workers on his starting hexagon
- Each player receives start resources

## Turn Structure

- Command Phase: All player simultaneously issues their commands
  - Constructing Buildings
  - Training Units
  - Move Units
  - Research Upgrades
- Resolution Phase: The engine resolves all player commands in a fixed sequence
- Initialization
  - Each players starts with a headquarter building, 3 workers on a single hexagon
  - Each player receives some resources

### Command Phase

#### Constructing Buildings

- A building is on exactly one hexagon, multiple buildings fit on the same hexagon
- Constructing a building which takes one or multiple turns
- Buildings under construction are fragile
- Prerequisites of constructing a building
  - Enough resources
  - idle worker
- Construction can be cancelled, reimbursing a fraction of the resources spent
- Purpose
  - Increase Supply Cap
  - Can train units
  - Enables resource gathering on that hexagon
- Buildings
  - Headquarters
    - Train Workers
    - Enables Gathering Resources
    - Increase Supply Cap
  - Supply Depots
    - Increase Supply Cap
  - Training Barracks: One per Unit Type
    - Trains a unit of the barracks type
    - Research Upgrades for that unit type

#### Training Units

- A unit is trained in a building taking one or multiple turns
- A building can train one unit per time
- Prerequisites
  - Enough supply
  - Enough resources
  - idle unit training building
- Units in training already consume supply
- Unit training can be cancelled, reimbursing a fraction of the resources spent
- A building destroyed during training cancels the training and the resources are reimbursed

#### Move Units

- A unit always resides at a single hexagon, multiple units of the same player can reside at the same hexagon
- Each unit can move separately up to the maximum number of hexagons
- Multiple units can move together
- A movement is defined by the start hexagon, the destination hexagon and the units moving from the start to the destination
- The terrain type of and buildings on hexagon can influence the movement range of passing units

#### Upgrade Units

- Buildings not training units can research upgrades
- Building researching upgrades cannot train units at the same time
- Researching upgrades takes on or multiple turns
- Upgrades can be cancelled, reimbursing a fraction of the resources spent
- Upgrades improve units
  - Movement
  - Vision
  - Exponential Bonus

### Resolution Phase

- The order of command resolution is fixed
- Cancel (handle before other steps to make the resources of cancelled orders available for other commands)
  - Cancelled construction, trainings and upgrades are removed and resources reimbursed
- Construction
  - Construction progress on all hexagons
  - If the required number of turns is reached, the construction completes
  - Canceled construction are removed and resources reimbursed
- Training
  - Training progress on all hexagons
  - If the required number of turns is reached, the unit is added to the hexagon of the building
- Movement
  - All movements of units are processed independently from each other
  - Units are moved to their destination hexagon
- Combat
  - All hexagons with units from multiple players process combat
  - A combat always result in a single winning player, every other player loses all units
  - Draws are possible, resulting in the death of all units of all players
  - See details about Combat in the dedicated combat section
- Building destruction
- Resource are gathered from all eligible hexagons

## Game Mechanics

### Combat

- Combat is based on a rock paper scissors like mechanic
- The combat mechanic encourages producing large amounts of the same type, but also scouting to counter the unit type of the opponent
- There are three type of Units
  - A counters B
  - B counters C
  - C counters A
- Two armies, each containing one or multiple units fight against each other
- Combat has three phases
  - 1 Counter Phase
    - Sum the strength of units per type
    - Remove the number of countered units from the opponents army
  - 2 Strength Phase
    - Sum the strength of surviving units per type
    - Take the sum per unit type with an exponent of 1.2 (or similar and possibly increased by updates)
    - To compute the final strength, sum all three computed values and add the strength of workers and similar units
  - 3 Completion Phase
    - If both strength values are equal, all units die in this tie
    - Otherwise, the weaker player loses everything and the stronger player loses units proportionally to the strength difference
- Fights with multiple players
  - Fight pairwise
  - Fights with no dominant players result in a tie for all players
  - Only a dominant player (who defeats everything in the direct fight) can win a fight

### Building destruction

- All units on a hexagon attack buildings on the same hexagon
- Units winning a fight are included and also attack buildings on that tile
- Destruction procedure
  - Sum the building damage value of all units
  - Order all opponent buildings on a hexagon
  - Destroy buildings with a sum of their health lower than the total building damage
  - Remove those buildings
- The order criterion depends on the game settings
  - Randomly
  - Building health ascending or descending
  - Predetermined order

### Resource Gathering and Supply Cap

- Resource depletion: Resources can be harvested until all available resources has been harvested on that pile
- Each player harvests a fixed number of resources per worker on a hexagon which does not construct a building
- A headquarters is required for workers to harvest resources
- Upkeep
  - There is a number of upkeep levels
  - The upkeep level is selected based on the used supply of that player
  - Each upkeep level defines a fraction of resources that is subtracted from harvested resources before they are added to the player
  - The upkeep resources are still harvested and reduce the remaining number of harvestable resources
  - A higher upkeep level increases the fraction of income lost

### Visibility - Fog of war

- Units have a vision range, defining how far they can see
- The vision range is determined by
  - unit type
  - upgrades
  - terrain
- There are four levels of visibility
  - Unseen: All hexagon are unseen during the start of the game
  - Previously seen: A hexagon that has been seen during one turn in the past, but is not currently visible
  - Currently visible: A hexagon in range of the visibility of the player
  - Infiltrated: Special scouting units can enhance the intel gained

| Visibility State         | Terrain Type | Initial Harvestable Resources | Remaining Harvestable Resources | Buildings (Last Seen) | Units (Count) | Units (Composition) |
|--------------------------|--------------|-------------------------------|----------------------------------|------------------------|----------------|----------------------|
| Unseen                   | ✅           | ✅                            | ❌                               | ❌                     | ❌             | ❌                   |
| Previously Seen          | ✅           | ✅                            | ✅                               | ✅                     | ❌             | ❌                   |
| Currently Visible        | ✅           | ✅                            | ✅                               | ✅                     | ✅             | ❌                   |
| Infiltrated (Visible +)  | ✅           | ✅                            | ✅                               | ✅                     | ✅             | ✅                   |

### Features

- Features are permanent and passive upgrades/skills for a player
- Feature selection
  - Random Selection: Each player receives a random set of features
  - Selection: Each player selects the features independently from all opponents
  - Draft System: Players alternate selecting features, possible including veto for banning features
- Features can be hidden or initially revealed depending on the game settings (draft is always revealed)
- Sample features
  - Increase starting resources
  - Visibility bonus for a unit type
  - Movement bonus for a unit type
  - Buildings have extra health
  - Bonus on the exponential factor of a unit type

## Game modes

- Various time formats
- The time of a turn is the duration between the completion of the last resolution phase (or start of the game) and the confirmation of all commands for that turn
- Sync Games:
  - Each player has a fixed amount of time for all his turn
  - Each turn adds a fixed duration to the players time pool
  - A player loses if running out of time
- Async games:
  - Each player has a fixed amount fo time per turn
  - Only commands issued in that duration are considered, not responding results in a turn without any commands

## Match types

- Single Player: Play versus AI opponents
- Custom Game: Invite players and freely adjust game settings
- Ranked Game:
  - Fixed (or seasonal) game settings
  - Pair with players with similar elo
  - Elo Rating per Game mode and time pool range

## Replay and Analysis

- Players can watch replays to analyze their games
- Compute statistical values based on each game for the game summary and insights
- Potentially allow watching replays of older game versions

## AI Opponent

## Tech Stack

### Architecture

- Core Logic Library: All game rules are defined
- Web API: Uses the core logic library and handles turn resolution, storing turns, game creation, replays and everything else
- Database:
  - Stores games, players turn
  - Only the Web API can read and write to the database
- Clients:
  - A simple console application for initial development
  - For example a web app, or mobile app
  - Communicates with the Web API
  - Uses the core logic library for instant validation etc.
  - Visualize game lobbies, profile and game creation
  - Partially informed during games
    - Only receives the information allowed for that player by the api
    - Cannot cheat, the Web API handles which information is visible
    - Can use game computation and validation without reaching out to the Web API
    - The Web API will validate all commands independently

### Updates

- Updates break old games, but older version should still work
- Each component has its own version
- Each game version is defined by a version of all components

#### Update Chain

- Core Logic Library
  - Requires an updated Web API
  - Can require an updated database
  - Requires an updated client
- Web API
  - Might required an updated client
  - Might required an updated database
- Client
  - Update to adjust to changes in the communication with the Web API
  - Changes in the UI does not affect other components

