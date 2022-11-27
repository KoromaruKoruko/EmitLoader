using System.Reflection;
using System.Text;

namespace EmitLoader.Mixed
{
    internal class MixedConstructedField : IField
    {
        public MixedConstructedField(IField Base, MixedConstructedType Parent)
        {
            this.Base = Base;
            this.Parent = Parent;
        }
        internal readonly IField Base;
        internal readonly MixedConstructedType Parent;

        public FieldInfo GetBuiltField() => this.Parent.GetBuiltType().GetRuntimeField(this.Base.GetBuiltField().Name);
        

        public IType FieldType
        {
            get
            {
                if (this._FieldType == null)
                    this._FieldType = this.Parent.Mappings.MapType(this.Base.FieldType);
                return this._FieldType;
            }
        }
        private IType _FieldType;
        public IType DeclaringType => this.Parent;
        public ICustomAttribute[] CustomAttributes => this.Base.CustomAttributes;
        public string Name => this.Base.Name;
        public AssemblyObjectKind Kind => AssemblyObjectKind.Field;
        public AssemblyLoader Context => this.Base.Context;
        public IAssembly Assembly => this.Base.Context.MixedSolver;
        public IConstant DefaultValue => this.Base.DefaultValue;

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
