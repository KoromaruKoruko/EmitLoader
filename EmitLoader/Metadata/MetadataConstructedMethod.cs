
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Metadata;

namespace EmitLoader.Metadata
{
    internal class MetadataConstructedMethod : MetadataMethodBase
    {
        public override MetadataSolver Assembly => this.Base.Assembly;
        public override string Name => this.Base.Name;
        public override MetadataTypeBase DeclaringType { get; }

        public override bool IsGeneric { get; }
        public override bool IsGenericDefinition { get; }

        public override MethodAttributes Attributes => this.Base.Attributes;

        internal override MethodBase BuildMethod()
        {
            Type[] types = new Type[this.GenericArguments.Length];
            for (int i = 0; i < this.GenericArguments.Length; i++)
                types[i] = this.GenericArguments[i].GetBuiltType();
            return ((MethodInfo)this.Base.BuildMethod()).MakeGenericMethod(types);
        }

        public override IType ReturnType => IsGeneric && !IsGenericDefinition ? this.Signature.ReturnType : this.Base.ReturnType;
        public override MetadataParameterBase[] Parameters
        {
            get
            {
                if (this._Parameters == null)
                {
                    this._Parameters = new MetadataParameterBase[this.Base.Parameters.Length];
                    if (this.IsGeneric && !this.IsGenericDefinition)
                        for (int x = 0; x < this.Base.Parameters.Length; x++)
                            this._Parameters[x] = new MetadataConstructedParameter((MetadataParameter)this.Base.Parameters[x], this.Signature.ParameterTypes[x], this);
                    else
                        for (int x = 0; x < this.Base.Parameters.Length; x++)
                            this._Parameters[x] = ((MetadataParameter)this.Base.Parameters[x]).Rebase(this);
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
                    this._CustomAttributes = new MetadataConstructedCustomAttribute[this.Base.CustomAttributes.Length];
                    for (int x = 0; x < this.Base.CustomAttributes.Length; x++)
                        this._CustomAttributes[x] = ((MetadataCustomAttribute)this.Base.CustomAttributes[x]).Rebase(this);
                }
                return this._CustomAttributes;
            }
        }
        private MetadataCustomAttributeBase[] _CustomAttributes;

        public override MetadataMethodBase GenericDefinition { get; }
        public override IType[] GenericArguments
        {
            get
            {
                if (this._GenericArguments == null)
                {
                    if (this.IsGenericDefinition)
                    {
                        this._GenericArguments = new IType[this.Base.GenericArguments.Length];
                        for (int x = 0; x < this.Base.GenericArguments.Length; x++)
                            this._GenericArguments[x] = ((MetadataGenericParameterType)this.Base.GenericArguments[x]).Rebase(this);
                    }
                }
                return this._GenericArguments;
            }
        }
        private IType[] _GenericArguments;

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

        public MetadataConstructedMethod(MetadataConstructedMethod definitionMethod, IType[] genericArguments)
        {
            this.Base = definitionMethod.Base;
            this.GenericDefinition = definitionMethod;
            this.IsGeneric = true;
            this.IsGenericDefinition = false;
            this._GenericArguments = genericArguments;
            this.DeclaringType = definitionMethod.DeclaringType;

            if (this.IsGenericDefinition)
                this.constructedMethods = new Dictionary<IType[], MetadataConstructedMethod>();
        }
        public MetadataConstructedMethod(MetadataMethod definitionMethod, IType[] genericArguments)
        {
            this.Base = definitionMethod;
            this.GenericDefinition = definitionMethod;
            this.IsGeneric = true;
            this.IsGenericDefinition = false;
            this._GenericArguments = genericArguments;
            this.DeclaringType = definitionMethod.DeclaringType;

            if (this.IsGenericDefinition)
                this.constructedMethods = new Dictionary<IType[], MetadataConstructedMethod>();
        }
        public MetadataConstructedMethod(MetadataMethod Base, MetadataTypeBase newDeclaringType)
        {
            this.Base = Base;
            this.GenericDefinition = null;
            this.IsGeneric = this.Base.IsGeneric;
            this.IsGenericDefinition = this.Base.IsGenericDefinition;
            this._GenericArguments = null;
            this.DeclaringType = newDeclaringType;

            if (this.IsGenericDefinition)
                this.constructedMethods = new Dictionary<IType[], MetadataConstructedMethod>();
        }
        private MetadataMethod Base;

        private MethodSignature<IType> Signature
        {
            get
            {
                if (!hasSignature)
                {
                    this._Signature = this.Base.Def.DecodeSignature(this.Assembly.SP, this);
                    hasSignature = true;
                }
                return this._Signature;
            }
        }
        private MethodSignature<IType> _Signature = default;
        private Boolean hasSignature = false;
    }
}
