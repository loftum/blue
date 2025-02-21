using Azure.Core;
using Azure.ResourceManager.DnsResolver;
using Azure.ResourceManager.Network;
using Azure.ResourceManager.Resources;
using Blue.Core;

namespace Blue.Models;

public class NetworkMap
{
    public Dictionary<string, SubscriptionData> Subscriptions { get; init; } = new();
    public Dictionary<string, VirtualNetworkData> VirtualNetworks { get; init; } = new();
    public Dictionary<string, Vnet> Vnets { get; init; } = new();
    public Dictionary<string, PrivateDnsZone> PrivateDnsZones { get; init; } = new();
    public Dictionary<string, DnsResolverData> DnsResolvers { get; init; } = new();
}

public class Vnet
{
    public required ResourceIdentifier Id { get; init; }
    public required string Name { get; init; }
    public string[] AddressPrefixes { get; init; } = [];
    public List<VnetPeering> Peerings { get; init; } = new();
}

public class VnetPeering
{
    public required string Name { get; init; }
    public PeeringDirection Traffic { get; init; }
    public PeeringDirection VnetAccess { get; init; }
    public PeeringDirection GatewayTransit { get; init; }
    public PeeringDirection RemoteGateways { get; init; }
    public required ResourceIdentifier RemoteVnetId { get; init; }
}

public enum PeeringDirection
{
    Unknown,
    None,
    Forward,
    Backward,
    BothWays
}