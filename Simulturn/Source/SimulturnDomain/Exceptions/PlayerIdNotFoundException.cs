namespace SimulturnDomain.Exceptions;
public class PlayerIdNotFoundException : Exception
{
    public string PlayerId { get; }
    public PlayerIdNotFoundException(string playerId) : base()
    {
        PlayerId = playerId;
    }
}