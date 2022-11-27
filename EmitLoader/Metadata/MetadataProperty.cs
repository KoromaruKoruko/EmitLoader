using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Metadata;

namespace EmitLoader.Metadata
{
    internal class MetadataProperty : MetadataPropertyBase
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

        public override PropertyAttributes Attributes => this.Def.Attributes;
        public override IType PropertyType => Getter.ReturnType;

        public override MetadataMethodBase Getter
        {
            get
            {
                if (this._Getter == null)
                    this._Getter = this.Assembly.GetMethodDefinition(this.Accessors.Getter);
                return this._Getter;
            }
        }
        public MetadataMethodBase _Getter;
        public override MetadataMethodBase Setter
        {
            get
            {
                if (this._Setter == null)
                    this._Setter = this.Assembly.GetMethodDefinition(this.Accessors.Setter);
                return this._Setter;
            }
        }
        public MetadataMethodBase _Setter;

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

        public MetadataProperty(PropertyDefinitionHandle Handle, MetadataType DeclaringType)
        {
            this.DeclaringType = DeclaringType;
            this.Def = this.Assembly.MD.GetPropertyDefinition(Handle);
            this.Handle = Handle;
            this.Accessors = this.Def.GetAccessors();
        }
        internal PropertyDefinition Def;
        internal PropertyDefinitionHandle Handle;
        internal PropertyAccessors Accessors;

        internal MetadataConstructedProperty Rebase(MetadataConstructedType newDeclaringType)
        {
            if (this.rebasedProperties.TryGetValue(newDeclaringType, out MetadataConstructedProperty property))
                return property;

            lock (this.rebasedProperties)
            {
                if (this.rebasedProperties.TryGetValue(newDeclaringType, out property))
                    return property;

                property = new MetadataConstructedProperty(this, newDeclaringType);
                this.rebasedProperties.Add(newDeclaringType, property);
                return property;
            }
        }
        private Dictionary<IType, MetadataConstructedProperty> rebasedProperties = new Dictionary<IType, MetadataConstructedProperty>(InterfaceComparer.Instance);
    }
}
