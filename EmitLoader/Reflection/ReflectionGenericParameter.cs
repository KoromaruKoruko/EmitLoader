
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Metadata;

namespace EmitLoader.Reflection
{
    internal class ReflectionGenericParameter : IGenericParameter
    {
        public ReflectionGenericParameter(Type type, IGeneric Parent)
        {
            this.type = type;
            this.Parent = Parent;
        }

        internal Type type;
        public IGeneric Parent { get; }

        public Type GetBuiltType() => this.type;
        public GenericParameterAttributes GenericParameterAttributes => this.type.GenericParameterAttributes;

        public AssemblyObjectKind Kind => AssemblyObjectKind.GenericParameter;
        public AssemblyLoader Context => this.Parent.Context;
        public IAssembly Assembly => this.Parent.Assembly;
        public bool IsNestedType => false;
        public bool IsGenericTypeParameter => true;
        public IGenericParameterConstraint[] Constraints
        {
            get
            {
                if (this._Constraints == null)
                {
                    Type[] constraints = type.GetGenericParameterConstraints();
                    this._Constraints = new IGenericParameterConstraint[constraints.Length];
                    for (int x = 0; x < constraints.Length; x++)
                        this._Constraints[x] = new ReflectionGenericParameterConstraint(constraints[x], this);
                }
                return this._Constraints;
            }
        }
        private IGenericParameterConstraint[] _Constraints;
        public string Name => this.type.Name;
        public ICustomAttribute[] CustomAttributes
        {
            get
            {
                if (this._CustomAttributes == null)
                {
                    IList<CustomAttributeData> attributes = this.type.GetCustomAttributesData();
                    this._CustomAttributes = new ReflectionCustomAttribute[attributes.Count];
                    for (int x = 0; x < attributes.Count; x++)
                        this._CustomAttributes[x] = new ReflectionCustomAttribute(attributes[x], (ReflectionSolver)this.Assembly);
                }
                return this._CustomAttributes;
            }
        }
        private ICustomAttribute[] _CustomAttributes;

        public INamespace Namespace => throw new NotSupportedException();
        public IType DeclaringType => throw new NotSupportedException();
        public IType GenericDefinition => throw new NotSupportedException();
        IGeneric IGeneric.GenericDefinition => throw new NotSupportedException();
        public IType[] Types => throw new NotSupportedException();
        public IField[] Fields => throw new NotSupportedException();
        public IMethod[] Methods => throw new NotSupportedException();
        public IMethod[] Constructors => throw new NotSupportedException();
        public IProperty[] Properties => throw new NotSupportedException();
        public IEvent[] Events => throw new NotSupportedException();
        public int ArrayRank => throw new NotSupportedException();
        public bool IsArray => throw new NotSupportedException();
        public bool IsByRef => throw new NotSupportedException();
        public bool IsPointer => throw new NotSupportedException();
        public bool IsSZArray => throw new NotSupportedException();
        public bool IsPinned => throw new NotSupportedException();
        public bool IsGeneric => throw new NotSupportedException();
        public bool IsGenericDefinition => throw new NotSupportedException();
        public IType[] GenericArguments => throw new NotSupportedException();
        public IType ConstructGeneric(IType[] genericArguments) => throw new NotSupportedException();
        IGeneric IGeneric.ConstructGeneric(IType[] genericArguments) => throw new NotSupportedException();
        public string GetFullyQualifiedName() => throw new NotSupportedException();
        public IType GetElementType() => throw new NotSupportedException();
        public IType MakeArrayType(ArrayShape shape) => throw new NotSupportedException();
        public IType MakeByRefType() => throw new NotSupportedException();
        public IType MakePinnedType() => throw new NotSupportedException();
        public IType MakePointerType() => throw new NotSupportedException();
        public IType MakeSZArrayType() => throw new NotSupportedException();
        public IType FindType(string Name) => throw new NotSupportedException();
        public IField FindField(string Name) => throw new NotSupportedException();
        public IMethod FindMethod(string Name, IType[] ParameterTypes) => throw new NotSupportedException();
        public IMethod FindConstructor(IType[] ParameterTypes) => throw new NotSupportedException();
        public IProperty FindProperty(string Name) => throw new NotSupportedException();
        public IEvent FindEvent(string Name) => throw new NotSupportedException();
        public IMethod StaticConstructor => throw new NotSupportedException();
        public Boolean IsStatic => false;
        public Boolean IsInterface => false;
        public Boolean IsEnum => this.type.IsEnum;
        public Boolean IsValueType => this.type.IsValueType;

        // NULLABLE
        public IType BaseType
        {
            get
            {
                if (this._BaseType == null)
                    this._BaseType = this.Context.ResolveType(this.type.BaseType);
                return this._BaseType;
            }
        }
        private IType _BaseType;
        public IType[] Interfaces
        {
            get
            {
                if (this._Interfaces == null)
                {
                    Type[] interfaces = this.type.GetInterfaces();
                    this._Interfaces = new IType[interfaces.Length];
                    for (int x = 0; x < interfaces.Length; x++)
                        this._Interfaces[x] = this.Context.ResolveType(interfaces[x]);
                }
                return this._Interfaces;
            }
        }
        private IType[] _Interfaces;
        public Boolean IsCastableTo(IType type)
        {
            return type.Assembly.Kind == AssemblyKind.Reflection
                ? type.IsGenericTypeParameter
                    ? ((ReflectionGenericParameter)type).type.IsAssignableFrom(this.type)
                    : ((ReflectionType)type).type.IsAssignableFrom(this.type)
                : AssemblyLoaderHelpers.IsCastableTo(this, type);
        }
    }
}
