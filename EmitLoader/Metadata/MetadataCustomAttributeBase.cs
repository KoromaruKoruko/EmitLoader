namespace EmitLoader.Metadata
{
    internal abstract class MetadataCustomAttributeBase : ICustomAttribute
    {
        public AssemblyObjectKind Kind => AssemblyObjectKind.CustomAttribute;
        public AssemblyLoader Context => this.Assembly.Context;
        IAssembly IAssemblySolverObject.Assembly => this.Assembly;
        public abstract MetadataSolver Assembly { get; }

        public IType AttributeType => this.Constructor.DeclaringType;

        public abstract IMethod Constructor { get; }
    }
}
