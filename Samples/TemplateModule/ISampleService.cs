using Quantum.Infrastructure.Abstractions;

namespace TemplateModule;
public interface ISampleService : IInitializableService, IAccountService // 可以继承 Quantum.Infrastructure 中的接口哦~
{
    string SayHello();
}
