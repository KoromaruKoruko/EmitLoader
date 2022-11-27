
using System;
using System.Collections.Generic;

namespace EmitLoader.Mixed
{
    internal class MixedTypeGenericMappings
    {
        private MixedTypeGenericMappings Parent;

        private IGenericParameter[] Parameters;
        private IType[] Arguments;

        public MixedTypeGenericMappings(IGenericParameter[] Parameters, IType[] Arguments)
        {
            if (Parameters.Length != Arguments.Length)
                throw new ArgumentOutOfRangeException("Parameters, Arguments", "Parameters.Length != Arguments.Length");

            for (int x = 0; x < Parameters.Length; x++)
            {
                if (!Parameters[x].IsGenericTypeParameter)
                    throw new ArgumentException("Parameters[x].IsGenericTypeParameter == false", nameof(Parameters));
                if (Arguments[x].IsGenericTypeParameter)
                    throw new ArgumentException("Arguments[x].IsGenericTypeParameter == true", nameof(Arguments));
            }

            this.Parameters = Parameters;
            this.Arguments = Arguments;
            this.Parent = null;
        }
        public MixedTypeGenericMappings(IGenericParameter[] Parameters, IType[] Arguments, MixedTypeGenericMappings ParentMappings) : this(Parameters, Arguments)
        {
            this.Parent = ParentMappings;
        }

        public IType GetArgument(Int32 index)
        {
            Stack<MixedTypeGenericMappings> stack = new Stack<MixedTypeGenericMappings>();
            MixedTypeGenericMappings current = this;
            do
            {
                stack.Push(current);
                current = current.Parent;
            }
            while (current != null);
            current = stack.Pop();

            while (current.Arguments.Length < index)
            {
                index -= current.Arguments.Length;
                current = stack.Pop();
            }

            return current.Arguments[index];
        }

        public IType MapType(IType Type) => this.MapTypes(new IType[] { Type })[0];
        public IType[] MapTypes(in IType[] Types)
        {
            Stack<MixedTypeGenericMappings> stack = new Stack<MixedTypeGenericMappings>();
            MixedTypeGenericMappings current = this;
            do
            {
                stack.Push(current);
                current = current.Parent;
            }
            while (current != null);
            current = stack.Pop();

            do
            {
                for (int x = 0; x < current.Parameters.Length; x++)
                {
                    IType cParam = current.Parameters[x];
                    IType cArg = current.Arguments[x];
                    for (int y = 0; y < Types.Length; y++)
                    {
                        if (Types[y] == cParam)
                            Types[y] = cArg;
                    }
                }

                current = stack.Pop();
            }
            while (current != null);

            return Types;
        }
    }
}
