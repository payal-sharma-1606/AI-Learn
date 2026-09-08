using Azure;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using SmartNotes.Api.Exceptions;
using SmartNotes.Api.Interfaces;
using SmartNotes.Api.Models;

namespace SmartNotes.Api.Services;

/// <summary>
/// Owns the Azure AI Search index that note embeddings will be stored in and searched against.
/// </summary>
public class SearchIndexService : ISearchIndexService
{
    /// <summary>Names tying a field to an algorithm: field -> profile -> algorithm configuration.</summary>
    private const string VectorProfileName = "note-vector-profile";

    private const string VectorAlgorithmName = "note-hnsw";

    /// <summary>
    /// Length of the vectors the index will store. It must match the embedding model's output
    /// (1536 for text-embedding-3-small); Azure rejects vectors of any other size at upload time.
    /// </summary>
    private readonly int _dimensions;

    private readonly Lazy<SearchIndexClient> _indexClient;
    private readonly string _indexName;
    private readonly ILogger<SearchIndexService> _logger;

    public SearchIndexService(IConfiguration configuration, ILogger<SearchIndexService> logger)
    {
        _logger = logger;
        _indexName = configuration["AzureSearch:IndexName"] ?? "notes-index";
        _dimensions = configuration.GetValue<int?>("AzureSearch:EmbeddingDimensions") ?? 1536;

        // Lazy for the same reason as the AI client: missing configuration should fail this
        // call with a clear message rather than prevent the application from starting.
        _indexClient = new Lazy<SearchIndexClient>(() =>
        {
            var (endpoint, credential) = ReadConfiguration(configuration);
            return new SearchIndexClient(endpoint, credential);
        });
    }

    public async Task EnsureIndexAsync(CancellationToken cancellationToken = default)
    {
        var index = new SearchIndex(_indexName)
        {
            Fields =
            {
                new SimpleField(nameof(NoteChunk.Id), SearchFieldDataType.String) { IsKey = true },
                new SimpleField(nameof(NoteChunk.NoteId), SearchFieldDataType.Int32)
                {
                    IsFilterable = true,
                    IsSortable = true,
                },
                new SimpleField(nameof(NoteChunk.ChunkIndex), SearchFieldDataType.Int32) { IsSortable = true },
                // Searchable rather than simple: these hold the text a keyword search would read.
                new SearchableField(nameof(NoteChunk.Title)),
                new SearchableField(nameof(NoteChunk.ChunkText)),
                new VectorSearchField(nameof(NoteChunk.Embedding), _dimensions, VectorProfileName),
            },
            VectorSearch = new VectorSearch
            {
                // HNSW is an approximate nearest-neighbour index: it gives up exactness in
                // return for search that stays fast as the number of chunks grows.
                Algorithms = { new HnswAlgorithmConfiguration(VectorAlgorithmName) },
                Profiles = { new VectorSearchProfile(VectorProfileName, VectorAlgorithmName) },
            },
        };

        try
        {
            // CreateOrUpdate rather than Create, so re-running this is not an error.
            await _indexClient.Value.CreateOrUpdateIndexAsync(index, cancellationToken: cancellationToken);
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(ex, "Could not create search index {IndexName} (status {Status}).", _indexName, ex.Status);
            throw new AiServiceException(
                "Could not create the search index. The search service returned an error.",
                isTransient: ex.Status is 429 or >= 500,
                ex);
        }

        _logger.LogInformation("Search index {IndexName} is ready.", _indexName);
    }

    private static (Uri Endpoint, AzureKeyCredential Credential) ReadConfiguration(IConfiguration configuration)
    {
        var endpoint = configuration["AzureSearch:Endpoint"];
        var apiKey = configuration["AzureSearch:ApiKey"];

        if (string.IsNullOrWhiteSpace(endpoint) || string.IsNullOrWhiteSpace(apiKey))
        {
            throw new AiServiceException(
                "Search is not configured. Set AzureSearch Endpoint and ApiKey.",
                isTransient: false);
        }

        return (new Uri(endpoint), new AzureKeyCredential(apiKey));
    }
}
