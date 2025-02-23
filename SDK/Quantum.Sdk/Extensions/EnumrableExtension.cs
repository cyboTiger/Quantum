using Quantum.Infrastructure.Abstractions;
using Quantum.Infrastructure.Utilities;
using System.Reflection;

namespace Quantum.Infrastructure.Extensions;

public static class EnumrableExtension
{
    public static IEnumerable<Assembly> GetAssemblies<T>(this IEnumerable<T> objs)
    {
        foreach (var obj in objs)
        {
            if (obj?.GetType() is { } type)
                yield return type.Assembly;
        }
    }

    public static Result<T> GetModule<T>(this IEnumerable<T> modules, string moduleId, Version? minVersion = null, Version? maxVersion = null)
        where T : IModule
    {
        var module = modules.FirstOrDefault(m => m.ModuleId == moduleId);
        if (module is null)
        { return Result.Failure($"{moduleId} is not loaded"); }
        if (minVersion is not null && module.Version < minVersion)
        { return Result.Failure($"{moduleId} version is less than required version {minVersion}"); }
        if (maxVersion is not null && module.Version > maxVersion)
        { return Result.Failure($"{moduleId} version is greater than required version {maxVersion}"); }
        return Result.Success(module);
    }
}
