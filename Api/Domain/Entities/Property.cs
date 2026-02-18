namespace Api.Domain.Entities;

public class Property
{
    public Guid Id { get; set; }
    public Guid ProducerId { get; set; }
    public Producer Producer { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public decimal TotalArea { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public virtual ICollection<Plot> Plots { get; set; } = [];
}
