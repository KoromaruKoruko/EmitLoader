using System.Collections.Generic;
using System.Reflection;

namespace EmitLoader.Reflection
{
    internal class ReflectionParameter : IParameter
    {
        public ReflectionParameter(ParameterInfo parameter, ReflectionMethod parent)
        {
            this.parameter = parameter;
            this.parent = parent;
        }
        internal ParameterInfo parameter;
        internal ReflectionMethod parent;

        public string Name => this.parameter.Name;
        public IMethod Parent => this.parent;

        public IType ParameterType
        {
            get
            {
                if (this._ParameterType == null)
                    this._ParameterType = this.Context.ResolveType(this.parameter.ParameterType);
                return this._ParameterType;
            }
        }
        private IType _ParameterType;
        public bool IsOptional => this.parameter.IsOptional;

        public ICustomAttribute[] CustomAttributes
        {
            get
            {
                if (this._CustomAttributes == null)
                {
                    IList<CustomAttributeData> attributes = this.parameter.GetCustomAttributesData();
                    this._CustomAttributes = new ReflectionCustomAttribute[attributes.Count];
                    for (int x = 0; x < attributes.Count; x++)
                        this._CustomAttributes[x] = new ReflectionCustomAttribute(attributes[x], this.parent.declaringType.assembly);
                }
                return this._CustomAttributes;
            }
        }
        private ICustomAttribute[] _CustomAttributes;

        public AssemblyObjectKind Kind => AssemblyObjectKind.Parameter;
        public AssemblyLoader Context => this.parent.Context;
        public IAssembly Assembly => this.parent.Assembly;
    }
}
