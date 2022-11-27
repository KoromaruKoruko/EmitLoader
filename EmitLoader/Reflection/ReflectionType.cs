using EmitLoader.Mixed;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Text;

namespace EmitLoader.Reflection
{
    internal class ReflectionType : IType
    {
        internal const BindingFlags SearchFlags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic;

        public ReflectionType(Type rawType, ReflectionSolver assembly)
        {
            this.type = rawType;
            this.assembly = assembly;
            this.DeclaringType = null;

            if (this.IsGenericDefinition)
                this.constructedLookup = new Dictionary<IType[], IType>();
        }
        public ReflectionType(Type rawType, ReflectionSolver assembly, ReflectionType Parent)
        {
            this.type = rawType;
            this.assembly = assembly;
            this.DeclaringType = Parent;

            if (this.IsGenericDefinition)
                this.constructedLookup = new Dictionary<IType[], IType>();
        }
        internal readonly Type type;
        internal readonly ReflectionSolver assembly;

        public Type GetBuiltType() => this.type;

        public INamespace Namespace
        {
            get
            {
                if (this._Namespace == null)
                    this._Namespace = this.assembly.GetNamespace(this.type.Namespace);
                return this._Namespace;
            }
        }
        private INamespace _Namespace;
        public bool IsNestedType => this.type.IsNested;

        public bool IsGenericTypeParameter => this.type.IsGenericParameter;

        public IType DeclaringType { get; }

        public IMethod StaticConstructor
        {
            get
            {
                if (this._StaticConstructor == null)
                    this._StaticConstructor = new ReflectionMethod(this.type.GetMethod(".cctor"), this, null);
                return this._StaticConstructor;
            }
        }
        private IMethod _StaticConstructor;

        public IType GenericDefinition
        {
            get
            {
                if (this.IsGeneric && this._GenericDefinition == null)
                    this._GenericDefinition = this.assembly.GetType(this.type.GetGenericTypeDefinition());
                return this._GenericDefinition;
            }
        }
        private IType _GenericDefinition;

        public IType[] Types
        {
            get
            {
                if (this._Types == null)
                {
                    Type[] types = this.type.GetNestedTypes(SearchFlags);
                    this._Types = new IType[types.Length];
                    for (int x = 0; x < types.Length; x++)
                        this._Types[x] = new ReflectionType(types[x], assembly, this);
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
                    FieldInfo[] fields = this.type.GetFields(SearchFlags);
                    this._Fields = new IField[fields.Length];
                    for (int x = 0; x < fields.Length; x++)
                        this._Fields[x] = new ReflectionField(fields[x], this);
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
                    MethodInfo[] methods = this.type.GetMethods(SearchFlags);
                    this._Methods = new IMethod[methods.Length];
                    for (int x = 0; x < methods.Length; x++)
                        this._Methods[x] = new ReflectionMethod(methods[x], this, null);
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
                    ConstructorInfo[] constructors = this.type.GetConstructors(SearchFlags);
                    this._Constructors = new IMethod[constructors.Length];
                    for (int x = 0; x < constructors.Length; x++)
                        this._Constructors[x] = new ReflectionMethod(constructors[x], this);
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
                    PropertyInfo[] properties = this.type.GetProperties(SearchFlags);
                    this._Properties = new IProperty[properties.Length];
                    for (int x = 0; x < properties.Length; x++)
                        this._Properties[x] = new ReflectionProperty(properties[x], this);
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
                    EventInfo[] events = this.type.GetEvents(SearchFlags);
                    this._Events = new IEvent[events.Length];
                    for (int x = 0; x < events.Length; x++)
                        this._Events[x] = new ReflectionEvent(events[x], this);
                }
                return this._Events;
            }
        }
        private IEvent[] _Events;



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
                if (ParameterTypes.Length == method.Parameters.Length)
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




