
using System;
using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Text;

namespace EmitLoader.Mixed
{
    internal class MixedConstructedType : IType
    {
        public MixedConstructedType(IType GenericDefinition, IType[] GenericArguments)
        {
            this.Base = GenericDefinition;
            this.GenericDefinition = GenericDefinition;
            this._GenericArguments = GenericArguments;
            this.DeclaringType = GenericDefinition.DeclaringType;
            this.IsGeneric = true;
            this.IsGenericDefinition = false;

            this.Mappings = new MixedTypeGenericMappings((IGenericParameter[])this.Base.GenericArguments, GenericArguments);
            this.constructedTypes = null;
        }
        private MixedConstructedType(IType GenericDefinition, IType[] GenericArguments, MixedConstructedType Parent)
        {
            this.Base = GenericDefinition;
            this.GenericDefinition = GenericDefinition;
            this._GenericArguments = GenericArguments;
            this.DeclaringType = Parent;
            this.IsGeneric = true;
            this.IsGenericDefinition = false;

            this.Mappings = new MixedTypeGenericMappings((IGenericParameter[])this.Base.GenericArguments, GenericArguments, Parent.Mappings);
            this.constructedTypes = null;
        }
        private MixedConstructedType(IType Base, MixedConstructedType Parent)
        {
            this.Base = Base;
            this.GenericDefinition = null;
            this.DeclaringType = Parent;
            this.IsGeneric = Base.IsGeneric;
            this.IsGenericDefinition = Base.IsGenericDefinition;

            this.Mappings = Parent.Mappings;

            if (this.IsGenericDefinition)
                this.constructedTypes = new Dictionary<IType[], MixedConstructedType>();
        }

        internal readonly IType Base;
        internal readonly MixedTypeGenericMappings Mappings;

        public Type GetBuiltType()
        {
            if (this.IsGeneric && !this.IsGenericDefinition)
            {
                Type @base = this.Base.GetBuiltType();
                Type[] types = new Type[this.GenericArguments.Length];
                for (int i = 0; i < types.Length; i++)
                    types[i] = ((IType)this.GenericArguments[i]).GetBuiltType();

                return @base.MakeGenericType(types);
            }
            else
                return this.Base.GetBuiltType();
        }

        public AssemblyObjectKind Kind => AssemblyObjectKind.Type;
        public AssemblyLoader Context => this.Base.Context;
        public IAssembly Assembly => this.Base.Context.MixedSolver;
        public INamespace Namespace => this.Base.Namespace;
        public bool IsNestedType => this.Base.IsNestedType;

        public IMethod StaticConstructor
        {
            get
            {
                if (this._StaticConstructor == null && this.Base.StaticConstructor != null)
                    this._StaticConstructor = new MixedConstructedMethod(this.Base.StaticConstructor, this);
                return this._StaticConstructor;
            }
        }
        private IMethod _StaticConstructor;
        public bool IsGenericTypeParameter => false;
        public IType DeclaringType { get; }
        public IType GenericDefinition { get; }
        public IType[] Types
        {
            get
            {
                if (this._Types == null)
                {
                    this._Types = new IType[this.Base.Types.Length];
                    for (int x = 0; x < this.Base.Types.Length; x++)
                        this._Types[x] = new MixedConstructedType(this.Base.Types[x], this);
                }
                return this._Types;
            }
        }
        private IType[] _Types;
        public IField[] Fields
        {
            get
            {
                if (this._Fields == null)
                {
                    this._Fields = new MixedConstructedField[this.Base.Fields.Length];
                    for (int x = 0; x < this.Base.Fields.Length; x++)
                        this._Fields[x] = new MixedConstructedField(this.Base.Fields[x], this);
                }
                return this._Fields;
            }
        }
        private IField[] _Fields;
        public IMethod[] Methods
        {
            get
            {
                if (this._Methods == null)
                {
                    this._Methods = new MixedConstructedMethod[this.Base.Methods.Length];
                    for (int x = 0; x < this.Base.Methods.Length; x++)
                        this._Methods[x] = new MixedConstructedMethod(this.Base.Methods[x], this);
                }
                return this._Methods;
            }
        }
        private IMethod[] _Methods;
        public IMethod[] Constructors
        {
            get
            {
                if (this._Constructors == null)
                {
                    this._Constructors = new MixedConstructedMethod[this.Base.Constructors.Length];
                    for (int x = 0; x < this.Base.Constructors.Length; x++)
                        this._Constructors[x] = new MixedConstructedMethod(this.Base.Constructors[x], this);
                }
                return this._Constructors;
            }
        }
        private IMethod[] _Constructors;
        public IProperty[] Properties
        {
            get
            {
                if (this._Properties == null)
                {
                    this._Properties = new MixedConstructedProperty[this.Base.Properties.Length];
                    for (int x = 0; x < this.Base.Properties.Length; x++)
                        this._Properties[x] = new MixedConstructedProperty(this.Base.Properties[x], this);
                }
                return this._Properties;
            }
        }
        private IProperty[] _Properties;
        public IEvent[] Events
        {
            get
            {
                if (this._Events == null)
                {
                    this._Events = new MixedConstructedEvent[this.Base.Events.Length];
                    for (int x = 0; x < this.Base.Events.Length; x++)
                        this._Events[x] = new MixedConstructedEvent(this.Base.Events[x], this);
                }
                return this._Events;
            }
        }
        private IEvent[] _Events;

