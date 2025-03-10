using Azure.Core;

namespace Blue.Models;

public class VnetPeering
{
    public required ResourceIdentifier Id { get; init; }
    public ResourceIdentifier? RemotePeeringId { get; init; }
    public required string Name { get; init; }
    public ResourceIdentifier VnetId { get; init; }
    public required ResourceIdentifier RemoteVnetId { get; init; }
    public PeeringDirection ForwardedTraffic { get; init; }
    public PeeringDirection VnetAccess { get; init; }
    public PeeringDirection GatewayTraffic { get; init; }

    public bool Covers(VnetPeering peering)
    {
        return ReferenceEquals(this, peering) ||
               VnetId == peering.VnetId &&
               RemoteVnetId == peering.RemoteVnetId ||
               VnetId == peering.RemoteVnetId &&
               RemoteVnetId == peering.VnetId;
    }
}