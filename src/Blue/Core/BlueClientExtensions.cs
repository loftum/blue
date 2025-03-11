using System.Diagnostics.CodeAnalysis;
using Azure.Core;
using Blue.Models;

namespace Blue.Core;

public static class BlueClientExtensions
{
    public static async Task<NetworkResources> GetNetworkResourcesAsync(this BlueClient client, bool clearCache, CancellationToken cancellationToken = default)
    {
        var model = new NetworkResources
        {
            Subscriptions = (await client.GetAllSubscriptionsAsync(cancellationToken)).ToDictionary(i => i.Id.ToString(), i => i),
            VirtualNetworks = (await client.GetAllVirtualNetworksAsync(clearCache: clearCache, cancellationToken: cancellationToken)).ToDictionary(i => i.Id.ToString(), i => i),
            PrivateDnsZones = (await client.GetAllPrivateDnsZonesAsync(clearCache: clearCache, cancellationToken: cancellationToken)).ToDictionary(i => i.Id.ToString(), i => i),
        };

        var networks = (await client.GetAllVirtualNetworksAsync(clearCache: clearCache, cancellationToken: cancellationToken)).ToDictionary(v => v.Id, v => v);
        foreach (var (id, network) in networks)
        {
            var vnet = new Vnet
            {
                Id = id,
                Name = network.Name,
                AddressPrefixes = network.AddressPrefixes.ToArray()
            };
            foreach (var peering in network.VirtualNetworkPeerings)
            {
                if (networks.TryGetValue(peering.RemoteVirtualNetworkId, out var remote))
                {
                    var remotePeering = remote.VirtualNetworkPeerings.SingleOrDefault(p => p.RemoteVirtualNetworkId == network.Id);
                    if (remotePeering == null)
                    {
                        vnet.Peerings.Add(new VnetPeering
                        {
                            Id = peering.Id,
                            Name = peering.Name,
                            VnetId = vnet.Id,
                            RemoteVnetId = peering.RemoteVirtualNetworkId,
                            ForwardedTraffic = GetDirection(peering.AllowForwardedTraffic),
                            VnetAccess = GetDirection(peering.AllowVirtualNetworkAccess),
                            GatewayTraffic = GetDirection(peering.AllowGatewayTransit)
                        });
                    }
                    else
                    {
                        vnet.Peerings.Add(new VnetPeering
                        {
                            Id = peering.Id,
                            RemotePeeringId = remotePeering.Id,
                            Name = peering.Name,
                            VnetId = vnet.Id,
                            RemoteVnetId = peering.RemoteVirtualNetworkId,
                            ForwardedTraffic = GetDirection(peering.AllowForwardedTraffic, remotePeering.AllowForwardedTraffic),
                            VnetAccess = GetDirection(peering.AllowVirtualNetworkAccess, remotePeering.AllowVirtualNetworkAccess),
                            GatewayTraffic = peering.AllowGatewayTransit.GetValueOrDefault() && remotePeering.UseRemoteGateways.GetValueOrDefault()
                                ? PeeringDirection.Forward
                                : remotePeering.AllowGatewayTransit.GetValueOrDefault() && peering.UseRemoteGateways.GetValueOrDefault()
                                ? PeeringDirection.Backward
                                : PeeringDirection.None
                        });
                    }
                }
            }

            model.Vnets[id] = vnet;
        }

        return model;
    }

