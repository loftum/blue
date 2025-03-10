using Azure.Core;

namespace Blue.Models;

public class Vnet
{
    public required ResourceIdentifier Id { get; init; }
    public required string Name { get; init; }
    public string[] AddressPrefixes { get; init; } = [];
    public List<VnetPeering> Peerings { get; init; } = [];
}