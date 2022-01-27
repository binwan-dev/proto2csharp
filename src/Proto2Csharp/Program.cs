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
            var options = new SettingOptions();
            Parser.Default.ParseArguments<SettingOptions>(args)
				.WithParsed(o=>
				{
					if(string.IsNullOrWhiteSpace(o.InputDir))
                        o.InputDir = Directory.GetCurrentDirectory();
                    shellOut($"Use Project -> {Directory.GetCurrentDirectory()}");
                }).WithParsed(o => options = o);

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

            var tempDir = $"{options.InputDir}/temp";
            try
            {
                var csprojFile = Directory.GetFiles(options.InputDir, "*.csproj");
                if (csprojFile.Length == 0)
                    throw new FileNotFoundException($"The InputDir({options.InputDir}) notfound csproj file!");
                var projectName = Path.GetFileNameWithoutExtension(csprojFile[0]);

                var shellHelper = new ShellHelper();

                var result = shellHelper.Run("dotnet", $"publish {options.InputDir} -o {tempDir}", shellOut);
                if (result.ExitCode != 0)
                    return;

                var context = new ProtoAssemblyLoadContext();
                using(var stream = new FileStream($"{tempDir}/{projectName}.dll",FileMode.Open,FileAccess.Read))
                {
                    context.LoadFromStream(stream);
                
                    var assembly = context.Assemblies.FirstOrDefault(p => p.FullName.Contains(projectName));
                    var servicerTypes = ServicerHelper.GetServicerTypes(new List<Assembly> { assembly });
                    var protoGenerator = new ServicerProtoGenerator(options.OutputFile, options.PackageName, servicerTypes);
                    protoGenerator.Generate();
                }
                context.Unload();
                
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
            finally
            {
				if(Directory.Exists(tempDir))
                    Directory.Delete(tempDir,true);
            }
        }

        static void shellOut(string output)
        {
            Console.WriteLine(output);
        }
    }
}
