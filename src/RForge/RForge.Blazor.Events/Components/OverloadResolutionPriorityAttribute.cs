#if !NET9_0_OR_GREATER
namespace System.Runtime.CompilerServices;

// The attribute is not in the .NET 8 base class library. Compilers that implement C# 13 overload
// resolution priority (.NET 9 SDK and later) match it by name, so defining it here lets net8 apps built
// with those compilers resolve SubscriptionBuilder.On(async e => ...) the same way net9 and net10 apps do.
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor | AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
internal sealed class OverloadResolutionPriorityAttribute(int priority) : Attribute
{
    public int Priority { get; } = priority;
}
#endif
