
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Metadata;
using System.Text;

namespace EmitLoader.Metadata
{
    internal class MetadataGenericParameterType : MetadataTypeBase, IGenericParameter
    {
        public override AssemblyObjectKind Kind => AssemblyObjectKind.GenericParameter;
        public override MetadataSolver Assembly { get; }
        public IGeneric Parent { get; }
        public override string Name
        {
            get
            {
                if (this._Name == null)
                    this._Name = this.Assembly.MD.GetString(this.Def.Name);

                return this._Name;
            }
        }
        private string _Name;

        internal override Type BuildType()
        {
            if (this.Parent is IMethod method)
            {
                foreach (Type t in ((MetadataMethodBase)method).BuildMethod().GetGenericArguments())
                    if (t.Name == this.Name)
                        return t;
                throw new Exception("Built Method does not Contain this GenericParameter");
            }
            else if (this.Parent is IType type)
            {
                foreach (Type t in ((MetadataTypeBase)type).BuildType().GetGenericArguments())
                    if (t.Name == this.Name)
                        return t;
                throw new Exception("Built Type does not Contain this GenericParameter");
            }

            return null;
        }

        public GenericParameterAttributes GenericParameterAttributes => this.Def.Attributes;

        IGenericParameterConstraint[] IGenericParameter.Constraints => this.Constraints;
        public MetadataGenericParameterConstraint[] Constraints
        {
            get
            {
                if (this._Constraints == null)
                {
                    GenericParameterConstraintHandleCollection collection = this.Def.GetConstraints();
                    this._Constraints = new MetadataGenericParameterConstraint[collection.Count];
                    int x = 0;
                    foreach (GenericParameterConstraintHandle handle in collection)
                        this._Constraints[x++] = new MetadataGenericParameterConstraint(this.Assembly.MD.GetGenericParameterConstraint(handle), this);
                }
                return this._Constraints;
            }
        }
        private MetadataGenericParameterConstraint[] _Constraints;

        public override MetadataCustomAttributeBase[] CustomAttributes
        {
            get
            {
                if (this._CustomAttributes == null)
                {
                    CustomAttributeHandleCollection collection = this.Def.GetCustomAttributes();
                    this._CustomAttributes = new MetadataCustomAttribute[collection.Count];
                    int x = 0;
                    foreach (CustomAttributeHandle handle in collection)
                        this.CustomAttributes[x++] = new MetadataCustomAttribute(this.Assembly.MD.GetCustomAttribute(handle), this.Parent);
                }
                return this._CustomAttributes;
            }
        }
        private MetadataCustomAttributeBase[] _CustomAttributes;

        public MetadataGenericParameterType(GenericParameter Def, MetadataType Parent)
        {
            this.Def = Def;
            this.Parent = Parent;
            this.Assembly = Parent.Assembly;
        }
        public MetadataGenericParameterType(GenericParameter Def, MetadataMethod Parent)
        {
            this.Def = Def;
            this.Parent = Parent;
            this.Assembly = Assembly;
        }
        private readonly GenericParameter Def;


        internal MetadataConstructedGenericParameterType Rebase(IGeneric newParent)
        {
            if (this.rebasedGenericParameters.TryGetValue(newParent, out MetadataConstructedGenericParameterType type))
                return type;

            lock (this.rebasedGenericParameters)
            {
                if (this.rebasedGenericParameters.TryGetValue(newParent, out type))
                    return type;

                type = new MetadataConstructedGenericParameterType(this, newParent);
                this.rebasedGenericParameters.Add(newParent, type);
                return type;
            }
        }
        private Dictionary<IGeneric, MetadataConstructedGenericParameterType> rebasedGenericParameters = new Dictionary<IGeneric, MetadataConstructedGenericParameterType>(InterfaceComparer.Instance);

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
