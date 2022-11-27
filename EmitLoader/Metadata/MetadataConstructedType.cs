
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Metadata;

namespace EmitLoader.Metadata
{
    internal class MetadataConstructedType : MetadataTypeBase
    {
        public override MetadataSolver Assembly => this.Base.Assembly;
        public override string Name => this.Base.Name;
        public override MetadataNamespace Namespace => this.Base.Namespace;
        public override MetadataTypeBase DeclaringType { get; }
        public override MetadataTypeBase GenericDefinition { get; }
        public override bool IsNestedType => this.DeclaringType != null;
        public override bool IsGenericTypeParameter => false;
        public override bool IsGeneric { get; }
        public override bool IsGenericDefinition { get; }


        internal override TypeAttributes Attributes => this.Base.Attributes;
        internal override Type BuildType()
        {
            Type[] types = new Type[this.GenericArguments.Length];
            for (int i = 0; i < this.GenericArguments.Length; i++)
                types[i] = this.GenericArguments[i].GetBuiltType();
            return this.Base.BuildType().MakeGenericType(types);
        }

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

        public override MetadataMethodBase StaticConstructor
        {
            get
            {
                if (this._StaticConstructor == null)
                    this._StaticConstructor = ((MetadataMethod)this.Base.StaticConstructor).Rebase(this);
                return this._StaticConstructor;
            }
        }
        private MetadataMethodBase _StaticConstructor;
        public override MetadataTypeBase[] Types
        {
            get
            {
                if (this._Types == null)
                {
                    this._Types = new MetadataTypeBase[this.Base.Types.Length];
                    for (int x = 0; x < this.Base.Types.Length; x++)
                        this._Types[x] = ((MetadataType)this.Base.Types[x]).Rebase(this);
                }
                return this._Types;
            }
        }
        private MetadataTypeBase[] _Types;
        public override MetadataFieldBase[] Fields
        {
            get
            {
                if (this._Fields == null)
                {
                    this._Fields = new MetadataFieldBase[this.Base.Fields.Length];
                    for (int x = 0; x < this.Base.Fields.Length; x++)
                        this._Fields[x] = ((MetadataField)this.Base.Fields[x]).Rebase(this);
                }
                return this._Fields;
            }
        }
        private MetadataFieldBase[] _Fields;
        public override MetadataMethodBase[] Methods
        {
            get
            {
                if (this._Methods == null)
                {
                    this._Methods = new MetadataMethodBase[this.Base.Methods.Length];
                    for (int x = 0; x < this.Base.Methods.Length; x++)
                        this._Methods[x] = ((MetadataMethod)this.Base.Methods[x]).Rebase(this);
                }
                return this._Methods;
            }
        }
        private MetadataMethodBase[] _Methods;
        public override MetadataMethodBase[] Constructors
        {
            get
            {
                if (this._Constructors == null)
                {
                    this._Constructors = new MetadataMethodBase[this.Base.Constructors.Length];
                    for (int x = 0; x < this.Base.Constructors.Length; x++)
                        this._Constructors[x] = ((MetadataMethod)this.Base.Constructors[x]).Rebase(this);
                }
                return this._Constructors;
            }
        }
        private MetadataMethodBase[] _Constructors;
        public override MetadataPropertyBase[] Properties
        {
            get
            {
                if (this._Properties == null)
                {
                    this._Properties = new MetadataPropertyBase[this.Base.Properties.Length];
                    for (int x = 0; x < this.Base.Properties.Length; x++)
                        this._Properties[x] = ((MetadataProperty)this.Base.Properties[x]).Rebase(this);
                }
                return this._Properties;
            }
        }
        private MetadataPropertyBase[] _Properties;
        public override MetadataEventBase[] Events
        {
            get
            {
                if (this._Events == null)
                {
                    this._Events = new MetadataEventBase[this.Base.Events.Length];
                    for (int x = 0; x < this.Base.Events.Length; x++)
                        this._Events[x] = ((MetadataEvent)this.Base.Events[x]).Rebase(this);
                }
                return this._Events;
            }
        }
        private MetadataEventBase[] _Events;

        public override IType[] GenericArguments
        {
            get
            {
                if (this._GenericArguments == null)
                {
                    if (this.IsGenericDefinition)
                    {
                        this._GenericArguments = new MetadataConstructedGenericParameterType[this.Base.GenericArguments.Length];
                        for (int x = 0; x < this.Base.GenericArguments.Length; x++)
                            this._GenericArguments[x] = ((MetadataGenericParameterType)this.Base.GenericArguments[x]).Rebase(this);
                    }
                }
                return this._GenericArguments;
            }
        }
        private IType[] _GenericArguments;

