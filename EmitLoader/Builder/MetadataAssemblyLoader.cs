using EmitLoader.Metadata;
using EmitLoader.Unsafe;

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Reflection.Metadata;

namespace EmitLoader.Builder
{
    // TODO: [EMIT] Implement Custom Attributes
    // TODO: [EMIT] Module Definition

    // TODO: [EMIT] [OPT] Type, Field, Method Redirection.

    internal class MetadataAssemblyLoader
    {
        internal readonly AccessControlManager AccessController;
        internal readonly MetadataSolver Assembly;
        internal readonly AssemblyBuilder AssemblyBuilder;
        internal readonly ModuleBuilder ModuleBuilder;

        internal MetadataAssemblyLoader(MetadataSolver Assembly, AccessControlManager AccessController)
        {
            this.Assembly = Assembly;
            this.AssemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(Assembly.Name, AssemblyBuilderAccess.RunAndCollect);
            this.ModuleBuilder = this.AssemblyBuilder.DefineDynamicModule(Assembly.MD.GetString(Assembly.MD.GetModuleDefinition().Name));
            this.AccessController = AccessController;

            AccessController.RegisterLoader(this.AssemblyBuilder);
        }

        private readonly SortedDictionary<TypeDefinitionHandle, TypeBuilder> _Types = new SortedDictionary<TypeDefinitionHandle, TypeBuilder>(UnsafeOperations.HandleComparers.Instance);
        private readonly SortedDictionary<FieldDefinitionHandle, FieldBuilder> _Fields = new SortedDictionary<FieldDefinitionHandle, FieldBuilder>(UnsafeOperations.HandleComparers.Instance);
        private readonly SortedDictionary<MethodDefinitionHandle, (ConstructorBuilder, MetadataMethod)> _Constructors = new SortedDictionary<MethodDefinitionHandle, (ConstructorBuilder, MetadataMethod)>(UnsafeOperations.HandleComparers.Instance);
        private readonly SortedDictionary<MethodDefinitionHandle, (MethodBuilder, MetadataMethod)> _Methods = new SortedDictionary<MethodDefinitionHandle, (MethodBuilder, MetadataMethod)>(UnsafeOperations.HandleComparers.Instance);
        private readonly SortedDictionary<PropertyDefinitionHandle, PropertyBuilder> _Properties = new SortedDictionary<PropertyDefinitionHandle, PropertyBuilder>(UnsafeOperations.HandleComparers.Instance);
        private readonly SortedDictionary<EventDefinitionHandle, EventBuilder> _Events = new SortedDictionary<EventDefinitionHandle, EventBuilder>(UnsafeOperations.HandleComparers.Instance);

        public void BuildMethods()
        {
            // build constructors
            foreach ((ConstructorBuilder builder, MetadataMethod method) in _Constructors.Values)
                method.GetMethodBody().Compile(builder.GetILGenerator());

            // build methods
            foreach ((MethodBuilder builder, MetadataMethod method) in _Methods.Values)
                method.GetMethodBody().Compile(builder.GetILGenerator());
        }

