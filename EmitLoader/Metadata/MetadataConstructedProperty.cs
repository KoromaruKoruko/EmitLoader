using System.Reflection;

namespace EmitLoader.Metadata
{
    internal class MetadataConstructedProperty : MetadataPropertyBase
    {
        public override MetadataSolver Assembly => this.Assembly;
        public override string Name => this.Base.Name;

        public override IType PropertyType => this.Getter.ReturnType;


        public override PropertyAttributes Attributes => this.Base.Attributes;

        public override MetadataMethodBase Getter
        {
            get
            {
                if (this._Getter == null)
                    this._Getter = ((MetadataMethod)this.Base.Getter).Rebase((MetadataConstructedType)this.DeclaringType);
                return this._Getter;
            }
        }
        private MetadataMethodBase _Getter;

        public override MetadataMethodBase Setter
        {
            get
            {
                if (this._Setter == null)
                    this._Setter = ((MetadataMethod)this.Base.Getter).Rebase((MetadataConstructedType)this.DeclaringType);
                return this._Setter;
            }
        }
        private MetadataMethodBase _Setter;

        public override MetadataTypeBase DeclaringType { get; }
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

        public MetadataConstructedProperty(MetadataProperty Base, MetadataConstructedType DeclaringType)
        {
            this.Base = Base;
            this.DeclaringType = DeclaringType;
        }

        private MetadataProperty Base;
    }
}
