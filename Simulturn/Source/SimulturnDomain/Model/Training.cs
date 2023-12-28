using SimulturnDomain.ValueTypes;

namespace SimulturnDomain.Model;
public record Training(ushort Turn, Army Army);
public record Construction(ushort Turn, Structure Structure);