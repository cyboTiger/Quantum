using Quantum.Sdk.Abstractions;

namespace SearchModule;
public interface ISearchService : IInitializableService // 可以继承 Quantum.Sdk 中的接口
{
    public Task<List<(string title, string url, string date, int number)>> getSearchResult(string searchText);
    
    public void setEnableAI(bool enableAI);
    public int sourceCnt();
    public int pingSource(int source);
    public void setEnableSource(int source, bool enable);

}
