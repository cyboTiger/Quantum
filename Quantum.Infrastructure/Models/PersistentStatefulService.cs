using Quantum.Infrastructure.Abstractions;
using Quantum.Infrastructure.Utilities;
using System.Text.Json;

namespace Quantum.Infrastructure.Models;

public abstract class PersistentStatefulService<T>(
    string stateFile,
    Func<T?, Task<string>> serializer,
    Func<string, Task<T?>> deserializer)
    : StatefulService<T>, IPersistentService
{
    protected PersistentStatefulService(string stateFile)
        : this(stateFile,
            serializer: state => Task.Run(() => JsonSerializer.Serialize(state)),
            deserializer: data => Task.Run(() => JsonSerializer.Deserialize<T>(data)))
    { }

    public virtual async Task LoadStateAsync()
    {
        if (!File.Exists(stateFile))
        {
            State = default;
            return;
        }
        try
        {
            var data = await File.ReadAllTextAsync(stateFile);
            State = await deserializer(data.Decrypt());
        }
        catch (Exception)
        {
            State = default;
        }
    }

    public virtual async Task SaveStateAsync()
    {
        if (State is null)
        {
            if (File.Exists(stateFile))
            {
                File.Delete(stateFile);
            }
            return;
        }

        var directory = Path.GetDirectoryName(stateFile);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var data = await serializer(State);
        await File.WriteAllTextAsync(stateFile, data.Encrypt());
    }
}