using System;
using System.Collections.Generic;
using System.Reflection;

namespace EmitLoader
{
    /// <summary>
    /// Field &amp; Property Access Flags Read/Write 
    /// </summary>
    [Flags]
    public enum FieldAccess
    {
        /// <summary>
        /// Read
        /// </summary>
        Read = 1,
        /// <summary>
        /// Write
        /// </summary>
        Write = 2,
        /// <summary>
        /// Read &amp; Write
        /// </summary>
        ReadWrite = 3,
    }

    /// <summary>
    /// Event Access Flags, Add/Remove/Raise
    /// </summary>
    [Flags]
    public enum EventAccess
    {
        /// <summary>
        /// Add
        /// </summary>
        Add = 1,
        /// <summary>
        /// Remove
        /// </summary>
        Remove = 2,
        /// <summary>
        /// Raise
        /// </summary>
        Raise = 4,
        /// <summary>
        /// Add, Remove &amp; Raise
        /// </summary>
        Full = Add | Remove | Raise,
        /// <summary>
        /// Add &amp; Remove
        /// </summary>
        AddAndRemove = Add | Remove,
    }
    /// <summary>
    /// Premade WhiteList Access Controller
    /// </summary>
    public sealed class WhiteListAccessController : IAccessController
    {
        internal List<Assembly> _FullAssemblies = new List<Assembly>();
        internal List<Assembly> _PartialAssemblies = new List<Assembly>();

        internal List<string> _FullNamespaces = new List<string>();
        internal List<string> _PartialNamespaces = new List<string>();

        internal List<Type> _FullTypes = new List<Type>();
        internal List<Type> _PartialTypes = new List<Type>();

        internal List<FieldInfo> _FullField = new List<FieldInfo>();
        internal List<FieldInfo> _FieldRead = new List<FieldInfo>();
        internal List<FieldInfo> _FieldWrite = new List<FieldInfo>();

        internal List<MethodInfo> _Methods = new List<MethodInfo>();
        internal List<ConstructorInfo> _Constructors = new List<ConstructorInfo>();

        /// <inheritdoc cref="IAccessController.CanAccess(Assembly)"/>
        public AccessKind CanAccess(Assembly assembly) =>
            this._FullAssemblies.Contains(assembly)
                ? AccessKind.Full
                : this._PartialAssemblies.Contains(assembly)
                    ? AccessKind.Partial
                    : AccessKind.None;


        /// <inheritdoc cref="IAccessController.CanAccess(string)"/>
        public AccessKind CanAccess(string @namespace) =>
            this._FullNamespaces.Contains(@namespace)
                ? AccessKind.Full
                : this._PartialNamespaces.Contains(@namespace)
                    ? AccessKind.Partial
                    : AccessKind.None;


        /// <inheritdoc cref="IAccessController.CanAccess(Type)"/>
        public AccessKind CanAccess(Type type) =>
            CanAccess(type.Assembly) == AccessKind.Full
                || CanAccess(type.Namespace) == AccessKind.Full
                || (type.IsNested && CanAccess(type.DeclaringType) == AccessKind.Full)
                || this._FullTypes.Contains(type)
                ? AccessKind.Full
                : this._PartialTypes.Contains(type)
                    ? AccessKind.Partial
                    : AccessKind.None;


        /// <inheritdoc cref="IAccessController.CanAccess(MethodInfo)"/>
        public AccessKind CanAccess(MethodInfo method) =>
            CanAccess(method.DeclaringType) == AccessKind.Full
                ? AccessKind.Full
                : this._Methods.Contains(method)
                    ? AccessKind.Full
                    : AccessKind.None;


        /// <inheritdoc cref="IAccessController.CanAccess(ConstructorInfo)"/>
        public AccessKind CanAccess(ConstructorInfo constructor) =>
            CanAccess(constructor.DeclaringType) == AccessKind.Full
                ? AccessKind.Full
                : this._Constructors.Contains(constructor)
                    ? AccessKind.Full
                    : AccessKind.None;


        /// <inheritdoc cref="IAccessController.CanAccess(FieldInfo)"/>
        public AccessKind CanAccess(FieldInfo field) =>
            CanAccess(field.DeclaringType) == AccessKind.Full || this._FullField.Contains(field)
                ? AccessKind.Full
                : this._FieldRead.Contains(field) || this._FieldWrite.Contains(field)
                    ? AccessKind.Partial
                    : AccessKind.None;


        /// <inheritdoc cref="IAccessController.CanGet(FieldInfo)"/>
        public bool CanGet(FieldInfo field) => CanAccess(field) == AccessKind.Full || this._FieldRead.Contains(field);
        /// <inheritdoc cref="IAccessController.CanSet(FieldInfo)"/>
        public bool CanSet(FieldInfo field) => CanAccess(field) == AccessKind.Full || this._FieldWrite.Contains(field);



