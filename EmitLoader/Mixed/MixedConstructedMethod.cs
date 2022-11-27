
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace EmitLoader.Mixed
{
    internal class MixedConstructedMethod : IMethod
    {
        public MixedConstructedMethod(IMethod GenericDefinition, IType[] GenericArguments)
        {
            this.Base = GenericDefinition;
            this.GenericDefinition = GenericDefinition;
            this._GenericArguments = GenericArguments;
            this.IsGeneric = true;
            this.IsGenericDefinition = false;
            this.DeclaringType = GenericDefinition.DeclaringType;

            this.Mappings = new MixedTypeGenericMappings((IGenericParameter[])GenericDefinition.GenericArguments, GenericArguments);
            this.constructedMethods = null;
        }
        public MixedConstructedMethod(IMethod GenericDefinition, IType[] GenericArguments, MixedConstructedType Parent)
        {
            this.Base = GenericDefinition;
            this.GenericDefinition = GenericDefinition;
            this._GenericArguments = GenericArguments;
            this.IsGeneric = true;
            this.IsGenericDefinition = false;
            this.DeclaringType = GenericDefinition.DeclaringType;

            this.Mappings = new MixedTypeGenericMappings((IGenericParameter[])GenericDefinition.GenericArguments, GenericArguments, Parent.Mappings);
            this.constructedMethods = null;
        }
        public MixedConstructedMethod(IMethod Base, MixedConstructedType Parent)
        {
            this.Base = Base;
            this.GenericDefinition = null;
            this._GenericArguments = null;
            this.IsGeneric = this.Base.IsGeneric;
            this.IsGenericDefinition = this.Base.IsGenericDefinition;
            this.DeclaringType = Parent;

            this.Mappings = Parent.Mappings;
            if (this.IsGenericDefinition)
                this.constructedMethods = new Dictionary<IType[], MixedConstructedMethod>();
        }
        internal readonly IMethod Base;
        internal readonly MixedTypeGenericMappings Mappings;

        public MethodBase GetBuiltMethod()
        {
            Type Parent = this.DeclaringType.GetBuiltType();
            MethodInfo @base = (MethodInfo)this.Base.GetBuiltMethod();
            ParameterInfo[] @params = @base.GetParameters();
            Type[] types = new Type[@params.Length];
            for (int x = 0; x < @params.Length; x++)
                types[x] = @params[x].ParameterType;

            @base = Parent.GetRuntimeMethod(this.Base.Name, types);

            if (this.IsGeneric && !this.IsGenericDefinition)
            {
                types = new Type[this.GenericArguments.Length];
                for (int i = 0; i < types.Length; i++)
                    types[i] = this.GenericArguments[i].GetBuiltType();

                return @base.MakeGenericMethod(types);
            }
            else
                return @base;
        }

        public AssemblyObjectKind Kind => AssemblyObjectKind.Method;
        public AssemblyLoader Context => this.Base.Context;
        public IAssembly Assembly => this.Base.Context.MixedSolver;

        public IType ReturnType
        {
            get
            {
                if (this._ReturnType == null)
                    this._ReturnType = Mappings.MapType(this.Base.ReturnType);
                return this._ReturnType;
            }
        }
        private IType _ReturnType;
        public IParameter[] Parameters
        {
            get
            {
                if (this._Parameters == null)
                {
                    this._Parameters = new MixedConstructedParameter[this.Base.Parameters.Length];
                    for (int x = 0; x < this.Base.Parameters.Length; x++)
                        this._Parameters[x] = new MixedConstructedParameter(this.Base.Parameters[x], this);
                }
                return this._Parameters;
            }
        }
        private IParameter[] _Parameters;
        public IType DeclaringType { get; }
        public ICustomAttribute[] CustomAttributes => this.Base.CustomAttributes;

        public string Name => this.Base.Name;
        public bool IsGeneric { get; }
        public bool IsGenericDefinition { get; }
        public IType[] GenericArguments
        {
            get
            {
                if (this._GenericArguments == null)
                {
                    this._GenericArguments = new IType[this.Base.GenericArguments.Length];
                    for (int x = 0; x < this.Base.GenericArguments.Length; x++)
                        this._GenericArguments[x] = new MixedConstructedGenericParameter((IGenericParameter)this.Base.GenericArguments[x], this);
                }
                return this._GenericArguments;
            }
        }
        private IType[] _GenericArguments;

        public IMethod GenericDefinition { get; }
        IGeneric IGeneric.GenericDefinition => this.GenericDefinition;

        IGeneric IGeneric.ConstructGeneric(IType[] genericArguments) => this.ConstructGeneric(genericArguments);
        public IMethod ConstructGeneric(IType[] genericArguments)
        {
            if (!this.IsGenericDefinition)
                throw new InvalidOperationException();

            if (genericArguments.Length != this.GenericArguments.Length)
                throw new ArgumentOutOfRangeException(nameof(genericArguments), "invalid number of generic arguments");

            if (this.constructedMethods.TryGetValue(genericArguments, out MixedConstructedMethod constructedMethod))
                return constructedMethod;

            lock (this.constructedMethods)
            {
                if (this.constructedMethods.TryGetValue(genericArguments, out constructedMethod))
                    return constructedMethod;

                if (!AssemblyLoaderHelpers.ValidateGenericParameterConstraints(genericArguments, (IGenericParameter[])this.GenericArguments))
                    throw new ArgumentException("Generic Constraints not Met", nameof(genericArguments));

                constructedMethod = new MixedConstructedMethod(this, genericArguments, (MixedConstructedType)this.DeclaringType);
                this.constructedMethods.Add(genericArguments, constructedMethod);
                return constructedMethod;
            }
        }
        private Dictionary<IType[], MixedConstructedMethod> constructedMethods;

        public string GetFullyQualifiedName()
        {
            if (this._FullyQualifiedName == null)
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(this.DeclaringType.GetFullyQualifiedName());
                sb.Append('.');
                sb.Append(this.Name);

                if (this.IsGeneric)
                {
                    sb.Append('[');
                    if (this.IsGenericDefinition)
                    {
                        sb.Append(this.GenericArguments[0].Name);
                        for (int x = 1; x < this.GenericArguments.Length; x++)
                        {
                            sb.Append(',');
                            sb.Append(this.GenericArguments[x].Name);
                        }
                    }
                    else
                    {
                        sb.Append(this.GenericArguments[0].GetFullyQualifiedName());
                        for (int x = 1; x < this.GenericArguments.Length; x++)
                        {
                            sb.Append(',');
                            sb.Append(this.GenericArguments[x].GetFullyQualifiedName());
                        }
                    }
                    sb.Append(']');
                }

                if (this.Parameters.Length > 0)
                {
                    sb.Append('(');
                    sb.Append(this.Parameters[0].ParameterType.GetFullyQualifiedName());
                    for (int x = 1; x < this.Parameters.Length; x++)
                    {
                        sb.Append(',');
                        sb.Append(this.Parameters[x].ParameterType.GetFullyQualifiedName());
                    }
                    sb.Append(')');
                }
                else
                    sb.Append("()");
                this._FullyQualifiedName = sb.ToString();
            }
            return this._FullyQualifiedName;
        }
        private string _FullyQualifiedName;
    }
}
