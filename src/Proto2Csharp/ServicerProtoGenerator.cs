using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Kadder;
using Kadder.Grpc;
using ProtoBuf.Meta;

namespace Proto2Csharp
{
    public class ServicerProtoGenerator
    {
        private readonly string _saveFile;
        private readonly string _packageName;
        private readonly IList<Type> _servicerTypes;
        private readonly List<string> _messages;

        public ServicerProtoGenerator(string saveFile, string packageName, List<Type> servicerTypes)
        {
            _saveFile = saveFile;
            _packageName = packageName;
            _servicerTypes = servicerTypes;
            _messages = new List<string>();
        }

        public void Generate()
        {
            var proto = new StringBuilder();
            proto.AppendLine(generateHead());

            var servicerProtos = new Dictionary<string, string>();
            foreach (var servicerType in _servicerTypes)
            {
                proto.AppendLine(generate(servicerType));
            }
            proto.AppendLine(genBclMessageProto());

            saveProtoFile(proto);
        }

        private void saveProtoFile(StringBuilder proto)
        {
            var saveDir = Path.GetDirectoryName(_saveFile);
            if (!Directory.Exists(saveDir))
                Directory.CreateDirectory(saveDir);

            File.WriteAllText(_saveFile, proto.ToString());
        }

        private string generateHead()
        {
            var head = new StringBuilder();
            head.AppendLine("syntax = \"proto3\";");
            head.AppendLine($"package {_packageName};");
            return head.ToString();
        }

        private string generate(Type servicerType)
        {
            var proto = new StringBuilder();
            var serviceProto = new StringBuilder();
            var messageProto = new StringBuilder();

            serviceProto.AppendLine($"service {servicerType.Name} {{");
            serviceProto.AppendLine();

            var grpcMethods = servicerType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            foreach (var method in grpcMethods)
            {
                if (method.CustomAttributes.FirstOrDefault(p => p.AttributeType.FullName == typeof(NotGrpcMethodAttribute).FullName) != null)
                    continue;

                var protoResult = generateMethodAndMessageForMethod(method);
                serviceProto.AppendLine(protoResult.MethodProto);
                serviceProto.AppendLine();
                messageProto.AppendLine(protoResult.MessageProto);
            }

            serviceProto.AppendLine("}");

            proto.AppendLine(serviceProto.ToString());
            proto.AppendLine(messageProto.ToString());

            return proto.ToString();
        }

        private (string MethodProto, string MessageProto) generateMethodAndMessageForMethod(MethodInfo method)
        {
            var parameterType = method.ParseMethodParameter();
            var returnType = method.ParseMethodReturnParameter();
            var callType = Helper.AnalyseCallType(parameterType, returnType);

            var methodProto = string.Empty;
            switch (callType)
            {
                case CallType.Rpc:
                    methodProto = $"\trpc {method.Name}({parameterType.Name}) returns({returnType.Name});";
                    break;
                case CallType.ClientStreamRpc:
                    methodProto = $"\trpc {method.Name}(stream {parameterType.Name}) returns({returnType.Name})";
                    parameterType = parameterType.GenericTypeArguments[0];
                    break;
                case CallType.ServerStreamRpc:
                    methodProto = $"\trpc {method.Name}({parameterType.Name}) returns(stream {returnType.Name})";
                    returnType = returnType.GenericTypeArguments[0];
                    break;
                case CallType.DuplexStreamRpc:
                    methodProto = $"\trpc {method.Name}(stream {parameterType.Name}) returns(stream {returnType.Name})";
                    parameterType = parameterType.GenericTypeArguments[0];
                    returnType = returnType.GenericTypeArguments[0];
                    break;
            }

            var paramProto = GetProto(parameterType);
            var returnProto = GetProto(returnType);
            var messageProto = $"{paramProto}\n{returnProto}";

            return (methodProto, messageProto);
        }

