using System.Reflection;
using System.Text;

namespace EmitLoader.Mixed
{
    internal class MixedConstructedProperty : IProperty
    {
        public MixedConstructedProperty(IProperty Base, MixedConstructedType Parent)
        {
            this.Base = Base;
            this.Parent = Parent;
        }
        internal readonly IProperty Base;
        internal readonly MixedConstructedType Parent;

        public PropertyInfo GetBuiltProperty() => this.Parent.GetBuiltType().GetRuntimeProperty(this.Base.GetBuiltProperty().Name);

        public IType PropertyType
        {
            get
            {
                if (this._PropertyType == null)
                    this._PropertyType = this.Parent.Mappings.MapType(this.Base.PropertyType);
                return this._PropertyType;
            }
        }
        private IType _PropertyType;
        public IMethod Getter
        {
            get
            {
                if (this._Getter == null)
                    this._Getter = new MixedConstructedMethod(this.Base.Getter, this.Parent);
                return this._Getter;
            }
        }
        private IMethod _Getter;
        public IMethod Setter
        {
            get
            {
                if (this._Setter == null)
                    this._Setter = new MixedConstructedMethod(this.Base.Getter, this.Parent);
                return this._Setter;
            }
        }
        private IMethod _Setter;
        public IType DeclaringType => this.Parent;

        public ICustomAttribute[] CustomAttributes => this.Base.CustomAttributes;

        public string Name => this.Base.Name;
        public AssemblyObjectKind Kind => AssemblyObjectKind.Property;
        public AssemblyLoader Context => this.Base.Context;
        public IAssembly Assembly => this.Base.Context.MixedSolver;

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
