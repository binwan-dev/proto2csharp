using System.IO;
using CommandLine;

namespace Proto2Csharp
{
    public interface IProtoOptions
    {
	[Option('p',"package",SetName="proto", Required = true, HelpText = "Set Package name(egg: test).")]
	public string PackageName{ get; set;}
    }

    public interface IProjectOptions
    {
	[Option("project_name",SetName="project", Required = true, HelpText = "Set Project name(egg: test).")]	
	public string ProjectName{ get;set; }
    }

    public class SettingOptions:IProtoOptions,IProjectOptions
    {
        [Value(0, MetaName = "Type", Required = true, HelpText = "Set oper type! egg: proto | project")]
        public string Type { get; set; }

        [Option('i', "input", Required = false, HelpText = "Set input dir/file(egg: ~/Documents/test).")]
        public string Input { get; set; }

        [Option('o', "output", Required = true, HelpText = "Set output file/dir(egg: ~/Documents/test/test.proto).")]
        public string Output { get; set; }
	
        public string PackageName { get;set; }
	
        public string ProjectName { get;set; }
    }
}
