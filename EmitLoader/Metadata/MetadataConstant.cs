using System.Reflection.Metadata;
using System;

namespace EmitLoader.Metadata
{
    internal class MetadataConstant : IConstant
    {
        public ValueType ValueType => (ValueType)this.Def.TypeCode;

        public object Value
        {
            get
            {
                if(this._Value == null && ValueType != ValueType.Null)
                {
                    BlobReader reader = this.Assembly.MD.GetBlobReader(this.Def.Value);
                    switch (ValueType)
                    {
                        case ValueType.Boolean:
                            this._Value = reader.ReadBoolean();
                            break;
                        case ValueType.Char:
                            this._Value = reader.ReadChar();
                            break;
                        case ValueType.SByte:
                            this._Value = reader.ReadSByte();
                            break;
                        case ValueType.Byte:
                            this._Value = reader.ReadByte();
                            break;
                        case ValueType.Int16:
                            this._Value = reader.ReadInt16();
                            break;
                        case ValueType.UInt16:
                            this._Value = reader.ReadUInt16();
                            break;
                        case ValueType.Int32:
                            this._Value = reader.ReadInt32();
                            break;
                        case ValueType.UInt32:
                            this._Value = reader.ReadUInt32();
                            break;
                        case ValueType.Int64:
                            this._Value = reader.ReadInt64();
                            break;
                        case ValueType.UInt64:
                            this._Value = reader.ReadUInt64();
                            break;
                        case ValueType.Single:
                            this._Value = reader.ReadSingle();
                            break;
                        case ValueType.Double:
                            this._Value = reader.ReadDouble();
                            break;
                        case ValueType.String:
                            this._Value = reader.ReadSerializedString();
                            break;
                        default:
                            throw new Exception("Unexpected ValueType");
                    }
                }
                return this._Value;
            }
        }
        private object _Value;

        internal MetadataConstant(Constant Def, MetadataSolver Assembly)
        {
            this.Def = Def;
            this.Assembly = Assembly;
        }
        private readonly Constant Def;

        public AssemblyObjectKind Kind => AssemblyObjectKind.Constant;
        public AssemblyLoader Context => this.Assembly.Context;
        IAssembly IAssemblySolverObject.Assembly => this.Assembly;
        public MetadataSolver Assembly { get; }

    }
}
