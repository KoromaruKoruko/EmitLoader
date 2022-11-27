using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace EmitLoader.Reflection
{
    internal class ReflectionProperty : IProperty
    {
        public ReflectionProperty(PropertyInfo property, ReflectionType declaringType)
        {
            this.property = property;
            this.declaringType = declaringType;
        }
        internal readonly PropertyInfo property;
        internal readonly ReflectionType declaringType;

        public PropertyInfo GetBuiltProperty() => this.property;
        public IType PropertyType
        {
            get
            {
                if (this._PropertyType == null)
                    this._PropertyType = this.Context.ResolveType(this.property.PropertyType);
                return this._PropertyType;
            }
        }
        private IType _PropertyType;
        public IMethod Getter
        {
            get
            {
                if (this._Getter == null)
                {
                    MethodInfo getter = this.property.GetMethod;
                    if (getter != null)
                        this._Getter = new ReflectionMethod(getter, this.declaringType, null);
                }
                return this._Getter;
            }
        }
        private IMethod _Getter;
        public IMethod Setter
        {
            get
            {
                if (this._Setter == null)
                {
                    MethodInfo setter = this.property.SetMethod;
                    if (setter != null)
                        this._Setter = new ReflectionMethod(setter, this.declaringType, null);
                }
                return this._Setter;
            }
        }
        private IMethod _Setter;
        public IType DeclaringType => this.declaringType;
        public ICustomAttribute[] CustomAttributes
        {
            get
            {
                if (this._CustomAttributes == null)
                {
                    IList<CustomAttributeData> attributes = this.property.GetCustomAttributesData();
                    this._CustomAttributes = new ReflectionCustomAttribute[attributes.Count];
                    for (int x = 0; x < attributes.Count; x++)
                        this._CustomAttributes[x] = new ReflectionCustomAttribute(attributes[x], this.declaringType.assembly);
                }
                return this._CustomAttributes;
            }
        }
        private ICustomAttribute[] _CustomAttributes;
        public string Name => this.property.Name;
        public AssemblyObjectKind Kind => AssemblyObjectKind.Property;
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
