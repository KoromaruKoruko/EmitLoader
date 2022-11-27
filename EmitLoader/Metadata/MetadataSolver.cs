using EmitLoader.Builder;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

using EmitLoader.Unsafe;

namespace EmitLoader.Metadata
{
    internal class MetadataSolver : IMetadataAssembly
    {
        public AssemblyName Name { get; }
        public AssemblyLoader Context { get; }
        public AssemblyKind Kind => AssemblyKind.Metadata;

        public IEnumerable<IType> GetDefinedTypes()
        {
            foreach (TypeDefinitionHandle handle in this.MD.TypeDefinitions)
                yield return GetTypeDefinition(handle);
        }
        public IEnumerable<INamespace> GetDefinedNamespaces()
        {
            NamespaceDefinition Def = this.MD.GetNamespaceDefinitionRoot();
            Stack<INamespace> Stack = new Stack<INamespace>();
            foreach (NamespaceDefinitionHandle handle in Def.NamespaceDefinitions)
                Stack.Push(this.GetNamespace(handle));

            while (Stack.Count > 0)
            {
                INamespace ns = Stack.Pop();

                foreach (INamespace child in ns.ChildNamespaces)
                    Stack.Push(child);

                yield return ns;
            }
        }
        public IEnumerable<IType> GetReferencedTypes()
        {
            foreach (TypeReferenceHandle typeRef in this.MD.TypeReferences)
                yield return this.GetTypeReference(typeRef);
        }
        public IEnumerable<IAssembly> GetReferencedAssemblies()
        {
            foreach (AssemblyReferenceHandle asmRef in this.MD.AssemblyReferences)
                yield return this.Context.GetAssembly(this.MD.GetAssemblyReference(asmRef).GetAssemblyName());
        }

        public IType FindType(String Namespace, String Name) => FindNamespace(Namespace).FindType(Name);
        public INamespace FindNamespace(String Namespace)
        {
            if (String.IsNullOrEmpty(Namespace))
                return GlobalNamespace;

            NamespaceDefinition current = this.MD.GetNamespaceDefinitionRoot();
            INamespace result = null;
            String[] segs = Namespace.Split('.');

            Boolean fail = true;
            foreach (NamespaceDefinitionHandle handle in current.NamespaceDefinitions)
            {
                result = this.GetNamespace(handle);
                if (result.Name == segs[0])
                {
                    fail = false;
                    break;
                }
            }
            if (fail)
                return null;

            for (int x = 1; x < segs.Length; x++)
            {
                fail = true;
                foreach (INamespace child in result.ChildNamespaces)
                    if (child.Name == segs[x])
                    {
                        result = child;
                        fail = false;
                        break;
                    }

                if (fail)
                    return null;
            }
            return result;
        }


        internal IType EnumType;
        internal IType ValueType;
        internal MetadataNamespace GlobalNamespace;
        internal MetadataAssemblyLoader Loader { get; private set; }

        internal PEReader PE { get; }
        internal MetadataReader MD { get; }
        internal SignatureProvider SP { get; }
        public MetadataSolver(AssemblyLoader Context, Stream DllStream, PEStreamOptions loadOptions = PEStreamOptions.PrefetchEntireImage | PEStreamOptions.LeaveOpen)
        {
            this.Context = Context;
            this.PE = new PEReader(DllStream, loadOptions);
            this.MD = this.PE.GetMetadataReader(MetadataReaderOptions.None);

            this.SP = new SignatureProvider(this);

            this.Name = this.MD.GetAssemblyDefinition().GetAssemblyName();

            this.EnumType = this.Context.ResolveType(typeof(Enum));
            this.ValueType = this.Context.ResolveType(typeof(ValueType));

            this.GlobalNamespace = new MetadataNamespace(this.MD.GetNamespaceDefinitionRoot(), this, true);
        }

        public MetadataType GetTypeDefinition(TypeDefinitionHandle handle)
        {
            if (handle.IsNil)
                return null;
            if (this.definedTypes.TryGetValue(handle, out MetadataType type))
                return type;
            lock (this.definedTypes)
            {
                if (this.definedTypes.TryGetValue(handle, out type))
                    return type;

                type = new MetadataType(handle, this);
                this.definedTypes.Add(handle, type);
                return type;
            }
        }
        private readonly SortedDictionary<TypeDefinitionHandle, MetadataType> definedTypes = new SortedDictionary<TypeDefinitionHandle, MetadataType>(UnsafeOperations.HandleComparers.Instance);

