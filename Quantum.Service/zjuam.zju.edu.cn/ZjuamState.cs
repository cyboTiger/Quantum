using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Text.Json;

namespace zjuam.zju.edu.cn;

[SuppressMessage("ReSharper", "InconsistentNaming")]
public record ZjuamState
{
    internal ZjuamState(string id, string password, Cookie cookie) => (Id, Password, IPlanetDirectoryPro) = (id, password, cookie);

    private record State(string Id, string Password, Cookie IPlanetDirectoryPro);

    public string Id { get; }
    internal Cookie IPlanetDirectoryPro { get; }
    internal string Password { get; }

    public static string Serialize(ZjuamState? state) =>
        state is null
            ? string.Empty
            : JsonSerializer.Serialize(new State(state.Id, state.Password, state.IPlanetDirectoryPro));

    public static ZjuamState? Deserialize(string data)
    {
        try
        {
            var res = JsonSerializer.Deserialize<State>(data);

            return res is null
                ? null
                : new ZjuamState(res.Id, res.Password, res.IPlanetDirectoryPro);
        }
        catch (Exception)
        {
            return null;
        }
    }
}
