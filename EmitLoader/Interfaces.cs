using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Reflection.Metadata;

namespace EmitLoader
{
    /// <summary>
    /// Assembly Object Kind
    /// </summary>
    public enum AssemblyObjectKind
    {
        /// <summary>
        /// <see cref="INamespace"/>
        /// </summary>
        Namespace,
        /// <summary>
        /// <see cref="IType"/>
        /// </summary>
        Type,
        /// <summary>
        /// <see cref="IGenericParameter"/>
        /// </summary>
        GenericParameter,
        /// <summary>
        /// <see cref="IGenericParameterConstraint"/>
        /// </summary>
        GenericParameterConstraint,
        /// <summary>
        /// <see cref="IField"/>
        /// </summary>
        Field,
        /// <summary>
        /// <see cref="IMethod"/>
        /// </summary>
        Method,
        /// <summary>
        /// <see cref="IParameter"/>
        /// </summary>
        Parameter,
        /// <summary>
        /// <see cref="IProperty"/>
        /// </summary>
        Property,
        /// <summary>
        /// <see cref="IEvent"/>
        /// </summary>
        Event,
        /// <summary>
        /// <see cref="ICustomAttribute"/>
        /// </summary>
        CustomAttribute,
        /// <summary>
        /// <see cref="IConstant"/>
        /// </summary>
        Constant,
    }
    /// <summary>
    /// Assembly Solver Kind
    /// </summary>
    public enum AssemblyKind
    {
        /// <summary>
        /// Reflection Solver, Assembly Exists and can be redily resolved to a Reflection Object
        /// </summary>
        Reflection,
        /// <summary>
        /// Metadata Solver, Is Not Loaded/ Is to be Loaded into the AppDomain
        /// </summary>
        Metadata,
        /// <summary>
        /// Mixed Solver, A Type is partially Reflection and partially Metadata (Reflection Generic Definition but Metadata Generic Arguments)
        /// </summary>
        Mixed,
    }
    /// <summary>
    /// Access Kind for <see cref="IAccessController"/>
    /// </summary>
    public enum AccessKind
    {
        /// <summary>
        /// None Access Implies that nothing is to be accessible.
        /// </summary>
        None,
        /// <summary>
        /// Full Access Implies that everything is to be fully accessible.
        /// </summary>
        Full,
        /// <summary>
        /// Partial Access Implies that some parts are to be accessible but not all.
        /// </summary>
        Partial,
    }
    /// <summary>
    /// Access Controller Interface. All Access will be determined top->down.
    /// </summary>
    public interface IAccessController
    {
        /// <summary>
        /// Can Access Assembly
        /// </summary>
        /// <param name="assembly">Assembly</param>
        AccessKind CanAccess(Assembly assembly);
        /// <summary>
        /// Can Access Namespace
        /// </summary>
        /// <param name="namespace">Namespace</param>
        AccessKind CanAccess(String @namespace);
        /// <summary>
        /// Can Access Type
        /// </summary>
        /// <param name="type">Type</param>
        AccessKind CanAccess(Type type);
        /// <summary>
        /// Can Access Method (Note: Can be Property/Event Accessor)
        /// </summary>
        /// <param name="method">Method</param>
        AccessKind CanAccess(MethodInfo method);
        /// <summary>
        /// Can Access Type Constructor
        /// </summary>
        /// <param name="constructor">Constructor</param>
        AccessKind CanAccess(ConstructorInfo constructor);
        /// <summary>
        /// Can Access Field
        /// </summary>
        /// <param name="field">Field</param>
        AccessKind CanAccess(FieldInfo field);

        /// <summary>
        /// Can Get Field
        /// </summary>
        /// <param name="field">Field</param>
        Boolean CanGet(FieldInfo field);
        /// <summary>
        /// Can Set Field
        /// </summary>
        /// <param name="field">Field</param>
        Boolean CanSet(FieldInfo field);
    }
    /// <summary>
    /// Assembly Representation
    /// </summary>
    public interface IAssembly
    {
        /// <summary>
        /// Assembly Name
        /// </summary>
        AssemblyName Name { get; }
        /// <summary>
        /// Context
        /// </summary>
        AssemblyLoader Context { get; }
        /// <summary>
        /// Assembly Kind
        /// </summary>
        AssemblyKind Kind { get; }

