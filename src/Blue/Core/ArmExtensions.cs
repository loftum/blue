using Azure;
using Azure.ResourceManager;
using Azure.ResourceManager.Resources;

namespace Blue.Core;

internal static class ArmExtensions
{
    public static async IAsyncEnumerable<T> GetAll<T>(this List<SubscriptionResource> subscriptions, Func<SubscriptionResource, AsyncPageable<T>> getResources) where T : ArmResource
    {
        foreach (var subscription in subscriptions)
        {
            Console.WriteLine($"{subscription.Data.DisplayName}.GetAll<{typeof(T).Name}>");
            var resources = getResources(subscription);
            var enumerator = resources.GetAsyncEnumerator();
            while (await enumerator.TryMoveNextAsync())
            {
                yield return enumerator.Current;
            }
        }
    }

    private static async ValueTask<bool> TryMoveNextAsync<T>(this IAsyncEnumerator<T> enumerator)
    {
        try
        {
            return await enumerator.MoveNextAsync();
        }
        catch (Exception e)
        {
            Console.WriteLine($"Bam: {e.Message}");
            return false;
        }
    }
}