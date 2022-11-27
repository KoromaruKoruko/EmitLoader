using System.Reflection;

namespace EmitLoader.Metadata
{
    internal class MetadataConstructedEvent : MetadataEventBase
    {
        public override MetadataSolver Assembly => this.Base.Assembly;

        public override string Name => this.Base.Name;

        public override IType EventType => this.Adder.Parameters[0].ParameterType;

        public override EventAttributes Attributes => this.Base.Attributes;

        public override MetadataMethodBase Adder
        {
            get
            {
                if (this._Adder == null)
                    this._Adder = ((MetadataMethod)this.Base.Adder).Rebase((MetadataConstructedType)this.DeclaringType);
                return this._Adder;
            }
        }
        private MetadataMethodBase _Adder;

        public override MetadataMethodBase Remover
        {
            get
            {
                if (this._Remover == null)
                    this._Remover = ((MetadataMethod)this.Base.Remover).Rebase((MetadataConstructedType)this.DeclaringType);
                return this._Remover;
            }
        }
        private MetadataMethodBase _Remover;

        public override MetadataMethodBase Raiser
        {
            get
            {
                if (this._Raiser == null)
                    this._Raiser = ((MetadataMethod)this.Base.Raiser).Rebase((MetadataConstructedType)this.DeclaringType);
                return this._Raiser;
            }
        }
        private MetadataMethodBase _Raiser;

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

        public MetadataConstructedEvent(MetadataEvent Base, MetadataConstructedType DeclaringType)
        {
            this.Base = Base;
            this.DeclaringType = DeclaringType;
        }
        private MetadataEvent Base;
    }
}
