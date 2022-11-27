namespace EmitLoader.Mixed
{
    internal class MixedConstructedParameter : IParameter
    {
        public MixedConstructedParameter(IParameter Base, MixedConstructedMethod Parent)
        {
            this.Base = Base;
            this.Parent = Parent;
        }
        private readonly IParameter Base;

        public string Name => this.Base.Name;
        IMethod IParameter.Parent => this.Parent;
        public MixedConstructedMethod Parent { get; }
        public IType ParameterType
        {
            get
            {
                if (this._ParameterType == null)
                    this._ParameterType = this.Parent.Mappings.MapType(this.Base.ParameterType);
                return this._ParameterType;
            }
        }
        private IType _ParameterType;
        public bool IsOptional => this.Base.IsOptional;
        public ICustomAttribute[] CustomAttributes => this.Base.CustomAttributes;
        public AssemblyObjectKind Kind => AssemblyObjectKind.Parameter;
        public AssemblyLoader Context => this.Base.Context;
        public IAssembly Assembly => this.Base.Context.MixedSolver;
    }
}
