using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

using EmitLoader.Metadata;
using EmitLoader.Mixed;
using EmitLoader.Reflection;

namespace EmitLoader
{
    /// <summary>
    /// Assembly Rsolver, You Must call LoadAssembly and return its return.
    /// </summary>
    /// <param name="Name">Assembly Name to Resolve</param>
    public delegate IAssembly ResolveAssemblyDelegate(AssemblyName Name);
    /// <summary>
    /// Assembly Loader Context
    /// </summary>
    public sealed class AssemblyLoader
    {
        /// <summary>
        /// Mixed Solver Instance (Acts as a Psudo Assembly)
        /// </summary>
        public IAssembly MixedSolver { get; }
        /// <summary>
        /// Assembly Resolver (Called When this is Unable to Resolve a Reference to an existing Assembly within the AppDomain).
        /// </summary>
        public ResolveAssemblyDelegate AssemblyResolver = NoLoad;
        private static IAssembly NoLoad(AssemblyName Name) => null;

        /// <inheritdoc cref="AssemblyLoader"/>
        public AssemblyLoader()
        {
            this.MixedSolver = new MixedSolver(this);
        }

        /// <summary>
        /// Get/Resolves an Assembly By Name
        /// </summary>
        /// <param name="name">Assembly Name</param>
        /// <exception cref="CrossOriginException">If the <see cref="AssemblyResolver"/> returns an Assembly from a different Context</exception>
        /// <exception cref="FailedToResolveAssemblyException">If <see cref="AssemblyResolver"/> returns null</exception>
        public IAssembly GetAssembly(AssemblyName name)
        {
            if(this.assemblyLookup.TryGetValue(name.Name, out IAssembly assembly))
                return assembly;
            lock (this.assemblyLookup)
            {
                if (this.assemblyLookup.TryGetValue(name.Name, out assembly))
                    return assembly;

                foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
                    if (asm.GetName().Name == name.Name)
                        return LoadAssembly(asm);;

                assembly = AssemblyResolver(name);
                return assembly == null
                    ? throw new FailedToResolveAssemblyException(name)
                    : assembly.Context != this
                        ? throw new CrossOriginException()
                        : assembly;
            }
        }

        /// <summary>
        /// Resolves / Wraps a Type
        /// </summary>
        /// <param name="type">Type</param>
        public IType ResolveType(Type type)
        {
            if(type.IsNested)
                return this.ResolveType(type.DeclaringType).FindType(type.Name);
            else
            {
                IAssembly asm = GetAssembly(type.Assembly.GetName());
                return asm is ReflectionSolver refAsm
                    ? refAsm.GetType(type)
                    : asm.FindType(type.Namespace, type.Name);
            }
        }

        /// <summary>
        /// Load a Metadata Assembly into this Load Context
        /// </summary>
        /// <param name="STM">Raw DLL File Stream</param>
        /// <exception cref="AssemblyAllreadExists">If AssemblyName allready Exists within this LoadContext</exception>
        public IMetadataAssembly LoadAssembly(Stream STM)
        {
            lock (this.assemblyLookup)
            {
                IMetadataAssembly assembly = new MetadataSolver(this, STM);
                if (this.assemblyLookup.ContainsKey(assembly.Name.Name))
                    throw new AssemblyAllreadExists(assembly.Name);

                this.assemblyLookup.Add(assembly.Name.Name, assembly);
                return assembly;
            }
        }
        /// <summary>
        /// Load a Reflection Assembly into this Load Context
        /// </summary>
        /// <param name="Asm">Reflection Assembly</param>
        /// <exception cref="AssemblyAllreadExists">If AssemblyName allready Exists</exception>
        public IAssembly LoadAssembly(Assembly Asm)
        {
            if (this.assemblyLookup.TryGetValue(Asm.GetName().Name, out IAssembly assembly))
                return assembly is ReflectionSolver
                    ? assembly
                    : throw new AssemblyAllreadExists(Asm.GetName());

            lock (this.assemblyLookup)
            {
                if (this.assemblyLookup.TryGetValue(Asm.GetName().Name, out assembly))
                    return assembly is ReflectionSolver
                        ? assembly
                        : throw new AssemblyAllreadExists(Asm.GetName());

                assembly = new ReflectionSolver(Asm, this);
                this.assemblyLookup.Add(Asm.GetName().Name, assembly);
                return assembly;
            }

        }
        private readonly SortedDictionary<String, IAssembly> assemblyLookup = new SortedDictionary<String, IAssembly>();

        /// <summary>
        /// Note once your desired asemblies are built, you can use the GetBuilt*() Methods to retrive a Standard Reflection Object.
        /// you should then discard this AssemblyLoader in its entirety. Remove all references to this class, and any Psudo-Reflection Type, it is a memory hog.
        /// </summary>
        /// <param name="AccessController">Access Controller</param>
        public void BuildMetadataAssemblies(IAccessController AccessController)
        {
            List<IMetadataAssembly> MetadataAssemblies = new List<IMetadataAssembly>();
            AccessControlManager ACM = new AccessControlManager(AccessController);
            foreach(IAssembly asm in this.assemblyLookup.Values)
                if (asm is IMetadataAssembly metaAssembly)
                {
                    metaAssembly.PrepBuild(ACM);
                    MetadataAssemblies.Add(metaAssembly);
                }

            foreach (IMetadataAssembly asm in MetadataAssemblies)
                asm.GetBuiltAssembly();
        }
    }

    /// <summary>
    /// Thrown when an AssemblySolver is Provided from a Different AssemblyLoadContext
    /// </summary>
    public class CrossOriginException : Exception { }
    /// <summary>
    /// Thrown when an AssemblySolver is Unable to Resolve an AssemblyName
    /// </summary>
    public class FailedToResolveAssemblyException : Exception
    {
        /// <summary>
        /// Assemblys Name That failed to be Resolved
        /// </summary>
        public readonly AssemblyName AssemblyName;

        /// <inheritdoc cref="FailedToResolveAssemblyException"/>
        /// <param name="AssemblyName">Assembly Name that Failed to be Resolved</param>
        public FailedToResolveAssemblyException(AssemblyName AssemblyName) : base($"Failed to Resolve Assembly '{AssemblyName.Name}'") => this.AssemblyName = AssemblyName;
    }

    /// <summary>
    /// Thrown when attempting to load an Assembly that allready exists
    /// </summary>
    public class AssemblyAllreadExists : Exception
    {
        /// <summary>
        /// Assemblys Name
        /// </summary>
        public readonly AssemblyName AssemblyName;

        /// <inheritdoc cref="FailedToResolveAssemblyException"/>
        /// <param name="AssemblyName">Assembly Name</param>
        public AssemblyAllreadExists(AssemblyName AssemblyName) : base($"Failed to Resolve Assembly '{AssemblyName.Name}'") => this.AssemblyName = AssemblyName;
    }
}
