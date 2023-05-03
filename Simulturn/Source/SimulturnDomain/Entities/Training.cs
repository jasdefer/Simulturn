namespace SimulturnDomain.Entities;
public record Training(ushort OrderTurn,
    ushort CompletionTurn,
    Army Army);