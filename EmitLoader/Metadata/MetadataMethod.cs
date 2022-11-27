using EmitLoader.Builder;

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Metadata;

namespace EmitLoader.Metadata
{
    internal class MetadataMethod : MetadataMethodBase
    {
        public override MetadataSolver Assembly => this.DeclaringType.Assembly;
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
        public override MetadataTypeBase DeclaringType { get; }

        public override bool IsGeneric => this.Def.GetGenericParameters().Count > 0;
        public override bool IsGenericDefinition => this.IsGeneric;


        public override MethodAttributes Attributes => this.Def.Attributes;
        internal override MethodBase BuildMethod()
        {
            Type[] @params = new Type[this.Parameters.Length];
            for (int x = 0; x < this.Parameters.Length; x++)
                @params[x] = this.Parameters[x].ParameterType.GetBuiltType();
            return this.DeclaringType.BuildType().GetRuntimeMethod(this.Name, @params);
        }

        public override IType ReturnType => this.Signature.ReturnType;
        public override MetadataParameterBase[] Parameters
        {
            get
            {
                if (this._Parameters == null)
                {
                    ParameterHandleCollection collection = this.Def.GetParameters();
                    this._Parameters = new MetadataParameter[collection.Count];
                    int x = 0;
                    foreach (ParameterHandle handle in collection)
                    {
                        this._Parameters[x] = new MetadataParameter(this.Assembly.MD.GetParameter(handle), this.Signature.ParameterTypes[x], x < this.Signature.RequiredParameterCount, this);
                        x++;
                    }
                }
                return this._Parameters;
            }
        }
        private MetadataParameterBase[] _Parameters;
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

        public override IType[] GenericArguments
        {
            get
            {
                if (this._GenericArguments == null)
                    if (this.IsGenericDefinition)
                    {
                        GenericParameterHandleCollection collection = this.Def.GetGenericParameters();
                        this._GenericArguments = new MetadataGenericParameterType[collection.Count];
                        int x = 0;
                        foreach (GenericParameterHandle handle in collection)
                            this._GenericArguments[x++] = new MetadataGenericParameterType(this.Assembly.MD.GetGenericParameter(handle), this);
                    }
                    else
                        this._GenericArguments = Array.Empty<MetadataGenericParameterType>();

                return this._GenericArguments;
            }
        }
        private MetadataGenericParameterType[] _GenericArguments;
        public override MetadataMethodBase GenericDefinition => null;

        public override MetadataMethodBase ConstructGeneric(IType[] genericArguments)
        {
            if (this.IsGenericDefinition)
            {
                if (this.DeclaringType.IsGenericDefinition)
                    throw new InvalidOperationException("Must Construct ContainingType First!");

                if (genericArguments.Length != this.GenericArguments.Length)
                    throw new ArgumentOutOfRangeException("Incorrect number of genericArguments");

                if (this.constructedMethods.TryGetValue(genericArguments, out MetadataConstructedMethod method))
                    return method;

                lock (this.constructedMethods)
                {
                    if (this.constructedMethods.TryGetValue(genericArguments, out method))
                        return method;

                    if (!AssemblyLoaderHelpers.ValidateGenericParameterConstraints(genericArguments, (IGenericParameter[])this.GenericArguments))
                        throw new ArgumentException("Generic Constraints not Met", nameof(genericArguments));

                    method = new MetadataConstructedMethod(this, genericArguments);
                    this.constructedMethods.Add(genericArguments, method);
                    return method;
                }
            }
            else
                throw new InvalidOperationException("Can't construct generic from non GenericDefinition Type");
        }
        private Dictionary<IType[], MetadataConstructedMethod> constructedMethods;

        public MetadataMethod(MethodDefinitionHandle Handle, MetadataType DeclaringType)
        {
            this.DeclaringType = DeclaringType;
            this.Def = this.Assembly.MD.GetMethodDefinition(Handle);
            this.Handle = Handle;

            if (this.IsGenericDefinition && !this.DeclaringType.IsGenericDefinition)
                constructedMethods = new Dictionary<IType[], MetadataConstructedMethod>(InterfaceComparer.Instance);
        }
        internal MethodDefinition Def;
        internal MethodDefinitionHandle Handle;

        public MetadataMethodBody GetMethodBody()
        {
            if (this._MethodBody == null)
                this._MethodBody = new MetadataMethodBody(this);
            return this._MethodBody;
        }
        private MetadataMethodBody _MethodBody;

        private MethodSignature<IType> Signature
        {
            get
            {
                if (!hasSignature)
                {
                    this._Signature = this.Def.DecodeSignature(this.Assembly.SP, this);
                    hasSignature = true;
                }
                return this._Signature;
            }
        }
        private MethodSignature<IType> _Signature = default;
        private Boolean hasSignature = false;

        internal MetadataConstructedMethod Rebase(MetadataConstructedType newDeclaringType)
        {
            if (this.rebasedMethods.TryGetValue(newDeclaringType, out MetadataConstructedMethod method))
                return method;

            lock (this.rebasedMethods)
            {
                if (this.rebasedMethods.TryGetValue(newDeclaringType, out method))
                    return method;

                method = new MetadataConstructedMethod(this, newDeclaringType);
                this.rebasedMethods.Add(newDeclaringType, method);
                return method;
            }
        }
        private Dictionary<IType, MetadataConstructedMethod> rebasedMethods = new Dictionary<IType, MetadataConstructedMethod>(InterfaceComparer.Instance);
    }
}
