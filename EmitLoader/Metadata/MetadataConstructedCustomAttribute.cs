using System.Reflection.Metadata;

namespace EmitLoader.Metadata
{
    internal class MetadataConstructedCustomAttribute : MetadataCustomAttributeBase
    {
        public override MetadataSolver Assembly => this.Base.Assembly;

        public override IMethod Constructor
        {
            get
            {
                if (this._Constructor == null)
                {
                    switch (this.Base.Def.Constructor.Kind)
                    {
                        case HandleKind.MemberReference:
                            this._Constructor = this.Assembly.GetMemberReference((MemberReferenceHandle)this.Base.Def.Constructor, this.GenericParent) as IMethod;
                            break;

                        case HandleKind.MethodDefinition:
                            this._Constructor = this.Base.Constructor;
                            break;
                    }
                }
                return this._Constructor;
            }
        }
        private IMethod _Constructor;

        public MetadataConstructedCustomAttribute(MetadataCustomAttribute Base, IGeneric GenericParent)
        {
            this.Base = Base;
            this.GenericParent = GenericParent;
        }
        internal readonly MetadataCustomAttribute Base;
        internal readonly IGeneric GenericParent;
    }
}
