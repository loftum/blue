using Azure;
using Azure.Core;
using Azure.ResourceManager;
using Azure.ResourceManager.DnsResolver;
using Azure.ResourceManager.Network;
using Azure.ResourceManager.PrivateDns;
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

    public Task<List<SubscriptionData>> GetAllSubscriptionsAsync()
    {
        return Task.FromResult(_subscriptions.Select(s => s.Data).ToList());
    }

    public async Task<List<VirtualNetworkData>> GetAllVirtualNetworksAsync(bool clearCache = false, CancellationToken cancellationToken = default)
    {
        return await GetAllAsync(s => s.GetVirtualNetworksAsync(), n => n.Data, clearCache, cancellationToken);
    }
    
    public async Task<List<PrivateDnsZoneData>> GetAllPrivateDnsZonesAsync(bool clearCache = false, CancellationToken cancellationToken = default)
    {
        return await GetAllAsync(s => s.GetPrivateDnsZonesAsync(), n => n.Data, clearCache, cancellationToken);
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

