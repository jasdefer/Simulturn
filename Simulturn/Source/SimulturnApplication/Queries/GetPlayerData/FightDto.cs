using SimulturnDomain.Entities;

namespace SimulturnApplication.Queries.GetPlayerData;
public record PlayerFightDto(Army Army,
    Army Losses,
    Army Survivors);

public record FightDto(PlayerFightDto PlayerFight,
    IReadOnlyDictionary<string, PlayerFightDto> OpponentFightPerName);