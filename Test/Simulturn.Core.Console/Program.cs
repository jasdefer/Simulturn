using Simulturn.Core.Console;
using Simulturn.Core.Helper;
using Simulturn.Core.Model;
using Simulturn.Core.Model.Commands;
using Simulturn.Core.Model.State;
using System.Collections.Immutable;

Console.WriteLine("Hello, Simulturn World!");

GameState gameState = new(ConsoleGameSettings.DefaultGame);
string stateString = Printer.PrintState(gameState, Printer.GetFullInfo);
File.WriteAllText($"Turn{gameState.Turn}.svg", stateString);
File.WriteAllText($"CurrentTurn.svg", stateString);
while (!gameState.IsGameOver)
{
    Console.WriteLine($"Turn {gameState.Turn}: Please add commands for the next turn in CSV format and press any key to continue.");
    Console.ReadKey();
    Dictionary<string, Dictionary<Hexagon, Command>> commands = [];
    bool errorInCommands = false;
    foreach (var player in gameState.PlayerIds)
    {
        if (!File.Exists($"{player}.csv"))
        {
            continue;
        }

        string[] lines = File.ReadAllLines($"{player}.csv");
        Dictionary<Hexagon, Command> playerCommands = [];
        for (int i = 1; i < lines.Length; i++)
        {
            var commandArgs = lines[i].Split(',');
            if (commandArgs.Length < 2 || !short.TryParse(commandArgs[0], out short x) || !short.TryParse(commandArgs[1], out short y))
            {
                continue;
            }
            Hexagon hexagon = new Hexagon(x, y);
            Army army = new Army()
            {
                Dot = commandArgs[4].ToShort(),
                Triangle = commandArgs[5].ToShort(),
                Circle = commandArgs[6].ToShort(),
                Square = commandArgs[7].ToShort(),
            };

            Compound construction = new()
            {
                Axis = commandArgs[8].ToShort(),
                Plane = commandArgs[9].ToShort(),
                Pyramid = commandArgs[10].ToShort(),
                Dome = commandArgs[11].ToShort(),
                Cube = commandArgs[12].ToShort()
            };

            Army training = Army.Empty;
            MovementCommand[] movementCommands = [];
            if (short.TryParse(commandArgs[2], out short destinationX) && short.TryParse(commandArgs[3], out short destinationY))
            {
                Hexagon destination = new Hexagon(destinationX, destinationY);
                movementCommands = [new MovementCommand()
                {
                    Destination = destination,
                    Army = army
                }];
            }
            else
            {
                training = army;
            }

            Command command = new Command()
            {
                Training = training,
                Construction = construction,
                MovementCommands = movementCommands.ToImmutableArray(),
            };

            if (playerCommands.TryGetValue(hexagon, out var existingCommand))
            {
                command = existingCommand with
                {
                    Training = existingCommand.Training + training,
                    Construction = existingCommand.Construction + construction,
                    MovementCommands = existingCommand.MovementCommands.AddRange(movementCommands)
                };
            }
            else
            {
                playerCommands.Add(hexagon, command);
            }
        }

        var errors = gameState.Validate(player, playerCommands);
        if (errors.Any())
        {
            errorInCommands = true;
            Console.WriteLine(string.Join(", ", errors));
        }
        commands.Add(player, playerCommands);
    }
    if (errorInCommands)
    {
        continue;
    }
    gameState = gameState.NextTurn(commands);
    stateString = Printer.PrintState(gameState, Printer.GetFullInfo);
    File.WriteAllText($"Turn{gameState.Turn}.svg", stateString);
    File.WriteAllText($"CurrentTurn.svg", stateString);
}

