using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using CommandLine;
using Kadder.Utils;

namespace Proto2Csharp
{
    class Program
    {
        static void Main(string[] args)
        {
            SettingOptions options = new SettingOptions();
            Parser.Default.ParseArguments<SettingOptions>(args).WithParsed(o => options = o);

            if (options.Type == "proto")
            {
                generateProto(options);
            }
        }

        static void generateProto(SettingOptions options)
        {
            Console.WriteLine("Analysing project...");

            if (!Directory.Exists(options.InputDir))
                throw new DirectoryNotFoundException($"Notfound inputdir! InputDir: {options.InputDir}");

            var csprojFile = Directory.GetFiles(options.InputDir, "*.csproj");
            if (csprojFile.Length == 0)
                throw new FileNotFoundException($"The InputDir({options.InputDir}) notfound csproj file!");
            var projectName = Path.GetFileNameWithoutExtension(csprojFile[0]);

            var shellHelper = new ShellHelper();

            var result = shellHelper.Run("dotnet", $"publish {options.InputDir} -o {options.InputDir}/temp/temp", shellOut);
            if (result.ExitCode != 0)
                return;

            var context=new AssemblyLoadContext("proto");
            foreach (var file in Directory.GetFiles($"{options.InputDir}/temp/temp/", "*.dll"))
                context.LoadFromAssemblyPath(file);

            var assembly = context.Assemblies.FirstOrDefault(p => p.FullName.Contains(projectName));
            var servicerTypes=ServicerHelper.GetServicerTypes(new List<Assembly>{assembly});
            var protoGenerator=new ServicerProtoGenerator(options.OutputDir,string.Empty,servicerTypes);
            protoGenerator.Generate();

            Directory.Delete($"{options.InputDir}/temp",true);
        }

        static void shellOut(string output)
        {
            Console.WriteLine(output);
        }
    }
}
