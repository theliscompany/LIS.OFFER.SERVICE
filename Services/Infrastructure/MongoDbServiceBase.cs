using MongoDB.Driver;
using MongoDB.Bson;
using QuoteOffer.Models;
using System.Reflection;

namespace QuoteOffer.Services.Infrastructure;

/// <summary>
/// Service de base pour l'accès aux données MongoDB
/// </summary>
public abstract class MongoDbServiceBase<T> where T : class
{
    protected readonly IMongoCollection<T> _collection;
    protected readonly ILogger<MongoDbServiceBase<T>> _logger;

    protected MongoDbServiceBase(IMongoDatabase database, ILogger<MongoDbServiceBase<T>> logger)
    {
        _logger = logger;
        _collection = database.GetCollection<T>(GetCollectionName());
    }

    /// <summary>
    /// Obtient le nom de la collection à partir de l'attribut BsonCollection
    /// </summary>
    protected virtual string GetCollectionName()
    {
        var collectionAttribute = typeof(T).GetCustomAttribute<BsonCollectionAttribute>();
        return collectionAttribute?.CollectionName ?? typeof(T).Name.ToLowerInvariant();
    }

    /// <summary>
    /// Crée un document
    /// </summary>
    protected async Task<string> CreateAsync(T entity)
    {
        try
        {
            await _collection.InsertOneAsync(entity);
            
            // Récupérer l'ID du document inséré
            var idProperty = typeof(T).GetProperty("Id");
            if (idProperty != null)
            {
                var id = idProperty.GetValue(entity);
                return id?.ToString() ?? string.Empty;
            }
            
            return string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating {EntityType}", typeof(T).Name);
            throw;
        }
    }

    /// <summary>
    /// Récupère un document par son ID
    /// </summary>
    protected async Task<T?> GetByIdAsync(string id)
    {
        try
        {
            var filter = Builders<T>.Filter.Eq("_id", id);
            return await _collection.Find(filter).FirstOrDefaultAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting {EntityType} by ID {Id}", typeof(T).Name, id);
            throw;
        }
    }

    /// <summary>
    /// Met à jour un document
    /// </summary>
    protected async Task<bool> UpdateAsync(string id, T entity)
    {
        try
        {
            var filter = Builders<T>.Filter.Eq("_id", id);
            var result = await _collection.ReplaceOneAsync(filter, entity);
            return result.ModifiedCount > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating {EntityType} with ID {Id}", typeof(T).Name, id);
            throw;
        }
    }

    /// <summary>
    /// Supprime un document
    /// </summary>
    protected async Task<bool> DeleteAsync(string id)
    {
        try
        {
            var filter = Builders<T>.Filter.Eq("_id", id);
            var result = await _collection.DeleteOneAsync(filter);
            return result.DeletedCount > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting {EntityType} with ID {Id}", typeof(T).Name, id);
            throw;
        }
    }

    /// <summary>
    /// Récupère tous les documents avec pagination
    /// </summary>
    protected async Task<(List<T> Items, long TotalCount)> GetAllAsync(int page = 1, int pageSize = 10)
    {
        try
        {
            var totalCount = await _collection.CountDocumentsAsync(FilterDefinition<T>.Empty);
            
            var items = await _collection
                .Find(FilterDefinition<T>.Empty)
                .Skip((page - 1) * pageSize)
                .Limit(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all {EntityType}", typeof(T).Name);
            throw;
        }
    }

    /// <summary>
    /// Recherche avec filtres
    /// </summary>
    protected async Task<(List<T> Items, long TotalCount)> SearchAsync(
        FilterDefinition<T> filter, 
        int page = 1, 
        int pageSize = 10,
        SortDefinition<T>? sort = null)
    {
        try
        {
            var totalCount = await _collection.CountDocumentsAsync(filter);
            
            var query = _collection.Find(filter);
            
            if (sort != null)
                query = query.Sort(sort);

            var items = await query
                .Skip((page - 1) * pageSize)
                .Limit(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching {EntityType}", typeof(T).Name);
            throw;
        }
    }
}
