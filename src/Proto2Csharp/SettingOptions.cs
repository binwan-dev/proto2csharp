using System.IO;
using CommandLine;

namespace Proto2Csharp
{
    public class SettingOptions
    {
        [Value(0, MetaName = "Type", Required = true, HelpText = "Set oper type! egg: proto | project")]
        public string Type { get; set; }

        [Option('i', "input", Required = false, HelpText = "Set input dir(egg: ~/Documents/test).")]
        public string InputDir { get; set; }

        [Option('o', "output", Required = true, HelpText = "Set output file(egg: ~/Documents/test/test.proto).")]
        public string OutputFile { get; set; }

        [Option('p', "package", Required = true, HelpText = "Set Package name(egg: test).")]		
		public string PackageName{get;set; }
    }
}
