
using System;
using System.Reflection;
using System.Reflection.Metadata;

namespace EmitLoader.Mixed
{
    internal class MixedConstructedGenericParameter : IGenericParameter
    {
        public MixedConstructedGenericParameter(IGenericParameter Base, MixedConstructedMethod Parent)
        {
            this.Base = Base;
            this.Mappings = Parent.Mappings;
            this.Parent = Parent;
        }
        public MixedConstructedGenericParameter(IGenericParameter Base, MixedConstructedType Parent)
        {
            this.Base = Base;
            this.Mappings = Parent.Mappings;
            this.Parent = Parent;
        }
        internal readonly IGenericParameter Base;
        internal readonly MixedTypeGenericMappings Mappings;


        public IGeneric Parent { get; }
        public ICustomAttribute[] CustomAttributes => this.Base.CustomAttributes;
        public IGenericParameterConstraint[] Constraints
        {
            get
            {
                if (this._Constraints == null)
                {
                    this._Constraints = new IGenericParameterConstraint[this.Base.Constraints.Length];
                    for (int x = 0; x < this.Base.Constraints.Length; x++)
                        this._Constraints[x] = new MixedConstructedGenericParameterConstraint(this.Base.Constraints[x], this);
                }
                return this._Constraints;
            }
        }
        private IGenericParameterConstraint[] _Constraints;
        public AssemblyObjectKind Kind => AssemblyObjectKind.GenericParameter;
        public AssemblyLoader Context => this.Base.Context;
        public IAssembly Assembly => this.Base.Context.MixedSolver;
        public string GetFullyQualifiedName() => this.Name;
        public string Name => this.Base.Name;

        public Type GetBuiltType()
        {
            if (this.Parent is IType type)
            {
                foreach (Type t in type.GenericArguments)
                    if (t.Name == this.Name)
                        return t;
            }
            else
                foreach (Type t in ((IMethod)this.Parent).GetBuiltMethod().GetGenericArguments())
                    if (t.Name == this.Name)
                        return t;
            throw new Exception("Unable to Find GenericParameter On Built Output");
        }
        public IMethod StaticConstructor => throw new NotSupportedException();
        public IType DeclaringType => throw new NotSupportedException();
        public INamespace Namespace => throw new NotImplementedException();
        public bool IsNestedType => throw new NotImplementedException();
        public bool IsGenericTypeParameter => throw new NotImplementedException();
        public IType GenericDefinition => throw new NotImplementedException();
        public IType[] Types => throw new NotImplementedException();
        public IField[] Fields => throw new NotImplementedException();
        public IMethod[] Methods => throw new NotImplementedException();
        public IMethod[] Constructors => throw new NotImplementedException();
        public IProperty[] Properties => throw new NotImplementedException();
        public IEvent[] Events => throw new NotImplementedException();
        public int ArrayRank => throw new NotImplementedException();
        public bool IsArray => throw new NotImplementedException();
        public bool IsByRef => throw new NotImplementedException();
        public bool IsPointer => throw new NotImplementedException();
        public bool IsSZArray => throw new NotImplementedException();
        public bool IsPinned => throw new NotImplementedException();
        public bool IsGeneric => throw new NotImplementedException();
        public bool IsGenericDefinition => throw new NotImplementedException();
        public IType[] GenericArguments => throw new NotImplementedException();
        IGeneric IGeneric.GenericDefinition => throw new NotImplementedException();
        public IType ConstructGeneric(IType[] genericArguments) => throw new NotSupportedException();
        public IMethod FindConstructor(IType[] ParameterTypes) => throw new NotSupportedException();
        public IEvent FindEvent(string Name) => throw new NotSupportedException();
        public IField FindField(string Name) => throw new NotSupportedException();
        public IMethod FindMethod(string Name, IType[] ParameterTypes) => throw new NotSupportedException();
        public IProperty FindProperty(string Name) => throw new NotSupportedException();
        public IType FindType(string Name) => throw new NotSupportedException();
        public IType GetElementType() => throw new NotSupportedException();
        public IType MakeArrayType(ArrayShape shape) => throw new NotSupportedException();
        public IType MakeByRefType() => throw new NotSupportedException();
        public IType MakePinnedType() => throw new NotSupportedException();
        public IType MakePointerType() => throw new NotSupportedException();
        public IType MakeSZArrayType() => throw new NotSupportedException();
        IGeneric IGeneric.ConstructGeneric(IType[] genericArguments) => throw new NotSupportedException();




        public Boolean IsStatic => false;
        public Boolean IsInterface => false;
        public Boolean IsEnum => this.Base.IsEnum;
        public Boolean IsValueType => this.Base.IsValueType;

        // NULLABLE
        public IType BaseType => this.Base.BaseType;
        public IType[] Interfaces => this.Base.Interfaces;

        public GenericParameterAttributes GenericParameterAttributes => this.Base.GenericParameterAttributes;

        public Boolean IsCastableTo(IType type) => AssemblyLoaderHelpers.IsCastableTo(this, type);
    }
}
