using System.Collections.Generic;
using System.Reflection.Metadata;

namespace EmitLoader.Metadata
{
    internal class MetadataCustomAttribute : MetadataCustomAttributeBase
    {
        public override MetadataSolver Assembly => (MetadataSolver)this.GenericParent.Assembly;

        public override IMethod Constructor
        {
            get
            {
                if (this._Constructor == null)
                {
                    switch (this.Def.Constructor.Kind)
                    {
                        case HandleKind.MemberReference:
                            this._Constructor = this.Assembly.GetMemberReference((MemberReferenceHandle)this.Def.Constructor, this.GenericParent) as IMethod;
                            break;

                        case HandleKind.MethodDefinition:
                            this._Constructor = this.Assembly.GetMethodDefinition((MethodDefinitionHandle)this.Def.Constructor);
                            break;
                    }
                }
                return this._Constructor;
            }
        }
        private IMethod _Constructor;

        public MetadataCustomAttribute(CustomAttribute Def, IGeneric GenericParent)
        {
            this.Def = Def;
            this.GenericParent = GenericParent;
        }
        internal readonly CustomAttribute Def;
        internal readonly IGeneric GenericParent;

        internal MetadataConstructedCustomAttribute Rebase(IGeneric GenericParent)
        {
            if (this.constructedLookup.TryGetValue(GenericParent, out MetadataConstructedCustomAttribute customAttribute))
                return customAttribute;
            lock (this.constructedLookup)
            {
                if (this.constructedLookup.TryGetValue(GenericParent, out customAttribute))
                    return customAttribute;
                customAttribute = new MetadataConstructedCustomAttribute(this, GenericParent);
                this.constructedLookup.Add(GenericParent, customAttribute);
                return customAttribute;
            }
        }
        private Dictionary<IGeneric, MetadataConstructedCustomAttribute> constructedLookup = new Dictionary<IGeneric, MetadataConstructedCustomAttribute>();
    }
}
