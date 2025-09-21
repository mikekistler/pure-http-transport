using System.ComponentModel.DataAnnotations;

public partial class Schema
{
    public string? Description;

    public Dictionary<string, object> Properties { get; set; } = default!;

    public List<string> Required { get; set; } = default!;

    [Required(AllowEmptyStrings = true)]
    public string Type { get; set; } = default!;
}
