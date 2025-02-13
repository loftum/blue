using Blue.Core;
using Blue.Models;
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

    [HttpGet("map")]
    public async Task<object> Map()
    {
        var model = await _client.GetNetworkMapAsync(HttpContext.RequestAborted);
        return View(model);
    } 
}