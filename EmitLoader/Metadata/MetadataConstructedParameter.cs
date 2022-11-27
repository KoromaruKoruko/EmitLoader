namespace EmitLoader.Metadata
{
    internal class MetadataConstructedParameter : MetadataParameterBase
    {
        public override MetadataSolver Assembly => this.Base.Assembly;
        public override string Name => this.Base.Name;
        public override MetadataMethodBase Parent { get; }
        public override bool IsOptional => this.Base.IsOptional;

        public override IType ParameterType { get; }

        public override MetadataCustomAttributeBase[] CustomAttributes
        {
            get
            {
                if (this._CustomAttributes == null)
                {
                    this._CustomAttributes = new MetadataConstructedCustomAttribute[this.Base.CustomAttributes.Length];
                    for (int x = 0; x < this.Base.CustomAttributes.Length; x++)
                        this._CustomAttributes[x] = ((MetadataCustomAttribute)this.Base.CustomAttributes[x]).Rebase(this.Parent);
                }
                return this._CustomAttributes;
            }
        }
        private MetadataCustomAttributeBase[] _CustomAttributes;
        public override MetadataConstant DefaultValue => this.Base.DefaultValue;

        public MetadataConstructedParameter(MetadataParameter Base, IType ParameterType, MetadataConstructedMethod Parent)
        {
            this.Base = Base;
            this.ParameterType = ParameterType;
            this.Parent = Parent;
        }
        private MetadataParameter Base;
    }
}
