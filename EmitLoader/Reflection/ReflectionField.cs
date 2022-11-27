using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace EmitLoader.Reflection
{
    internal class ReflectionField : IField
    {
        public ReflectionField(FieldInfo field, ReflectionType declaringType)
        {
            this.field = field;
            this.declaringType = declaringType;
        }
        internal readonly FieldInfo field;
        internal readonly ReflectionType declaringType;

        public FieldInfo GetBuiltField() => this.field;

        public IType FieldType => this.Context.ResolveType(this.field.FieldType);
        public IType DeclaringType => declaringType;

        public ICustomAttribute[] CustomAttributes
        {
            get
            {
                if (this._CustomAttributes == null)
                {
                    IList<CustomAttributeData> attributes = this.field.GetCustomAttributesData();
                    this._CustomAttributes = new ReflectionCustomAttribute[attributes.Count];
                    for (int x = 0; x < attributes.Count; x++)
                        this._CustomAttributes[x] = new ReflectionCustomAttribute(attributes[x], this.declaringType.assembly);
                }
                return this._CustomAttributes;
            }
        }
        private ICustomAttribute[] _CustomAttributes;

        // reflection doesn't support us pulling the default value (we also don't really need it so...)
        public IConstant DefaultValue => null;

        public string Name => this.field.Name;
        public AssemblyObjectKind Kind => AssemblyObjectKind.Field;
        public AssemblyLoader Context => this.declaringType.assembly.Context;
        public IAssembly Assembly => this.declaringType.assembly;

        public string GetFullyQualifiedName()
        {
            if (this._FullyQualifiedName == null)
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(this.DeclaringType.GetFullyQualifiedName());
                sb.Append('.');
                sb.Append(this.Name);
                this._FullyQualifiedName = sb.ToString();
            }
            return this._FullyQualifiedName;
        }
        private string _FullyQualifiedName;
    }
}