        public TypeBuilder GetTypeBuilder(MetadataType metaType)
        {
            if (this._Types.TryGetValue(metaType.Handle, out TypeBuilder type))
                return type;

            if (metaType.Name == "<Module>")
                throw new InvalidOperationException("Can't define Main Module as Type!");

            lock (this._Types)
            {
                if (this._Types.TryGetValue(metaType.Handle, out type))
                    return type;

                if (metaType.DeclaringType != null)
                {
                    type = GetTypeBuilder((MetadataType)metaType.DeclaringType);
                    if (metaType.BaseType != null)
                    {
                        Type BuiltType = metaType.BaseType.GetBuiltType();
                        AccessController.ValidateAccess(BuiltType);
                        type = type.DefineNestedType(metaType.Name, metaType.Attributes, BuiltType);
                    }
                    else
                        type = type.DefineNestedType(metaType.Name, metaType.Attributes);
                }
                else
                {
                    string fullName = metaType.Namespace != null && !metaType.Namespace.IsGlobalNamespace
                        ? $"{metaType.Namespace.GetFullyQualifiedName()}.{metaType.Name}"
                        : metaType.Name;

                    if (metaType.BaseType != null)
                    {
                        Type BuiltType = metaType.BaseType.GetBuiltType();
                        AccessController.ValidateAccess(BuiltType);
                        type = this.ModuleBuilder.DefineType(fullName, metaType.Attributes, BuiltType);
                    }
                    else
                        type = this.ModuleBuilder.DefineType(fullName, metaType.Attributes);
                }

                if (metaType.IsGenericDefinition)
                {
                    MetadataGenericParameterType[] genericParams = (MetadataGenericParameterType[])metaType.GenericArguments;
                    String[] Names = new string[genericParams.Length];
                    for (int x = 0; x < Names.Length; x++)
                        Names[x] = genericParams[x].Name;

                    GenericTypeParameterBuilder[] paramBuilders = type.DefineGenericParameters(Names);

                    BuildGenericParameters(genericParams, paramBuilders);
                }

                for (int x = 0; x < metaType.Interfaces.Length; x++)
                {
                    Type BuiltType = metaType.Interfaces[x].GetBuiltType();
                    AccessController.ValidateAccess(BuiltType);
                    type.AddInterfaceImplementation(BuiltType);
                }

                this._Types.Add(metaType.Handle, type);
                return type;
            }
        }
        public FieldBuilder GetFieldBuilder(MetadataField metaField)
        {
            if (this._Fields.TryGetValue(metaField.Handle, out FieldBuilder field))
                return field;

            lock (this._Fields)
            {
                if (this._Fields.TryGetValue(metaField.Handle, out field))
                    return field;

                TypeBuilder type = this.GetTypeBuilder((MetadataType)metaField.DeclaringType);

                Type fieldType = metaField.FieldType.GetBuiltType();
                AccessController.ValidateAccess(fieldType);
                field = type.DefineField(metaField.Name, fieldType, metaField.Attributes);

                if (metaField.DefaultValue != null)
                    field.SetConstant(metaField.DefaultValue.Value);

                this._Fields.Add(metaField.Handle, field);
                return field;
            }
        }
        public ConstructorBuilder GetConstructorBuilder(MetadataMethod metaMethod)
        {
            if (metaMethod.Name != ".ctor" && metaMethod.Name != ".cctor")
                throw new InvalidOperationException("Not a Constructor Method!");

            if (this._Constructors.TryGetValue(metaMethod.Handle, out (ConstructorBuilder ctor, MetadataMethod _) result))
                return result.ctor;

            lock (this._Constructors)
            {
                if (this._Constructors.TryGetValue(metaMethod.Handle, out result))
                    return result.ctor;

                TypeBuilder type = this.GetTypeBuilder((MetadataType)metaMethod.DeclaringType);

                Type[] types = new Type[metaMethod.Parameters.Length];
                for (int x = 0; x < types.Length; x++)
                {
                    Type builtType = metaMethod.Parameters[x].ParameterType.GetBuiltType();
                    AccessController.ValidateAccess(builtType);
                    types[x] = builtType;
                }

                ConstructorBuilder ctor = type.DefineConstructor(
                    metaMethod.Attributes,
                    (metaMethod.Attributes & MethodAttributes.Static) == MethodAttributes.Static ? CallingConventions.Standard : CallingConventions.HasThis,
                    types
                );

                this._Constructors.Add(metaMethod.Handle, (ctor, metaMethod));
                return ctor;
            }
        }
        public MethodBuilder GetMethodBuilder(MetadataMethod metaMethod)
        {
            if (metaMethod.Name == ".ctor" || metaMethod.Name == ".cctor")
                throw new InvalidOperationException("Special Name Method!");

            if (this._Methods.TryGetValue(metaMethod.Handle, out (MethodBuilder method, MetadataMethod _) result))
                return result.method;

            lock (this._Methods)
            {
                if (this._Methods.TryGetValue(metaMethod.Handle, out result))
                    return result.method;

                TypeBuilder type = this.GetTypeBuilder((MetadataType)metaMethod.DeclaringType);

                Type[] types = new Type[metaMethod.Parameters.Length];
                for (int x = 0; x < types.Length; x++)
                {
                    Type builtType = metaMethod.Parameters[x].ParameterType.GetBuiltType();
                    AccessController.ValidateAccess(builtType);
                    types[x] = builtType;
                }

                Type returnType = metaMethod.ReturnType.GetBuiltType();
                AccessController.ValidateAccess(returnType);
                MethodBuilder method = type.DefineMethod(metaMethod.Name, metaMethod.Attributes, returnType, types);

                // implementation overrides
                int i = metaMethod.Name.LastIndexOf('.');
                if (i > 1) // we want to ignore special names
                {
                    string TypeName = metaMethod.Name.Substring(0, i);
                    string Name = metaMethod.Name.Substring(i + 1);

                    MethodInfo overrideMethod = null;
                    if (type.BaseType.FullName == TypeName)
                        overrideMethod = type.BaseType.GetMethod(Name, BindingFlags.Instance);
                    else
                        foreach (IType @interface in metaMethod.DeclaringType.Interfaces)
                            if (@interface.GetFullyQualifiedName() == TypeName)
                            {
                                overrideMethod = @interface.GetBuiltType().GetRuntimeMethod(Name, types);
                                break;
                            }


                    if (overrideMethod == null)
                        throw new Exception("Unable to Resolve OverrideMethod");

                    type.DefineMethodOverride(method, overrideMethod);
                }
                else if (i != -1)
                    throw new Exception("Unknown Special Name!");

                if (metaMethod.IsGenericDefinition)
                {
                    MetadataGenericParameterType[] genericParams = (MetadataGenericParameterType[])metaMethod.GenericArguments;
                    String[] Names = new String[genericParams.Length];
                    for (int x = 0; x < genericParams.Length; x++)
                        Names[x] = genericParams[x].Name;

                    GenericTypeParameterBuilder[] paramBuilders = method.DefineGenericParameters(Names);

                    BuildGenericParameters(genericParams, paramBuilders);
                }

                this._Methods.Add(metaMethod.Handle, (method, metaMethod));
                return method;
            }
        }
        public PropertyBuilder GetPropertyBuilder(MetadataProperty metaProperty)
        {
            if (this._Properties.TryGetValue(metaProperty.Handle, out PropertyBuilder property))
                return property;

            lock (this._Properties)
            {
                if (this._Properties.TryGetValue(metaProperty.Handle, out property))
                    return property;

                TypeBuilder type = this.GetTypeBuilder((MetadataType)metaProperty.DeclaringType);

                Type propertyType = metaProperty.PropertyType.GetBuiltType();
                AccessController.ValidateAccess(propertyType);
                property = type.DefineProperty(metaProperty.Name, metaProperty.Attributes, propertyType, null);

                this._Properties.Add(metaProperty.Handle, property);
                return property;
            }
        }
        public EventBuilder GetEventBuilder(MetadataEvent metaEvent)
        {
            if (this._Events.TryGetValue(metaEvent.Handle, out EventBuilder @event))
                return @event;

            lock (this._Events)
            {
                if (this._Events.TryGetValue(metaEvent.Handle, out @event))
                    return @event;

                TypeBuilder type = this.GetTypeBuilder((MetadataType)metaEvent.DeclaringType);

                Type eventType = metaEvent.EventType.GetBuiltType();
                AccessController.ValidateAccess(eventType);
                @event = type.DefineEvent(metaEvent.Name, metaEvent.Attributes, eventType);

                this._Events.Add(metaEvent.Handle, @event);
                return @event;
            }
        }

        private static void BuildGenericParameters(MetadataGenericParameterType[] @params, GenericTypeParameterBuilder[] builders)
        {
            List<Type> interfaceConstraints = new List<Type>();
            for (int x = 0; x < @params.Length; x++)
            {
                MetadataGenericParameterType param = @params[x];
                GenericTypeParameterBuilder paramBuilder = builders[x];

                paramBuilder.SetGenericParameterAttributes(param.GenericParameterAttributes);

                for (int y = 0; y < param.Constraints.Length; y++)
                {
                    MetadataGenericParameterConstraint constraint = param.Constraints[y];
                    if (constraint.ConstrainType.IsInterface)
                        interfaceConstraints.Add(constraint.ConstrainType.GetBuiltType());
                    else
                        paramBuilder.SetBaseTypeConstraint(constraint.ConstrainType.GetBuiltType());
                }
                if (interfaceConstraints.Count > 0)
                {
                    paramBuilder.SetInterfaceConstraints(interfaceConstraints.ToArray());
                    interfaceConstraints.Clear();
                }
            }
        }
    }
}
