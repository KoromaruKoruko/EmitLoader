
using System;
using System.Reflection.Metadata;

namespace EmitLoader.Mixed
{
    internal class MixedByRefType : IType
    {
        public AssemblyObjectKind Kind => AssemblyObjectKind.Type;
        public AssemblyLoader Context => this.elementType.Context;
        public IAssembly Assembly => this.Context.MixedSolver;


        public MixedByRefType(IType elementType) => this.elementType = elementType;
        private readonly IType elementType;

        public Type GetBuiltType() => this.elementType.GetBuiltType().MakeByRefType();

        public INamespace Namespace => this.elementType.Namespace;


        public bool IsNestedType => false;
        public IType DeclaringType => null;
        public bool IsGenericTypeParameter => false;
        public IMethod StaticConstructor => null;


        public IType GenericDefinition => null;
        IGeneric IGeneric.GenericDefinition => null;


        public IType[] Types => Array.Empty<IType>();
        public IField[] Fields => Array.Empty<IField>();
        public IMethod[] Methods => Array.Empty<IMethod>();
        public IMethod[] Constructors => Array.Empty<IMethod>();
        public IProperty[] Properties => Array.Empty<IProperty>();
        public IEvent[] Events => Array.Empty<IEvent>();


        public IType FindType(string Name) => null;
        public IField FindField(string Name) => null;
        public IMethod FindMethod(string Name, IType[] ParameterTypes) => null;
        public IMethod FindConstructor(IType[] ParameterTypes) => null;
        public IProperty FindProperty(string Name) => null;
        public IEvent FindEvent(string Name) => null;

        public ICustomAttribute[] CustomAttributes => Array.Empty<ICustomAttribute>();

        public string Name => $"{this.elementType.Name}&";
        public bool IsGeneric => false;
        public bool IsGenericDefinition => false;
        public IType[] GenericArguments => Array.Empty<IType>();


        public string GetFullyQualifiedName() => $"{this.elementType.GetFullyQualifiedName()}&";


        public Int32 ArrayRank => 0;
        public Boolean IsArray => false;
        public Boolean IsSZArray => false;
        public Boolean IsByRef => true;
        public Boolean IsPointer => false;
        public Boolean IsPinned => false;

        public IType MakeArrayType(ArrayShape shape) => new MixedArrayType(this, shape);
        public IType MakeByRefType()
        {
            if (this._ByRefType == null)
                this._ByRefType = new MixedByRefType(this);
            return this._ByRefType;
        }
        private IType _ByRefType;
        public IType MakePointerType()
        {
            if (this._PointerType == null)
                this._PointerType = new MixedPointerType(this);
            return this._PointerType;
        }
        private IType _PointerType;
        public IType MakeSZArrayType()
        {
            if (this._SZArrayType == null)
                this._SZArrayType = new MixedArrayType(this);
            return this._SZArrayType;
        }
        private IType _SZArrayType;
        public IType GetElementType() => this.elementType;

        public IType ConstructGeneric(IType[] genericArguments) => throw new NotSupportedException();
        IGeneric IGeneric.ConstructGeneric(IType[] genericArguments) => this.ConstructGeneric(genericArguments);


        public Boolean IsStatic => false;
        public Boolean IsInterface => false;
        public Boolean IsEnum => false;
        public Boolean IsValueType => false;

        // NULLABLE
        public IType BaseType => null;
        public IType[] Interfaces => Array.Empty<IType>();
        public Boolean IsCastableTo(IType type) => AssemblyLoaderHelpers.IsCastableTo(this, type);
    }
}
