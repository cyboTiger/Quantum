using Quantum.Sdk.Abstractions;

namespace SearchModule;
public interface ISearchService : IInitializableService // 可以继承 Quantum.Sdk 中的接口
{
    public Task<List<(string title, string url, string date, int number, string source)>> getSearchResult(string searchText);
    public int sourceCnt();
    public int pingSource(int source);
    public string sourceName(int source);
    public bool getEnableSource(int source);
    public void setEnableSource(int source, bool enable);

}