        /// <summary>
        /// Get Defined Types
        /// </summary>
        IEnumerable<IType> GetDefinedTypes();
        /// <summary>
        /// Get Defined Namespaces
        /// </summary>
        IEnumerable<INamespace> GetDefinedNamespaces();
        /// <summary>
        /// Attempt to Find a Type within this Assembly
        /// </summary>
        /// <param name="Namespace"></param>
        /// <param name="TypeName"></param>
        /// <returns>(NULLABLE)</returns>
        IType FindType(String Namespace, String TypeName);
    }
    /// <summary>
    /// Metadata Assembly Representation
    /// </summary>
    public interface IMetadataAssembly : IAssembly
    {
        /// <summary>
        /// Get All Referenced Types
        /// </summary>
        /// <returns></returns>
        IEnumerable<IType> GetReferencedTypes();
        /// <summary>
        /// Get All Referenced Assemblies
        /// </summary>
        IEnumerable<IAssembly> GetReferencedAssemblies();

        /// <summary>
        /// Prepares this Assembly for Building/Loading into the Runtime AppDomain
        /// </summary>
        /// <param name="AccessControlManager"></param>
        void PrepBuild(AccessControlManager AccessControlManager);
        /// <summary>
        /// Build Assembly (Must call PrepBuild on all Metadata Assemblies within this Context Prior to this)
        /// </summary>
        Assembly GetBuiltAssembly();
    }
    /// <summary>
    /// Container for all Assembly Objects
    /// </summary>
    public interface IAssemblySolverObject
    {
        /// <summary>
        /// Object Kind
        /// </summary>
        AssemblyObjectKind Kind { get; }
        /// <summary>
        /// Assembly Loader Context
        /// </summary>
        AssemblyLoader Context { get; }
        /// <summary>
        /// Assembly Containing this Object
        /// </summary>
        IAssembly Assembly { get; }
    }
    /// <summary>
    /// Named Objects (<see cref="IType"/>/<see cref="IMethod"/>/<see cref="IField"/>/<see cref="IProperty"/>/<see cref="IEvent"/>/<see cref="INamespace"/>)
    /// </summary>
    public interface INamedObject : IAssemblySolverObject
    {
        /// <summary>
        /// Name
        /// </summary>
        String Name { get; }
        /// <summary>
        /// Gets the Fully Qualified Name 
        /// </summary>
        /// <returns></returns>
        String GetFullyQualifiedName();
    }
    /// <summary>
    /// Generic Context
    /// </summary>
    public interface IGeneric : IAssemblySolverObject
    {
        /// <summary>
        /// Is Generic
        /// </summary>
        Boolean IsGeneric { get; }
        /// <summary>
        /// Is Generic Definition
        /// </summary>
        Boolean IsGenericDefinition { get; }
        /// <summary>
        /// Generic Arguments / Parameters
        /// </summary>
        IType[] GenericArguments { get; }

        /// <summary>
        /// (NULLABLE) Generic Definition
        /// </summary>
        IGeneric GenericDefinition { get; }
        /// <summary>
        /// Constructs a Generic from a Generic Definition
        /// </summary>
        /// <param name="genericArguments">Generic Arguments</param>
        /// <returns>Constructed Generic</returns>
        IGeneric ConstructGeneric(IType[] genericArguments);
    }
    /// <summary>
    /// Generic Parameter Representation
    /// </summary>
    public interface IGenericParameter : IType
    {
        /// <summary>
        /// Generic Parent
        /// </summary>
        IGeneric Parent { get; }
        /// <summary>
        /// Type Constraints
        /// </summary>
        IGenericParameterConstraint[] Constraints { get; }
        /// <summary>
        /// Generic Parameter Attributes
        /// </summary>
        GenericParameterAttributes GenericParameterAttributes { get; }
        //TODO: [REF] Implement Generic Parameter Attributes Flags
    }
    /// <summary>
    /// Generic Parameter Constraint Representation
    /// </summary>
    public interface IGenericParameterConstraint : IAssemblySolverObject
    {
        /// <summary>
        /// Generic Parameter
        /// </summary>
        IGenericParameter Parent { get; }
        /// <summary>
        /// Constrain Type
        /// </summary>
        IType ConstrainType { get; }
    }
    /// <summary>
    /// Namespace Representation (Only Valid within a Single Assembly)
    /// </summary>
    public interface INamespace : INamedObject
    {
        /// <summary>
        /// Is Global Namespace
        /// </summary>
        Boolean IsGlobalNamespace { get; }
        /// <summary>
        /// (NULLABLE) Parent Namesapce
        /// </summary>
        INamespace ParentNamespace { get; }
        /// <summary>
        /// Child Namespaces
        /// </summary>
        INamespace[] ChildNamespaces { get; }
        /// <summary>
        /// Defined Types
        /// </summary>
        IType[] Types { get; }

