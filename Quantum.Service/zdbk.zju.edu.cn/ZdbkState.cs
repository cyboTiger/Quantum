using System.Net;
using System.Text.Json;

namespace zdbk.zju.edu.cn;
public record ZdbkState
{
    internal ZdbkState(Cookie jSessionId, Cookie route, string id, string name, string grade, string major, string academicYear, string semester)
        => (JSessionId, Route, Id, Name, Grade, Major, AcademicYear, Semester) = (jSessionId, route, id, name, grade, major, academicYear, semester);

    private record State(Cookie JSessionId, Cookie Route, string Id, string Name, string Grade, string Major, string AcademicYear, string Semester);

    internal Cookie JSessionId { get; }
    internal Cookie Route { get; }

    public string Id { get; }
    public string Name { get; }
    public string Grade { get; }
    public string Major { get; }
    public string AcademicYear { get; }
    public string Semester { get; }

    public static string Serialize(ZdbkState? state) =>
        state is null
            ? string.Empty
            : JsonSerializer.Serialize(new State(
                state.JSessionId,
                state.Route,
                state.Id,
                state.Name,
                state.Grade,
                state.Major,
                state.AcademicYear,
                state.Semester));

    public static ZdbkState? Deserialize(string data)
    {
        try
        {
            var res = JsonSerializer.Deserialize<State>(data);

            return res is null
                ? null
                : new ZdbkState(
                    res.JSessionId,
                    res.Route,
                    res.Id,
                    res.Name,
                    res.Grade,
                    res.Major,
                    res.AcademicYear,
                    res.Semester);
        }
        catch (Exception)
        {
            return null;
        }
    }
}