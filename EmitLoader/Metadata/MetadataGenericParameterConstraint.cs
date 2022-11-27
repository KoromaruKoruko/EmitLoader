using System.Reflection.Metadata;

namespace EmitLoader.Metadata
{
    internal class MetadataGenericParameterConstraint : IGenericParameterConstraint
    {
        public AssemblyObjectKind Kind => AssemblyObjectKind.GenericParameterConstraint;
        public AssemblyLoader Context => this.Assembly.Context;
        IAssembly IAssemblySolverObject.Assembly => this.Assembly;
        public MetadataSolver Assembly { get; }

        IGenericParameter IGenericParameterConstraint.Parent => this.Parent;
        public MetadataGenericParameterType Parent { get; }

        public IType ConstrainType
        {
            get
            {
                if (this._ConstrainType == null)
                {
                    switch (this.Def.Type.Kind)
                    {
                        case HandleKind.TypeDefinition:
                            this._ConstrainType = this.Assembly.GetTypeDefinition((TypeDefinitionHandle)this.Def.Type);
                            break;
                        case HandleKind.TypeReference:
                            this._ConstrainType = this.Assembly.GetTypeReference((TypeReferenceHandle)this.Def.Type);
                            break;

                        case HandleKind.TypeSpecification:
                            this._ConstrainType = this.Assembly.GetTypeSpecification((TypeSpecificationHandle)this.Def.Type, this.Parent.Parent);
                            break;
                    }
                }
                return this._ConstrainType;
            }
        }
        private IType _ConstrainType;

        public MetadataGenericParameterConstraint(GenericParameterConstraint Def, MetadataGenericParameterType Parent)
        {
            this.Def = Def;
            this.Parent = Parent;
        }
        private GenericParameterConstraint Def;
    }
}
