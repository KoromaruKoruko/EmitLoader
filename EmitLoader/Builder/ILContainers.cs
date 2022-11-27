using System;
using System.Reflection;
using System.Reflection.Emit;

namespace EmitLoader.Builder
{
    internal class EmitContext
    {
        public readonly AccessControlManager ACM;
        public readonly ILGenerator IL;
        public readonly LocalBuilder[] locals;
        public readonly Label[] labels;
        public readonly MetadataMethodBody Method;

        internal EmitContext(AccessControlManager ACM, MetadataMethodBody Method, ILGenerator IL, Label[] labels, LocalBuilder[] locals)
        {
            this.ACM = ACM;
            this.Method = Method;
            this.IL = IL;
            this.labels = labels;
            this.locals = locals;
        }
    }
    /// <summary>
    /// Emit Container Kind
    /// </summary>
    public enum EmitContainerKind
    {
        /// <summary>
        /// <see cref="EmitContainer_NoData"/>
        /// </summary>
        NoData,
        /// <summary>
        /// <see cref="EmitContainer_Switch"/>
        /// </summary>
        Switch,

        /// <summary>
        /// <see cref="EmitContainer_Label"/>
        /// </summary>
        Label,
        /// <summary>
        /// <see cref="EmitContainer_LocalVar"/>
        /// </summary>
        LocalVar,
        /// <summary>
        /// <see cref="EmitContainer_EmitLabel"/>
        /// </summary>
        EmitLabel,

        /// <summary>
        /// <see cref="EmitContainer_InlineString"/>
        /// </summary>
        InlineString,
        /// <summary>
        /// <see cref="EmitContainer_InlineI"/>
        /// </summary>
        InlineI,
        /// <summary>
        /// <see cref="EmitContainer_InlineI8"/>
        /// </summary>
        InlineI8,
        /// <summary>
        /// <see cref="EmitContainer_InlineR"/>
        /// </summary>
        InlineR,
        /// <summary>
        /// <see cref="EmitContainer_InlineShortR"/>
        /// </summary>
        InlineShortR,

        /// <summary>
        /// <see cref="EmitContainer_Type"/>
        /// </summary>
        Type,
        /// <summary>
        /// <see cref="EmitContainer_Method"/>
        /// </summary>
        Method,
        /// <summary>
        /// <see cref="EmitContainer_Field"/>
        /// </summary>
        Field,

        /// <summary>
        /// <see cref="EmitContainer_BeginExceptionBlock"/>
        /// </summary>
        BeginExceptionBlock,
        /// <summary>
        /// <see cref="EmitContainer_EndExceptionBlock"/>
        /// </summary>
        EndExceptionBlock,
        /// <summary>
        /// <see cref="EmitContainer_BeginCatchBlock"/>
        /// </summary>
        BeginCatchBlock,
        /// <summary>
        /// <see cref="EmitContainer_BeginFinallyBlock"/>
        /// </summary>
        BeginFinallyBlock,
        /// <summary>
        /// <see cref="EmitContainer_BeginFilterBlock"/>
        /// </summary>
        BeginFilterBlock,
        /// <summary>
        /// <see cref="EmitContainer_BeginFaultBlock"/>
        /// </summary>
        BeginFaultBlock,
    }
    /// <summary>
    /// IL Container Code Instruction
    /// </summary>
    public interface ILContainer
    {
        /// <summary>
        /// Container Kind
        /// </summary>
        EmitContainerKind Kind { get; }
    }
    internal interface IEmitContainer : ILContainer
    {
        void Emit(EmitContext context);
    }

    /// <summary>
    /// Container with only an OpCode
    /// </summary>
    public class EmitContainer_NoData : IEmitContainer
    {
        internal EmitContainer_NoData(OpCode OpCode)
        {
            this.OpCode = OpCode;
        }
        /// <summary>
        /// IL OpCode Instruction
        /// </summary>
        public readonly OpCode OpCode;


        /// <inheritdoc cref="ILContainer.Kind"/>
        public EmitContainerKind Kind => EmitContainerKind.NoData;
        void IEmitContainer.Emit(EmitContext context) => context.IL.Emit(OpCode);
    }
    /// <summary>
    /// a Switch Instruction
    /// </summary>
    public class EmitContainer_Switch : IEmitContainer
    {
        internal EmitContainer_Switch(int[] brL) => this.brL = brL;

