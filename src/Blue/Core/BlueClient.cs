using Azure;
using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.DnsResolver;
using Azure.ResourceManager.Network;
using Azure.ResourceManager.PrivateDns;
using Azure.ResourceManager.PrivateDns.Models;
using Azure.ResourceManager.Resources;
using Blue.Data;
using Blue.IO;
using LiteDB;

namespace Blue.Core;

public sealed class BlueClient
{
    private readonly LiteDatabase _db;
    private readonly List<SubscriptionResource> _subscriptions;

    public BlueClient(ArmClient client)
    {
        
        _subscriptions = client.GetSubscriptions().ToList();
        var mapper = new MyMapper();

        mapper.RegisterType<ResourceIdentifier>(i => i.ToString(), b => new ResourceIdentifier(b));
        var file = new FileInfo(PathTranslator.TranslateHomeDir("~/.blue/data/azure.db"));
        file.Directory?.Create();
        _db = new LiteDatabase(file.FullName, mapper);
    }

    public Task<List<SubscriptionData>> GetAllSubscriptionsAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_subscriptions.Select(s => s.Data).ToList());
    }

    public async Task<List<VirtualNetworkData>> GetAllVirtualNetworksAsync(bool clearCache = false, CancellationToken cancellationToken = default)
    {
        return await GetAllAsync(s => s.GetVirtualNetworksAsync(), n => n.Data, clearCache, cancellationToken);
    }
    
    public Task<List<PrivateDnsZone>> GetAllPrivateDnsZonesAsync(bool clearCache = false, CancellationToken cancellationToken = default)
    {
        return GetAllAsync(s => s.GetPrivateDnsZonesAsync(), r =>
        {
            var zone = r.Data;
            var links = r.GetVirtualNetworkLinks().Select(l => l.Data);
            var item = new PrivateDnsZone
            {
                Id = zone.Id,
                Name = zone.Name,
                ETag = zone.ETag,
                MaxNumberOfRecords = zone.MaxNumberOfRecords,
                VirtualNetworkLinks = links.Select(l => new VNetLink
                {
                    Id = l.Id,
                    Name = l.Name,
                    VirtualNetworkId = l.VirtualNetworkId,
                    State = l.VirtualNetworkLinkState
                }).ToList()
            };
            return item;
        }, clearCache, cancellationToken);
    }
    
    public async Task<List<DnsResolverData>> GetAllDnsResolversAsync(bool clearCache = false, CancellationToken cancellationToken = default)
    {
        return await GetAllAsync(s => s.GetDnsResolversAsync(), n => n.Data, clearCache, cancellationToken);
    }

    public async Task<List<VpnGatewayData>> GetAllVpnGatewaysAsync(bool clearCache = false, CancellationToken cancellationToken = default)
    {
        return await GetAllAsync(s => s.GetVpnGatewaysAsync(), n => n.Data, clearCache, cancellationToken);
    }
    
    public async Task<List<TData>> GetAllAsync<T,TData>(Func<SubscriptionResource, AsyncPageable<T>> getAll, Func<T, TData> getData, bool clearCache, CancellationToken cancellationToken) where T : ArmResource
    {
        if (!clearCache && _db.CollectionExists<TData>())
        {
            return _db.GetCollection<TData>().FindAll().ToList();
        }

        var collection = _db.GetCollection<TData>();
        if (clearCache)
        {
            collection.DeleteAll();
        }

        await foreach (var thing in _subscriptions.GetAll(getAll).WithCancellation(cancellationToken))
        {
            collection.Upsert(thing.Id.ToString(), getData(thing));
        }

        return collection.FindAll().ToList();
    }
}

public class PrivateDnsZone
{
    public required ResourceIdentifier Id { get; init; }
    public required string Name { get; init; }
    public ETag? ETag { get; set; }
    /// <summary> The maximum number of record sets that can be created in this Private DNS zone. This is a read-only property and any attempt to set this value will be ignored. </summary>
    public long? MaxNumberOfRecords { get; init; }
    /// <summary> The current number of record sets in this Private DNS zone. This is a read-only property and any attempt to set this value will be ignored. </summary>
    public long? NumberOfRecords { get; init; }
    /// <summary> The maximum number of virtual networks that can be linked to this Private DNS zone. This is a read-only property and any attempt to set this value will be ignored. </summary>
    public long? MaxNumberOfVirtualNetworkLinks { get; init; }
    /// <summary> The current number of virtual networks that are linked to this Private DNS zone. This is a read-only property and any attempt to set this value will be ignored. </summary>
    public long? NumberOfVirtualNetworkLinks { get; init; }
    /// <summary> The maximum number of virtual networks that can be linked to this Private DNS zone with registration enabled. This is a read-only property and any attempt to set this value will be ignored. </summary>
    public long? MaxNumberOfVirtualNetworkLinksWithRegistration { get; init; }
    /// <summary> The current number of virtual networks that are linked to this Private DNS zone with registration enabled. This is a read-only property and any attempt to set this value will be ignored. </summary>
    public long? NumberOfVirtualNetworkLinksWithRegistration { get; init; }
    /// <summary> The provisioning state of the resource. This is a read-only property and any attempt to set this value will be ignored. </summary>
    public PrivateDnsProvisioningState? PrivateDnsProvisioningState { get; init; }

    public List<VNetLink> VirtualNetworkLinks { get; init; } = [];
    
}

public class VNetLink
{
    public required ResourceIdentifier Id { get; init; }
    public required string Name { get; init; }
    public required ResourceIdentifier VirtualNetworkId { get; init; }
    public VirtualNetworkLinkState? State { get; init; }
}