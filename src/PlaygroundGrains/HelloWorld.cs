using GrainInterfaces;
namespace Grains;

public sealed class HelloGrain : Grain, IHelloGrain
{
    public ValueTask<string> SayHello(string greeting) =>
        ValueTask.FromResult($"Hello, {greeting}!");
}