
using System;

namespace EmitLoader.Metadata
{
    internal abstract class MetadataParameterBase : IParameter
    {
        public AssemblyObjectKind Kind => AssemblyObjectKind.Parameter;
        public AssemblyLoader Context => this.Assembly.Context;
        IAssembly IAssemblySolverObject.Assembly => this.Assembly;
        public abstract MetadataSolver Assembly { get; }

        public abstract string Name { get; }

        IMethod IParameter.Parent => this.Parent;
        public abstract MetadataMethodBase Parent { get; }

        public abstract IType ParameterType { get; }
        public abstract Boolean IsOptional { get; }

        ICustomAttribute[] IParameter.CustomAttributes => this.CustomAttributes;
        public abstract MetadataCustomAttributeBase[] CustomAttributes { get; }

        public abstract MetadataConstant DefaultValue { get; }
    }
}
