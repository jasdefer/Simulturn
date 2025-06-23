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

## Turn Structure

- Command Phase: All player simultaneously issues their commands
  - Constructing Buildings
  - Training Units
  - Move Units
  - Research Upgrades
- Resolution Phase: The engine resolves all player commands in a fixed sequence

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
- Resource are gathered from all eligible hexagons

## Game Mechanics

### Combat

- Combat is based on a rock paper scissors like mechanic

### Resource Gathering and Supply Cap

### Visibility