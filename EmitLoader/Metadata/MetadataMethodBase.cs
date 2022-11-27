
using System;
using System.Reflection;
using System.Text;

namespace EmitLoader.Metadata
{
    internal abstract class MetadataMethodBase : IMethod
    {
        public AssemblyObjectKind Kind => AssemblyObjectKind.Method;
        IAssembly IAssemblySolverObject.Assembly => this.Assembly;
        public AssemblyLoader Context => this.Assembly.Context;
        public abstract MetadataSolver Assembly { get; }

        public abstract MethodAttributes Attributes { get; }
        MethodBase IMethod.GetBuiltMethod() =>
            this.Assembly.Loader == null
                ? throw new InvalidOperationException("Metadata Assembly not permited to build!")
                : this.BuildMethod();
        internal abstract MethodBase BuildMethod();


        public abstract IType ReturnType { get; }

        public abstract MetadataParameterBase[] Parameters { get; }
        IParameter[] IMethod.Parameters => this.Parameters;

        public abstract MetadataTypeBase DeclaringType { get; }
        IType IMember.DeclaringType => this.DeclaringType;

        public abstract string Name { get; }
        public abstract bool IsGeneric { get; }
        public abstract bool IsGenericDefinition { get; }
        public abstract IType[] GenericArguments { get; }


        ICustomAttribute[] IMember.CustomAttributes => this.CustomAttributes;
        public abstract MetadataCustomAttributeBase[] CustomAttributes { get; }

        // NULLABLE
        public abstract MetadataMethodBase GenericDefinition { get; }
        IMethod IMethod.GenericDefinition => this.GenericDefinition;
        IGeneric IGeneric.GenericDefinition => this.GenericDefinition;

        IGeneric IGeneric.ConstructGeneric(IType[] genericArguments) => this.ConstructGeneric(genericArguments);
        IMethod IMethod.ConstructGeneric(IType[] genericArguments) => this.ConstructGeneric(genericArguments);
        public abstract MetadataMethodBase ConstructGeneric(IType[] genericArguments);


        public string GetFullyQualifiedName()
        {
            StringBuilder sb = new StringBuilder();
            GetFullyQualifiedName(sb);
            return sb.ToString();
        }
        public void GetFullyQualifiedName(StringBuilder sb)
        {
            this.DeclaringType.GetFullyQualifiedName(sb);
            sb.Append('.');
            sb.Append(this.Name);

            if (this.IsGeneric)
            {
                sb.Append('[');
                if (this.IsGenericDefinition)
                {
                    sb.Append(this.GenericArguments[0].Name);
                    for (int x = 1; x < this.GenericArguments.Length; x++)
                    {
                        sb.Append(',');
                        sb.Append(this.GenericArguments[x].Name);
                    }
                }
                else
                {
                    sb.Append(this.GenericArguments[0].GetFullyQualifiedName());
                    for (int x = 1; x < this.GenericArguments.Length; x++)
                    {
                        sb.Append(',');
                        sb.Append(this.GenericArguments[x].GetFullyQualifiedName());
                    }
                }
                sb.Append(']');
            }

            if (this.Parameters.Length > 0)
            {
                sb.Append('(');
                sb.Append(this.Parameters[0].ParameterType.GetFullyQualifiedName());
                for (int x = 1; x < this.Parameters.Length; x++)
                {
                    sb.Append(',');
                    sb.Append(this.Parameters[x].ParameterType.GetFullyQualifiedName());
                }
                sb.Append(')');
            }
            else
                sb.Append("()");
        }
    }
}
