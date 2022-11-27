/*
Testing Assembly for EmitLoader.Tester
For Testing the Layout and Building Capabilities of EmitLoader
*/

using EmitLoader.Tester;

using System;

namespace EmitLoader.ExampleDLL
{
    public static class ExampleType
    {
        // Test Simple References
        public static void HelloWorld() => Out.WriteLine("Hello World! from emitLoader.ExampleDLL.ExampleType.HelloWorld()");

        // Test Exception Block Building
        public static Boolean ComplexTest_IntToBoolean(int x)
        {
            Boolean why;
            try
            {
                switch (x)
                {
                    case 0: return false;
                    case 1: return true;

                    case 3: break;

                    case 4: throw new Exception();
                    case 5: throw new InvalidOperationException();

                    default:
                        throw new IndexOutOfRangeException();
                }
            }
            catch (InvalidOperationException)
            {

            }
            catch (Exception ex)
            {
                if (ex is IndexOutOfRangeException)
                    throw;
            }
            finally
            {
                why = false;
            }
            return why;
        }

        // Test Nested Types
        public enum NestedEnum : UInt16
        {
            Hello = 1,
            Goodbye = UInt16.MaxValue,
        }

        // Test Simple Generic Types
        public static ExampleGeneric<String> ExampleConstructedGenericFieldType;
    }

    // Test Simple Generic Definitions
    public class ExampleGeneric<T> where T : class
    {
        public T GenericField;
    }

    // Test Enum Value Preservation
    public enum TestEnum
    {
        _1 = 1,
        _2 = 2,
        _4 = 4,
        _8 = 8,
        _16 = 16,
    }

    // Test Interface Explicit Implementations
    public class InterfaceImplementations : IConstant
    {
        public AssemblyObjectKind Kind => AssemblyObjectKind.Constant;
        public ValueType ValueType => ValueType.Null;
        object IConstant.Value => null;
        public object Value => "HiddenValue";
        public AssemblyLoader Context => null;
        public IAssembly Assembly => null;

        ~InterfaceImplementations()
        {
            Console.WriteLine("Dying");
        }
    }
}
// Test Global Types
public enum GlobalEnum : Byte
{
    Hello = 1,
    Goodbye = Byte.MaxValue,
}
