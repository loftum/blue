using Azure.Core;
using Azure.ResourceManager.DnsResolver;
using Azure.ResourceManager.Network;
using Azure.ResourceManager.PrivateDns;
using Azure.ResourceManager.Resources;

namespace Blue.Models;

public class NetworkMap
{
    public Dictionary<ResourceIdentifier, SubscriptionData> Subscriptions { get; init; } = new();
    public Dictionary<ResourceIdentifier, VirtualNetworkData> VirtualNetworks { get; init; } = new();
    public Dictionary<ResourceIdentifier, Vnet> Vnets { get; init; } = new();
    public Dictionary<ResourceIdentifier, PrivateDnsZoneData> PrivateDnsZones { get; init; } = new();
    public Dictionary<ResourceIdentifier, DnsResolverData> DnsResolvers { get; init; } = new();
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