        public MetadataNamespace GetNamespace(NamespaceDefinitionHandle handle)
        {
            if (handle.IsNil)
                return GlobalNamespace;
            if (this.definedNamespaces.TryGetValue(handle, out MetadataNamespace @namespace))
                return @namespace;
            lock (this.definedNamespaces)
            {
                if (this.definedNamespaces.TryGetValue(handle, out @namespace))
                    return @namespace;

                @namespace = new MetadataNamespace(this.MD.GetNamespaceDefinition(handle), this, false);
                this.definedNamespaces.Add(handle, @namespace);
                return @namespace;
            }
        }
        private readonly SortedDictionary<NamespaceDefinitionHandle, MetadataNamespace> definedNamespaces = new SortedDictionary<NamespaceDefinitionHandle, MetadataNamespace>(UnsafeOperations.HandleComparers.Instance);

        public MetadataMethod GetMethodDefinition(MethodDefinitionHandle handle, MetadataType declaringType = null)
        {
            if (handle.IsNil)
                return null;
            if (this.definedMethods.TryGetValue(handle, out MetadataMethod method))
                return method;
            lock (this.definedMethods)
            {
                if (this.definedMethods.TryGetValue(handle, out method))
                    return method;

                method = declaringType == null
                    ? new MetadataMethod(handle, this.GetTypeDefinition(this.MD.GetMethodDefinition(handle).GetDeclaringType()))
                    : new MetadataMethod(handle, declaringType);

                this.definedMethods.Add(handle, method);
                return method;
            }
        }
        private readonly SortedDictionary<MethodDefinitionHandle, MetadataMethod> definedMethods = new SortedDictionary<MethodDefinitionHandle, MetadataMethod>(UnsafeOperations.HandleComparers.Instance);

        public MetadataField GetFieldDefinition(FieldDefinitionHandle handle, MetadataType declaringType = null)
        {
            if (handle.IsNil)
                return null;
            if (this.definedFields.TryGetValue(handle, out MetadataField field))
                return field;
            lock (this.definedFields)
            {
                if (this.definedFields.TryGetValue(handle, out field))
                    return field;

                field = declaringType == null
                    ? new MetadataField(handle, this.GetTypeDefinition(this.MD.GetFieldDefinition(handle).GetDeclaringType()))
                    : new MetadataField(handle, declaringType);

                this.definedFields.Add(handle, field);
                return field;
            }
        }
        private readonly SortedDictionary<FieldDefinitionHandle, MetadataField> definedFields = new SortedDictionary<FieldDefinitionHandle, MetadataField>(UnsafeOperations.HandleComparers.Instance);

        public IType GetTypeReference(TypeReferenceHandle handle)
        {
            if (handle.IsNil)
                return null;
            if (this.referencedTypes.TryGetValue(handle, out IType type))
                return type;
            lock (this.referencedTypes)
                return VGetTypeReference(handle);
        }
        private IType VGetTypeReference(TypeReferenceHandle handle)
        {
            if (this.referencedTypes.TryGetValue(handle, out IType type))
                return type;

            TypeReference typeRef = this.MD.GetTypeReference(handle);
            String Name = this.MD.GetString(typeRef.Name);

            switch (typeRef.ResolutionScope.Kind)
            {
                case HandleKind.TypeReference:
                    IType Parent = VGetTypeReference((TypeReferenceHandle)typeRef.ResolutionScope);
                    type = Parent.FindType(Name);
                    break;

                case HandleKind.AssemblyReference:
                    IAssembly Asm = this.Context.GetAssembly(this.MD.GetAssemblyReference((AssemblyReferenceHandle)typeRef.ResolutionScope).GetAssemblyName());
                    type = Asm.FindType(this.MD.GetString(typeRef.Namespace), Name);
                    break;

                default:
                    throw new NotSupportedException($"Unknown TypeReference ResolutionScope Kind {typeRef.ResolutionScope.Kind}");
            }

            if (type == null)
                throw new Exception("Failed to Resolve TypeReference!");

            this.referencedTypes.Add(handle, type);
            return type;
        }
        private readonly SortedDictionary<TypeReferenceHandle, IType> referencedTypes = new SortedDictionary<TypeReferenceHandle, IType>(UnsafeOperations.HandleComparers.Instance);

