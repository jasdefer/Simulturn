using SimulturnDomain.Entities;

namespace SimulturnApplication.Common.Interfaces;
public interface IPlayerRepository
{
    Task EndTurn(string gameId, string player);
    Task AddTraining(string gameId, string player, ushort startTrainingTurn, ushort completeTrainingTurn, Army army);
    public Task<Army> GetTraining(string gameId, string player, ushort completeTrainingTurn, Army army);
    public Task<ushort> GetMatter(string gameId, string player);
    public Task AddIncome(string gameId, string player, ushort turn, Income income);
}
