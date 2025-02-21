using Blue.Models;

namespace Blue.Core;

public static class BlueClientExtensions
{
    public static async Task<NetworkMap> GetNetworkMapAsync(this BlueClient client, bool clearCache, CancellationToken cancellationToken = default)
    {
        var model = new NetworkMap
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
                            Name = peering.Name,
                            RemoteVnetId = peering.RemoteVirtualNetworkId,
                            Traffic = GetDirection(peering.AllowForwardedTraffic),
                            VnetAccess = GetDirection(peering.AllowVirtualNetworkAccess),
                            GatewayTransit = GetDirection(peering.AllowGatewayTransit),
                            RemoteGateways = GetDirection(peering.UseRemoteGateways)
                        });
                    }
                    else
                    {
                        vnet.Peerings.Add(new VnetPeering
                        {
                            Name = peering.Name,
                            RemoteVnetId = peering.RemoteVirtualNetworkId,
                            Traffic = GetDirection(peering.AllowForwardedTraffic, remotePeering.AllowForwardedTraffic),
                            VnetAccess = GetDirection(peering.AllowVirtualNetworkAccess, remotePeering.AllowVirtualNetworkAccess),
                            GatewayTransit = GetDirection(peering.AllowGatewayTransit, remotePeering.AllowGatewayTransit),
                            RemoteGateways = GetDirection(peering.UseRemoteGateways, remotePeering.UseRemoteGateways)
                        });
                    }
                }
            }

            model.Vnets[id] = vnet;
        }

        return model;
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