        /// <summary>
        /// Attempt to Find a Type within this Namespace
        /// </summary>
        /// <param name="TypeName">Type Name</param>
        /// <returns>(NULLABLE)</returns>
        IType FindType(String TypeName);
    }
    /// <summary>
    /// Type Representation
    /// </summary>
    public interface IType : INamedObject, IGeneric
    {
        /// <summary>
        /// Type Namespace
        /// </summary>
        INamespace Namespace { get; }
        /// <summary>
        /// Is Nested Type
        /// </summary>
        Boolean IsNestedType { get; }
        /// <summary>
        /// Is Type A Generic Parameter
        /// </summary>
        Boolean IsGenericTypeParameter { get; }

        /// <summary>
        /// (NULLABLE) Declaring Type
        /// </summary>
        IType DeclaringType { get; }
        /// <summary>
        /// Generic Type Definition
        /// </summary>
        new IType GenericDefinition { get; }

        /// <summary>
        /// (NULLABLE) Type Initializer / Static Constructor
        /// </summary>
        IMethod StaticConstructor { get; }

        /// <summary>
        /// Defined Nested Types
        /// </summary>
        IType[] Types { get; }
        /// <summary>
        /// Defined Fields
        /// </summary>
        IField[] Fields { get; }
        /// <summary>
        /// Defined Methods
        /// </summary>
        IMethod[] Methods { get; }
        /// <summary>
        /// Defined Constructors
        /// </summary>
        IMethod[] Constructors { get; }
        /// <summary>
        /// Defined Properties
        /// </summary>
        IProperty[] Properties { get; }
        /// <summary>
        /// Defined Events
        /// </summary>
        IEvent[] Events { get; }

        /// <summary>
        /// Returns the Loaded Type. You must first Load/Build the target Assembly
        /// </summary>
        Type GetBuiltType();

        /// <summary>
        /// Attempt to Find a Nested Type with the Specified Name
        /// </summary>
        /// <param name="Name">Type Name</param>
        /// <returns>(NULLABLE)</returns>
        IType FindType(String Name);
        /// <summary>
        /// Attempt to Find a Field with the Specified Name
        /// </summary>
        /// <param name="Name">Field Name</param>
        /// <returns>(NULLABLE)</returns>
        IField FindField(String Name);
        /// <summary>
        /// Attempt to Find a Method with the Specified Name and Arguments
        /// </summary>
        /// <param name="Name">Method Name</param>
        /// <param name="ParameterTypes">Argument Types</param>
        /// <returns>(NULLABLE)</returns>
        IMethod FindMethod(String Name, IType[] ParameterTypes);
        /// <summary>
        /// Attempt to Find a Constructor with the Provided Argument Types
        /// </summary>
        /// <param name="ParameterTypes">Parameter Types</param>
        /// <returns>(NULLABLE)</returns>
        IMethod FindConstructor(IType[] ParameterTypes);
        /// <summary>
        /// Attempt to Find a Property with the Specified Name
        /// </summary>
        /// <param name="Name">Property Name</param>
        /// <returns>(NULLABLE)</returns>
        IProperty FindProperty(String Name);
        /// <summary>
        /// Attempt to Find an Event with the Specified Name
        /// </summary>
        /// <param name="Name">Field Name</param>
        /// <returns>(NULLABLE)</returns>
        IEvent FindEvent(String Name);
        /// <summary>
        /// Custom Attributes
        /// </summary>
        ICustomAttribute[] CustomAttributes { get; }
        /// <summary>
        /// Constructs a Generic With the provided Arguments
        /// </summary>
        /// <param name="genericArguments">Generic Arguments</param>
        /// <returns></returns>
        new IType ConstructGeneric(IType[] genericArguments);

