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
        private readonly string _saveDir;
        private readonly string _packageName;
        private readonly IList<Type> _servicerTypes;
        private readonly List<string> _messages;

        public ServicerProtoGenerator(string saveDir, string packageName, List<Type> servicerTypes)
        {
            _saveDir = saveDir;
            _packageName = packageName;
            _servicerTypes = servicerTypes;
            _messages = new List<string>();
        }

        public void Generate()
        {
            var servicerProtos = new Dictionary<string, string>();
            foreach (var servicerType in _servicerTypes)
            {
                var servicerProto = generate(servicerType);
                servicerProtos.Add($"{servicerType.Name}.proto", servicerProto);
            }
            servicerProtos.Add("main.proto", generateMainProto(servicerProtos.Keys.ToList()));

            saveProtoFile(servicerProtos);
        }

        private void saveProtoFile(Dictionary<string, string> protos)
        {
            if (!Directory.Exists(_saveDir))
                Directory.CreateDirectory(_saveDir);

            foreach (var proto in protos)
                File.WriteAllText(Path.Combine(_saveDir, proto.Key), proto.Value);
        }

        private string generateMainProto(List<string> servicerProtoFiles)
        {
            var proto = new StringBuilder();
            proto.AppendLine("syntax = \"proto3\";");
            proto.AppendLine();
            foreach (var servicerProtoFile in servicerProtoFiles)
                proto.AppendLine($"import {servicerProtoFile}");

            return proto.ToString();
        }

        private string generate(Type servicerType)
        {
            var proto = new StringBuilder();
            proto.AppendLine(generateHead(servicerType));

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

        private string generateHead(Type servicerType)
        {
            var head = new StringBuilder();
            head.AppendLine("syntax = \"proto3\";");

            var namespaceName = servicerType.Namespace;
            if (!string.IsNullOrWhiteSpace(_packageName))
                namespaceName = _packageName;
            head.AppendLine($"package {namespaceName};");
            return head.ToString();
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
                if (item.StartsWith("syntax ") || item.StartsWith("package ") || item.StartsWith("import "))
                    continue;

                if (item.StartsWith("message"))
                {
                    currentType = item.Replace("message", "").Replace("{", "").Replace(" ", "");
                    isContent = true;
                }
                if (item.StartsWith("enum"))
                {
                    currentType = item.Replace("enum", "").Replace("{", "").Replace(" ", "");
                    isEnum = true;
                    isContent = true;
                }
                if (!isContent && _messages.Contains(currentType))
                {
                    continue;
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
                _messages.Add(currentType);
                proto.AppendLine(item);
                if (item.EndsWith("}") && i < arr.Length - 2)
                    proto.AppendLine();
            }
            return proto.ToString();
        }

    }
}