        public IMember GetMemberReference(MemberReferenceHandle handle, IGeneric context)
        {
            if (handle.IsNil)
                return null;
            if (this.referencedMembers.TryGetValue(handle, out IMember member))
                return member;

            lock (this.referencedMembers)
            {
                if (this.referencedMembers.TryGetValue(handle, out member))
                    return member;

                Boolean spec = false;
                MemberReference memRef = this.MD.GetMemberReference(handle);
                String Name = this.MD.GetString(memRef.Name);
                IType Parent;

                switch (memRef.Parent.Kind)
                {
                    case HandleKind.TypeReference:
                        Parent = GetTypeReference((TypeReferenceHandle)memRef.Parent);
                        break;

                    case HandleKind.TypeSpecification:
                        Parent = GetTypeSpecification((TypeSpecificationHandle)memRef.Parent, context);
                        spec = true;
                        break;

                    default:
                        throw new NotSupportedException($"Unknown MemberReference Parent Kind {memRef.Parent.Kind}");
                }


                switch (memRef.GetKind())
                {
                    case MemberReferenceKind.Field:
                        member = Parent.FindField(Name);
                        break;

                    case MemberReferenceKind.Method:
                        IType[] types = memRef.DecodeMethodSignature(this.SP, null).ParameterTypes.ToArray();
                        if (Name[0] == '.')
                        {
                            switch (Name)
                            {
                                case ".ctor":
                                    member = Parent.FindConstructor(types);
                                    break;
                                case ".cctor":
                                    member = Parent.StaticConstructor;
                                    break;
                            }
                        }
                        else
                            member = Parent.FindMethod(Name, types);
                        break;

                    default:
                        throw new NotSupportedException($"Unknown MemberReference Kind {memRef.GetKind()}");
                }

                if (member == null)
                    throw new Exception("Failed to Resolve MemberReference!");
                if (!spec)
                    this.referencedMembers.Add(handle, member);
                return member;
            }
        }
        private readonly SortedDictionary<MemberReferenceHandle, IMember> referencedMembers = new SortedDictionary<MemberReferenceHandle, IMember>(UnsafeOperations.HandleComparers.Instance);

        public IType GetTypeSpecification(TypeSpecificationHandle handle, IGeneric genericContext) =>
            handle.IsNil
                ? null
                : this.MD.GetTypeSpecification(handle).DecodeSignature(this.SP, genericContext);
        public IMethod GetMethodSpecification(MethodSpecificationHandle handle, IGeneric genericContext)
        {
            MethodSpecification spec = this.MD.GetMethodSpecification(handle);
            IMethod Base = null;
            switch (spec.Method.Kind)
            {
                case HandleKind.MemberReference:
                    Base = this.GetMemberReference((MemberReferenceHandle)spec.Method, genericContext) as IMethod;
                    break;
                case HandleKind.MethodDefinition:
                    Base = this.GetMethodDefinition((MethodDefinitionHandle)spec.Method);
                    break;
            }

            return Base == null
                ? throw new Exception("Falied to Resolve Method")
                : Base.ConstructGeneric(spec.DecodeSignature(this.SP, genericContext).ToArray());
        }


        public MetadataConstant GetConstant(ConstantHandle handle)
        {
            if (handle.IsNil)
                return null;
            if (this.constantValues.TryGetValue(handle, out MetadataConstant constant))
                return constant;
            lock (this.constantValues)
            {
                if (this.constantValues.TryGetValue(handle, out constant))
                    return constant;

                constant = new MetadataConstant(this.MD.GetConstant(handle), this);

                this.constantValues.Add(handle, constant);
                return constant;
            }
        }
        private readonly SortedDictionary<ConstantHandle, MetadataConstant> constantValues = new SortedDictionary<ConstantHandle, MetadataConstant>(UnsafeOperations.HandleComparers.Instance);

        public void PrepBuild(AccessControlManager AccessControlManager)
        {
            if (this.Loader != null)
                throw new InvalidOperationException("Assembly Already Preped");
            this.Loader = new MetadataAssemblyLoader(this, AccessControlManager);
        }

