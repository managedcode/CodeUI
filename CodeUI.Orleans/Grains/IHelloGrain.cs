using Orleans;

namespace CodeUI.Orleans.Grains;

public interface IHelloGrain : IGrainWithIntegerKey
{
    ValueTask<string> SayHello(string greeting);
}