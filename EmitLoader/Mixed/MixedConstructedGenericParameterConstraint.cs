namespace EmitLoader.Mixed
{
    internal class MixedConstructedGenericParameterConstraint : IGenericParameterConstraint
    {
        public MixedConstructedGenericParameterConstraint(IGenericParameterConstraint Base, MixedConstructedGenericParameter Parent)
        {
            this.Base = Base;
            this.Parent = Parent;
        }
        private readonly IGenericParameterConstraint Base;
        private readonly MixedConstructedGenericParameter Parent;

        IGenericParameter IGenericParameterConstraint.Parent => this.Parent;
        public IType ConstrainType
        {
            get
            {
                if (this._ConstrainType == null)
                    this._ConstrainType = this.Parent.Mappings.MapType(this.Base.ConstrainType);
                return this._ConstrainType;
            }
        }
        private IType _ConstrainType;
        public AssemblyObjectKind Kind => AssemblyObjectKind.GenericParameterConstraint;
        public AssemblyLoader Context => this.Base.Context;
        public IAssembly Assembly => this.Base.Context.MixedSolver;
    }
}
