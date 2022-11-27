using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace EmitLoader.Reflection
{
    internal class ReflectionEvent : IEvent
    {
        public ReflectionEvent(EventInfo @event, ReflectionType declaringType)
        {
            this.@event = @event;
            this.declaringType = declaringType;
        }
        internal readonly EventInfo @event;
        internal readonly ReflectionType declaringType;

        public EventInfo GetBuiltEvent() => this.@event;
        public IType EventType
        {
            get
            {
                if (this._EventType == null)
                    this._EventType = this.Context.ResolveType(this.@event.AddMethod.GetParameters()[0].ParameterType);
                return this._EventType;
            }
        }
        private IType _EventType;
        public IMethod Adder
        {
            get
            {
                if (this._Adder == null)
                {
                    MethodInfo adder = this.@event.AddMethod;
                    if (adder != null)
                        this._Adder = new ReflectionMethod(adder, this.declaringType, null);
                }
                return this._Adder;
            }
        }
        private IMethod _Adder;
        public IMethod Remover
        {
            get
            {
                if (this._Remover == null)
                {
                    MethodInfo remove = this.@event.RemoveMethod;
                    if (remove != null)
                        this._Remover = new ReflectionMethod(remove, this.declaringType, null);
                }
                return this._Remover;
            }
        }
        private IMethod _Remover;
        public IMethod Raiser
        {
            get
            {
                if (this._Raiser == null)
                {
                    MethodInfo raiser = this.@event.RaiseMethod;
                    if (raiser != null)
                        this._Raiser = new ReflectionMethod(raiser, this.declaringType, null);
                }
                return this._Raiser;
            }
        }
        private IMethod _Raiser;
        public IType DeclaringType => this.declaringType;
        public ICustomAttribute[] CustomAttributes
        {
            get
            {
                if (this._CustomAttributes == null)
                {
                    IList<CustomAttributeData> attributes = this.@event.GetCustomAttributesData();
                    this._CustomAttributes = new ReflectionCustomAttribute[attributes.Count];
                    for (int x = 0; x < attributes.Count; x++)
                        this._CustomAttributes[x] = new ReflectionCustomAttribute(attributes[x], this.declaringType.assembly);
                }
                return this._CustomAttributes;
            }
        }
        private ICustomAttribute[] _CustomAttributes;
        public string Name => this.@event.Name;
        public AssemblyObjectKind Kind => AssemblyObjectKind.Property;
        public AssemblyLoader Context => this.declaringType.assembly.Context;
        public IAssembly Assembly => this.declaringType.assembly;

        public string GetFullyQualifiedName()
        {
            if (this._FullyQualifiedName == null)
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(this.DeclaringType.GetFullyQualifiedName());
                sb.Append('.');
                sb.Append(this.Name);
                this._FullyQualifiedName = sb.ToString();
            }
            return this._FullyQualifiedName;
        }
        private string _FullyQualifiedName;
    }
}