        /// <summary>
        /// Branch Label List
        /// </summary>
        public readonly int[] brL;


        /// <inheritdoc cref="ILContainer.Kind"/>
        public EmitContainerKind Kind => EmitContainerKind.Switch;
        void IEmitContainer.Emit(EmitContext context)
        {
            Label[] arr = new Label[brL.Length];
            for (int x = 0; x < brL.Length; x++)
                arr[x] = context.labels[brL[x]];
            context.IL.Emit(OpCodes.Switch, arr);
        }
    }

    /// <summary>
    /// an OpCode that has a Local Variable
    /// </summary>
    public class EmitContainer_Label : IEmitContainer
    {
        internal EmitContainer_Label(OpCode OpCode, int LabelId)
        {
            this.OpCode = OpCode;
            this.LabelId = LabelId;
        }
        /// <summary>
        /// OpCode
        /// </summary>
        public readonly OpCode OpCode;
        /// <summary>
        /// Label Id
        /// </summary>
        public readonly int LabelId;


        /// <inheritdoc cref="ILContainer.Kind"/>
        public EmitContainerKind Kind => EmitContainerKind.Label;
        void IEmitContainer.Emit(EmitContext context) => context.IL.Emit(OpCode, context.labels[LabelId]);
    }
    /// <summary>
    /// Marks where a Label needs to be
    /// </summary>
    public class EmitContainer_EmitLabel : IEmitContainer
    {
        internal EmitContainer_EmitLabel(int LabelId) => this.LabelId = LabelId;

        /// <summary>
        /// Label Id
        /// </summary>
        public readonly int LabelId;


        /// <inheritdoc cref="ILContainer.Kind"/>
        public EmitContainerKind Kind => EmitContainerKind.EmitLabel;
        void IEmitContainer.Emit(EmitContext context) => context.IL.MarkLabel(context.labels[LabelId]);
    }
    /// <summary>
    /// an OpCode that has an inline Local Variable
    /// </summary>
    public class EmitContainer_LocalVar : IEmitContainer
    {
        internal EmitContainer_LocalVar(OpCode OpCode, int LocalId)
        {
            this.OpCode = OpCode;
            this.LocalId = LocalId;
        }

        /// <summary>
        /// OpCode
        /// </summary>
        public readonly OpCode OpCode;
        /// <summary>
        /// Local Variable Id
        /// </summary>
        public readonly int LocalId;


        /// <inheritdoc cref="ILContainer.Kind"/>
        public EmitContainerKind Kind => EmitContainerKind.LocalVar;
        void IEmitContainer.Emit(EmitContext context) => context.IL.Emit(OpCode, context.locals[LocalId]);
    }


    /// <summary>
    /// an OpCode that has an inline String
    /// </summary>
    public class EmitContainer_InlineString : IEmitContainer
    {
        internal EmitContainer_InlineString(OpCode OpCode, string String)
        {
            this.String = String;
            this.OpCode = OpCode;
        }
        /// <summary>
        /// OpCode
        /// </summary>
        public readonly OpCode OpCode;
        /// <summary>
        /// String
        /// </summary>
        public readonly string String;


        /// <inheritdoc cref="ILContainer.Kind"/>
        public EmitContainerKind Kind => EmitContainerKind.InlineString;
        void IEmitContainer.Emit(EmitContext context) => context.IL.Emit(OpCode, String);
    }
    /// <summary>
    /// an OpCode that has an inline Int32
    /// </summary>
    public class EmitContainer_InlineI : IEmitContainer
    {
        internal EmitContainer_InlineI(OpCode OpCode, int I)
        {
            this.I = I;
            this.OpCode = OpCode;
        }
        /// <summary>
        /// OpCode
        /// </summary>
        public readonly OpCode OpCode;
        /// <summary>
        /// Signed Integer Value
        /// </summary>
        public readonly int I;


