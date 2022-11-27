
using System;
using System.Reflection;
using System.Text;

namespace EmitLoader.Metadata
{
    internal abstract class MetadataEventBase : IEvent
    {

        public AssemblyObjectKind Kind => AssemblyObjectKind.Event;
        public AssemblyLoader Context => this.Assembly.Context;
        IAssembly IAssemblySolverObject.Assembly => this.Assembly;
        public abstract MetadataSolver Assembly { get; }

        public abstract EventAttributes Attributes { get; }
        public EventInfo GetBuiltEvent() =>
             this.Assembly.Loader == null
                ? throw new InvalidOperationException("Metadata Assembly not permited to built!")
                : this.DeclaringType.BuildType().GetRuntimeEvent(this.Name);


        public abstract string Name { get; }

        public abstract IType EventType { get; }

        IMethod IEvent.Adder => this.Adder;
        public abstract MetadataMethodBase Adder { get; }

        IMethod IEvent.Remover => this.Remover;
        public abstract MetadataMethodBase Remover { get; }

        IMethod IEvent.Raiser => this.Raiser;
        public abstract MetadataMethodBase Raiser { get; }

        IType IMember.DeclaringType => this.DeclaringType;
        public abstract MetadataTypeBase DeclaringType { get; }

        ICustomAttribute[] IMember.CustomAttributes => this.CustomAttributes;
        public abstract MetadataCustomAttributeBase[] CustomAttributes { get; }

        public string GetFullyQualifiedName()
        {
            StringBuilder sb = new StringBuilder();
            GetFullyQualifiedName(sb);
            return sb.ToString();
        }
        public void GetFullyQualifiedName(StringBuilder sb)
        {
            this.DeclaringType.GetFullyQualifiedName(sb);
            sb.Append('.');
            sb.Append(this.Name);
        }
    }
}
