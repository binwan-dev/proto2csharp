using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;
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
		    if(string.IsNullOrWhiteSpace(o.Input))
                        o.Input = Directory.GetCurrentDirectory();
                    shellOut($"Use Project -> {Directory.GetCurrentDirectory()}");
		}).WithParsed(o => options = o);

            if (options.Type == "proto")
            {
                generateProto(options);
            }
            if (options.Type == "project")
            {
                generateProject(options);
            }
        }

        static void generateProject(SettingOptions options)
        {
            Console.WriteLine("Analysing proto...");

            if (!File.Exists(options.Input))
                throw new FileNotFoundException($"Notfound input proto file! Input: {options.Input}");

            var shellHelper = new ShellHelper();
            var createProjectResult = shellHelper.Run("dotnet", $"new classlib -n -o {options.Output}", shellOut);
            if (createProjectResult.ExitCode != 0)
                return;

            var addPackageResult = shellHelper.Run("dotnet", $"add package Kadder --package-directory {options.Output}");
            if (addPackageResult.ExitCode != 0)
                return;


            // void parseProto()
            // {
	    // 	var servicerArea=false;
	    // 	var protocolArea=false;
		
            //     var lines = File.ReadAllLines(options.Input);

	    // 	var usingCode=new StringBuilder();
            //     var namespaceCode = new StringBuilder();
	    // 	var servicerCode=new StringBuilder();
	    // 	var protocolCode=new StringBuilder();

	    // 	usingCode.AppendLine("using ProtoBuf");

            //     foreach (var line in lines)
            //     {
            //         if (line.StartsWith("package"))
            //         {
            //             var namespaceName = line.Split(' ')[1].Replace(";", "");
            //             namespaceCode.AppendLine($"namespace {namespaceName};");
            //         }

            //         if (line.StartsWith("service"))
            //         {
            //             var servicerName = line.Split(' ')[1].Replace("{", "").Trim();

            //             servicerArea = true;
            //             servicerCode.AppendLine($"    public interface {servicerName}");
            //             servicerCode.AppendLine("    {");
            //         }
            //         if (servicerArea && !string.IsNullOrWhiteSpace(line))
            //         {
            //             var bracketArr = line.Substring(line.IndexOf("rpc ") + 4).Split('(');
            //             var method = bracketArr[0];
            //             var request = bracketArr[1].Substring(0, bracketArr[1].IndexOf(')'));
            //             var response = bracketArr[2].Substring(0, bracketArr[2].IndexOf(')'));

            //             servicerCode.AppendLine($"        Task<{response}> {method}Async({request} request);");
            //             servicerCode.AppendLine();
            //         }
            //         if (servicerArea && line.StartsWith("}"))
            //         {
            //             servicerArea = false;
            //             servicerCode.AppendLine("    }");
            //             servicerCode.AppendLine();
            //         }

            //         if (line.StartsWith("message"))
            //         {
            //             var messageName = line.Split(' ')[1].Replace("{", "").Trim();

            //             protocolArea = true;
            //             protocolCode.AppendLine($"    public class {messageName}");
            //             protocolCode.AppendLine("    {");
            //         }
	    // 	    if(protocolArea&&)
            //     }
            // }

        }

        static void generateProto(SettingOptions options)
        {
            Console.WriteLine("Analysing project...");

            if (!Directory.Exists(options.Input))
                throw new DirectoryNotFoundException($"Notfound inputdir! InputDir: {options.Input}");

            var tempDir = $"{options.Input}/temp";
            try
            {
                var csprojFile = Directory.GetFiles(options.Input, "*.csproj");
                if (csprojFile.Length == 0)
                    throw new FileNotFoundException($"The Input({options.Input}) notfound csproj file!");
                var projectName = Path.GetFileNameWithoutExtension(csprojFile[0]);

                var shellHelper = new ShellHelper();

                var result = shellHelper.Run("dotnet", $"publish {options.Input} -o {tempDir}", shellOut);
                if (result.ExitCode != 0)
                    return;

                var projectReferences = GetReferencePackageNames(csprojFile[0]);
                projectReferences.Add(projectName);

                var context = new ProtoAssemblyLoadContext();
                context = LoadAssemblies(projectReferences, tempDir, context);

                var assembly = context.Assemblies.FirstOrDefault(p => p.FullName.Contains(projectName));
                var servicerTypes = ServicerHelper.GetServicerTypes(new List<Assembly> { assembly });
                var protoGenerator = new ServicerProtoGenerator(options.Output, options.PackageName, servicerTypes);
                protoGenerator.Generate();

                context.Unload();
                GC.Collect();
                GC.WaitForPendingFinalizers();
                deleteTempDir();
            }
            catch (Exception ex)
            {
                deleteTempDir();
                throw new Exception("See inner exception!", ex);
            }

            void deleteTempDir()
            {
                if(Directory.Exists(tempDir))
                    Directory.Delete(tempDir,true);
            }
        }

        static List<String> GetReferencePackageNames(string csprojFile)
        {
            var references = new List<string>();

            var contents = File.ReadAllLines(csprojFile);
            foreach (var line in contents)
            {
		if(!line.Contains("<ProjectReference"))
                    continue;

                var path = line.Substring(line.IndexOf("Include=\"") + 9).Split("\"")[0].Replace("\\","/");
                references.Add(Path.GetFileNameWithoutExtension(path));
            }

            return references;
        }

        static ProtoAssemblyLoadContext LoadAssemblies(List<string> references,string dllDir,ProtoAssemblyLoadContext context)
        {
            var d = new DirectoryInfo(dllDir);
            foreach (var reference in d.GetFiles("*.dll"))
            {
                using (var stream = new FileStream(reference.FullName, FileMode.Open, FileAccess.Read))
                {
                    context.LoadFromStream(stream);
                }
            }
            return context;
        }

        static void shellOut(string output)
        {
            Console.WriteLine(output);
        }
    }
}