        /// <inheritdoc cref="ILContainer.Kind"/>
        public EmitContainerKind Kind => EmitContainerKind.InlineString;
        void IEmitContainer.Emit(EmitContext context) => context.IL.Emit(OpCode, I);
    }
    /// <summary>
    /// an OpCode that has an inline Int64
    /// </summary>
    public class EmitContainer_InlineI8 : IEmitContainer
    {
        internal EmitContainer_InlineI8(OpCode OpCode, long I8)
        {
            this.I8 = I8;
            this.OpCode = OpCode;
        }
        /// <summary>
        /// OpCode
        /// </summary>
        public readonly OpCode OpCode;
        /// <summary>
        /// Signed Integer Value
        /// </summary>
        public readonly long I8;


        /// <inheritdoc cref="ILContainer.Kind"/>
        public EmitContainerKind Kind => EmitContainerKind.InlineString;
        void IEmitContainer.Emit(EmitContext context) => context.IL.Emit(OpCode, I8);
    }
    /// <summary>
    /// an OpCode that has an inline Double
    /// </summary>
    public class EmitContainer_InlineR : IEmitContainer
    {
        internal EmitContainer_InlineR(OpCode OpCode, double R)
        {
            this.R = R;
            this.OpCode = OpCode;
        }

        /// <summary>
        /// OpCode
        /// </summary>
        public readonly OpCode OpCode;
        /// <summary>
        /// Real Value
        /// </summary>
        public readonly double R;


        /// <inheritdoc cref="ILContainer.Kind"/>
        public EmitContainerKind Kind => EmitContainerKind.InlineString;
        void IEmitContainer.Emit(EmitContext context) => context.IL.Emit(OpCode, R);
    }
    /// <summary>
    /// an OpCode that has an inline Float
    /// </summary>
    public class EmitContainer_InlineShortR : IEmitContainer
    {
        internal EmitContainer_InlineShortR(OpCode OpCode, float R)
        {
            this.R = R;
            this.OpCode = OpCode;
        }
        /// <summary>
        /// OpCode
        /// </summary>
        public readonly OpCode OpCode;
        /// <summary>
        /// Real Value
        /// </summary>
        public readonly float R;


        /// <inheritdoc cref="ILContainer.Kind"/>
        public EmitContainerKind Kind => EmitContainerKind.InlineString;
        void IEmitContainer.Emit(EmitContext context) => context.IL.Emit(OpCode, R);
    }

    /// <summary>
    /// an OpCode that has a Type Token
    /// </summary>
    public class EmitContainer_Type : IEmitContainer
    {
        internal EmitContainer_Type(OpCode OpCode, IType Type)
        {
            this.OpCode = OpCode;
            this.Type = Type;
        }
        /// <summary>
        /// OpCode
        /// </summary>

        public readonly OpCode OpCode;
        /// <summary>
        /// Type
        /// </summary>
        public readonly IType Type;


        /// <inheritdoc cref="ILContainer.Kind"/>
        public EmitContainerKind Kind => EmitContainerKind.Type;
        void IEmitContainer.Emit(EmitContext context)
        {
            Type type = Type.GetBuiltType();
            context.ACM.ValidateAccess(type);
            context.IL.Emit(OpCode, type);
        }
    }
    /// <summary>
    /// an OpCode that has a Field Token
    /// </summary>
    public class EmitContainer_Field : IEmitContainer
    {
        internal EmitContainer_Field(OpCode OpCode, IField Field)
        {
            this.OpCode = OpCode;
            this.Field = Field;
        }
        /// <summary>
        /// OpCode
        /// </summary>
        public readonly OpCode OpCode;
        /// <summary>
        /// Field
        /// </summary>
        public readonly IField Field;


        /// <inheritdoc cref="ILContainer.Kind"/>
        public EmitContainerKind Kind => EmitContainerKind.Field;
        void IEmitContainer.Emit(EmitContext context)
        {
            FieldInfo field = Field.GetBuiltField();

            if (OpCode == OpCodes.Stfld || OpCode == OpCodes.Stsfld)
                context.ACM.ValidateAccess_set(field);
            else if (OpCode == OpCodes.Ldfld)
                context.ACM.ValidateAccess_get(field);
            else if (OpCode == OpCodes.Ldflda || OpCode == OpCodes.Ldtoken)
            {
                context.ACM.ValidateAccess_get(field);
                context.ACM.ValidateAccess_set(field);
            }
            else
                throw new Exception("Unknown OpCode with field Parameter");
            context.IL.Emit(OpCode, field);
        }
    }
    /// <summary>
    /// an OpCode that has a Method Token
    /// </summary>
    public class EmitContainer_Method : IEmitContainer
    {
        internal EmitContainer_Method(OpCode OpCode, IMethod Method)
        {
            this.OpCode = OpCode;
            this.Method = Method;
        }
        /// <summary>
        /// OpCode
        /// </summary>
        public readonly OpCode OpCode;
        /// <summary>
        /// Method
        /// </summary>
        public readonly IMethod Method;


