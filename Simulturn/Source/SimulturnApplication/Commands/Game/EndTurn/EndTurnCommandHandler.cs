using MediatR;
using SimulturnApplication.Common.Interfaces;
using SimulturnDomain.Entities;
using SimulturnDomain.Settings;

namespace SimulturnApplication.Commands.Game.EndTurn;
public class EndTurnCommandHandler : IRequestHandler<EndTurnCommand>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IPlayerRepository _playerRepository;
    private readonly IGameRepository _gameRepository;

    public EndTurnCommandHandler(ICurrentUserService currentUserService,
        IPlayerRepository playerRepository,
        IGameRepository gameRepository)
    {
        _currentUserService = currentUserService;
        _playerRepository = playerRepository;
        _gameRepository = gameRepository;
    }
    public async Task Handle(EndTurnCommand request, CancellationToken cancellationToken)
    {
        var playerId = _currentUserService.UserId;
        await _playerRepository.EndTurn(request.GameId, playerId);

        var endTurn = await _gameRepository.AllPlayersEndedTheirTurn(request.GameId);
        if (!endTurn)
        {
            return;
        }

        await EndTurn(request.GameId);
    }

    public async Task EndTurn(string gameId)
    {
        var game = await _gameRepository.GetGame(gameId);
        var settings = game.Settings;
        foreach (var player in game.Players)
        {
            var grossIncomePerCoordinates = GetGrossIncome(settings.ArmySettings.Income,
                game.MatterPerCoordinates,
                game.Settings.HexagonSettingsPerCoordinates,
                player.Armies);
            await _gameRepository.SubtractMatter(grossIncomePerCoordinates);
            var totalGrossIncome = grossIncomePerCoordinates.Sum(x => x.Value);
            var income = GetIncome(totalGrossIncome,
                player.Armies,
                settings.UpkeepLevels,
                settings.ArmySettings.RequiredSpace);
            await _playerRepository.AddIncome(gameId, player.Name, game.Turn, income);
        }
    }

    public static Income GetIncome(int totalGrossIncome, IReadOnlyDictionary<Coordinates, Army> armies, IReadOnlyList<UpkeepLevel> upkeepLevels, Army requiredSpace)
    {
        throw new NotImplementedException();
    }

    public static IReadOnlyDictionary<Coordinates, ushort> GetGrossIncome(Army income, IReadOnlyDictionary<Coordinates, ushort> matterPerCoordinates, IReadOnlyDictionary<Coordinates, HexagonSettings> hexagonSettingsPerCoordinates, IReadOnlyDictionary<Coordinates, Army> armies)
    {
        var grossIncomePerCoordinates = new Dictionary<Coordinates, ushort>();
        foreach (var matterCoordinates in matterPerCoordinates.Where(x => x.Value > 0))
        {
            throw new NotImplementedException();
        }
        return grossIncomePerCoordinates;
    }
}
