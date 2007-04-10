using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using RakNetDotNet;

namespace ProtocolGenerator
{
    internal sealed class EventClassGenerator : IGenerator
    {
        public EventClassGenerator(Type type, int eventId, Type protocolInfoType)
        {
            this.type = type;
            this.eventId = eventId;
            this.protocolInfoType = protocolInfoType;
        }

        public void AddChildGenerator(IGenerator generator)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public void RemoveChildGenerator(IGenerator generator)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public void Write(ICodeWriter o)
        {
            o.BeginBlock("public partial class {0} : IEvent {{", type.Name);
            WriteCtorWithId(o);
            WriteCtorWithStream(o);
            WriteSetData(o);
            WriteGetStream(o);
            WriteId(o);
            WriteSourceOid(o);
            WriteTargetOid(o);
            WriteSender(o);
            WriteProtocolInfo(o);
            o.EndBlock("}");
        }

        private void WriteCtorWithId(ICodeWriter o)
        {
            o.BeginBlock("public {0}() {{", type.Name);
            o.WriteLine("id = {0};", eventId);
            o.EndBlock("}");
        }

        private void WriteCtorWithStream(ICodeWriter o)
        {
            o.BeginBlock("public {0}(BitStream source) {{", type.Name);
            WriteStreamReadStatement(o, "out", "id");
            WriteStreamReadStatement(o, "out", "sourceOId");
            WriteStreamReadStatement(o, "out", "targetOId");

            foreach (FieldInfo field in GetFields())
            {
                WriteSerializeFieldStatementRecursive(o, false, field.FieldType, field.Name);
            }
            o.EndBlock("}");
        }

        private void WriteSetData(ICodeWriter o)
        {
            StringBuilder arg = new StringBuilder();
            FieldInfo[] fields = GetFields();
            for (int i = 0; i < fields.Length; i++)
            {
                if (0 < i) arg.Append(", ");
                arg.Append(fields[i].FieldType.ToString());
                arg.Append(" ");
                arg.Append(fields[i].Name);
            }
            o.BeginBlock("public void SetData({0}) {{", arg.ToString());
            foreach (FieldInfo field in fields)
            {
                o.WriteLine("this.{0} = {0};", field.Name);
            }
            o.EndBlock("}");
        }

        private static void WriteStreamReadStatement(ICodeWriter o, string modifier, string variableName)
        {
            o.WriteLine("if (!source.Read({0} {1})) {{ throw new NetworkException(\"Deserialization is failed.\"); }}", modifier, variableName);
        }

        // TODO: ProtocolGenerator can't handle null reference.
        private void WriteGetStream(ICodeWriter o)
        {
            o.BeginBlock("public BitStream Stream {");
            o.BeginBlock("get {");
            o.WriteLine("BitStream eventStream = new BitStream();");
            WriteStreamWriteStatement(o, "id");
            WriteStreamWriteStatement(o, "sourceOId");
            WriteStreamWriteStatement(o, "targetOId");

            foreach (FieldInfo field in GetFields())
            {
                WriteSerializeFieldStatementRecursive(o, true, field.FieldType, field.Name);
            }
            o.WriteLine("return eventStream;");
            o.EndBlock("}");
            o.EndBlock("}");
        }

        private static void WriteStreamWriteStatement(ICodeWriter o, string variableName)
        {
            o.WriteLine("eventStream.Write({0});", variableName);
        }

        private static void WriteSerializeNonCollectionStatement(ICodeWriter o, bool writeToBitstream, Type variableType, string variableName)
        {
            if (BitstreamSerializationHelper.DoesSupportPrimitiveType(variableType))
            {
                WriteStreamWriteOrReadStatement(o, writeToBitstream, "out", variableName);
            }
            else if (variableType.Equals(typeof (NetworkID)) || variableType.Equals(typeof (SystemAddress)))
            {
                WriteStreamWriteOrReadStatement(o, writeToBitstream, "", variableName);
            }
            else if (variableType.IsEnum)
            {
                // TODO
            }
            else
            {
                throw new ApplicationException("This type " + variableType + " doesn't support.");
            }
        }

        private static void WriteSerializeFieldStatementRecursive(ICodeWriter o, bool writeToBitstream, Type variableType, string variableName)
        {
            if (variableType.IsArray)
            {
                Type elemType = variableType.GetElementType();
                if (writeToBitstream)
                {
                    WriteStreamWriteStatement(o, variableName + ".Length");
                    o.BeginBlock("for (int i = 0; i < {0}.Length; i++) {{", variableName);
                    WriteSerializeFieldStatementRecursive(o, writeToBitstream, elemType, variableName + "[i]");
                    o.EndBlock("}");
                }
                else
                {
                    string lengthVariableName = "_" + variableName + "Length";
                    o.WriteLine("int {0};", lengthVariableName);
                    WriteStreamReadStatement(o, "out", lengthVariableName);
                    o.WriteLine("{0} = new {1}[{2}];", variableName, elemType.ToString(), lengthVariableName);
                    o.BeginBlock("for (int i = 0; i < {0}; i++) {{", lengthVariableName);
                    WriteSerializeFieldStatementRecursive(o, writeToBitstream, elemType, variableName + "[i]");
                    o.EndBlock("}");
                }
            }
            else
            {
                WriteSerializeNonCollectionStatement(o, writeToBitstream, variableType, variableName);
            }
        }

