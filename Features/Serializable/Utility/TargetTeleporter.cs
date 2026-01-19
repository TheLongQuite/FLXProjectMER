namespace ProjectMER.Features.Serializable.Utility;

public class TargetTeleporter
{
    public TargetTeleporter()
    {
    }

    public TargetTeleporter(string id, int chance)
    {
        Id = id;
        Chance = chance;
    }

    public string Id { get; set; }

    public int Chance { get; set; } = 100;
}