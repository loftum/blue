using Azure;
using Azure.ResourceManager;
using Azure.ResourceManager.Resources;

namespace Blue.Core;

internal static class ArmExtensions
{
    public static async IAsyncEnumerable<T> GetAll<T>(this List<SubscriptionResource> subscriptions, Func<SubscriptionResource, AsyncPageable<T>> getResources) where T : ArmResource
    {
        foreach (var resources in subscriptions.Select(getResources))
        {
            await foreach (var resource in resources)
            {
                yield return resource;
            }
        }
    }
}