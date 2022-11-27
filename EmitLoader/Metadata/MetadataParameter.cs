
using System;
using System.Collections.Generic;
using System.Reflection.Metadata;

namespace EmitLoader.Metadata
{
    internal class MetadataParameter : MetadataParameterBase
    {
        public override MetadataSolver Assembly => this.Parent.Assembly;

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

        public override MetadataMethodBase Parent { get; }

        public override IType ParameterType { get; }
        public override Boolean IsOptional { get; }

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
                        this.CustomAttributes[x++] = new MetadataCustomAttribute(this.Assembly.MD.GetCustomAttribute(handle), this.Parent);
                }
                return this._CustomAttributes;
            }
        }
        private MetadataCustomAttributeBase[] _CustomAttributes;

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

        public MetadataParameter(Parameter Def, IType ParameterType, Boolean IsOptional, MetadataMethodBase Parent)
        {
            this.Def = Def;
            this.Parent = Parent;
            this.ParameterType = ParameterType;
            this.IsOptional = IsOptional;
        }
        private Parameter Def;

        internal MetadataConstructedParameter Rebase(MetadataConstructedMethod newParent)
        {
            if (this.rebasedEvents.TryGetValue(newParent, out MetadataConstructedParameter parameter))
                return parameter;

            lock (this.rebasedEvents)
            {
                if (this.rebasedEvents.TryGetValue(newParent, out parameter))
                    return parameter;

                parameter = new MetadataConstructedParameter(this, this.ParameterType, newParent);
                this.rebasedEvents.Add(newParent, parameter);
                return parameter;
            }
        }
        private Dictionary<IMethod, MetadataConstructedParameter> rebasedEvents = new Dictionary<IMethod, MetadataConstructedParameter>(InterfaceComparer.Instance);
    }
}
