using System.Collections.Generic;

namespace Proto2Csharp;

public class ProtoGenerateInfo
{
    public string ProjectName{ get; set; }

    public IList<ProtoModelInfo> Servicers{ get;set; }

    public IList<ProtoModelInfo> Protocols{ get;set; }
}

public class ProtoModelInfo
{
    public const string ServicerModule="Servicer";
    public const string ProtocolModule = "Protocol";

    public string Name { get; set; }

    public string Module { get; set; }

    public string Code { get; set; }
}