        private static void WriteStreamWriteOrReadStatement(ICodeWriter o, bool writeToBitstream, string modifier,
                                                            string variableName)
        {
            if (writeToBitstream)
                WriteStreamWriteStatement(o, variableName);
            else
                WriteStreamReadStatement(o, modifier, variableName);
        }

        private static void WriteId(ICodeWriter o)
        {
            WriteProperty(o, "int", "id", null, null, "protected");
        }

        private static void WriteSourceOid(ICodeWriter o)
        {
            WriteProperty(o, "int", "sourceOId", null, null, null);
        }

        private static void WriteTargetOid(ICodeWriter o)
        {
            WriteProperty(o, "int", "targetOId", null, null, null);
        }

        private static void WriteSender(ICodeWriter o)
        {
            WriteProperty(o, "SystemAddress", "sender", "RakNetBindings.UNASSIGNED_SYSTEM_ADDRESS", null, null);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="o"></param>
        /// <param name="typeName"></param>
        /// <param name="variableNameInLowerCamelCase"></param>
        /// <param name="defaultValue">Can be null</param>
        /// <param name="getterAccessibility">Can be null</param>
        /// <param name="setterAccessibility">Can be null</param>
        private static void WriteProperty(ICodeWriter o, string typeName, string variableNameInLowerCamelCase, string defaultValue, string getterAccessibility, string setterAccessibility)
        {
            string getterAccessibilityWithSpace = "";
            string setterAccessibilityWithSpace = "";
            string defaultValueWithAssignMark = "";

            if (getterAccessibility != null)
            {
                getterAccessibilityWithSpace = getterAccessibility + " ";
            }
            if (setterAccessibility != null)
            {
                setterAccessibilityWithSpace = setterAccessibility + " ";
            }
            if(defaultValue != null)
            {
                defaultValueWithAssignMark = " = " + defaultValue;
            }

            string variableNameInUpperCamelCase = GetVariableNameInUpperCamelCase(variableNameInLowerCamelCase);

            o.BeginBlock("public {0} {1} {{", typeName, variableNameInUpperCamelCase);
            o.WriteLine("{0}get {{ return {1}; }}", getterAccessibilityWithSpace, variableNameInLowerCamelCase);
            o.WriteLine("{0}set {{ {1} = value; }}", setterAccessibilityWithSpace, variableNameInLowerCamelCase);
            o.EndBlock("}");
            o.WriteLine("{0} {1}{2};", typeName, variableNameInLowerCamelCase, defaultValueWithAssignMark);            
        }

        private static string GetVariableNameInUpperCamelCase(string variableNameInLowerCamelCase)
        {
            string firstChar = variableNameInLowerCamelCase.Substring(0, 1);
            string remains;
            if(1 < variableNameInLowerCamelCase.Length)
            {
                remains = variableNameInLowerCamelCase.Substring(1);
            }
            else
            {
                remains = "";
            }
            return firstChar.ToUpper() + remains;
        }

        private void WriteProtocolInfo(ICodeWriter o)
        {
            o.BeginBlock("public IProtocolInfo ProtocolInfo {");
            o.WriteLine("get {{ return {0}.Instance; }}", protocolInfoType.FullName);
            o.EndBlock("}");
        }

        private FieldInfo[] GetFields()
        {
            return type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        }

        private readonly Type type;
        private readonly int eventId;
        private readonly Type protocolInfoType;
    }

    internal static class BitstreamSerializationHelper
    {
        static BitstreamSerializationHelper()
        {
            supportingPrimitives = new List<Type>();
            supportingPrimitives.Add(typeof (bool));
            supportingPrimitives.Add(typeof (byte));
            supportingPrimitives.Add(typeof (double));
            supportingPrimitives.Add(typeof (float));
            supportingPrimitives.Add(typeof (int));
            supportingPrimitives.Add(typeof (sbyte));
            supportingPrimitives.Add(typeof (short));
            supportingPrimitives.Add(typeof (string));
            supportingPrimitives.Add(typeof (uint));
            supportingPrimitives.Add(typeof (ushort));
        }

        public static bool DoesSupportPrimitiveType(Type t)
        {
            return supportingPrimitives.Contains(t);
        }

        private static IList<Type> supportingPrimitives;
    }
}