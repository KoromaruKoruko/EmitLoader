using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Metadata;

namespace EmitLoader.Metadata
{
    internal class MetadataEvent : MetadataEventBase
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
        public override EventAttributes Attributes => this.Def.Attributes;

        public override IType EventType
        {
            get
            {
                if(this._EventType == null)
                {
                    switch(this.Def.Type.Kind)
                    {
                        case HandleKind.TypeReference:
                            this._EventType = this.Assembly.GetTypeReference((TypeReferenceHandle)this.Def.Type);
                            break;
                        case HandleKind.TypeDefinition:
                            this._EventType = this.Assembly.GetTypeDefinition((TypeDefinitionHandle)this.Def.Type);
                            break;
                        case HandleKind.TypeSpecification:
                            this._EventType = this.Assembly.GetTypeSpecification((TypeSpecificationHandle)this.Def.Type, this.DeclaringType);
                            break;
                    }
                }
                return this._EventType;
            }
        }
        private IType _EventType;


        public override MetadataMethodBase Adder
        {
            get
            {
                if (this._Adder == null)
                    this._Adder = this.Assembly.GetMethodDefinition(this.Accessors.Adder);
                return this._Adder;
            }
        }
        public MetadataMethodBase _Adder;
        public override MetadataMethodBase Remover
        {
            get
            {
                if (this._Remover == null)
                    this._Remover = this.Assembly.GetMethodDefinition(this.Accessors.Remover);
                return this._Remover;
            }
        }
        public MetadataMethodBase _Remover;
        public override MetadataMethodBase Raiser
        {
            get
            {
                if (this._Raiser == null)
                    this._Raiser = this.Assembly.GetMethodDefinition(this.Accessors.Raiser);
                return this._Raiser;
            }
        }
        public MetadataMethodBase _Raiser;

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

        public MetadataEvent(EventDefinitionHandle Handle, MetadataType DeclaringType)
        {
            this.DeclaringType = DeclaringType;
            this.Def = this.Assembly.MD.GetEventDefinition(Handle);
            this.Handle = Handle;
            this.Accessors = this.Def.GetAccessors();
        }
        internal EventDefinition Def;
        internal EventDefinitionHandle Handle;
        internal EventAccessors Accessors;

        internal MetadataConstructedEvent Rebase(MetadataConstructedType newDeclaringType)
        {
            if (this.rebasedEvents.TryGetValue(newDeclaringType, out MetadataConstructedEvent @event))
                return @event;

            lock (this.rebasedEvents)
            {
                if (this.rebasedEvents.TryGetValue(newDeclaringType, out @event))
                    return @event;

                @event = new MetadataConstructedEvent(this, newDeclaringType);
                this.rebasedEvents.Add(newDeclaringType, @event);
                return @event;
            }
        }
        private readonly Dictionary<IType, MetadataConstructedEvent> rebasedEvents = new Dictionary<IType, MetadataConstructedEvent>(InterfaceComparer.Instance);
    }
}
