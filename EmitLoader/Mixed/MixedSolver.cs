using System;
using System.Collections.Generic;
using System.Reflection;

namespace EmitLoader.Mixed
{
    internal class MixedSolver : IAssembly
    {
        public AssemblyName Name => null;
        public AssemblyLoader Context { get; }
        public AssemblyKind Kind => AssemblyKind.Mixed;

        public MixedSolver(AssemblyLoader Context) => this.Context = Context;

        public IEnumerable<INamespace> GetDefinedNamespaces() => throw new InvalidOperationException("Not A Real Assembly");
        public IEnumerable<IType> GetDefinedTypes() => throw new InvalidOperationException("Not A Real Assembly");
        public IType FindType(string Namespace, string TypeName) => throw new InvalidOperationException("Not A Real Assembly");
    }
}
