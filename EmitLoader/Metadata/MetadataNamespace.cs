
using System;
using System.Collections.Immutable;
using System.Reflection.Metadata;
using System.Text;

namespace EmitLoader.Metadata
{
    internal class MetadataNamespace : INamespace
    {
        public AssemblyObjectKind Kind => AssemblyObjectKind.Namespace;
        public AssemblyLoader Context => this.Assembly.Context;
        IAssembly IAssemblySolverObject.Assembly => this.Assembly;
        public MetadataSolver Assembly { get; }

        public bool IsGlobalNamespace { get; }

        public string Name
        {
            get
            {
                if (this._Name == null)
                    this._Name = this.Assembly.MD.GetString(this.Def.Name);
                return this._Name;
            }
        }
        private string _Name;

        // NULLABLE
        INamespace INamespace.ParentNamespace => this.ParentNamespace;
        public INamespace ParentNamespace
        {
            get
            {
                if (this._ParentNamespace == null && !this.IsGlobalNamespace)
                    this._ParentNamespace = this.Assembly.GetNamespace(this.Def.Parent);
                return this._ParentNamespace;
            }
        }
        private MetadataNamespace _ParentNamespace;

        INamespace[] INamespace.ChildNamespaces => this.ChildNamespaces;
        public MetadataNamespace[] ChildNamespaces
        {
            get
            {
                if (this._ChildNamespaces == null)
                {
                    ImmutableArray<NamespaceDefinitionHandle> namespaceDefs = this.Def.NamespaceDefinitions;
                    this._ChildNamespaces = new MetadataNamespace[namespaceDefs.Length];
                    for (int x = 0; x < namespaceDefs.Length; x++)
                        this._ChildNamespaces[x] = this.Assembly.GetNamespace(namespaceDefs[x]);
                }
                return this._ChildNamespaces;
            }
        }
        private MetadataNamespace[] _ChildNamespaces;

        IType[] INamespace.Types => this.Types;
        public MetadataType[] Types
        {
            get
            {
                if (this._Types == null)
                {
                    ImmutableArray<TypeDefinitionHandle> typeDefs = this.Def.TypeDefinitions;
                    this._Types = new MetadataType[typeDefs.Length];
                    for (int x = 0; x < typeDefs.Length; x++)
                        this._Types[x] = this.Assembly.GetTypeDefinition(typeDefs[x]);
                }
                return this._Types;
            }
        }
        private MetadataType[] _Types;

        public IType FindType(String TypeName)
        {
            foreach (MetadataType type in this.Types)
                if (type.Name == TypeName)
                    return type;
            return null;
        }

        internal MetadataNamespace(NamespaceDefinition Def, MetadataSolver Assembly, Boolean IsGlobalNamespace)
        {
            this.Def = Def;
            this.Assembly = Assembly;
            this.IsGlobalNamespace = IsGlobalNamespace;
        }
        private NamespaceDefinition Def;

        public string GetFullyQualifiedName()
        {
            StringBuilder sb = new StringBuilder();
            GetFullyQualifiedName(sb);
            return sb.ToString();
        }
        internal void GetFullyQualifiedName(StringBuilder sb)
        {
            if (this.ParentNamespace != null && !this.ParentNamespace.IsGlobalNamespace)
            {
                this._ParentNamespace.GetFullyQualifiedName(sb);
                sb.Append('.');
            }
            sb.Append(this.Name);
        }
    }
}
