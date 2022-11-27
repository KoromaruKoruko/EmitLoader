using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace EmitLoader.Reflection
{
    internal class ReflectionSolver : IAssembly
    {
        public AssemblyName Name => Assembly.GetName();
        public AssemblyLoader Context { get; }
        public AssemblyKind Kind => AssemblyKind.Reflection;

        internal Assembly Assembly { get; }
        public INamespace GlobalNamespace { get; }

        private class NamespaceContainer
        {
            public String Name;
            public NamespaceContainer Parent;
            public SortedDictionary<String, NamespaceContainer> Children = new SortedDictionary<String, NamespaceContainer>();
            public List<IType> Types = new List<IType>();
        }

        public ReflectionSolver(Assembly Assembly, AssemblyLoader Context)
        {
            this.Context = Context;
            this.Assembly = Assembly;

            // full defined type scan
            // we need to build 
            NamespaceContainer Global = new NamespaceContainer
            {
                Name = String.Empty,
                Children = new SortedDictionary<string, NamespaceContainer>()
            };
            SortedDictionary<String, NamespaceContainer> lookupContainer = new SortedDictionary<String, NamespaceContainer>();
            foreach (Type type in Assembly.GetTypes())
            {
                if (type.IsNested)
                    continue; // ignore nested types

                IType refType = new ReflectionType(type, this);

                if (type.Namespace == null)
                {
                    Global.Types.Add(refType);
                    continue;
                }

                if (lookupContainer.TryGetValue(type.Namespace, out NamespaceContainer container))
                    container.Types.Add(refType);
                else
                {
                    String[] segs = type.Namespace.Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
                    if (!Global.Children.TryGetValue(segs[0], out container))
                    {
                        container = new NamespaceContainer
                        {
                            Name = segs[0]
                        };
                        Global.Children.Add(segs[0], container);
                    }

                    for (int y = 1; y < segs.Length; y++)
                        if (container.Children.TryGetValue(segs[y], out NamespaceContainer child))
                            container = child;
                        else
                        {
                            child = new NamespaceContainer
                            {
                                Name = segs[y],
                                Parent = container
                            };
                            container.Children.Add(segs[y], child);
                            container = child;
                        }

                    lookupContainer.Add(type.Namespace, container);
                    container.Types.Add(refType);
                }
            }

            ReflectionNamespace createNamespace(NamespaceContainer Container, String Prefix, Boolean HasParent = false)
            {
                if (HasParent)
                    Prefix += "." + Container.Name;
                else
                    Prefix = Container.Name;

                ReflectionNamespace[] Children = new ReflectionNamespace[Container.Children.Count];
                int y = 0;
                foreach (NamespaceContainer child in Container.Children.Values)
                    Children[y++] = createNamespace(child, Prefix, true);

                ReflectionNamespace newNamespace = new ReflectionNamespace(Prefix, this, Children, Container.Types.ToArray());
                this.namespaceLookup.Add(Prefix, newNamespace);

                for (y = 0; y < Children.Length; y++)
                    Children[y].ParentNamespace = newNamespace;

                return newNamespace;
            }

            ReflectionNamespace[] children = new ReflectionNamespace[Global.Children.Count];
            int x = 0;
            foreach (NamespaceContainer child in Global.Children.Values)
                children[x++] = createNamespace(child, string.Empty, false);
            this.GlobalNamespace = new ReflectionNamespace(string.Empty, this, children, Global.Types.ToArray());
            for (x = 0; x < children.Length; x++)
                children[x].ParentNamespace = this.GlobalNamespace;

            this.namespaceLookup.Add(string.Empty, this.GlobalNamespace);
        }

        public IEnumerable<INamespace> GetDefinedNamespaces()
        {
            foreach (INamespace @namespace in namespaceLookup.Values)
                yield return @namespace;
        }
        public IEnumerable<IType> GetDefinedTypes()
        {
            Queue<INamespace> queue = new Queue<INamespace>();
            foreach (INamespace @namespace in this.namespaceLookup.Values)
                queue.Enqueue(@namespace);

            while (queue.Count > 0)
            {
                INamespace @namespace = queue.Dequeue();
                foreach (INamespace child in @namespace.ChildNamespaces)
                    queue.Enqueue(child);
                foreach (IType type in @namespace.Types)
                    yield return type;
            }
        }

        public INamespace GetNamespace(String Namespace) =>
            this.namespaceLookup.TryGetValue(Namespace, out INamespace @namespace)
            ? @namespace
            : null;

        private readonly SortedDictionary<String, INamespace> namespaceLookup = new SortedDictionary<String, INamespace>();

        public IType GetType(Type rawType)
        {
            if (rawType == null)
                return null;
            if (rawType.IsNested)
            {
                IType Parent = GetType(rawType.DeclaringType);
                return Parent.Types.First((type) => type.Name == rawType.Name);
            }
            else
                return FindType(rawType.Namespace ?? String.Empty, rawType.Name);
        }
        public IType FindType(string Namespace, string TypeName) => this.GetNamespace(Namespace)?.FindType(TypeName);
    }
}