        /// <summary>
        /// Gives Full Access to everything within this Assembly
        /// </summary>
        /// <param name="assembly">Assembly</param>
        /// <exception cref="InvalidOperationException">You MUST go top down, Assembly->Type->Member</exception>
        public void Whitelist(Assembly assembly)
        {
            if (this._FullTypes.Count > 0 || this._PartialTypes.Count > 0)
                throw new InvalidOperationException("Must be setup top, down");

            if (!this._FullAssemblies.Contains(assembly))
                this._FullAssemblies.Add(assembly);
        }
        /// <summary>
        /// Gives Full Access to everything found within this Namespace (NOT RECOMMENDED)
        /// </summary>
        /// <param name="namespace">Namespace</param>
        /// <exception cref="InvalidOperationException">You MUST go top down, Assembly->Type->Member</exception>
        public void Whitelist(String @namespace)
        {
            if (this._FullTypes.Count > 0 || this._PartialTypes.Count > 0)
                throw new InvalidOperationException("Must be setup top, down");

            if (!this._FullNamespaces.Contains(@namespace))
                this._FullNamespaces.Add(@namespace);
        }
        private void PartialWhitelist(Assembly assembly)
        {
            if (!this._PartialAssemblies.Contains(assembly))
                this._PartialAssemblies.Add(assembly);
        }
        private void PartialWhitelist(String @namespace)
        {
            if (!this._PartialNamespaces.Contains(@namespace))
                this._PartialNamespaces.Add(@namespace);
        }
        private void PartialWhitelist(Type type)
        {
            if (type.IsNested)
            {
                Type t = type.DeclaringType;
                while (t != null)
                {
                    if (!this._PartialTypes.Contains(t))
                        this._PartialTypes.Add(t);
                    t = t.DeclaringType;
                }
            }
            if (!this._PartialTypes.Contains(type))
                this._PartialTypes.Add(type);
        }
        /// <summary>
        /// Give Full Access to this Type
        /// </summary>
        /// <param name="type">Type</param>
        /// <exception cref="InvalidOperationException">You MUST go top down, Assembly->Type->Member</exception>
        public void Whitelist(Type type)
        {
            if (this._FieldRead.Count > 0 || this._FieldWrite.Count > 0 || this._Methods.Count > 0 || this._Constructors.Count > 0)
                throw new InvalidOperationException("Must be setup top, down");

            if (CanAccess(type) == AccessKind.Full)
                return;

            PartialWhitelist(type.Assembly);
            PartialWhitelist(type.Namespace);

            if (type.IsNested)
                PartialWhitelist(type.DeclaringType);

            this._FullTypes.Add(type);
        }
        /// <summary>
        /// Give Access to call this Method
        /// </summary>
        /// <param name="constructor">Method</param>
        public void Whitelist(ConstructorInfo constructor)
        {
            if (CanAccess(constructor) == AccessKind.Full)
                return;

            PartialWhitelist(constructor.DeclaringType);

            if (!this._Constructors.Contains(constructor))
                this._Constructors.Add(constructor);
        }
        /// <summary>
        /// Give Access to call this Method
        /// </summary>
        /// <param name="method">Method</param>
        public void Whitelist(MethodInfo method)
        {
            if (CanAccess(method) == AccessKind.Full)
                return;

            PartialWhitelist(method.DeclaringType);

            if (!this._Methods.Contains(method))
                this._Methods.Add(method);
        }
        /// <summary>
        /// Give Access to this Field
        /// </summary>
        /// <param name="field">Field</param>
        /// <param name="access">Read/Write Access</param>
        public void Whitelist(FieldInfo field, FieldAccess access)
        {
            if (CanAccess(field) == AccessKind.Full)
                return;
            PartialWhitelist(field.DeclaringType);
            switch (access)
            {
                case FieldAccess.Read:
                    if (CanSet(field))
                    {
                        this._FullField.Add(field);
                        this._FieldWrite.Remove(field);
                    }
                    else if (!this._FieldRead.Contains(field))
                        this._FieldRead.Add(field);
                    break;
                case FieldAccess.Write:
                    if (CanGet(field))
                    {
                        this._FullField.Add(field);
                        this._FieldRead.Remove(field);
                    }
                    else if (!this._FieldWrite.Contains(field))
                        this._FieldWrite.Add(field);
                    break;
                case FieldAccess.ReadWrite:
                    if (!this._FullField.Contains(field))
                        this._FullField.Add(field);
                    break;
            }
        }
        /// <summary>
        /// Gives Access to this Propertys Getter/Setter
        /// </summary>
        /// <param name="property">Property</param>
        /// <param name="access">Get/Set Access</param>
        public void Whitelist(PropertyInfo property, FieldAccess access)
        {
            if (property.GetMethod != null && access.HasFlag(FieldAccess.Read))
                Whitelist(property.GetMethod);
            if (property.SetMethod != null && access.HasFlag(FieldAccess.Write))
                Whitelist(property.SetMethod);
        }
        /// <summary>
        /// Gives Access to this Events Add/Remove/Raise Methods
        /// </summary>
        /// <param name="event">Event</param>
        /// <param name="access">Add/Remove/Raise Access</param>
        public void Whitelist(EventInfo @event, EventAccess access)
        {
            if (@event.AddMethod != null && access.HasFlag(EventAccess.Add))
                Whitelist(@event.AddMethod);

            if (@event.RemoveMethod != null && access.HasFlag(EventAccess.Remove))
                Whitelist(@event.RemoveMethod);

            if (@event.RaiseMethod != null && access.HasFlag(EventAccess.Raise))
                Whitelist(@event.RaiseMethod);
        }
    }
}
