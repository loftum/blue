using Azure.ResourceManager;
using Azure.ResourceManager.Network;
using Blue.Core;
using LiteDB.Queryable;
using Microsoft.AspNetCore.Mvc;

namespace Blue.Web.Controllers;

[Route("network")]
public class NetworkController : Controller
{
    private readonly BlueStore _store;
    private readonly ArmClient _client;

    public NetworkController(ArmClient client, BlueStore store)
    {
        _client = client;
        _store = store;
    }

    [HttpGet("")]
    public async Task<object> Index()
    {
        var networks = await _store.Query<VirtualNetworkData>().ToListAsync();

        if (networks.Count == 0)
        {
            var subscriptions = _client.GetSubscriptions();
            foreach (var subscription in subscriptions)
            {
                await foreach (var network in subscription.GetVirtualNetworksAsync())
                {
                    if (network is { HasData: true })
                    {
                        networks.Add(network.Data);    
                    }
                }
            }

            foreach (var network in networks)
            {
                await _store.SaveAsync(network.Id.ToString(), network.Name);
            }
        }
        
        return View(networks);
    }
}