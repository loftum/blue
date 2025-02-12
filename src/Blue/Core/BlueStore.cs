using Azure.Core;
using LiteDB;
using LiteDB.Async;
using LiteDB.Queryable;

namespace Blue.Core;

public sealed class BlueStore : IDisposable
{
    private readonly LiteDatabaseAsync _db;
    
    public BlueStore()
    {
        var mapper = BsonMapper.Global;
        
        new BsonMapper()
        mapper.RegisterType<ResourceIdentifier>(i => i.ToString(), b => ResourceIdentifier.Parse(b));
        var file = new FileInfo(PathTranslator.TranslateHomeDir("~/.blue/data/data.db"));
        file.Directory?.Create();
        _db = new LiteDatabaseAsync(file.FullName, mapper);
    }

    public IQueryable<T> Query<T>()
    {
        return _db.GetCollection<T>().AsQueryable();
    }

    public Task SaveAsync<T>(string id, T data)
    {
        return _db.GetCollection<T>().UpsertAsync(id, data);
    }

    public void Dispose()
    {
        _db.Dispose();
    }
}

