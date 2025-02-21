namespace Blue.Web.Controllers;

public class VNetModel
{
    public required string Name { get; set; }
    public string[] Cidrs { get; set; } = [];
}