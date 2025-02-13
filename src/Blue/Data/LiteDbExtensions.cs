using LiteDB;

namespace Blue.Data;

internal static class LiteDbExtensions
{
    public static bool CollectionExists<T>(this LiteDatabase db)
    {
        return db.CollectionExists(db.Mapper.ResolveCollectionName(typeof(T)));
    }
}