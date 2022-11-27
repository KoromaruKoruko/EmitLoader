using EmitLoader.Mixed;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace EmitLoader.Reflection
{
    internal class ReflectionMethod : IMethod
    {
        public ReflectionMethod(ConstructorInfo constructor, ReflectionType declaringType)
        {
            this.method = constructor;
            this.declaringType = declaringType;
            this.GenericDefinition = null;
            this._ReturnType = declaringType;

            if (this.IsGenericDefinition)
                this.constructedLookup = new Dictionary<IType[], IMethod>();
        }
        public ReflectionMethod(MethodInfo method, ReflectionType declaringType, ReflectionMethod GenericDefinition)
        {
            this.method = method;
            this.declaringType = declaringType;
            this.GenericDefinition = GenericDefinition;

            if (this.IsGenericDefinition)
                this.constructedLookup = new Dictionary<IType[], IMethod>();
        }
        internal readonly MethodBase method;
        internal readonly ReflectionType declaringType;

        public MethodBase GetBuiltMethod() => this.method;

        public IType ReturnType
        {
            get
            {
                if (this._ReturnType == null)
                    this._ReturnType = this.Context.ResolveType(((MethodInfo)method).ReturnType);
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
                    ParameterInfo[] @params = this.method.GetParameters();
                    this._Parameters = new IParameter[@params.Length];
                    for (int x = 0; x < @params.Length; x++)
                        this._Parameters[x] = new ReflectionParameter(@params[x], this);
                }
                return this._Parameters;
            }
        }
        private IParameter[] _Parameters;
        public IMethod GenericDefinition { get; }
        IGeneric IGeneric.GenericDefinition => this.GenericDefinition;
        public IType DeclaringType => this.declaringType;
        public ICustomAttribute[] CustomAttributes
        {
            get
            {
                if (this._CustomAttributes == null)
                {
                    IList<CustomAttributeData> attributes = this.method.GetCustomAttributesData();
                    this._CustomAttributes = new ReflectionCustomAttribute[attributes.Count];
                    for (int x = 0; x < attributes.Count; x++)
                        this._CustomAttributes[x] = new ReflectionCustomAttribute(attributes[x], this.declaringType.assembly);
                }
                return this._CustomAttributes;
            }
        }
        private ICustomAttribute[] _CustomAttributes;
        public string Name => this.method.Name;
        public bool IsGeneric => this.method.IsGenericMethod;
        public bool IsGenericDefinition => this.method.IsGenericMethodDefinition;
        public IType[] GenericArguments
        {
            get
            {
                if (this._GenericArguments == null)
                {
                    if (this.IsGeneric)
                        if (this.IsGenericDefinition)
                        {
                            Type[] @params = this.method.GetGenericArguments();
                            this._GenericArguments = new IType[@params.Length];
                            for (int x = 0; x < @params.Length; x++)
                                this._GenericArguments[x] = new ReflectionGenericParameter(@params[x], this);
                        }
                        else
                        {
                            Type[] args = this.method.GetGenericArguments();
                            this._GenericArguments = new IType[args.Length];
                            for (int x = 0; x < args.Length; x++)
                                this._GenericArguments[x] = this.Context.ResolveType(args[x]);
                        }
                    else
                        this._GenericArguments = Array.Empty<IType>();
                }
                return this._GenericArguments;
            }
        }
        private IType[] _GenericArguments;
        public AssemblyObjectKind Kind => AssemblyObjectKind.Method;
        public AssemblyLoader Context => this.declaringType.Context;
        public IAssembly Assembly => this.declaringType.Assembly;


        public IMethod ConstructGeneric(IType[] genericArguments)
        {
            if (!this.IsGenericDefinition)
                throw new InvalidOperationException();

            if (genericArguments.Length != this.GenericArguments.Length)
                throw new ArgumentOutOfRangeException(nameof(genericArguments), "invalid number of generic arguments");

            if (this.constructedLookup.TryGetValue(genericArguments, out IMethod constructedMethod))
                return constructedMethod;

            lock (this.constructedLookup)
            {
                if (this.constructedLookup.TryGetValue(genericArguments, out constructedMethod))
                    return constructedMethod;

                if (genericArguments.All((t) => t is ReflectionType))
                {
                    Type[] generics = new Type[genericArguments.Length];
                    for (int x = 0; x < genericArguments.Length; x++)
                        generics[x] = ((ReflectionType)genericArguments[x]).type;
                    constructedMethod = new ReflectionMethod(((MethodInfo)this.method).MakeGenericMethod(generics), (ReflectionType)this.DeclaringType, this);
                }
                else
                {
                    if (!AssemblyLoaderHelpers.ValidateGenericParameterConstraints(genericArguments, (IGenericParameter[])this.GenericArguments))
                        throw new ArgumentException("Generic Constraints not Met", nameof(genericArguments));

                    constructedMethod = new MixedConstructedMethod(this, genericArguments);
                }
                this.constructedLookup.Add(genericArguments, constructedMethod);
                return constructedMethod;
            }
        }
        private Dictionary<IType[], IMethod> constructedLookup;
        IGeneric IGeneric.ConstructGeneric(IType[] genericArguments) => this.ConstructGeneric(genericArguments);

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
