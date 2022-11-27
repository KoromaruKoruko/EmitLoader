
using System;

namespace EmitLoader.Reflection
{
    internal class ReflectionGenericParameterConstraint : IGenericParameterConstraint
    {
        public ReflectionGenericParameterConstraint(Type constraint, ReflectionGenericParameter Parent)
        {
            this.Parent = Parent;
            this.ConstrainType = this.Context.ResolveType(constraint);
        }

        public IGenericParameter Parent { get; }
        public IType ConstrainType { get; }
        public AssemblyObjectKind Kind => AssemblyObjectKind.GenericParameterConstraint;
        public AssemblyLoader Context => this.Parent.Context;
        public IAssembly Assembly => this.Parent.Assembly;
    }
}