        /// <inheritdoc cref="ILContainer.Kind"/>
        public EmitContainerKind Kind => EmitContainerKind.Method;
        void IEmitContainer.Emit(EmitContext context)
        {
            MethodBase m = Method.GetBuiltMethod();
            if (m is MethodInfo method)
            {
                context.ACM.ValidateAccess(method);
                context.IL.Emit(OpCode, method);
            }
            else if (m is ConstructorInfo constructor)
            {
                context.ACM.ValidateAccess(constructor);
                context.IL.Emit(OpCode, constructor);
            }
            else
                throw new Exception("Constructed MethodBase is not a ConstructorInfo nor MethodInfo");
        }
    }

    // TODO: [OPT] Convert Some ContainerTypes to Singletons

    /// <summary>
    /// Exception Block Begining
    /// </summary>
    public class EmitContainer_BeginExceptionBlock : IEmitContainer
    {
        internal EmitContainer_BeginExceptionBlock() { }

        /// <inheritdoc cref="ILContainer.Kind"/>
        public EmitContainerKind Kind => EmitContainerKind.BeginExceptionBlock;
        void IEmitContainer.Emit(EmitContext context) => context.IL.BeginExceptionBlock();
    }
    /// <summary>
    /// Exception Block Ending
    /// </summary>
    public class EmitContainer_EndExceptionBlock : IEmitContainer
    {
        internal EmitContainer_EndExceptionBlock() { }
        /// <inheritdoc cref="ILContainer.Kind"/>
        public EmitContainerKind Kind => EmitContainerKind.EndExceptionBlock;
        void IEmitContainer.Emit(EmitContext context) => context.IL.EndExceptionBlock();
    }
    /// <summary>
    /// Catch Block Begining
    /// </summary>
    public class EmitContainer_BeginCatchBlock : IEmitContainer
    {
        internal EmitContainer_BeginCatchBlock(IType ExceptionType) => this.ExceptionType = ExceptionType;
        /// <summary>
        /// Exception Type to be Caught
        /// </summary>
        public readonly IType ExceptionType;


        /// <inheritdoc cref="ILContainer.Kind"/>
        public EmitContainerKind Kind => EmitContainerKind.BeginCatchBlock;
        void IEmitContainer.Emit(EmitContext context)
        {
            Type catchType = this.ExceptionType.GetBuiltType();
            context.ACM.ValidateAccess(catchType);
            context.IL.BeginCatchBlock(catchType);
        }
    }
    /// <summary>
    /// Finally Block Begining
    /// </summary>
    public class EmitContainer_BeginFinallyBlock : IEmitContainer
    {
        internal EmitContainer_BeginFinallyBlock() { }

        /// <inheritdoc cref="ILContainer.Kind"/>
        public EmitContainerKind Kind => EmitContainerKind.BeginFinallyBlock;
        void IEmitContainer.Emit(EmitContext context) => context.IL.BeginFinallyBlock();
    }
    /// <summary>
    /// Filter Block Begining
    /// </summary>
    public class EmitContainer_BeginFilterBlock : IEmitContainer
    {
        internal EmitContainer_BeginFilterBlock() { }

        /// <inheritdoc cref="ILContainer.Kind"/>
        public EmitContainerKind Kind => EmitContainerKind.BeginFilterBlock;
        void IEmitContainer.Emit(EmitContext context) => context.IL.BeginExceptFilterBlock();
    }
    /// <summary>
    /// Fault Block Begining
    /// </summary>
    public class EmitContainer_BeginFaultBlock : IEmitContainer
    {
        internal EmitContainer_BeginFaultBlock() { }

        /// <inheritdoc cref="ILContainer.Kind"/>
        public EmitContainerKind Kind => EmitContainerKind.BeginFaultBlock;
        void IEmitContainer.Emit(EmitContext context) => context.IL.BeginFaultBlock();
    }
}
