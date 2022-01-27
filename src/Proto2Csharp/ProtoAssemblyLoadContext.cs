using System.Reflection;
using System.Runtime.Loader;

namespace Proto2Csharp;

public class ProtoAssemblyLoadContext:AssemblyLoadContext
{
    public ProtoAssemblyLoadContext():base(isCollectible:true)
    {}

    protected override Assembly Load(AssemblyName name)=>null;
}