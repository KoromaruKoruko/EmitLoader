using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Metadata;

namespace EmitLoader.Metadata
{
    internal class MetadataField : MetadataFieldBase
    {
        public override MetadataSolver Assembly => this.DeclaringType.Assembly;
        public override MetadataTypeBase DeclaringType { get; }

        public override string Name
        {
            get
            {
                if (this._Name == null)
                    this._Name = this.Assembly.MD.GetString(this.Def.Name);

                return this._Name;
            }
        }
        private string _Name;

        public override IType FieldType
        {
            get
            {
                if (this._FieldType == null)
                    this._FieldType = this.Def.DecodeSignature(this.Assembly.SP, this.DeclaringType);

                return this._FieldType;
            }
        }
        internal IType _FieldType;


        public override FieldAttributes Attributes => this.Def.Attributes;

        public override MetadataConstant DefaultValue
        {
            get
            {
                if (this._DefaultValue == null)
                    this._DefaultValue = this.Assembly.GetConstant(this.Def.GetDefaultValue());
                return this._DefaultValue;
            }
        }
        private MetadataConstant _DefaultValue;

        public override MetadataCustomAttributeBase[] CustomAttributes
        {
            get
            {
                if (this._CustomAttributes == null)
                {
                    CustomAttributeHandleCollection collection = this.Def.GetCustomAttributes();
                    this._CustomAttributes = new MetadataCustomAttribute[collection.Count];
                    int x = 0;
                    foreach (CustomAttributeHandle handle in collection)
                        this.CustomAttributes[x++] = new MetadataCustomAttribute(this.Assembly.MD.GetCustomAttribute(handle), this.DeclaringType);
                }
                return this._CustomAttributes;
            }
        }
        private MetadataCustomAttributeBase[] _CustomAttributes;

        public MetadataField(FieldDefinitionHandle Handle, MetadataType DeclaringType)
        {
            this.DeclaringType = DeclaringType;
            this.Def = this.Assembly.MD.GetFieldDefinition(Handle);
            this.Handle = Handle;
        }
        internal FieldDefinition Def;
        internal FieldDefinitionHandle Handle;

        internal MetadataConstructedField Rebase(MetadataConstructedType newDeclaringType)
        {
            if (this.rebasedFields.TryGetValue(newDeclaringType, out MetadataConstructedField field))
                return field;

            lock (this.rebasedFields)
            {
                if (this.rebasedFields.TryGetValue(newDeclaringType, out field))
                    return field;

                field = new MetadataConstructedField(this, newDeclaringType);
                this.rebasedFields.Add(newDeclaringType, field);
                return field;
            }
        }
        private Dictionary<IType, MetadataConstructedField> rebasedFields = new Dictionary<IType, MetadataConstructedField>(InterfaceComparer.Instance);
    }
}
