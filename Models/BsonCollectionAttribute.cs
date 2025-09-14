using System;

namespace QuoteOffer.Models;

/// <summary>
/// Attribut pour spécifier le nom de la collection MongoDB
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class BsonCollectionAttribute : Attribute
{
    public string CollectionName { get; }

    public BsonCollectionAttribute(string collectionName)
    {
        CollectionName = collectionName;
    }
}
