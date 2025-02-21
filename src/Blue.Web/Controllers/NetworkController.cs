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
    public async Task<object> Index()
    {
        var model = await _client.GetNetworkMapAsync(HttpContext.RequestAborted);
        return View(model);
    }

    [HttpGet("vnets")]
    public async Task<object> VNets()
    {
        var model = await _client.GetNetworkMapAsync(HttpContext.RequestAborted);
        var entries = model.Vnets.Values
            .Select(v => new VNetModel { Name = v.Name, Cidrs = v.AddressPrefixes })
            .ToList();
        return View(entries);
    }

    [HttpGet("map")]
    public async Task<object> Map()
    {
        var model = await _client.GetNetworkMapAsync(HttpContext.RequestAborted);
        return View(model);
    } 
}

public class VNetModel
{
    public required string Name { get; set; }
    public string[] Cidrs { get; set; } = [];
}