        public ICustomAttribute[] CustomAttributes => this.Base.CustomAttributes;
        public int ArrayRank => 0;
        public bool IsArray => false;
        public bool IsByRef => false;
        public bool IsPointer => false;
        public bool IsSZArray => false;
        public bool IsPinned => false;

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
        IGeneric IGeneric.GenericDefinition => this.GenericDefinition;

        IGeneric IGeneric.ConstructGeneric(IType[] genericArguments) => this.ConstructGeneric(genericArguments);
        public IType ConstructGeneric(IType[] genericArguments)
        {
            if (!this.IsGenericDefinition)
                throw new InvalidOperationException();

            if (genericArguments.Length != this.GenericArguments.Length)
                throw new ArgumentOutOfRangeException(nameof(genericArguments), "invalid number of generic arguments");

            if (this.constructedTypes.TryGetValue(genericArguments, out MixedConstructedType type))
                return type;

            lock (this.constructedTypes)
            {
                if (this.constructedTypes.TryGetValue(genericArguments, out type))
                    return type;

                if (!AssemblyLoaderHelpers.ValidateGenericParameterConstraints(genericArguments, (IGenericParameter[])this.GenericArguments))
                    throw new ArgumentException("Generic Constraints not Met", nameof(genericArguments));

                type = new MixedConstructedType(this.Base, genericArguments, (MixedConstructedType)this.DeclaringType);
                this.constructedTypes.Add(genericArguments, type);
                return type;
            }
        }
        private readonly Dictionary<IType[], MixedConstructedType> constructedTypes;

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
        public IType GetElementType() => null;


        public string GetFullyQualifiedName()
        {
            if (this._FullyQualifiedName == null)
            {
                StringBuilder sb = new StringBuilder();
                Stack<IType> typeStack = new Stack<IType>();

                IType current = this;
                while (current != null)
                {
                    typeStack.Push(current);
                    current = current.DeclaringType;
                }

                current = typeStack.Pop();
                Queue<IType> generics = new Queue<IType>(current.GenericArguments);
                sb.Append(current.Namespace.GetFullyQualifiedName());
                sb.Append('.');
                sb.Append(current.Name);

                while (typeStack.Count > 0)
                {
                    current = typeStack.Pop();
                    for (int x = 0; x < current.GenericArguments.Length; x++)
                        generics.Enqueue(current.GenericArguments[x]);
                    sb.Append('+');
                    sb.Append(current.Name);
                }

                if (generics.Count > 0)
                {
                    sb.Append('[');
                    IType generic = generics.Dequeue();
                    sb.Append(generic.GetFullyQualifiedName());
                    while (generics.Count > 0)
                    {
                        sb.Append(',');
                        sb.Append(generic.GetFullyQualifiedName());
                    }
                    sb.Append(']');
                }
                this._FullyQualifiedName = sb.ToString();
            }
            return this._FullyQualifiedName;
        }
        private String _FullyQualifiedName;

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


        public Boolean IsStatic => false;
        public Boolean IsInterface => false;
        public Boolean IsEnum => this.Base.IsEnum;
        public Boolean IsValueType => this.Base.IsValueType;

        // TODO: [REF] Wrap Mixed BaseType & Interfaces Correctly

        // NULLABLE
        public IType BaseType => this.Base.BaseType;
        public IType[] Interfaces => this.Base.Interfaces;
        public Boolean IsCastableTo(IType type) => AssemblyLoaderHelpers.IsCastableTo(this, type);
    }
}
