
using System;
using System.Reflection;
using System.Text;

namespace EmitLoader.Metadata
{
    internal abstract class MetadataPropertyBase : IProperty
    {
        public AssemblyObjectKind Kind => AssemblyObjectKind.Property;
        public AssemblyLoader Context => this.Assembly.Context;
        IAssembly IAssemblySolverObject.Assembly => this.Assembly;
        public abstract MetadataSolver Assembly { get; }

        public abstract PropertyAttributes Attributes { get; }
        public PropertyInfo GetBuiltProperty() =>
            this.Assembly.Loader == null
                ? throw new InvalidOperationException("Metadata Assembly not permited to built!")
                : this.DeclaringType.BuildType().GetRuntimeProperty(this.Name);


        public abstract string Name { get; }

        public abstract IType PropertyType { get; }


        IMethod IProperty.Getter => this.Getter;
        public abstract MetadataMethodBase Getter { get; }

        IMethod IProperty.Setter => this.Setter;
        public abstract MetadataMethodBase Setter { get; }

        IType IMember.DeclaringType => this.DeclaringType;
        public abstract MetadataTypeBase DeclaringType { get; }

        ICustomAttribute[] IMember.CustomAttributes => this.CustomAttributes;
        public abstract MetadataCustomAttributeBase[] CustomAttributes { get; }

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
        }
    }
}
