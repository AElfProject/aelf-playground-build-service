namespace GrainInterfaces;

public interface IHelloGrain : IGrainWithStringKey
{
    ValueTask<string> SayHello(string greeting);
}