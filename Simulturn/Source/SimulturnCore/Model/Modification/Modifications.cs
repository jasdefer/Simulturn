namespace SimulturnCore.Model.Modification;
public class Modifications
{
    public List<Construction> Constructions { get; set; } = new List<Construction>();
    public List<Training> Trainings { get; set; } = new List<Training>();
    public List<Movement> Movements { get; set; } = new List<Movement>();
}
