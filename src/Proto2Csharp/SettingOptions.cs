using CommandLine;

namespace Proto2Csharp
{
    public class SettingOptions
    {
        [Value(0, MetaName = "Type", Required = true, HelpText = "Set oper type! egg: proto | project")]
        public string Type { get; set; }

        [Option('i', "input", Required = true, HelpText = "Set input dir(egg: ~/Documents/test).")]
        public string InputDir { get; set; }

        [Option('o', "output", Required = true, HelpText = "Set output dir(egg: ~/Documents/test).")]
        public string OutputDir { get; set; }

        [Option("single_proto", Required = false, Default = false, HelpText = "Set generate single proto file. default: false")]
        public bool SingleProto { get; set; }
    }
}
