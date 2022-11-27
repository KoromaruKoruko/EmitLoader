
using System;
using System.Reflection;
using System.Text;

namespace EmitLoader.Metadata
{
    internal class MetadataConstructedGenericParameterType : MetadataTypeBase, IGenericParameter
    {
        public override AssemblyObjectKind Kind => AssemblyObjectKind.GenericParameter;
        public override MetadataSolver Assembly => this.Base.Assembly;
        public IGeneric Parent { get; }
        public override string Name => this.Base.Name;

        internal override Type BuildType()
        {
            String Name = this.Base.BuildType().Name;
            if (this.Parent is IMethod method)
            {
                foreach (Type t in ((MetadataMethodBase)method).BuildMethod().GetGenericArguments())
                    if (t.Name == Name)
                        return t;
                throw new Exception("Built Method does not Contain this GenericParameter");
            }
            else if (this.Parent is IType type)
            {
                foreach (Type t in ((MetadataTypeBase)type).BuildType().GetGenericArguments())
                    if (t.Name == Name)
                        return t;
                throw new Exception("Built Type does not Contain this GenericParameter");
            }

            return null;
        }

        IGenericParameterConstraint[] IGenericParameter.Constraints => this.Constraints;
        public MetadataGenericParameterConstraint[] Constraints => this.Base.Constraints;
        public override MetadataCustomAttributeBase[] CustomAttributes
        {
            get
            {
                if (this._CustomAttributes == null)
                {
                    this._CustomAttributes = new MetadataConstructedCustomAttribute[this.Base.CustomAttributes.Length];
                    for (int x = 0; x < this.Base.CustomAttributes.Length; x++)
                        this._CustomAttributes[x] = ((MetadataCustomAttribute)this.Base.CustomAttributes[x]).Rebase(this);
                }
                return this._CustomAttributes;
            }
        }
        private MetadataCustomAttributeBase[] _CustomAttributes;

        public MetadataConstructedGenericParameterType(MetadataGenericParameterType Base, IGeneric newParent)
        {
            this.Base = Base;
            this.Parent = newParent;
        }
        private readonly MetadataGenericParameterType Base;


        public GenericParameterAttributes GenericParameterAttributes => this.Base.GenericParameterAttributes;
        internal override TypeAttributes Attributes => throw new NotSupportedException();
        public override MetadataMethodBase StaticConstructor => null;
        public override MetadataNamespace Namespace => null;
        public override MetadataTypeBase DeclaringType => null;
        public override MetadataTypeBase GenericDefinition => null;
        public override bool IsNestedType => false;
        public override bool IsGenericTypeParameter => true;
        public override bool IsGeneric => false;
        public override bool IsGenericDefinition => false;
        public override MetadataTypeBase[] Types => Array.Empty<MetadataTypeBase>();
        public override MetadataFieldBase[] Fields => Array.Empty<MetadataFieldBase>();
        public override MetadataMethodBase[] Methods => Array.Empty<MetadataMethodBase>();
        public override MetadataMethodBase[] Constructors => Array.Empty<MetadataMethodBase>();
        public override MetadataPropertyBase[] Properties => Array.Empty<MetadataPropertyBase>();
        public override MetadataEventBase[] Events => Array.Empty<MetadataEventBase>();
        public override IType[] GenericArguments => Array.Empty<IType>();
        public override MetadataTypeBase ConstructGeneric(IType[] genericArguments) => throw new InvalidOperationException();
        public override void GetFullyQualifiedName(StringBuilder sb) => sb.Append(this.Name);
        public override bool IsStatic => false;
        public override IType BaseType => null;
        public override IType[] Interfaces => Array.Empty<IType>();

    }
}
