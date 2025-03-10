using Azure.ResourceManager.DnsResolver;
using Azure.ResourceManager.Network;
using Azure.ResourceManager.Resources;
using Blue.Core;

namespace Blue.Models;

public class NetworkResources
{
    public Dictionary<string, SubscriptionData> Subscriptions { get; init; } = new();
    public Dictionary<string, VirtualNetworkData> VirtualNetworks { get; init; } = new();
    public Dictionary<string, Vnet> Vnets { get; init; } = new();
    public Dictionary<string, PrivateDnsZone> PrivateDnsZones { get; init; } = new();
    public Dictionary<string, DnsResolverData> DnsResolvers { get; init; } = new();
}