        public ICustomAttribute[] CustomAttributes
        {
            get
            {
                if (this._CustomAttributes == null)
                {
                    IList<CustomAttributeData> attributes = this.type.GetCustomAttributesData();
                    this._CustomAttributes = new ReflectionCustomAttribute[attributes.Count];
                    for (int x = 0; x < attributes.Count; x++)
                        this._CustomAttributes[x] = new ReflectionCustomAttribute(attributes[x], this.assembly);
                }
                return this._CustomAttributes;
            }
        }
        private ICustomAttribute[] _CustomAttributes;

        public int ArrayRank => this.type.GetArrayRank();
        public bool IsArray => this.type.IsArray;
        public bool IsByRef => this.type.IsByRef;
        public bool IsPointer => this.type.IsPointer;
        public bool IsSZArray => this.ArrayRank == 1;
        public bool IsPinned => false;

        public string Name => this.type.Name;

        public bool IsGeneric => this.type.IsGenericType;
        public bool IsGenericDefinition => this.type.IsGenericTypeDefinition;

        public IType[] GenericArguments
        {
            get
            {
                if (this._GenericArguments == null)
                {
                    if (this.IsGeneric)
                        if (this.IsGenericDefinition)
                        {
                            Type[] @params = this.type.GetGenericArguments();
                            this._GenericArguments = new IGenericParameter[@params.Length];
                            for (int x = 0; x < @params.Length; x++)
                                this._GenericArguments[x] = new ReflectionGenericParameter(@params[x], this);
                        }
                        else
                        {
                            Type[] args = this.type.GetGenericArguments();
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
        public AssemblyObjectKind Kind => AssemblyObjectKind.Type;
        public AssemblyLoader Context => this.assembly.Context;
        public IAssembly Assembly => this.assembly;
        IGeneric IGeneric.GenericDefinition => this.GenericDefinition;

        public IType ConstructGeneric(IType[] genericArguments)
        {
            if (!this.IsGenericDefinition)
                throw new InvalidOperationException();

            if (genericArguments.Length != this.GenericArguments.Length)
                throw new ArgumentOutOfRangeException(nameof(genericArguments), "invalid number of generic arguments");

            if (this.constructedLookup.TryGetValue(genericArguments, out IType constructedType))
                return constructedType;

            lock (this.constructedLookup)
            {
                if (this.constructedLookup.TryGetValue(genericArguments, out constructedType))
                    return constructedType;

                if (genericArguments.All((t) => t is ReflectionType))
                {
                    Type[] generics = new Type[genericArguments.Length];
                    for (int x = 0; x < genericArguments.Length; x++)
                        generics[x] = ((ReflectionType)genericArguments[x]).type;

                    constructedType = new ReflectionType(this.type.MakeGenericType(generics), this.assembly);
                }
                else
                {
                    if (!AssemblyLoaderHelpers.ValidateGenericParameterConstraints(genericArguments, (IGenericParameter[])this.GenericArguments))
                        throw new ArgumentException("Generic Constraints not Met", nameof(genericArguments));

                    constructedType = new MixedConstructedType(this, genericArguments);
                }
                this.constructedLookup.Add(genericArguments, constructedType);
                return constructedType;
            }
        }
        private Dictionary<IType[], IType> constructedLookup;
        IGeneric IGeneric.ConstructGeneric(IType[] genericArguments) => this.ConstructGeneric(genericArguments);

        public IType GetElementType()
        {
            if (this._ElementType == null)
                this._ElementType = this.Context.ResolveType(this.type.GetElementType());
            return this._ElementType;
        }
        private IType _ElementType;

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
                this._ByRefType = new ReflectionType(this.type.MakeByRefType(), this.assembly);
            return this._ByRefType;
        }
        private IType _ByRefType;
        public IType MakePointerType()
        {
            if (this._PointerType == null)
                this._PointerType = new ReflectionType(this.type.MakePointerType(), this.assembly);
            return this._PointerType;
        }
        private IType _PointerType;
        public IType MakeSZArrayType()
        {
            if (this._SZArrayType == null)
                this._SZArrayType = new ReflectionType(this.type.MakeArrayType(), this.assembly);
            return this._SZArrayType;
        }
        private IType _SZArrayType;



        public Boolean IsStatic => this.type.IsAbstract && this.type.IsSealed;
        public Boolean IsInterface => this.type.IsInterface;
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