        public Assembly GetBuiltAssembly()
        {
            if (this._BuiltAssembly != null)
                return this._BuiltAssembly;

            List<(TypeBuilder builder, MetadataType type)> types = new List<(TypeBuilder builder, MetadataType type)>();
            List<(MethodBuilder builder, MetadataMethod method)> methods = new List<(MethodBuilder builder, MetadataMethod method)>();
            List<(ConstructorBuilder builder, MetadataMethod method)> constructors = new List<(ConstructorBuilder builder, MetadataMethod method)>();
            MetadataType moduleType;
            // declare types
            foreach (TypeDefinitionHandle typeDef in this.MD.TypeDefinitions)
            {
                MetadataType type = this.GetTypeDefinition(typeDef);
                if (type.Name == "<Module>")
                    moduleType = type;
                else
                    types.Add((this.Loader.GetTypeBuilder(type), type));
            }

            // define types
            foreach ((TypeBuilder typeBuilder, MetadataType type) in types)
            {
                // define fields
                foreach (MetadataFieldBase Field in type.Fields)
                    if (Field is MetadataField field)
                        this.Loader.GetFieldBuilder(field);

                List<(PropertyBuilder builder, MetadataProperty property)> properties = new List<(PropertyBuilder builder, MetadataProperty property)>();
                List<(EventBuilder builder, MetadataEvent @event)> events = new List<(EventBuilder builder, MetadataEvent @event)>();

                // declare properties
                foreach (MetadataPropertyBase Property in type.Properties)
                    if (Property is MetadataProperty property)
                        properties.Add((this.Loader.GetPropertyBuilder(property), property));
                // declare events
                foreach (MetadataEventBase Event in type.Events)
                    if (Event is MetadataEvent @event)
                        events.Add((this.Loader.GetEventBuilder(@event), @event));

                // declare constructors
                foreach (MetadataMethodBase Method in type.Constructors)
                    if (Method is MetadataMethod method)
                        constructors.Add((this.Loader.GetConstructorBuilder(method), method));

                if (type.StaticConstructor != null && type.StaticConstructor is MetadataMethod cctor)
                    constructors.Add((this.Loader.GetConstructorBuilder(cctor), cctor));

                // declare methods
                foreach (MetadataMethodBase Method in type.Methods)
                    if (Method is MetadataMethod method)
                        methods.Add((this.Loader.GetMethodBuilder(method), method));

                // define events
                foreach ((EventBuilder eventBuilder, MetadataEvent @event) in events)
                {
                    if (@event.Adder != null)
                        eventBuilder.SetAddOnMethod(this.Loader.GetMethodBuilder((MetadataMethod)@event.Adder));
                    if (@event.Remover != null)
                        eventBuilder.SetRemoveOnMethod(this.Loader.GetMethodBuilder((MetadataMethod)@event.Remover));
                    if (@event.Raiser != null)
                        eventBuilder.SetRaiseMethod(this.Loader.GetMethodBuilder((MetadataMethod)@event.Raiser));
                }

                // define properties
                foreach ((PropertyBuilder propertyBuilder, MetadataProperty property) in properties)
                {
                    if (property.Getter != null)
                        propertyBuilder.SetGetMethod(this.Loader.GetMethodBuilder((MetadataMethod)property.Getter));
                    if (property.Setter != null)
                        propertyBuilder.SetSetMethod(this.Loader.GetMethodBuilder((MetadataMethod)property.Setter));
                }
            }

            // define methods
            this.Loader.BuildMethods();

            // finalize
            foreach ((TypeBuilder builder, MetadataType type) in types)
                type._BuiltType = builder.CreateType();

            this._BuiltAssembly = this.Loader.AssemblyBuilder;

            return this._BuiltAssembly;
        }
        private Assembly _BuiltAssembly;
        internal class SignatureProvider : ISignatureTypeProvider<IType, IGeneric>
        {
            // class P<T1, T2> { public class C<T3, T4> { } }
            // P`2+C`2[System.Int32,System.Int32,System.String,System.String]
            // class P<T1> { public class C { public class N<T2> { } } }
            //P`1+C+N`1[System.Int32,System.Int32]]

            private readonly MetadataSolver Assembly;

            public SignatureProvider(MetadataSolver Assembly) => this.Assembly = Assembly;

            public IType GetModifiedType(IType modifier, IType unmodifiedType, bool isRequired)
            {
                // Ignoreable (makes 0 difference for the CLI see $7.1.1)
                return unmodifiedType;
            }