        public override MetadataTypeBase ConstructGeneric(IType[] genericArguments)
        {
            if (this.IsGenericDefinition)
            {
                if (this.DeclaringType?.IsGenericDefinition ?? false)
                    throw new InvalidOperationException("Must Construct ContainingType First!");

                if (genericArguments.Length != this.GenericArguments.Length)
                    throw new ArgumentOutOfRangeException("Incorrect number of genericArguments");

                if (this.constructedTypes.TryGetValue(genericArguments, out MetadataConstructedType type))
                    return type;

                lock (this.constructedTypes)
                {
                    if (this.constructedTypes.TryGetValue(genericArguments, out type))
                        return type;

                    if (!AssemblyLoaderHelpers.ValidateGenericParameterConstraints(genericArguments, (MetadataGenericParameterType[])this.GenericArguments))
                        throw new ArgumentException("Generic Constrain not Met", nameof(genericArguments));

                    type = new MetadataConstructedType(this, genericArguments);
                    this.constructedTypes.Add(genericArguments, type);
                    return type;
                }
            }
            else
                throw new InvalidOperationException("Can't construct generic from non GenericDefinition Type");
        }
        private Dictionary<IType[], MetadataConstructedType> constructedTypes;

        public override bool IsStatic => this.Base.IsStatic;
        public override IType BaseType
        {
            get
            {
                if (this._BaseType != null && !this.IsStatic && !this.Base.IsInterface)
                {
                    switch (this.Base.Def.BaseType.Kind)
                    {
                        case HandleKind.TypeSpecification:
                            this._BaseType = this.Assembly.GetTypeSpecification((TypeSpecificationHandle)this.Base.Def.BaseType, this);
                            break;
                        case HandleKind.TypeReference:
                            this._BaseType = this.Base.BaseType;
                            break;
                        case HandleKind.TypeDefinition:
                            this._BaseType = this.Base.BaseType;
                            break;

                        default:
                            throw new Exception($"Unexpected BaseType Kind {this.Base.Def.BaseType}");
                    }
                }
                return this._BaseType;
            }
        }
        private IType _BaseType;
        public override IType[] Interfaces
        {
            get
            {
                if (this._Interfaces == null)
                {
                    InterfaceImplementationHandleCollection collection = this.Base.Def.GetInterfaceImplementations();
                    this._Interfaces = new IType[collection.Count];
                    int x = 0;
                    foreach (InterfaceImplementationHandle handle in collection)
                    {
                        InterfaceImplementation Impl = this.Assembly.MD.GetInterfaceImplementation(handle);
                        switch (Impl.Interface.Kind)
                        {
                            case HandleKind.TypeDefinition:
                                this._Interfaces[x] = this.Base.Interfaces[x];
                                break;
                            case HandleKind.TypeReference:
                                this._Interfaces[x] = this.Base.Interfaces[x];
                                break;
                            case HandleKind.TypeSpecification:
                                this._Interfaces[x] = this.Assembly.GetTypeSpecification((TypeSpecificationHandle)Impl.Interface, this);
                                break;

                            default:
                                throw new Exception($"Unexpected InterfaceImplementation Interface Kind {Impl.Interface.Kind}");
                        }
                        x++;
                    }
                }
                return this._Interfaces;
            }
        }
        private IType[] _Interfaces;

        public MetadataConstructedType(MetadataConstructedType definitionType, IType[] genericArguments)
        {
            this.Base = definitionType.Base;
            this.GenericDefinition = definitionType;
            this.IsGeneric = true;
            this.IsGenericDefinition = false;
            this._GenericArguments = genericArguments;
            this.DeclaringType = definitionType.DeclaringType;

            this.constructedTypes = null;
        }
        public MetadataConstructedType(MetadataType definitionType, IType[] genericArguments)
        {
            this.Base = definitionType;
            this.GenericDefinition = definitionType;
            this.IsGeneric = true;
            this.IsGenericDefinition = false;
            this._GenericArguments = genericArguments;
            this.DeclaringType = definitionType.DeclaringType;

            this.constructedTypes = null;
        }
        public MetadataConstructedType(MetadataType Base, MetadataTypeBase newDeclaringType)
        {
            this.Base = Base;
            this.GenericDefinition = null;
            this.IsGeneric = this.Base.IsGeneric;
            this.IsGenericDefinition = this.Base.IsGenericDefinition;
            this._GenericArguments = null;
            this.DeclaringType = newDeclaringType;

            if (this.IsGenericDefinition)
                this.constructedTypes = new Dictionary<IType[], MetadataConstructedType>();
        }
        private MetadataType Base;
    }
}
