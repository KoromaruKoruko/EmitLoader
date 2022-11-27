/* NOTE
This is a Generalized (Non-Standardized) Project just for Testing and Ensuring Changes to the EmitLoader Library
A More Standardized Testing Method Should be Implemented later on.
*/

using EmitLoader;
using System;
using System.IO;
using System.Reflection;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

using System.Reflection.Emit;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace EmitLoader.Tester
{
    public delegate void HelloDelegate();

    public class Test
    {
        private static String BuildCustomAttributes(ICustomAttribute[] Attributes)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append('[');
            sb.Append(BuildFullyQualifiedName(Attributes[0].AttributeType));
            for (int x = 1; x < Attributes.Length; x++)
            {
                sb.Append(',');
                sb.Append(BuildFullyQualifiedName(Attributes[x].AttributeType));
            }
            sb.Append(']');
            return sb.ToString();
        }
        private static String BuildFullyQualifiedName(IType Type)
        {
            StringBuilder sb = new StringBuilder();

            if (!Type.IsGenericTypeParameter)
            {
                if (Type.IsArray)
                {
                    sb.Append(Type.GetFullyQualifiedName());
                }
                else
                {
                    if (Type.DeclaringType != null)
                    {
                        sb.Append(BuildFullyQualifiedName(Type.DeclaringType));
                        sb.Append('/');
                    }
                    else if (Type.Namespace != null)
                    {
                        sb.Append(Type.Namespace.GetFullyQualifiedName());
                        sb.Append('.');
                    }

                    sb.Append(Type.Name);

                    if (Type.IsGeneric && !Type.IsGenericDefinition)
                    {
                        Boolean f = true;
                        sb.Append('[');
                        foreach (IType genericArg in Type.GenericArguments)
                        {
                            if (f)
                                f = false;
                            else
                                sb.Append(", ");
                            sb.Append(BuildFullyQualifiedName(genericArg));
                        }
                        sb.Append(']');
                    }
                }
            }
            else
                sb.Append(Type.Name);

            return sb.ToString();
        }
        private static String BuildMethodLayoutString(IMethod Method)
        {
            StringBuilder sb = new StringBuilder(Method.Name);
            Boolean f = true;
            if (Method.IsGeneric)
            {
                sb.Append("<");
                foreach (IGenericParameter Param in Method.GenericArguments)
                {
                    sb.Append(f ? Param.Name : $", {Param.Name}");
                    f = false;
                }
                sb.Append(">");
            }

            f = true;
            sb.Append("(");
            foreach (IParameter Param in Method.Parameters)
            {
                sb.Append(f ? $"{BuildFullyQualifiedName(Param.ParameterType)} {Param.Name}" : $", {BuildFullyQualifiedName(Param.ParameterType)} {Param.Name}");
                f = false;
            }
            sb.Append(')');
            return sb.ToString();
        }

        public static void Main(String[] _)
        {
            using (Stream fout = File.OpenWrite("output.txt"))
            {

                fout.Position = 0;
                Out.FOut = fout;
                Out.COut = Console.Error;

                PrintAssembly();

                Out.WriteLine("Ensuring Cleanup");
                CleanupTest();

                fout.SetLength(fout.Position);
                fout.Flush();
            }

            Assembly BuiltAssembly;
            using (Stream DLLStream = File.OpenRead("example.DLL"))
            {
                AssemblyLoader Loader = new AssemblyLoader();

                IMetadataAssembly exampleAsm = Loader.LoadAssembly(DLLStream);

                Loader.BuildMetadataAssemblies(DefaultAccessControllers.NoRestrictions);

                BuiltAssembly = exampleAsm.GetBuiltAssembly();
            }
        }

        public static void PrintAssembly()
        {
            using (Stream stm = File.OpenRead(@"..\..\..\..\EmitLoader.ExampleDLL\bin\Release\net48\emitLoader.ExampleDLL.dll"))
            {
                AssemblyLoader loader = new AssemblyLoader();
                IMetadataAssembly asm = loader.LoadAssembly(stm);

                Out.WriteLine("[AssemblyReferences] START");
                Out.BeginIndentRegion();
                foreach (IAssembly referenceAssembly in asm.GetReferencedAssemblies())
                    Out.WriteLine(referenceAssembly.Name.Name);
                Out.EndIndentRegion();
                Out.WriteLine("[AssemblyReferences] END");

                Out.WriteLine("[TypeReferences] START");
                Out.BeginIndentRegion();
                foreach (IType referenceType in asm.GetReferencedTypes())
                    Out.WriteLine(BuildFullyQualifiedName(referenceType));
                Out.EndIndentRegion();
                Out.WriteLine("[TypeReferences] END");

                Out.WriteLine("[DefinedTypes] START");
                Out.BeginIndentRegion();
                foreach (IType Type in asm.GetDefinedTypes())
                {
                    Out.WriteLine($"[{Type.Name}]");
                    Out.BeginIndentRegion();
                    Out.WriteLine($"Namespace: {Type.Namespace?.GetFullyQualifiedName()}");
                    Out.WriteLine($"IsNested: {Type.IsNestedType}");
                    Out.WriteLine($"IsInterface: {Type.IsInterface}");
                    Out.WriteLine($"IsValueType: {Type.IsValueType}");
                    Out.WriteLine($"IsEnum: {Type.IsEnum}");
                    Out.WriteLine($"IsStatic: {Type.IsStatic}");
                    Out.WriteLine($"IsPointer: {Type.IsPointer}");
                    Out.WriteLine($"IsByRef: {Type.IsByRef}");
                    Out.WriteLine($"IsArray: {Type.IsArray}");
                    Out.WriteLine($"IsSZArray: {Type.IsSZArray}");

                    if (Type.BaseType != null)
                        Out.WriteLine($"BaseType: {BuildFullyQualifiedName(Type.BaseType)}");
                    else
                        Out.WriteLine($"BaseType: null");

                    Out.WriteLine($"HasStaticConstructor: {Type.StaticConstructor != null}");

                    if (Type.Constructors.Length > 0)
                    {
                        Out.WriteLine("[Constructors]");
                        Out.BeginIndentRegion();
                        foreach (IMethod Method in Type.Constructors)
                            Out.WriteLine($"{BuildFullyQualifiedName(Method.ReturnType)} {BuildMethodLayoutString(Method)}");
                        Out.EndIndentRegion();
                    }

                    if (Type.Interfaces.Length > 0)
                    {
                        Out.WriteLine("[Interfaces]");
                        Out.BeginIndentRegion();
                        foreach (IType Interface in Type.Interfaces)
                            Out.WriteLine(BuildFullyQualifiedName(Interface));
                        Out.EndIndentRegion();
                    }

                    if (Type.CustomAttributes.Length > 0)
                    {
                        Out.WriteLine("[CustomAttributes]");
                        Out.BeginIndentRegion();
                        foreach (ICustomAttribute Attribute in Type.CustomAttributes)
                            Out.WriteLine($"{BuildFullyQualifiedName(Attribute.AttributeType)}");
                        Out.EndIndentRegion();
                    }

                    if (Type.Fields.Length > 0)
                    {
                        Out.WriteLine("[Fields]");
                        Out.BeginIndentRegion();
                        foreach (IField Field in Type.Fields)
                        {
                            if (Field.CustomAttributes.Length > 0)
                                Out.WriteLine(BuildCustomAttributes(Field.CustomAttributes));
                            Out.WriteLine($"{BuildFullyQualifiedName(Field.FieldType)} {Field.Name};");
                        }
                        Out.EndIndentRegion();
                    }

                    if (Type.Methods.Length > 0)
                    {
                        Out.WriteLine("[Methods]");
                        Out.BeginIndentRegion();
                        foreach (IMethod Method in Type.Methods)
                        {
                            if (Method.CustomAttributes.Length > 0)
                                Out.WriteLine(BuildCustomAttributes(Method.CustomAttributes));
                            Out.WriteLine($"{BuildFullyQualifiedName(Method.ReturnType)} {BuildMethodLayoutString(Method)}");
                        }
                        Out.EndIndentRegion();
                    }

                    if (Type.Events.Length > 0)
                    {

                        Out.WriteLine("[Events]");
                        Out.BeginIndentRegion();
                        foreach (IEvent Event in Type.Events)
                        {
                            if (Event.CustomAttributes.Length > 0)
                                Out.WriteLine(BuildCustomAttributes(Event.CustomAttributes));
                            Out.WriteLine($"{BuildFullyQualifiedName(Event.EventType)} {Event.Name}");
                        }
                        Out.EndIndentRegion();
                    }

                    if (Type.Properties.Length > 0)
                    {
                        Out.WriteLine("[Properties]");
                        Out.BeginIndentRegion();
                        foreach (IProperty Property in Type.Properties)
                        {
                            if (Property.CustomAttributes.Length > 0)
                                Out.WriteLine(BuildCustomAttributes(Property.CustomAttributes));

                            Out.Write($"{BuildFullyQualifiedName(Property.PropertyType)} {Property.Name} {{");
                            if (Property.Getter != null)
                                Out.Write(" get;");
                            if (Property.Setter != null)
                                Out.Write(" set;");
                            Out.WriteLine(" }");
                        }
                        Out.EndIndentRegion();
                    }
                    Out.EndIndentRegion();
                }
                Out.EndIndentRegion();
                Out.WriteLine("[DefinedTypes] END");

                WhiteListAccessController wlac = new WhiteListAccessController();

                wlac.Whitelist(typeof(string).Assembly);
                wlac.Whitelist(typeof(Out));
                wlac.Whitelist(typeof(IType));
                wlac.Whitelist(typeof(AssemblyObjectKind));
                wlac.Whitelist(typeof(IAssembly));
                wlac.Whitelist(typeof(ValueType));
                wlac.Whitelist(typeof(AssemblyLoader));
                wlac.Whitelist(typeof(IAssemblySolverObject));
                wlac.Whitelist(typeof(IConstant));

                loader.BuildMetadataAssemblies(wlac);

                Out.WriteLine("[Loaded Assemblies] START");
                Out.BeginIndentRegion();
                foreach (Assembly appdmAsm in AppDomain.CurrentDomain.GetAssemblies())
                    Out.WriteLine(appdmAsm.FullName);
                Out.EndIndentRegion();
                Out.WriteLine("[Loaded Assemblies] END");


                Out.Write("[MethodInvokTest] ");
                asm.FindType("EmitLoader.ExampleDLL", "ExampleType").FindMethod("HelloWorld", Array.Empty<IType>()).GetBuiltMethod().Invoke(null, null);

                Out.Write("[ConstraintTest] ");
                try
                {
                    IType TBase = asm.FindType("EmitLoader.ExampleDLL", "ExampleGeneric`1");
                    IType TConstruacted = TBase.ConstructGeneric(new IType[] { loader.ResolveType(typeof(int)) });
                    try
                    {
                        Type TConstruactedBuilt = TConstruacted.GetBuiltType();
                        Type TBaseBuilt = TBase.GetBuiltType();
                        Out.WriteLine("Test Invalid (Constraint not Physical)");
                    }
                    catch
                    {
                        Out.WriteLine("PreCheck Failed");
                    }
                }
                catch
                {
                    Out.WriteLine("Success");
                }

                Out.Write("[ConstraintTest2] ");
                try
                {
                    IType TBase = asm.FindType("EmitLoader.ExampleDLL", "ExampleGeneric`1");
                    IType TConstruacted = TBase.ConstructGeneric(new IType[] { loader.ResolveType(typeof(String)) });
                    Type TConstruactedBuilt = TConstruacted.GetBuiltType();
                    Type TBaseBuilt = TBase.GetBuiltType();

                    Out.WriteLine("Success");
                }
                catch
                {
                    Out.WriteLine("Failed");
                }

                Out.Write("[EnumValuesTest] ");
                try
                {
                    IType TestEnum = asm.FindType("EmitLoader.ExampleDLL", "TestEnum");
                    Type TestEnumBuilt = TestEnum.GetBuiltType();

                    foreach(IField field in TestEnum.Fields)
                    {
                        if (field.Name == "value__")
                            continue;
                        object value = field.GetBuiltField().GetValue(null);
                        object tvalue = Enum.ToObject(TestEnumBuilt, int.Parse(field.Name.Substring(1)));
                        if (Enum.GetName(TestEnumBuilt, value) != Enum.GetName(TestEnumBuilt, tvalue))
                            throw new Exception("Failed");
                    }
                    Out.WriteLine("Success");
                }
                catch
                {
                    Out.WriteLine("Failed");
                }

                Out.WriteLine($"[Global Lookup Test] {(asm.FindType("", "GlobalEnum") == null ? "Failed" : "Success")}");
            }
        }

        public static Boolean IsLoaded()
        {
            foreach (Assembly appdmAsm in AppDomain.CurrentDomain.GetAssemblies())
                if (appdmAsm.GetName().Name == "emitLoader.ExampleDLL")
                    return true;
            return false;
        }
        public static void CleanupTest()
        {
            int x = 0;
            while (IsLoaded())
            {
                x++;
                Thread.Sleep(1000);
                GC.Collect();
            }
            Console.WriteLine($"Cleanup Concluded After {x} tries");
        }
    }

    public static class Out
    {
        private static readonly Byte[] endline = Encoding.Unicode.GetBytes(Environment.NewLine);
        private static Int32 Indent = 0;
        private static String Prefix = String.Empty;
        private static Byte[] PrefixBytes = Array.Empty<Byte>();
        private static Boolean newLine = true;
        public static Stream FOut { get; set; }
        public static TextWriter COut { get; set; }
        public static void WriteLine(String line)
        {
            DoPrefix();

            Task c = COut.WriteLineAsync(line);

            Byte[] bytes = Encoding.Unicode.GetBytes(line);
            FOut.Write(bytes, 0, bytes.Length);
            FOut.Write(endline, 0, endline.Length);

            c.Wait();

            newLine = true;
        }
        public static void Write(String data)
        {
            DoPrefix();

            Task c = COut.WriteAsync(data);

            Byte[] bytes = Encoding.Unicode.GetBytes(data);
            FOut.Write(bytes, 0, bytes.Length);

            c.Wait();
        }
        private static void DoPrefix()
        {
            if (newLine && Indent > 0)
            {
                Task c = COut.WriteAsync(Prefix);
                FOut.Write(PrefixBytes, 0, PrefixBytes.Length);
                newLine = false;
                c.Wait();
            }
        }


        public static void BeginIndentRegion()
        {
            Indent++;
            Prefix = new string(' ', Indent * 4);
            PrefixBytes = Encoding.Unicode.GetBytes(Prefix);
        }
        public static void EndIndentRegion()
        {
            if (Indent == 0)
                return;
            Indent--;
            Prefix = new string(' ', Indent * 4);
            PrefixBytes = Encoding.Unicode.GetBytes(Prefix);
        }
    }


    public static class Generator
    {
        public static void Generate()
        {
            GenerateOpCodeLookup();
        }

        public static void GenerateOpCodeLookup()
        {
            Type OpCodes_type = typeof(OpCodes);
            SortedList<byte, String> List = new SortedList<byte, String>();
            SortedList<byte, String> List2 = new SortedList<byte, String>();
            foreach (FieldInfo field in OpCodes_type.GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                OpCode op = (OpCode)field.GetValue(null);

                if (op.Size == 1)
                    if (op.Name == "prefix1")
                        continue;
                    else
                        List.Add(unchecked((byte)op.Value), field.Name);
                else if ((op.Value & 0xFF00) == 0xFE00)
                    List2.Add(unchecked((byte)op.Value), field.Name);
                else
                    throw new Exception("unexpected OpCode!");
            }
            Out.WriteLine("switch(Reader.ReadByte())");
            Out.WriteLine("{");
            foreach (KeyValuePair<byte, String> x in List)
                Out.WriteLine($"    case 0x{x.Key:X2}: return OpCodes.{x.Value};");

            Out.WriteLine("    case 0xFE:");
            Out.WriteLine("        switch(Reader.ReadByte())");
            Out.WriteLine("        {");
            foreach (KeyValuePair<byte, String> x in List2)
                Out.WriteLine($"            case 0x{x.Key & 0xFF:X2}: return OpCodes.{x.Value};");
            Out.WriteLine("            break;");
            Out.WriteLine("        }");
            Out.WriteLine("}");
        }
    }
}
