using EmitLoader.Mixed;

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Metadata;
using System.Text;

namespace EmitLoader.Metadata
{
    internal abstract class MetadataTypeBase : IType
    {
        public virtual AssemblyObjectKind Kind => AssemblyObjectKind.Type;
        public AssemblyLoader Context => this.Assembly.Context;
        IAssembly IAssemblySolverObject.Assembly => this.Assembly;
        public abstract MetadataSolver Assembly { get; }
        public abstract string Name { get; }


        internal abstract TypeAttributes Attributes { get; }
        Type IType.GetBuiltType() =>
            this.Assembly.Loader == null
                ? throw new InvalidOperationException("Metadata Assembly not permited to build!")
                : this.BuildType();
        internal abstract Type BuildType();


        INamespace IType.Namespace => this.Namespace;
        public abstract MetadataNamespace Namespace { get; }

        IMethod IType.StaticConstructor => this.StaticConstructor;
        public abstract MetadataMethodBase StaticConstructor { get; }

        // NULLABLE
        IType IType.DeclaringType => this.DeclaringType;
        public abstract MetadataTypeBase DeclaringType { get; }

        // NULLABLE
        IGeneric IGeneric.GenericDefinition => this.GenericDefinition;
        IType IType.GenericDefinition => this.GenericDefinition;
        public abstract MetadataTypeBase GenericDefinition { get; }

        public abstract bool IsNestedType { get; }
        public abstract bool IsGenericTypeParameter { get; }
        public abstract bool IsGeneric { get; }
        public abstract bool IsGenericDefinition { get; }

        ICustomAttribute[] IType.CustomAttributes => this.CustomAttributes;
        public abstract MetadataCustomAttributeBase[] CustomAttributes { get; }

        IType[] IType.Types => this.Types;
        IField[] IType.Fields => this.Fields;
        IMethod[] IType.Methods => this.Methods;
        IMethod[] IType.Constructors => this.Constructors;
        IProperty[] IType.Properties => this.Properties;
        IEvent[] IType.Events => this.Events;



        public abstract MetadataTypeBase[] Types { get; }
        public abstract MetadataFieldBase[] Fields { get; }
        public abstract MetadataMethodBase[] Methods { get; }
        public abstract MetadataMethodBase[] Constructors { get; }
        public abstract MetadataPropertyBase[] Properties { get; }
        public abstract MetadataEventBase[] Events { get; }
        public abstract IType[] GenericArguments { get; }


        public Int32 ArrayRank => 0;
        public Boolean IsArray => false;
        public Boolean IsByRef => false;
        public Boolean IsPointer => false;
        public Boolean IsSZArray => false;
        public Boolean IsPinned => false;

        public IType MakeArrayType(ArrayShape shape) =>
            this.IsGenericTypeParameter
                ? throw new InvalidOperationException()
                : new MixedArrayType(this, shape);
        public IType MakeByRefType()
        {
            if (this.IsGenericTypeParameter) throw new InvalidOperationException();

            if (this._ByRefType == null)
                this._ByRefType = new MixedByRefType(this);
            return this._ByRefType;
        }
        private IType _ByRefType;
        public IType MakePointerType()
        {
            if (this.IsGenericTypeParameter) throw new InvalidOperationException();

            if (this._PointerType == null)
                this._PointerType = new MixedPointerType(this);
            return this._PointerType;
        }
        private IType _PointerType;
        public IType MakeSZArrayType()
        {
            if (this.IsGenericTypeParameter) throw new InvalidOperationException();

            if (this._SZArrayType == null)
                this._SZArrayType = new MixedArrayType(this);
            return this._SZArrayType;
        }
        private IType _SZArrayType;
        public IType GetElementType() => throw new InvalidOperationException();

        IGeneric IGeneric.ConstructGeneric(IType[] genericArguments) => this.ConstructGeneric(genericArguments);
        IType IType.ConstructGeneric(IType[] genericArguments) => this.ConstructGeneric(genericArguments);
        public abstract MetadataTypeBase ConstructGeneric(IType[] genericArguments);

        public string GetFullyQualifiedName()
        {
            if (this._FullyQualifiedName == null)
            {
                StringBuilder sb = new StringBuilder();
                GetFullyQualifiedName(sb);
                this._FullyQualifiedName = sb.ToString();
            }
            return this._FullyQualifiedName;
        }
        private string _FullyQualifiedName;
        public virtual void GetFullyQualifiedName(StringBuilder sb)
        {
            // System.Collections.Generic.Dictionary`2+Enumerator[System.String,System.String]
            // {Namespace}.{ParentType}+{ChildType}[Generics]

            Stack<MetadataTypeBase> typeStack = new Stack<MetadataTypeBase>();

            MetadataTypeBase current = this;
            while (current != null)
            {
                typeStack.Push(current);
                current = current.DeclaringType;
            }

            current = typeStack.Pop();
            Queue<IType> generics = new Queue<IType>(current.GenericArguments);
            if (!current.Namespace.IsGlobalNamespace)
            {
                current.Namespace.GetFullyQualifiedName(sb);
                sb.Append('.');
            }
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
        }

        public IType FindType(string Name)
        {
            foreach (MetadataTypeBase type in this.Types)
                if (type.Name == Name)
                    return type;
            return null;
        }
        public IField FindField(string Name)
        {
            foreach (MetadataFieldBase field in this.Fields)
                if (field.Name == Name)
                    return field;
            return null;
        }
        public IMethod FindMethod(string Name, IType[] ParameterTypes)
        {
            foreach (MetadataMethodBase method in this.Methods)
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
            foreach (MetadataMethodBase method in this.Constructors)
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
            foreach (MetadataPropertyBase property in this.Properties)
                if (property.Name == Name)
                    return property;
            return null;
        }
        public IEvent FindEvent(string Name)
        {
            foreach (MetadataEventBase @event in this.Events)
                if (@event.Name == Name)
                    return @event;
            return null;
        }


        public abstract Boolean IsStatic { get; }
        public Boolean IsInterface => this.BaseType == null && !this.IsStatic;
        public Boolean IsEnum => this.BaseType == this.Assembly.EnumType || (this.BaseType?.IsEnum ?? false);
        public Boolean IsValueType => this.BaseType == this.Assembly.ValueType || (this.BaseType?.IsValueType ?? false);

        // NULLABLE
        public abstract IType BaseType { get; }
        public abstract IType[] Interfaces { get; }
        public Boolean IsCastableTo(IType type) => AssemblyLoaderHelpers.IsCastableTo(this, type);
    }
}