        /// <summary>
        /// Number of Dimensions (-1/1-32)
        /// </summary>
        Int32 ArrayRank { get; }
        /// <summary>
        /// Is Array Type
        /// </summary>
        Boolean IsArray { get; }
        /// <summary>
        /// Is By Reference Type
        /// </summary>
        Boolean IsByRef { get; }
        /// <summary>
        /// Is Pointer Type
        /// </summary>
        Boolean IsPointer { get; }
        /// <summary>
        /// Is Single Dimensional Array Type
        /// </summary>
        Boolean IsSZArray { get; }

        /// <summary>
        /// Creates a Pointer Type of this Type
        /// </summary>
        IType MakePointerType();
        /// <summary>
        /// Creates a By Reference Type (in/out/ref) of this Type
        /// </summary>
        IType MakeByRefType();
        /// <summary>
        /// Creates a Single Dimension Array of this Type
        /// </summary>
        IType MakeSZArrayType();
        /// <summary>
        /// Creates an Array Type of this Type
        /// </summary>
        /// <param name="shape">ArrayShape (Currently only Supports Ranks)</param>
        IType MakeArrayType(ArrayShape shape);
        /// <summary>
        /// Returns the Element Type IE Array/Pointer/ByRef Element
        /// </summary>
        IType GetElementType();


        /// <summary>
        /// Is Static Type
        /// </summary>
        Boolean IsStatic { get; }
        /// <summary>
        /// Is Interface
        /// </summary>
        Boolean IsInterface { get; }
        /// <summary>
        /// Is Enum (<see cref="Enum"/>)
        /// </summary>
        Boolean IsEnum { get; }
        /// <summary>
        /// Is Value Type (<see cref="System.ValueType"/>)
        /// </summary>
        Boolean IsValueType { get; }
        /// <summary>
        /// (NULLABLE) Base Type
        /// </summary>
        IType BaseType { get; }
        /// <summary>
        /// Implemented Interfaces
        /// </summary>
        IType[] Interfaces { get; }

        /// <summary>
        /// Validates that this type can be cast to the target type (Does not check Implicit/Explicit casts)
        /// </summary>
        Boolean IsCastableTo(IType type);
    }
    /// <summary>
    /// Member Representation can be <see cref="IProperty"/>/<see cref="IEvent"/>/<see cref="IField"/>/<see cref="IMethod"/>
    /// </summary>
    public interface IMember : INamedObject
    {
        /// <summary>
        /// Declaring Type
        /// </summary>
        IType DeclaringType { get; }
        /// <summary>
        /// Custom Attributes
        /// </summary>
        ICustomAttribute[] CustomAttributes { get; }
    }
    /// <summary>
    /// Property Representation
    /// </summary>
    public interface IProperty : IMember
    {
        /// <summary>
        /// Property Type
        /// </summary>
        IType PropertyType { get; }

        /// <summary>
        /// (NULLABLE) Getter
        /// </summary>
        IMethod Getter { get; }
        /// <summary>
        /// (NULLABLE) Setter
        /// </summary>
        IMethod Setter { get; }

        /// <summary>
        /// Returns the Loaded Property. You must first Load/Build the target Assembly
        /// </summary>
        PropertyInfo GetBuiltProperty();
    }

    /// <summary>
    /// Event Representation
    /// </summary>
    public interface IEvent : IMember
    {
        /// <summary>
        /// Event Type
        /// </summary>
        IType EventType { get; }

        /// <summary>
        /// (NULLABLE) Adder
        /// </summary>
        IMethod Adder { get; }
        /// <summary>
        /// (NULLABLE) Remover
        /// </summary>
        IMethod Remover { get; }
        /// <summary>
        /// (NULLABLE) Event Raiser (Invoker)
        /// </summary>
        IMethod Raiser { get; }

        /// <summary>
        /// Returns the Loaded Event. You must first Load/Build the target Assembly
        /// </summary>
        EventInfo GetBuiltEvent();
    }

    /// <summary>
    /// Field Representation
    /// </summary>
    public interface IField : IMember
    {
        /// <summary>
        /// (NULLABLE) Default Type
        /// </summary>
        IConstant DefaultValue { get; }

        /// <summary>
        /// Field Type
        /// </summary>
        IType FieldType { get; }

        /// <summary>
        /// Returns the Loaded Field. You must first Load/Build the target Assembly
        /// </summary>
        FieldInfo GetBuiltField();
    }

