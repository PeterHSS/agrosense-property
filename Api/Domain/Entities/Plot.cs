namespace Api.Domain.Entities;

public class Plot
{
    public Guid  Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Crop { get; set; } = string.Empty;
    public decimal Area { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Guid PropertyId { get; set; }
    public virtual Property Property { get; set; } = null!;
}