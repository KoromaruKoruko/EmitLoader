using System;

namespace EmitLoader.Reflection
{
    internal class ReflectionNamespace : INamespace
    {
        public ReflectionNamespace(String @namespace, ReflectionSolver assembly, INamespace[] ChildNamespaces, IType[] Types)
        {
            this.@namespace = @namespace;
            this.assembly = assembly;
            this.Types = Types;
            this.ChildNamespaces = ChildNamespaces;
        }
        private readonly String @namespace;
        private readonly ReflectionSolver assembly;

        public bool IsGlobalNamespace => this.ParentNamespace == null;

        public INamespace ParentNamespace { get; internal set; }

        public INamespace[] ChildNamespaces { get; }
        public IType[] Types { get; }
        public IType FindType(String TypeName)
        {
            foreach (IType type in Types)
                if (type.Name == TypeName)
                    return type;
            return null;
        }

        public string Name => this.@namespace.Substring(this.@namespace.LastIndexOf('.') + 1);

        public AssemblyObjectKind Kind => AssemblyObjectKind.Namespace;
        public AssemblyLoader Context => this.assembly.Context;
        public IAssembly Assembly => this.assembly;


        public string GetFullyQualifiedName() => this.@namespace;
    }
}