    /// <summary>
    /// Method/Constructor Representation
    /// </summary>
    public interface IMethod : IMember, IGeneric
    {
        /// <summary>
        /// Method Return Type
        /// </summary>
        IType ReturnType { get; }
        /// <summary>
        /// Parameters for this Method
        /// </summary>
        IParameter[] Parameters { get; }

        /// <summary>
        /// Returns the Loaded Method can be <see cref="MethodInfo"/> or <see cref="ConstructorInfo"/>. You must first Load/Build the target Assembly
        /// </summary>
        MethodBase GetBuiltMethod();

        /// <summary>
        /// Generic Method Definition
        /// </summary>
        new IMethod GenericDefinition { get; }
        /// <summary>
        /// Construct a Generic Method with the provided Arguments
        /// </summary>
        /// <param name="genericArguments">Generic Arguments</param>
        /// <returns>Constructed Generic Method</returns>
        new IMethod ConstructGeneric(IType[] genericArguments);
    }

    /// <summary>
    /// Method Parameter Representation
    /// </summary>
    public interface IParameter : IAssemblySolverObject
    {
        /// <summary>
        /// Parameter Name
        /// </summary>
        String Name { get; }
        /// <summary>
        /// Method which this Parameter is From
        /// </summary>
        IMethod Parent { get; }

        /// <summary>
        /// Parameter Type
        /// </summary>
        IType ParameterType { get; }
        /// <summary>
        /// Is Parameter Optional
        /// </summary>
        Boolean IsOptional { get; }
        /// <summary>
        /// Custom Attributes
        /// </summary>
        ICustomAttribute[] CustomAttributes { get; }
    }

    /// <summary>
    /// Custom Attribute Representation (Not fully Implemented)
    /// </summary>
    public interface ICustomAttribute : IAssemblySolverObject
    {
        /// <summary>
        /// Custom Attribute BaseType
        /// </summary>
        IType AttributeType { get; }
    }

    /// <summary>
    /// Lexical Value Types
    /// </summary>
    public enum ValueType
    {
        /// <summary>
        /// <see langword="null"/>
        /// </summary>
        Null = ConstantTypeCode.NullReference,
        /// <summary>
        /// <see cref="System.Boolean"/>
        /// </summary>
        Boolean = ConstantTypeCode.Boolean,
        /// <summary>
        /// <see cref="System.Char"/>
        /// </summary>
        Char = ConstantTypeCode.Char,
        /// <summary>
        /// <see cref="System.SByte"/>
        /// </summary>
        SByte = ConstantTypeCode.SByte,
        /// <summary>
        /// <see cref="System.Byte"/>
        /// </summary>
        Byte = ConstantTypeCode.Byte,
        /// <summary>
        /// <see cref="System.Int16"/>
        /// </summary>
        Int16 = ConstantTypeCode.Int16,
        /// <summary>
        /// <see cref="System.UInt16"/>
        /// </summary>
        UInt16 = ConstantTypeCode.UInt16,
        /// <summary>
        /// <see cref="System.Int32"/>
        /// </summary>
        Int32 = ConstantTypeCode.Int32,
        /// <summary>
        /// <see cref="System.UInt32"/>
        /// </summary>
        UInt32 = ConstantTypeCode.UInt32,
        /// <summary>
        /// <see cref="System.Int64"/>
        /// </summary>
        Int64 = ConstantTypeCode.Int64,
        /// <summary>
        /// <see cref="System.UInt64"/>
        /// </summary>
        UInt64 = ConstantTypeCode.UInt64,
        /// <summary>
        /// <see cref="System.Single"/>
        /// </summary>
        Single = ConstantTypeCode.Single,
        /// <summary>
        /// <see cref="System.Double"/>
        /// </summary>
        Double = ConstantTypeCode.Double,
        /// <summary>
        /// <see cref="System.String"/>
        /// </summary>
        String = ConstantTypeCode.String,
    }

    /// <summary>
    /// Constant Value Representation
    /// </summary>
    public interface IConstant : IAssemblySolverObject
    {
        /// <summary>
        /// <see cref="EmitLoader.ValueType"/> of <see cref="IConstant.Value"/>
        /// </summary>
        ValueType ValueType { get; }
        /// <summary>
        /// Constant Value (Must be lexicalType IE Primitives + String &amp; Null)
        /// </summary>
        object Value { get; }
    }
}
