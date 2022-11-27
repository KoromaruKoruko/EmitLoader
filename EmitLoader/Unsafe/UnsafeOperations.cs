using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace EmitLoader.Unsafe
{
    /// <summary>
    /// Host Type to all the Poor Practice Unsafe code.
    /// will ensure safety on initial use
    /// </summary>
    internal static unsafe class UnsafeOperations
    {
        // => https://www.codeproject.com/Articles/1257186/Determining-Object-Layout-using-FieldDescs
        [StructLayout(LayoutKind.Explicit, Size = 40)]
        private struct FixedStructureReference
        {
#pragma warning disable IDE0044 // Add readonly modifier
            [FieldOffset(0)] private Byte _0;
            [FieldOffset(8)] public UInt64 _8;
            [FieldOffset(10)] private Int64 _10;
            [FieldOffset(16)] public UInt32 _16;
            [FieldOffset(20)] private Int32 _20;
            [FieldOffset(24)] public readonly UInt16 _24;
            [FieldOffset(32)] private readonly UInt64 _32;
#pragma warning restore IDE0044 // Add readonly modifier
        }

        // relative offsets
        internal static readonly uint TypeDefinitionHandle_rowid_ro;
        internal static readonly uint TypeReferenceHandle_rowid_ro;
        internal static readonly uint TypeSpecificationHandle_rowid_ro;
        internal static readonly uint MethodDefinitionHandle_rowid_ro;
        internal static readonly uint MemberReferenceHandle_rowid_ro;
        internal static readonly uint ConstantHandle_rowid_ro;
        internal static readonly uint InterfaceImplementationHandle_rowid_ro;
        internal static readonly uint NamespaceDefinitionHandle_value_ro;
        internal static readonly uint FieldDefinitionHandle_rowid_ro;
        internal static readonly uint EventDefinitionHandle_rowid_ro;
        internal static readonly uint PropertyDefinitionHandle_rowid_ro;

        static UnsafeOperations()
        {
            // Runtime Sanity Check
            Type FSR = typeof(FixedStructureReference);
            foreach (FieldInfo field in FSR.GetRuntimeFields())
            {
                UInt32 expectedOffset = UInt32.Parse(field.Name.Trim('_'));
                UInt32 marshalOffset = unchecked((UInt32)Marshal.OffsetOf(FSR, field.Name).ToInt32());
                if (marshalOffset != expectedOffset)
                    throw new NotSupportedException("Runtime Not Supported! (Marshal.OffsetOf failed sanity check!)");
            }

            if (unchecked((UInt32)Marshal.OffsetOf<EntityHandle>("_vToken").ToInt32()) != 0 || Marshal.SizeOf<EntityHandle>() != sizeof(uint))
                throw new NotSupportedException("System.Reflection.Metadata.EntityHandle has an unexpected Structure!");

            TypeDefinitionHandle_rowid_ro = unchecked((UInt32)Marshal.OffsetOf<TypeDefinitionHandle>("_rowId").ToInt32());
            TypeReferenceHandle_rowid_ro = unchecked((UInt32)Marshal.OffsetOf<TypeReferenceHandle>("_rowId").ToInt32());
            TypeSpecificationHandle_rowid_ro = unchecked((UInt32)Marshal.OffsetOf<TypeSpecificationHandle>("_rowId").ToInt32());
            MethodDefinitionHandle_rowid_ro = unchecked((UInt32)Marshal.OffsetOf<MethodDefinitionHandle>("_rowId").ToInt32());
            MemberReferenceHandle_rowid_ro = unchecked((UInt32)Marshal.OffsetOf<MemberReferenceHandle>("_rowId").ToInt32());
            ConstantHandle_rowid_ro = unchecked((UInt32)Marshal.OffsetOf<ConstantHandle>("_rowId").ToInt32());
            InterfaceImplementationHandle_rowid_ro = unchecked((UInt32)Marshal.OffsetOf<InterfaceImplementationHandle>("_rowId").ToInt32());
            NamespaceDefinitionHandle_value_ro = unchecked((UInt32)Marshal.OffsetOf<NamespaceDefinitionHandle>("_value").ToInt32());
            FieldDefinitionHandle_rowid_ro = unchecked((UInt32)Marshal.OffsetOf<FieldDefinitionHandle>("_rowId").ToInt32());
            EventDefinitionHandle_rowid_ro = unchecked((UInt32)Marshal.OffsetOf<EventDefinitionHandle>("_rowId").ToInt32());
            PropertyDefinitionHandle_rowid_ro = unchecked((UInt32)Marshal.OffsetOf<PropertyDefinitionHandle>("_rowId").ToInt32());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static EntityHandle CastMetaTokenToEntityHandle(uint metaToken) => *(EntityHandle*)&metaToken;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetRowId(TypeDefinitionHandle handle) => *(int*)(((byte*)&handle) + TypeDefinitionHandle_rowid_ro);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetRowId(TypeReferenceHandle handle) => *(int*)(((byte*)&handle) + TypeReferenceHandle_rowid_ro);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetRowId(TypeSpecificationHandle handle) => *(int*)(((byte*)&handle) + TypeSpecificationHandle_rowid_ro);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetRowId(MethodDefinitionHandle handle) => *(int*)(((byte*)&handle) + MethodDefinitionHandle_rowid_ro);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetRowId(MemberReferenceHandle handle) => *(int*)(((byte*)&handle) + MemberReferenceHandle_rowid_ro);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetRowId(ConstantHandle handle) => *(int*)(((byte*)&handle) + ConstantHandle_rowid_ro);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetRowId(InterfaceImplementationHandle handle) => *(int*)(((byte*)&handle) + InterfaceImplementationHandle_rowid_ro);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetRowId(NamespaceDefinitionHandle handle) => *(int*)(((byte*)&handle) + NamespaceDefinitionHandle_value_ro);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetRowId(FieldDefinitionHandle handle) => *(int*)(((byte*)&handle) + FieldDefinitionHandle_rowid_ro);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetRowId(EventDefinitionHandle handle) => *(int*)(((byte*)&handle) + EventDefinitionHandle_rowid_ro);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetRowId(PropertyDefinitionHandle handle) => *(int*)(((byte*)&handle) + PropertyDefinitionHandle_rowid_ro);


        internal unsafe class HandleComparers
                : IComparer<TypeDefinitionHandle>,
                IComparer<TypeReferenceHandle>,
                IComparer<TypeSpecificationHandle>,
                IComparer<MethodDefinitionHandle>,
                IComparer<MemberReferenceHandle>,
                IComparer<ConstantHandle>,
                IComparer<InterfaceImplementationHandle>,
                IComparer<NamespaceDefinitionHandle>,
                IComparer<FieldDefinitionHandle>,
                IComparer<EventDefinitionHandle>,
                IComparer<PropertyDefinitionHandle>,
                IEqualityComparer<TypeDefinitionHandle>,
                IEqualityComparer<TypeReferenceHandle>,
                IEqualityComparer<TypeSpecificationHandle>,
                IEqualityComparer<MethodDefinitionHandle>,
                IEqualityComparer<MemberReferenceHandle>,
                IEqualityComparer<ConstantHandle>,
                IEqualityComparer<InterfaceImplementationHandle>,
                IEqualityComparer<NamespaceDefinitionHandle>,
                IEqualityComparer<FieldDefinitionHandle>,
                IEqualityComparer<EventDefinitionHandle>,
                IEqualityComparer<PropertyDefinitionHandle>
        {
            public static readonly HandleComparers Instance = new HandleComparers();
            private HandleComparers() { }


            public int Compare(TypeDefinitionHandle x, TypeDefinitionHandle y) => GetRowId(x) - GetRowId(y);
            public bool Equals(TypeDefinitionHandle x, TypeDefinitionHandle y) => GetRowId(x) == GetRowId(y);
            public int GetHashCode(TypeDefinitionHandle obj) => GetRowId(obj);


            public int Compare(TypeSpecificationHandle x, TypeSpecificationHandle y) => GetRowId(x) - GetRowId(y);
            public bool Equals(TypeSpecificationHandle x, TypeSpecificationHandle y) => GetRowId(x) == GetRowId(y);
            public int GetHashCode(TypeSpecificationHandle obj) => GetRowId(obj);


            public int Compare(TypeReferenceHandle x, TypeReferenceHandle y) => GetRowId(x) - GetRowId(y);
            public bool Equals(TypeReferenceHandle x, TypeReferenceHandle y) => GetRowId(x) == GetRowId(y);
            public int GetHashCode(TypeReferenceHandle obj) => GetRowId(obj);


            public int Compare(MethodDefinitionHandle x, MethodDefinitionHandle y) => GetRowId(x) - GetRowId(y);
            public bool Equals(MethodDefinitionHandle x, MethodDefinitionHandle y) => GetRowId(x) == GetRowId(y);
            public int GetHashCode(MethodDefinitionHandle obj) => GetRowId(obj);


            public int Compare(MemberReferenceHandle x, MemberReferenceHandle y) => GetRowId(x) - GetRowId(y);
            public bool Equals(MemberReferenceHandle x, MemberReferenceHandle y) => GetRowId(x) == GetRowId(y);
            public int GetHashCode(MemberReferenceHandle obj) => GetRowId(obj);


            public int Compare(ConstantHandle x, ConstantHandle y) => GetRowId(x) - GetRowId(y);
            public bool Equals(ConstantHandle x, ConstantHandle y) => GetRowId(x) == GetRowId(y);
            public int GetHashCode(ConstantHandle obj) => GetRowId(obj);


            public int Compare(InterfaceImplementationHandle x, InterfaceImplementationHandle y) => GetRowId(x) - GetRowId(y);
            public bool Equals(InterfaceImplementationHandle x, InterfaceImplementationHandle y) => GetRowId(x) == GetRowId(y);
            public int GetHashCode(InterfaceImplementationHandle obj) => GetRowId(obj);


            public int Compare(NamespaceDefinitionHandle x, NamespaceDefinitionHandle y) => GetRowId(x) - GetRowId(y);
            public bool Equals(NamespaceDefinitionHandle x, NamespaceDefinitionHandle y) => GetRowId(x) == GetRowId(y);
            public int GetHashCode(NamespaceDefinitionHandle obj) => GetRowId(obj);


            public int Compare(FieldDefinitionHandle x, FieldDefinitionHandle y) => GetRowId(x) - GetRowId(y);
            public bool Equals(FieldDefinitionHandle x, FieldDefinitionHandle y) => GetRowId(x) == GetRowId(y);
            public int GetHashCode(FieldDefinitionHandle obj) => GetRowId(obj);


            public int Compare(EventDefinitionHandle x, EventDefinitionHandle y) => GetRowId(x) - GetRowId(y);
            public bool Equals(EventDefinitionHandle x, EventDefinitionHandle y) => GetRowId(x) == GetRowId(y);
            public int GetHashCode(EventDefinitionHandle obj) => GetRowId(obj);


            public int Compare(PropertyDefinitionHandle x, PropertyDefinitionHandle y) => GetRowId(x) - GetRowId(y);
            public bool Equals(PropertyDefinitionHandle x, PropertyDefinitionHandle y) => GetRowId(x) == GetRowId(y);
            public int GetHashCode(PropertyDefinitionHandle obj) => GetRowId(obj);
        }
    }
}
