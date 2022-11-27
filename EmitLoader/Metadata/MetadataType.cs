
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection;
using System.Reflection.Metadata;

namespace EmitLoader.Metadata
{
    internal class MetadataType : MetadataTypeBase
    {
        public override MetadataSolver Assembly { get; }
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

        public override MetadataNamespace Namespace
        {
            get
            {
                if (this._Namespace == null)
                    this._Namespace = this.Assembly.GetNamespace(this.Def.NamespaceDefinition);
                return this._Namespace;
            }
        }
        private MetadataNamespace _Namespace;

        internal override TypeAttributes Attributes => this.Def.Attributes;

        internal override Type BuildType()
        {
            if (this._BuiltType == null)
                this._BuiltType = this.Assembly.Loader.GetTypeBuilder(this);
            return this._BuiltType;
        }
        internal Type _BuiltType;

        // NULLABLE
        public override MetadataTypeBase DeclaringType
        {
            get
            {
                if (this._DeclaringType == null)
                    this._DeclaringType = this.Assembly.GetTypeDefinition(this.Def.GetDeclaringType());
                return this._DeclaringType;
            }
        }
        private MetadataTypeBase _DeclaringType;

        // NULLABLE
        public override MetadataTypeBase GenericDefinition => null;

        public override bool IsNestedType => this.Def.IsNested;
        public override bool IsGenericTypeParameter => false;
        public override bool IsGeneric => this.Def.GetGenericParameters().Count > 0;
        public override bool IsGenericDefinition => this.IsGeneric;

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
                        this.CustomAttributes[x++] = new MetadataCustomAttribute(this.Assembly.MD.GetCustomAttribute(handle), this);
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
                    this.LoadMethods();
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
                    ImmutableArray<TypeDefinitionHandle> collection = this.Def.GetNestedTypes();
                    this._Types = new MetadataType[collection.Length];
                    for (int x = 0; x < collection.Length; x++)
                        this._Types[x] = this.Assembly.GetTypeDefinition(collection[x]);
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
                    FieldDefinitionHandleCollection collection = this.Def.GetFields();
                    this._Fields = new MetadataField[collection.Count];
                    int x = 0;
                    foreach (FieldDefinitionHandle handle in collection)
                        this._Fields[x++] = this.Assembly.GetFieldDefinition(handle, this);
                }
                return this._Fields;
            }
        }
        private MetadataField[] _Fields;
        public override MetadataMethodBase[] Methods
        {
            get
            {
                if (this._Methods == null)
                    LoadMethods();
                return this._Methods;
            }
        }
        private MetadataMethod[] _Methods;
        public override MetadataMethodBase[] Constructors
        {
            get
            {
                if (this._Constructors == null)
                    LoadMethods();
                return this._Constructors;
            }
        }
        private MetadataMethod[] _Constructors;
        public override MetadataPropertyBase[] Properties
        {
            get
            {
                if (this._Properties == null)
                {
                    PropertyDefinitionHandleCollection collection = this.Def.GetProperties();
                    this._Properties = new MetadataProperty[collection.Count];
                    int x = 0;
                    foreach (PropertyDefinitionHandle handle in collection)
                        this._Properties[x++] = new MetadataProperty(handle, this);
                }
                return this._Properties;
            }
        }
        private MetadataProperty[] _Properties;
        public override MetadataEventBase[] Events
        {
            get
            {
                if (this._Events == null)
                {
                    EventDefinitionHandleCollection collection = this.Def.GetEvents();
                    this._Events = new MetadataEvent[collection.Count];
                    int x = 0;
                    foreach (EventDefinitionHandle handle in collection)
                        this._Events[x++] = new MetadataEvent(handle, this);
                }
                return this._Events;
            }
        }
        private MetadataEvent[] _Events;

        public override IType[] GenericArguments
        {
            get
            {
                if (this._GenericArguments == null)
                    if (this.IsGeneric)
                    {
                        GenericParameterHandleCollection collection = this.Def.GetGenericParameters();
                        this._GenericArguments = new MetadataGenericParameterType[collection.Count];
                        foreach (GenericParameterHandle handle in collection)
                        {
                            GenericParameter genericParameter = this.Assembly.MD.GetGenericParameter(handle);
                            this._GenericArguments[genericParameter.Index] = new MetadataGenericParameterType(genericParameter, this);
                        }
                    }
                    else
                        this._GenericArguments = Array.Empty<MetadataGenericParameterType>();

                return this._GenericArguments;

            }
        }
        private MetadataGenericParameterType[] _GenericArguments;

        public MetadataType(TypeDefinitionHandle Handle, MetadataSolver Assembly)
        {
            this.Assembly = Assembly;
            this.Def = Assembly.MD.GetTypeDefinition(Handle);
            this.Handle = Handle;

            if (this.IsGenericDefinition && !(this.DeclaringType?.IsGenericDefinition ?? false))
                constructedTypes = new Dictionary<IType[], MetadataConstructedType>(InterfaceComparer.Instance);
        }
        internal TypeDefinition Def;
        internal TypeDefinitionHandle Handle;

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

                    if (!AssemblyLoaderHelpers.ValidateGenericParameterConstraints(genericArguments, (IGenericParameter[])this.GenericArguments))
                        throw new ArgumentException("Generic Constraints not Met", nameof(genericArguments));

                    type = new MetadataConstructedType(this, genericArguments);
                    this.constructedTypes.Add(genericArguments, type);
                    return type;
                }
            }
            else
                throw new InvalidOperationException("Can't construct generic from non GenericDefinition Type");
        }
        private Dictionary<IType[], MetadataConstructedType> constructedTypes;

        private void LoadMethods()
        {
            List<MetadataMethod> Methods = new List<MetadataMethod>();
            List<MetadataMethod> Constructors = new List<MetadataMethod>();

            foreach (MethodDefinitionHandle handle in this.Def.GetMethods())
            {
                MetadataMethod method = this.Assembly.GetMethodDefinition(handle, this);
                if (method.Name[0] == '.')
                    switch (method.Name)
                    {
                        case ".ctor":
                            Constructors.Add(method);
                            continue;
                        case ".cctor":
                            _StaticConstructor = method;
                            continue;
                        default:
                            throw new Exception($"Unknwon Special MethodName {method.Name}");
                    }
                else
                    Methods.Add(method);
            }

            this._Methods = Methods.ToArray();
            this._Constructors = Constructors.ToArray();
        }

        internal MetadataConstructedType Rebase(MetadataConstructedType newDeclaringType)
        {
            if (this.rebasedTypes.TryGetValue(newDeclaringType, out MetadataConstructedType type))
                return type;

            lock (this.rebasedTypes)
            {
                if (this.rebasedTypes.TryGetValue(newDeclaringType, out type))
                    return type;

                type = new MetadataConstructedType(this, newDeclaringType);
                this.rebasedTypes.Add(newDeclaringType, type);
                return type;
            }
        }

        private Dictionary<IType, MetadataConstructedType> rebasedTypes = new Dictionary<IType, MetadataConstructedType>(InterfaceComparer.Instance);


        public override bool IsStatic => (this.Def.Attributes & (TypeAttributes.Abstract | TypeAttributes.Sealed)) == (TypeAttributes.Abstract | TypeAttributes.Sealed);
        public override IType BaseType
        {
            get
            {
                if (this._BaseType == null && !this.IsStatic && (this.Def.Attributes & TypeAttributes.Interface) != TypeAttributes.Interface)
                {
                    switch (this.Def.BaseType.Kind)
                    {
                        case HandleKind.TypeSpecification:
                            this._BaseType = this.Assembly.GetTypeSpecification((TypeSpecificationHandle)this.Def.BaseType, this);
                            break;
                        case HandleKind.TypeReference:
                            this._BaseType = this.Assembly.GetTypeReference((TypeReferenceHandle)this.Def.BaseType);
                            break;
                        case HandleKind.TypeDefinition:
                            this._BaseType = this.Assembly.GetTypeDefinition((TypeDefinitionHandle)this.Def.BaseType);
                            break;

                        default:
                            throw new Exception($"Unexpected BaseType Kind {this.Def.BaseType}");
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
                    InterfaceImplementationHandleCollection collection = this.Def.GetInterfaceImplementations();
                    this._Interfaces = new IType[collection.Count];
                    int x = 0;
                    foreach (InterfaceImplementationHandle handle in collection)
                    {
                        InterfaceImplementation Impl = this.Assembly.MD.GetInterfaceImplementation(handle);
                        switch (Impl.Interface.Kind)
                        {
                            case HandleKind.TypeDefinition:
                                this._Interfaces[x] = this.Assembly.GetTypeDefinition((TypeDefinitionHandle)Impl.Interface);
                                break;
                            case HandleKind.TypeReference:
                                this._Interfaces[x] = this.Assembly.GetTypeReference((TypeReferenceHandle)Impl.Interface);
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
    }
}
