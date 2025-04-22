using System;
using System.Collections.Generic;

namespace SearchModule;
public interface ISourceRetriever
{
    string SourceId { get; }
    string SourceName { get; }
    string SourceUrl { get; }

    Task<List<(string title, string url, string date, int number)>> RetrieveSourcesAsync(int numPosts = 10, string keyWord = "");
}