
using System;
using System.Reflection;
using System.Text;

namespace EmitLoader.Metadata
{
    internal abstract class MetadataFieldBase : IField
    {
        public AssemblyObjectKind Kind => AssemblyObjectKind.Field;
        public AssemblyLoader Context => this.Assembly.Context;
        IAssembly IAssemblySolverObject.Assembly => this.Assembly;
        public abstract MetadataSolver Assembly { get; }

        public abstract FieldAttributes Attributes { get; }
        public FieldInfo GetBuiltField() =>
            this.Assembly.Loader == null
                ? throw new InvalidOperationException("Metadata Assembly not permited to build!")
                : this.DeclaringType.BuildType().GetRuntimeField(this.Name);


        public abstract string Name { get; }

        IType IMember.DeclaringType => this.DeclaringType;
        public abstract MetadataTypeBase DeclaringType { get; }

        public abstract IType FieldType { get; }


        IConstant IField.DefaultValue => this.DefaultValue;
        public abstract MetadataConstant DefaultValue { get; }

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
