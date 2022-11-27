using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace EmitLoader
{
    /// <summary>
    /// The Access Control Manager that wraps an <see cref="IAccessController"/>
    /// </summary>
    public sealed class AccessControlManager
    {
        private class Comparer :
            IComparer<EventInfo>,
            IComparer<PropertyInfo>,
            IComparer<FieldInfo>,
            IComparer<ConstructorInfo>,
            IComparer<MethodInfo>,
            IComparer<Type>,
            IComparer<Assembly>,
            IComparer<String>
        {
            public static readonly Comparer Instance = new Comparer();

            public int Compare(string x, string y) => x.CompareTo(y);
            public int Compare(Assembly x, Assembly y) => Compare(x.GetName().FullName, y.GetName().FullName);
            public int Compare(Type x, Type y) => x.MetadataToken - y.MetadataToken;
            public int Compare(MethodInfo x, MethodInfo y) => x.MetadataToken - y.MetadataToken;
            public int Compare(ConstructorInfo x, ConstructorInfo y) => x.MetadataToken - y.MetadataToken;
            public int Compare(FieldInfo x, FieldInfo y) => x.MetadataToken - y.MetadataToken;
            public int Compare(PropertyInfo x, PropertyInfo y) => x.MetadataToken - y.MetadataToken;
            public int Compare(EventInfo x, EventInfo y) => x.MetadataToken - y.MetadataToken;
        }

        private readonly IAccessController Controller;
        internal AccessControlManager(IAccessController Controller)
        {
            this.Controller = Controller;
        }

        private readonly List<AssemblyBuilder> loaderAssemblies = new List<AssemblyBuilder>();
        internal void RegisterLoader(AssemblyBuilder asmBuilder) => this.loaderAssemblies.Add(asmBuilder);
        

        private readonly SortedDictionary<Assembly, AccessKind> _Assemblies = new SortedDictionary<Assembly, AccessKind>(Comparer.Instance);
        private readonly SortedDictionary<string, AccessKind> _Namespaces = new SortedDictionary<string, AccessKind>(Comparer.Instance);
        private readonly SortedDictionary<Type, AccessKind> _Types = new SortedDictionary<Type, AccessKind>(Comparer.Instance);
        private readonly SortedDictionary<MethodInfo, AccessKind> _Methods = new SortedDictionary<MethodInfo, AccessKind>(Comparer.Instance);
        private readonly SortedDictionary<ConstructorInfo, AccessKind> _Constructors = new SortedDictionary<ConstructorInfo, AccessKind>(Comparer.Instance);
        private readonly SortedDictionary<FieldInfo, AccessKind> _Fields = new SortedDictionary<FieldInfo, AccessKind>(Comparer.Instance);

        private readonly SortedDictionary<FieldInfo, Boolean> _FieldGet = new SortedDictionary<FieldInfo, bool>(Comparer.Instance);
        private readonly SortedDictionary<FieldInfo, Boolean> _FieldSet = new SortedDictionary<FieldInfo, bool>(Comparer.Instance);

        private AccessKind GetAccess(Assembly assembly)
        {
            if(assembly is AssemblyBuilder builder)
                if (loaderAssemblies.Contains(builder))
                    return AccessKind.Full;

            if (this._Assemblies.TryGetValue(assembly, out AccessKind access))
                return access;
            lock (this._Assemblies)
            {
                if (this._Assemblies.TryGetValue(assembly, out access))
                    return access;

                access = this.Controller.CanAccess(assembly);
                this._Assemblies.Add(assembly, access);
                return access;
            }
        }
        private AccessKind GetAccess(string @namespace)
        {
            if (this._Namespaces.TryGetValue(@namespace, out AccessKind access))
                return access;
            lock (this._Namespaces)
            {
                if (this._Namespaces.TryGetValue(@namespace, out access))
                    return access;

                access = this.Controller.CanAccess(@namespace);
                this._Namespaces.Add(@namespace, access);
                return access;
            }
        }
        private AccessKind GetAccess(Type type)
        {
            if (this._Types.TryGetValue(type, out AccessKind access))
                return access;
            lock (this._Types)
            {
                if (this._Types.TryGetValue(type, out access))
                    return access;

                access = this.Controller.CanAccess(type);
                this._Types.Add(type, access);
                return access;
            }
        }
        private AccessKind GetAccess(MethodInfo method)
        {
            if (this._Methods.TryGetValue(method, out AccessKind access))
                return access;
            lock (this._Methods)
            {
                if (this._Methods.TryGetValue(method, out access))
                    return access;

                access = this.Controller.CanAccess(method);
                this._Methods.Add(method, access);
                return access;
            }
        }
        private AccessKind GetAccess(ConstructorInfo constructor)
        {
            if (this._Constructors.TryGetValue(constructor, out AccessKind access))
                return access;
            lock (this._Constructors)
            {
                if (this._Constructors.TryGetValue(constructor, out access))
                    return access;

                access = this.Controller.CanAccess(constructor);
                this._Constructors.Add(constructor, access);
                return access;
            }
        }
        private AccessKind GetAccess(FieldInfo field)
        {
            if (this._Fields.TryGetValue(field, out AccessKind access))
                return access;
            lock (this._Fields)
            {
                if (this._Fields.TryGetValue(field, out access))
                    return access;

                access = this.Controller.CanAccess(field);
                this._Fields.Add(field, access);
                return access;
            }
        }

        private AccessKind VGetAccess(Type type)
        {
            AccessKind Access = GetAccess(type.Assembly);
            if(Access == AccessKind.None)
                throw new EmitLoaderAccessViolationException(type.Assembly.GetName().FullName);
            if (Access == AccessKind.Full)
                return AccessKind.Full;

            if (!String.IsNullOrEmpty(type.Namespace))
            {
                Access = GetAccess(type.Namespace);
                if (Access == AccessKind.None)
                    throw new EmitLoaderAccessViolationException(type.Namespace);
                if (Access == AccessKind.Full)
                    return AccessKind.Full;
            }

            if (type.IsNested)
            {
                Stack<Type> tstack = new Stack<Type>();
                do
                {
                    tstack.Push(type);
                    type = type.DeclaringType;
                } while (type.IsNested);

                do
                {
                    Access = GetAccess(type);
                    if (Access == AccessKind.None)
                        throw new EmitLoaderAccessViolationException(type.FullName);
                    if (Access == AccessKind.Full)
                        return AccessKind.Full;
                    type = tstack.Pop();
                } while (tstack.Count > 0);
            }
            else
            {
                Access = GetAccess(type);
                return Access == AccessKind.None
                    ? throw new EmitLoaderAccessViolationException(type.FullName)
                    : Access;
            }
            return AccessKind.Partial;
        }

        /// <summary>
        /// Validates Access to an Assembly
        /// </summary>
        /// <exception cref="EmitLoaderAccessViolationException"></exception>
        public void ValidateAccess(Assembly assembly)
        {
            if (GetAccess(assembly) == AccessKind.None)
                throw new EmitLoaderAccessViolationException(assembly.GetName().FullName);
        }
        /// <summary>
        /// Validates Access to a Type
        /// </summary>
        /// <exception cref="EmitLoaderAccessViolationException"></exception>
        public void ValidateAccess(Type type) => VGetAccess(type);
        /// <summary>
        /// Validates Access to a Method
        /// </summary>
        /// <exception cref="EmitLoaderAccessViolationException"></exception>
        public void ValidateAccess(MethodInfo method)
        {
            AccessKind Access = VGetAccess(method.DeclaringType);
            if (Access == AccessKind.Full)
                return;
            
            Access = GetAccess(method);
            if (Access == AccessKind.None)
                throw new EmitLoaderAccessViolationException($"{method.DeclaringType.FullName}.{method.Name}({String.Join(",", method.GetParameters().Select((param) => param.ParameterType.FullName))})");
        }
        /// <summary>
        /// Validates Access to a Construactor
        /// </summary>
        /// <exception cref="EmitLoaderAccessViolationException"></exception>
        public void ValidateAccess(ConstructorInfo constructor)
        {
            AccessKind Access = VGetAccess(constructor.DeclaringType);
            if (Access == AccessKind.Full)
                return;

            Access = GetAccess(constructor);
            if (Access == AccessKind.None)
                throw new EmitLoaderAccessViolationException($"{constructor.DeclaringType.FullName}({String.Join(",", constructor.GetParameters().Select((param) => param.ParameterType.FullName))})");
        }
        /// <summary>
        /// Validates Set Access to a Field
        /// </summary>
        /// <exception cref="EmitLoaderAccessViolationException"></exception>
        public void ValidateAccess_set(FieldInfo field)
        {
            AccessKind Access = VGetAccess(field.DeclaringType);
            if (Access == AccessKind.Full)
                return;

            Access = GetAccess(field);
            if (Access == AccessKind.None)
                throw new EmitLoaderAccessViolationException($"{field.DeclaringType.FullName}.{field.Name}");
            if (Access == AccessKind.Full)
                return;

            if (this._FieldSet.TryGetValue(field, out Boolean canAccess))
                if (!canAccess) throw new EmitLoaderAccessViolationException($"{field.DeclaringType.FullName}.{field.Name}..set");
                else return;
            lock(this._FieldSet)
            {
                if (this._FieldSet.TryGetValue(field, out canAccess))
                    if (!canAccess) throw new EmitLoaderAccessViolationException($"{field.DeclaringType.FullName}.{field.Name}..set");
                    else return;

                canAccess = this.Controller.CanSet(field);
                this._FieldSet.Add(field, canAccess);
                if (!canAccess) throw new EmitLoaderAccessViolationException($"{field.DeclaringType.FullName}.{field.Name}..set");
            }
        }
        /// <summary>
        /// Validates Get Access to a Field
        /// </summary>
        /// <exception cref="EmitLoaderAccessViolationException"></exception>
        public void ValidateAccess_get(FieldInfo field)
        {
            AccessKind Access = VGetAccess(field.DeclaringType);
            if (Access == AccessKind.Full)
                return;

            Access = GetAccess(field);
            if (Access == AccessKind.None)
                throw new EmitLoaderAccessViolationException($"{field.DeclaringType.FullName}.{field.Name}");
            if (Access == AccessKind.Full)
                return;

            if (this._FieldGet.TryGetValue(field, out Boolean canAccess))
                if (!canAccess) throw new EmitLoaderAccessViolationException($"{field.DeclaringType.FullName}.{field.Name}..get");
                else return;
            lock (this._FieldGet)
            {
                if (this._FieldGet.TryGetValue(field, out canAccess))
                    if (!canAccess) throw new EmitLoaderAccessViolationException($"{field.DeclaringType.FullName}.{field.Name}..get");
                    else return;

                canAccess = this.Controller.CanGet(field);
                this._FieldGet.Add(field, canAccess);
                if (!canAccess) throw new EmitLoaderAccessViolationException($"{field.DeclaringType.FullName}.{field.Name}..get");
            }
        }
    }

    /// <summary>
    /// thrown when loading an assembly and that assembly Access a restricted Object
    /// </summary>
    public class EmitLoaderAccessViolationException : Exception
    {
        /// <summary>
        /// Fully Qualified Identifier
        /// </summary>
        public String FullyQualifiedIdentifier { get; }

        /// <inheritdoc cref="EmitLoaderAccessViolationException"/>
        /// <param name="FullyQualifiedIdentifier">Fully Qualified Identifier</param>
        public EmitLoaderAccessViolationException(String FullyQualifiedIdentifier) : base($"Access Violation! Attempted to access {FullyQualifiedIdentifier}") => this.FullyQualifiedIdentifier = FullyQualifiedIdentifier;
        
    }
}
