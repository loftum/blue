using Blue.Core;
using Microsoft.AspNetCore.Mvc;

namespace Blue.Web.Controllers;

[Route("network")]
public class NetworkController : Controller
{
    private readonly BlueClient _client;

    public NetworkController(BlueClient client)
    {
        _client = client;
    }

    [HttpGet("")]
    public async Task<object> Index([FromQuery] bool clearCache)
    {
        var model = await _client.GetNetworkResourcesAsync(clearCache, HttpContext.RequestAborted);
        return View(model);
    }

    [HttpGet("vnets")]
    public async Task<object> VNets([FromQuery] bool clearCache)
    {
        var model = await _client.GetNetworkResourcesAsync(clearCache, HttpContext.RequestAborted);
        var entries = model.Vnets.Values
            .Select(v => new VNetModel { Name = v.Name, Cidrs = v.AddressPrefixes })
            .ToList();
        return View(entries);
    }

    [HttpGet("dns")]
    public async Task<object> Dns([FromQuery] bool clearCache)
    {
        var model = await _client.GetNetworkResourcesAsync(clearCache, HttpContext.RequestAborted);
        return View(model);
    }

    [HttpGet("map")]
    public async Task<object> Map([FromQuery] bool clearCache)
    {
        var model = await _client.GetNetworkGraphAsync(clearCache, HttpContext.RequestAborted);
        
        return View(model);
    }
}