            public IType GetFunctionPointerType(MethodSignature<IType> signature) => throw new NotSupportedException("Function Pointers are not Supported!");

            public IType GetArrayType(IType elementType, ArrayShape shape) => elementType.MakeArrayType(shape);

            public IType GetByReferenceType(IType elementType) => elementType.MakeByRefType();
            public IType GetPinnedType(IType elementType) => throw new NotSupportedException("Pinned Types are not Supported!");
            public IType GetPointerType(IType elementType) => elementType.MakePointerType();
            public IType GetSZArrayType(IType elementType) => elementType.MakeSZArrayType();


            public IType GetPrimitiveType(PrimitiveTypeCode typeCode)
            {
                switch (typeCode)
                {
                    case PrimitiveTypeCode.Boolean: return this.Assembly.Context.ResolveType(typeof(Boolean));
                    case PrimitiveTypeCode.Byte: return this.Assembly.Context.ResolveType(typeof(Byte));
                    case PrimitiveTypeCode.SByte: return this.Assembly.Context.ResolveType(typeof(SByte));
                    case PrimitiveTypeCode.Char: return this.Assembly.Context.ResolveType(typeof(Char));
                    case PrimitiveTypeCode.Int16: return this.Assembly.Context.ResolveType(typeof(Int16));
                    case PrimitiveTypeCode.UInt16: return this.Assembly.Context.ResolveType(typeof(UInt16));
                    case PrimitiveTypeCode.Int32: return this.Assembly.Context.ResolveType(typeof(Int32));
                    case PrimitiveTypeCode.UInt32: return this.Assembly.Context.ResolveType(typeof(UInt32));
                    case PrimitiveTypeCode.Int64: return this.Assembly.Context.ResolveType(typeof(Int64));
                    case PrimitiveTypeCode.UInt64: return this.Assembly.Context.ResolveType(typeof(UInt64));
                    case PrimitiveTypeCode.Single: return this.Assembly.Context.ResolveType(typeof(Single));
                    case PrimitiveTypeCode.Double: return this.Assembly.Context.ResolveType(typeof(Double));
                    case PrimitiveTypeCode.IntPtr: return this.Assembly.Context.ResolveType(typeof(IntPtr));
                    case PrimitiveTypeCode.UIntPtr: return this.Assembly.Context.ResolveType(typeof(UIntPtr));
                    case PrimitiveTypeCode.Object: return this.Assembly.Context.ResolveType(typeof(Object));
                    case PrimitiveTypeCode.String: return this.Assembly.Context.ResolveType(typeof(String));
                    case PrimitiveTypeCode.TypedReference: return this.Assembly.Context.ResolveType(typeof(TypedReference));
                    case PrimitiveTypeCode.Void: return this.Assembly.Context.ResolveType(typeof(void));
                }

                throw new NotSupportedException("Illegal/Unknown PrimitiveTypeCode!");
            }


            public IType GetGenericInstantiation(IType genericType, ImmutableArray<IType> typeArguments) => genericType.ConstructGeneric(typeArguments.ToArray());

            public IType GetGenericMethodParameter(IGeneric genericContext, int index)
            {
                return genericContext is IMethod method
                    ? method.GenericArguments[index]
                    : throw new Exception("Unexpected Behaivour!");
            }
            public IType GetGenericTypeParameter(IGeneric genericContext, int index)
            {
                if (genericContext is IMethod method)
                    genericContext = method.DeclaringType;

                if (!(genericContext is IType current))
                    throw new Exception("Unexpected Behaivour!");

                Stack<IType> genericStack = new Stack<IType>();

                while (current != null)
                {
                    genericStack.Push(current);
                    current = current.DeclaringType;
                }

                while (index >= 0)
                {
                    current = genericStack.Pop();
                    if (current.GenericArguments.Length > index)
                        return genericContext.GenericArguments[index];
                    else
                        index -= current.GenericArguments.Length;
                }

                throw new Exception("Generic Resolution Stack Overrun");
            }

            public IType GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind) => this.Assembly.GetTypeDefinition(handle);
            public IType GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind) => this.Assembly.GetTypeReference(handle);
            public IType GetTypeFromSpecification(MetadataReader reader, IGeneric genericContext, TypeSpecificationHandle handle, byte rawTypeKind) => Assembly.GetTypeSpecification(handle, genericContext);
        }
    }
}
