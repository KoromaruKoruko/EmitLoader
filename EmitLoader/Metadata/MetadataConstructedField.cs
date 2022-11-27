using System.Reflection;

namespace EmitLoader.Metadata
{
    internal class MetadataConstructedField : MetadataFieldBase
    {
        public override MetadataSolver Assembly => this.Base.Assembly;
        public override string Name => this.Base.Name;

        public override MetadataTypeBase DeclaringType { get; }


        public override FieldAttributes Attributes => this.Base.Attributes;

        public override IType FieldType
        {
            get
            {
                if (this._FieldType == null)
                    this._FieldType =
                        this.DeclaringType.IsGeneric && !this.DeclaringType.IsGenericDefinition
                        ? this.Base.Def.DecodeSignature(this.Assembly.SP, this.DeclaringType)
                        : this.Base.FieldType;
                return this._FieldType;
            }
        }
        private IType _FieldType;

        public override MetadataConstant DefaultValue => this.Base.DefaultValue;
        public override MetadataCustomAttributeBase[] CustomAttributes
        {
            get
            {
                if (this._CustomAttributes == null)
                {
                    this._CustomAttributes = new MetadataConstructedCustomAttribute[this.Base.CustomAttributes.Length];
                    for (int x = 0; x < this.Base.CustomAttributes.Length; x++)
                        this._CustomAttributes[x] = ((MetadataCustomAttribute)this.Base.CustomAttributes[x]).Rebase(this.DeclaringType);
                }
                return this._CustomAttributes;
            }
        }
        private MetadataCustomAttributeBase[] _CustomAttributes;

        public MetadataConstructedField(MetadataField Base, MetadataConstructedType declaringType)
        {
            this.Base = Base;
            this.DeclaringType = declaringType;
        }
        private MetadataField Base;
    }
}
