using System;
using System.IO;
using CommandLine;
using Kadder.Grpc.Server;
using Kadder.Utils;

namespace Proto2Csharp
{
    class Program
    {
        static void Main(string[] args)
        {
            SettingOptions options=new SettingOptions();
            Parser.Default.ParseArguments<SettingOptions>(args).WithParsed(o => options = o);

            if(options.Type=="proto")
            {
            }


        }

        static void generateProto(SettingOptions options)
        {
            if(!Directory.Exists(options.InputDir))
                throw new DirectoryNotFoundException($"Notfound inputdir! InputDir: {options.InputDir}");
            
            var buildTempDir=$"{options.InputDir}/temp";
            if(!Directory.Exists(buildTempDir))
                Directory.CreateDirectory(buildTempDir);
            var csprojFile=Directory.GetFiles(options.InputDir,"*.csproj");
            if(csprojFile.Length==0)
                throw new FileNotFoundException($"The InputDir({options.InputDir}) notfound csproj file!");
            var projectName=Path.GetFileNameWithoutExtension(csprojFile[0]);
            
            var shellHelper=new ShellHelper();
            
            
            var result=shellHelper.Run("cd", options.InputDir);
            if(result.ExitCode!=0)
                throw new ApplicationException($"exec cd inputdir failed! InputDir: {options.InputDir}");
            result=shellHelper.Run("dotnet", $"build -o temp");
            if(result.ExitCode!=0)
                return;
                
            // var servicerTypes=ServicerHelper.GetServicerTypes(List<Assembly> assemblies)
            // var protoGenerator=new ServicerProtoGenerator(options.OutputDir,string.Empty,);
        }

        static void shellOut(string output)
        {
            Console.WriteLine(output);
        }
    }
}
