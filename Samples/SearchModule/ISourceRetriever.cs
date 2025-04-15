using System;
using System.Collections.Generic;

namespace SearchModule;
public interface ISourceRetriever
{
    string SourceId { get; }
    string SourceName { get; }
    string SourceUrl { get; }

    public HttpClient _httpClient { get; set;}

    Task<IEnumerable<(int id, string publishTime, string Url, string title)>> RetrieveSourcesAsync(int numPosts = 10, string keyWord = "");
}