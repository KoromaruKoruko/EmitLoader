
using System;
using System.Collections.Immutable;
using System.Reflection.Metadata;

namespace EmitLoader.Mixed
{
    internal class MixedArrayType : IType
    {
        public AssemblyObjectKind Kind => AssemblyObjectKind.Type;
        public AssemblyLoader Context => this.elementType.Context;
        public IAssembly Assembly => this.Context.MixedSolver;


        public MixedArrayType(IType elementType, ArrayShape shape)
        {
            this.shape = shape;
            this.elementType = elementType;

            this.arrayType = this.Context.ResolveType(typeof(Array));
        }
        public MixedArrayType(IType elementType)
        {
            this.shape = new ArrayShape(1, ImmutableArray<int>.Empty, ImmutableArray<int>.Empty);
            this.elementType = elementType;

            this.arrayType = this.Context.ResolveType(typeof(Array));
        }
        private readonly ArrayShape shape;
        private readonly IType elementType;
        private readonly IType arrayType;

        public Type GetBuiltType() => this.elementType.GetBuiltType().MakeArrayType(shape.Rank);


        public INamespace Namespace => this.elementType.Namespace;


        public bool IsNestedType => false;
        public IType DeclaringType => null;
        public bool IsGenericTypeParameter => false;
        public IMethod StaticConstructor => null;

        public IType GenericDefinition => null;
        IGeneric IGeneric.GenericDefinition => null;


        public IType[] Types => this.arrayType.Types;
        public IField[] Fields => this.arrayType.Fields;
        public IMethod[] Methods => this.arrayType.Methods;
        public IMethod[] Constructors => this.arrayType.Constructors;
        public IProperty[] Properties => this.arrayType.Properties;
        public IEvent[] Events => this.arrayType.Events;

        public IType FindType(string Name)
        {
            foreach (IType type in this.Types)
                if (type.Name == Name)
                    return type;
            return null;
        }
        public IField FindField(string Name)
        {
            foreach (IField field in this.Fields)
                if (field.Name == Name)
                    return field;
            return null;
        }
        public IMethod FindMethod(string Name, IType[] ParameterTypes)
        {
            foreach (IMethod method in this.Methods)
                if (method.Name == Name && ParameterTypes.Length == method.Parameters.Length)
                {
                    Boolean f = true;

                    for (int x = 0; x < ParameterTypes.Length; x++)
                        if (ParameterTypes[x] != method.Parameters[x].ParameterType)
                        {
                            f = false;
                            break;
                        }

                    if (f)
                        return method;
                }
            return null;
        }
        public IMethod FindConstructor(IType[] ParameterTypes)
        {
            foreach (IMethod method in this.Constructors)
                if (method.Name == Name && ParameterTypes.Length == method.Parameters.Length)
                {
                    Boolean f = true;

                    for (int x = 0; x < ParameterTypes.Length; x++)
                        if (ParameterTypes[x] != method.Parameters[x].ParameterType)
                        {
                            f = false;
                            break;
                        }

                    if (f)
                        return method;
                }
            return null;
        }
        public IProperty FindProperty(string Name)
        {
            foreach (IProperty property in this.Properties)
                if (property.Name == Name)
                    return property;
            return null;
        }
        public IEvent FindEvent(string Name)
        {
            foreach (IEvent @event in this.Events)
                if (@event.Name == Name)
                    return @event;
            return null;
        }


        public ICustomAttribute[] CustomAttributes => Array.Empty<ICustomAttribute>();

        public string Name => $"{this.elementType.Name}[{new string(',', this.ArrayRank - 1)}]";
        public bool IsGeneric => false;
        public bool IsGenericDefinition => false;
        public IType[] GenericArguments => Array.Empty<IType>();


        public string GetFullyQualifiedName() => $"{this.elementType.GetFullyQualifiedName()}[{new string(',', this.ArrayRank - 1)}]";


        public Int32 ArrayRank => shape.Rank;
        public Boolean IsArray => true;
        public Boolean IsByRef => false;
        public Boolean IsPointer => false;
        public Boolean IsSZArray => this.ArrayRank == 1;
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

        public IType ConstructGeneric(IType[] genericArguments) => throw new InvalidOperationException();
        IGeneric IGeneric.ConstructGeneric(IType[] genericArguments) => throw new InvalidOperationException();




        public Boolean IsStatic => false;
        public Boolean IsInterface => false;
        public Boolean IsEnum => false;
        public Boolean IsValueType => false;

        // NULLABLE
        public IType BaseType => this.arrayType;
        public IType[] Interfaces => Array.Empty<IType>();
        public Boolean IsCastableTo(IType type) => AssemblyLoaderHelpers.IsCastableTo(this, type);
    }
}