        private string GetProto(Type type)
        {
            var p = RuntimeTypeModel.Default.GetSchema(type, ProtoSyntax.Proto3).Replace("\r", "");
            var arr = p.Split('\n');
            var proto = new StringBuilder();
            var currentType = string.Empty;
            var isEnum = false;
            var isContent = false;
            for (var i = 0; i < arr.Length; i++)
            {
                var item = arr[i];
                if (item.StartsWith("syntax ") || item.StartsWith("package ") || item.StartsWith("import ") || string.IsNullOrWhiteSpace(item))
                    continue;

                if (item.StartsWith("message"))
                {
                    currentType = item.Replace("message", "").Replace("{", "").Replace(" ", "");
                }
                if (item.StartsWith("enum"))
                {
                    currentType = item.Replace("enum", "").Replace("{", "").Replace(" ", "");
                    isEnum = true;
                }
                if (!isContent && _messages.Contains(currentType))
                {
                    continue;
                }
                if (item.EndsWith("{"))
                {
                    isContent = true;
                    _messages.Add(currentType);
                }
                if (item.EndsWith("}"))
                {
                    isContent = false;
                    isEnum = false;
                }
                if (isEnum && !item.Contains("{"))
                {
                    var key = item.Replace(" ", "").Split('=')[0];
                    item = item.Replace(key, $"{currentType}_{key}");
                }
                if (item.Contains(".bcl."))
                {
                    item = item.Replace(".bcl.", "");
                }
                proto.AppendLine(item);
                if (item.EndsWith("}") && i < arr.Length - 2)
                    proto.AppendLine();
            }
            return proto.ToString();
        }

        private string genBclMessageProto()
        {
            var bclMessages = new StringBuilder();
            bclMessages.AppendLine("message TimeSpan {");
            bclMessages.AppendLine("    sint64 value = 1; // the size of the timespan (in units of the selected scale)");
            bclMessages.AppendLine("    TimeSpanScale scale = 2; // the scale of the timespan [default = DAYS]");
            bclMessages.AppendLine("    enum TimeSpanScale {");
            bclMessages.AppendLine("        DAYS = 0;");
            bclMessages.AppendLine("        HOURS = 1;");
            bclMessages.AppendLine("        MINUTES = 2;");
            bclMessages.AppendLine("        SECONDS = 3;");
            bclMessages.AppendLine("        MILLISECONDS = 4;");
            bclMessages.AppendLine("        TICKS = 5;");
            bclMessages.AppendLine("        MINMAX = 15; // dubious");
            bclMessages.AppendLine("    }");
            bclMessages.AppendLine("}");
            bclMessages.AppendLine();
            bclMessages.AppendLine("message DateTime {");
            bclMessages.AppendLine("    sint64 value = 1; // the offset (in units of the selected scale) from 1970/01/01");
            bclMessages.AppendLine("    TimeSpanScale scale = 2; // the scale of the timespan [default = DAYS]");
            bclMessages.AppendLine("    DateTimeKind kind = 3; // the kind of date/time being represented [default = UNSPECIFIED]");
            bclMessages.AppendLine("    enum TimeSpanScale {");
            bclMessages.AppendLine("        DAYS = 0;");
            bclMessages.AppendLine("        HOURS = 1;");
            bclMessages.AppendLine("        MINUTES = 2;");
            bclMessages.AppendLine("        SECONDS = 3;");
            bclMessages.AppendLine("        MILLISECONDS = 4;");
            bclMessages.AppendLine("        TICKS = 5;");
            bclMessages.AppendLine("        MINMAX = 15; // dubious");
            bclMessages.AppendLine("    }");
            bclMessages.AppendLine("    enum DateTimeKind {");
            bclMessages.AppendLine("        UNSPECIFIED = 0;// The time represented is not specified as either local time or Coordinated Universal Time (UTC).");
            bclMessages.AppendLine("        UTC = 1;// The time represented is UTC.");
            bclMessages.AppendLine("        LOCAL = 2;// The time represented is local time.");
            bclMessages.AppendLine("    }");
            bclMessages.AppendLine("}");
	    bclMessages.AppendLine();
            bclMessages.AppendLine("message Guid { ");
            bclMessages.AppendLine("    fixed64 lo = 1; // the first 8 bytes of the guid (note:crazy-endian)");
            bclMessages.AppendLine("    fixed64 hi = 2; // the second 8 bytes of the guid (note:crazy-endian)");
            bclMessages.AppendLine("}");
	    bclMessages.AppendLine();
            bclMessages.AppendLine("message Decimal {");
            bclMessages.AppendLine("    uint64 lo = 1; // the first 64 bits of the underlying value");
            bclMessages.AppendLine("    uint32 hi = 2; // the last 32 bis of the underlying value");
            bclMessages.AppendLine("    uint32 signScale = 3; // the number of decimal digits (bits 1-16), and the sign (bit 0)");
            bclMessages.AppendLine("}");
	    
            return bclMessages.ToString();
        }
    }
}
  
