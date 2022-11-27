
using System;

namespace EmitLoader.Reflection
{
    internal class ReflectionConstant : IConstant
    {
        public ValueType ValueType { get; }
        public Object Value { get; }

        internal ReflectionConstant(Object Value, ReflectionSolver Assembly)
        {
            this.Value = Value;
            this.Assembly = Assembly;

            if (Value == null)
                ValueType = ValueType.Null;
            else
            {
                Type t = Value.GetType();
                if (t == typeof(string))
                    ValueType = ValueType.String;
                else if (t == typeof(bool))
                    ValueType = ValueType.Boolean;
                else if (t == typeof(byte))
                    ValueType = ValueType.Byte;
                else if (t == typeof(sbyte))
                    ValueType = ValueType.SByte;
                else if (t == typeof(short))
                    ValueType = ValueType.Int16;
                else if (t == typeof(int))
                    ValueType = ValueType.Int32;
                else if (t == typeof(long))
                    ValueType = ValueType.Int64;
                else if (t == typeof(ushort))
                    ValueType = ValueType.UInt16;
                else if (t == typeof(uint))
                    ValueType = ValueType.UInt32;
                else if (t == typeof(ulong))
                    ValueType = ValueType.UInt64;
                else if (t == typeof(float))
                    ValueType = ValueType.Single;
                else if (t == typeof(double))
                    ValueType = ValueType.Double;
                else if (t == typeof(char))
                    ValueType = ValueType.Char;
                else
                    throw new ArgumentException("Value type not supported", nameof(Value));
            }
        }

        public AssemblyObjectKind Kind => AssemblyObjectKind.Constant;
        public AssemblyLoader Context => this.Context;
        public IAssembly Assembly { get; }
    }
}