    public static async Task<NetworkGraph> GetNetworkGraphAsync(this BlueClient client, 
        bool clearCache,
        CancellationToken cancellationToken = default)
    {
        var resources = await client.GetNetworkResourcesAsync(clearCache, cancellationToken);
        var graph = new NetworkGraph();
        var peerings = new List<VnetPeering>();

        foreach (var vnet in resources.Vnets.Values.OrderBy(v => v.Name))
        {
            var subscription = resources.Subscriptions.Values.FirstOrDefault(s => s.SubscriptionId == vnet.Id.SubscriptionId);
            
            var node = new GraphNode
            {
                ResourceType = "vnet",
                Id = vnet.Id.ToString(),
                Label = vnet.Name,
                Shape = "ellipsis",
                Group = subscription?.DisplayName,
                Title = subscription == null
                    ? vnet.Id.ResourceGroupName
                    :$"{subscription.DisplayName} / {vnet.Id.ResourceGroupName}"
            };

            graph.Nodes.Add(node);
            
            foreach (var peering in vnet.Peerings)
            {
                if (peerings.Any(p => p.Covers(peering)))
                {
                    continue;
                }
                
                if (peering.TryGetEdge(peering.VnetAccess, "vnet access", "#00ff00", out var edge))
                {
                    graph.Edges.Add(edge);
                }

                if (peering.TryGetEdge(peering.ForwardedTraffic, "forwarded traffic", "#22ccff", out edge))
                {
                    graph.Edges.Add(edge);
                }

                if (peering.TryGetEdge(peering.GatewayTraffic, "gateway traffic", "#ff9900", out edge))
                {
                    graph.Edges.Add(edge);
                }
                peerings.Add(peering);
            }
        }

        foreach (var dns in resources.PrivateDnsZones.Values.GroupBy(d => new DnsKey(d)))
        {
            var subscription = resources.Subscriptions.Values.FirstOrDefault(s => s.SubscriptionId == dns.Key.SubscriptionId);
            
            var node = new GraphNode
            {
                ResourceType = "private dns",
                Id = Guid.NewGuid().ToString("N"),
                Label = string.Join('\n', dns.Select(d => d.Name)),
                Shape = "box",
                Group = subscription?.DisplayName,
                Title = subscription == null
                    ? dns.Key.ResourceGroupName
                    :$"{subscription.DisplayName} / {dns.Key.ResourceGroupName}"
            };
            graph.Nodes.Add(node);

            foreach (var vnetId in dns.Key.VnetIds)
            {
                graph.Edges.Add(new GraphEdge
                {
                    Id = Guid.NewGuid().ToString("N"),
                    From = node.Id,
                    To = vnetId.ToString(),
                    Color = "#cccccc",
                    Title = "private DNS link"
                });
            }
        }

        return graph;
    }

    private sealed class DnsKey
    {
        public string? SubscriptionId { get; }
        public string? ResourceGroupName { get; }
        public string VnetIdsString { get; }
        public ResourceIdentifier[] VnetIds { get; }
        public DnsKey(PrivateDnsZone zone)
        {
            SubscriptionId = zone.Id.SubscriptionId;
            ResourceGroupName = zone.Id.ResourceGroupName;
            VnetIds = zone.VirtualNetworkLinks.Select(l => l.VirtualNetworkId).ToArray();
            VnetIdsString = string.Join(",", VnetIds.Select(i => i.ToString()));
        }

        public override bool Equals(object? obj)
        {
            return Equals(obj as DnsKey);
        }

        public bool Equals(DnsKey? other)
        {
            return other != null &&
                   SubscriptionId == other.SubscriptionId &&
                   ResourceGroupName == other.ResourceGroupName &&
                   VnetIdsString == other.VnetIdsString;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(SubscriptionId, ResourceGroupName, VnetIdsString);
        }
    }

    private static bool TryGetEdge(this VnetPeering peering, PeeringDirection direction, string title, string color, [MaybeNullWhen(false)] out GraphEdge edge)
    {
        if (direction is PeeringDirection.None or PeeringDirection.Unknown)
        {
            edge = null;
            return false;
        }
        edge = new GraphEdge
        {
            Id = Guid.NewGuid().ToString("N"),
            From = peering.VnetId.ToString(),
            To = peering.RemoteVnetId.ToString(),
            Title = title,
            Arrows =
            {
                From =
                {
                    Enabled = direction is PeeringDirection.Backward or PeeringDirection.BothWays
                },
                To =
                {
                    Enabled = direction is PeeringDirection.Forward or PeeringDirection.BothWays
                }
            },
            Color = color,
            
        };
        return true;
    }
    
    private static PeeringDirection GetDirection(bool? forward)
    {
        return forward == true ? PeeringDirection.Forward : PeeringDirection.Unknown;
    }
    
    private static PeeringDirection GetDirection(bool? forward, bool? backward)
    {
        if (forward == null && backward == null)
        {
            return PeeringDirection.None;
        }

        if (forward == true)
        {
            return backward == true ? PeeringDirection.BothWays : PeeringDirection.Forward; 
        }

        return backward == true ? PeeringDirection.Backward : PeeringDirection.None;
    }
}