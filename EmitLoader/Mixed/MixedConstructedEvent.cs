using System.Reflection;
using System.Text;

namespace EmitLoader.Mixed
{
    internal class MixedConstructedEvent : IEvent
    {
        public MixedConstructedEvent(IEvent Base, MixedConstructedType Parent)
        {
            this.Base = Base;
            this.Parent = Parent;
        }
        internal readonly IEvent Base;
        internal readonly MixedConstructedType Parent;

        public EventInfo GetBuiltEvent() => this.Parent.GetBuiltType().GetRuntimeEvent(this.Base.GetBuiltEvent().Name);

        public IType EventType
        {
            get
            {
                if (this._EventType == null)
                    this._EventType = this.Parent.Mappings.MapType(this.Base.EventType);
                return this._EventType;
            }
        }
        private IType _EventType;
        public IMethod Adder
        {
            get
            {
                if (this._Adder == null)
                    this._Adder = new MixedConstructedMethod(this.Base.Adder, this.Parent);
                return this._Adder;
            }
        }
        private IMethod _Adder;
        public IMethod Remover
        {
            get
            {
                if (this._Remover == null)
                    this._Remover = new MixedConstructedMethod(this.Base.Remover, this.Parent);
                return this._Remover;
            }
        }
        private IMethod _Remover;
        public IMethod Raiser
        {
            get
            {
                if (this._Raiser == null)
                    this._Raiser = new MixedConstructedMethod(this.Base.Raiser, this.Parent);
                return this._Raiser;
            }
        }
        private IMethod _Raiser;
        public IType DeclaringType => this.Parent;

        public ICustomAttribute[] CustomAttributes => this.Base.CustomAttributes;

        public string Name => this.Name;
        public AssemblyObjectKind Kind => AssemblyObjectKind.Event;
        public AssemblyLoader Context => this.Base.Context;
        public IAssembly Assembly => this.Base.Context.MixedSolver;

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
