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

            saveProtoFile(proto);
        }

        private void saveProtoFile(StringBuilder proto)
        {
            var saveDir = Path.GetDirectoryName(_saveFile);
            if (!Directory.Exists(saveDir))
                Directory.CreateDirectory(saveDir);

            File.WriteAllText(_saveFile,proto.ToString());
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
                if (method.CustomAttributes.FirstOrDefault(p => p.AttributeType == typeof(NotGrpcMethodAttribute)) != null)
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
                proto.AppendLine(item);
                if (item.EndsWith("}") && i < arr.Length - 2)
                    proto.AppendLine();
            }
            return proto.ToString();
        }

    }
}

