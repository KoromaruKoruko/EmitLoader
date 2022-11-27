using System.Reflection;

namespace EmitLoader.Reflection
{
    internal class ReflectionCustomAttribute : ICustomAttribute
    {
        public ReflectionCustomAttribute(CustomAttributeData attribute, ReflectionSolver assembly)
        {
            this.attribute = attribute;
            this.assembly = assembly;
        }
        private readonly ReflectionSolver assembly;
        private readonly CustomAttributeData attribute;

        public IType AttributeType
        {
            get
            {
                if (this._AttributeType == null)
                    this._AttributeType = this.Context.ResolveType(this.attribute.AttributeType);
                return this._AttributeType;
            }
        }
        private IType _AttributeType;
        public AssemblyObjectKind Kind => AssemblyObjectKind.CustomAttribute;
        public AssemblyLoader Context => this.assembly.Context;
        public IAssembly Assembly => this.assembly;
    }
}
