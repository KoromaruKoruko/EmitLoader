using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection.Emit;
using System.Reflection.Metadata;

using EmitLoader.Metadata;
using EmitLoader.Unsafe;

namespace EmitLoader.Builder
{
    /// <summary>
    /// Represents a Metadata Methods Underlying IL Code
    /// </summary>
    public class MetadataMethodBody
    {
        internal MetadataSolver Assembly => Method.Assembly;
        internal readonly MethodBodyBlock MethodBody;
        internal readonly MetadataMethod Method;
        /// <summary>
        /// Enumerates all the IL Container Code
        /// </summary>
        public IEnumerable<ILContainer> EnumerateILC()
        {
            if (ILc == null)
                BuildInstructions();

            for (int x = 0; x < ILc.Length; x++)
                yield return ILc[x];
        }

        internal MetadataMethodBody(MetadataMethod Method)
        {
            this.Method = Method;
            MethodBody = Assembly.PE.GetMethodBody(Method.Def.RelativeVirtualAddress);
        }
        internal void Compile(ILGenerator Generator)
        {
            if (ILc == null)
                BuildInstructions();

            AccessControlManager ACM = Assembly.Loader.AccessController;
            LocalBuilder[] locals;

            if (MethodBody.LocalSignature.IsNil)
                locals = null;
            else
            {
                ImmutableArray<IType> sig = Assembly.MD.GetStandaloneSignature(MethodBody.LocalSignature).DecodeLocalSignature(Assembly.SP, Method);
                locals = new LocalBuilder[sig.Length];
                for (int x = 0; x < sig.Length; x++)
                {
                    Type t = sig[x].GetBuiltType();
                    ACM.ValidateAccess(t);
                    locals[x] = Generator.DeclareLocal(t);
                }
            }

            Label[] labels;
            if (Labels > 0)
            {
                labels = new Label[Labels];
                for (int x = 0; x < Labels; x++)
                    labels[x] = Generator.DefineLabel();
            }
            else
                labels = null;

            EmitContext context = new EmitContext(ACM, this, Generator, labels, locals);
            foreach (IEmitContainer ilc in ILc)
                ilc.Emit(context);
        }

        private struct PreContainer
        {
            public PreContainer(long offset, IEmitContainer container)
            {
                this.offset = offset;
                this.container = container;
            }
            public readonly long offset;
            public readonly IEmitContainer container;
        }
        private struct BranchContainer
        {
            public BranchContainer(long offset, int id)
            {
                _offset = offset;
                _id = id;
            }

            public readonly long _offset;
            public readonly int _id;

            public class Comparer : IComparer<BranchContainer>
            {
                public static readonly Comparer Instance = new Comparer();
                private Comparer() { }

                public int Compare(BranchContainer x, BranchContainer y) => (int)(x._offset - y._offset);
            }
        }
        private class ExceptionBlock
        {
            public long tryOffset;
            public long handlerOffset;
            public long handlerEndOffset;
            public IEmitContainer handler;
            public bool shouldEmitEnd;
        }


        private IEmitContainer[] ILc;
        private int Labels;
        internal unsafe void BuildInstructions()
        {
            // List of Ignores (compatibility with Emit library)
            // Leave
            // Leave_S
            // EndFinally
            // EndFilter

            List<PreContainer> IL = new List<PreContainer>();
            SortedSet<BranchContainer> BranchList = new SortedSet<BranchContainer>(BranchContainer.Comparer.Instance);

            BlobReader Reader = MethodBody.GetILReader();

            while (Reader.RemainingBytes > 0)
            {
                long offset = Reader.CurrentPointer - Reader.StartPointer;
                OpCode OpCode = ReadOpCode(ref Reader);

#pragma warning disable CS0618 // Type or member is obsolete
                if (OpCode.FlowControl == FlowControl.Phi || OpCode.OperandType == OperandType.InlinePhi)
                    throw new NotSupportedException($"illegal OpCode! ({OpCode.Name}->Phi)");
#pragma warning restore CS0618 // Type or member is obsolete

                double R;
                float ShortR;
                int I;
                long I8;

                EntityHandle tok;
                IMethod method;
                IField field;
                IType type;
                IMember member;

                BranchContainer brC;
                int x;
                int brId, locId;
                long brOffset, brTarget;
                switch (OpCode.OperandType)
                {
                    case OperandType.InlineBrTarget:
                        brId = BranchList.Count;
                        brTarget = Reader.ReadInt32();
                        if (OpCode == OpCodes.Leave)
                            IL.Add(new PreContainer(offset, new EmitContainer_NoData(OpCodes.Nop)));
                        else
                        {
                            brOffset = Reader.CurrentPointer - Reader.StartPointer;
                            brTarget += brOffset;
                            brC = new BranchContainer(brOffset + brTarget, brId);
                            if (!BranchList.Add(brC) && !BranchList.TryGetValue(brC, out brC))
                                throw new Exception("Unable to Add Branch!");
                            IL.Add(new PreContainer(offset, new EmitContainer_Label(OpCode, brC._id)));
                        }
                        break;

                    case OperandType.ShortInlineBrTarget:
                        brId = BranchList.Count;
                        brTarget = Reader.ReadSByte();
                        if (OpCode == OpCodes.Leave_S)
                            IL.Add(new PreContainer(offset, new EmitContainer_NoData(OpCodes.Nop)));
                        else
                        {
                            brOffset = Reader.CurrentPointer - Reader.StartPointer;
                            brC = new BranchContainer(brOffset + brTarget, brId);
                            if (!BranchList.Add(brC) && !BranchList.TryGetValue(brC, out brC))
                                throw new Exception("Unable to Add Branch!");
                            IL.Add(new PreContainer(offset, new EmitContainer_Label(OpCode, brC._id)));
                        }
                        break;

                    case OperandType.InlineSwitch:
                        brId = BranchList.Count;
                        List<int> brL = new List<int>();
                        uint N = Reader.ReadUInt32();
                        brOffset = Reader.CurrentPointer - Reader.StartPointer + (4L * N);

                        for (x = 0; x < N; x++)
                        {
                            brTarget = Reader.ReadInt32();
                            brC = new BranchContainer(brOffset + brTarget, brId + x);
                            if (!BranchList.Add(brC) && !BranchList.TryGetValue(brC, out brC))
                                throw new Exception("Unable to Add Branch!");
                            BranchList.Add(new BranchContainer(brOffset + brTarget, brC._id));
                            brL.Add(brC._id);
                        }
                        IL.Add(new PreContainer(offset, new EmitContainer_Switch(brL.ToArray())));
                        break;

                    case OperandType.InlineSig: // Method Signature (UNSUPPORTED!)
                        throw new NotSupportedException("Inline Signatures are not supported ! Calli is not supported by System.Reflection.Emit!");

                    case OperandType.InlineField: // FieldDef, FieldRef
                        tok = UnsafeOperations.CastMetaTokenToEntityHandle(Reader.ReadUInt32());
                        switch (tok.Kind)
                        {
                            case HandleKind.FieldDefinition:
                                field = Assembly.GetFieldDefinition((FieldDefinitionHandle)tok);
                                break;
                            case HandleKind.MemberReference:
                                field = Assembly.GetMemberReference((MemberReferenceHandle)tok, Method) as IField;
                                break;

                            default:
                                throw new NotSupportedException($"unexpected Token for InlineMethod Operand {OpCode.Name}");
                        }
                        if (field == null)
                            throw new Exception("Failed to Resolve Field");
                        IL.Add(new PreContainer(offset, new EmitContainer_Field(OpCode, field)));
                        break;


                    case OperandType.InlineMethod: // MethodRef, MethodDef, MethodSpec
                        tok = UnsafeOperations.CastMetaTokenToEntityHandle(Reader.ReadUInt32());
                        switch (tok.Kind)
                        {
                            case HandleKind.MethodDefinition:
                                method = Assembly.GetMethodDefinition((MethodDefinitionHandle)tok, null);
                                break;
                            case HandleKind.MemberReference:
                                method = Assembly.GetMemberReference((MemberReferenceHandle)tok, Method) as IMethod;
                                break;
                            case HandleKind.MethodSpecification:
                                method = Assembly.GetMethodSpecification((MethodSpecificationHandle)tok, Method);
                                break;

                            default:
                                throw new NotSupportedException($"unexpected Token for InlineMethod Operand {OpCode.Name}");
                        }
                        if (method == null)
                            throw new Exception("Failed to Resolve Method");
                        IL.Add(new PreContainer(offset, new EmitContainer_Method(OpCode, method)));
                        break;


                    case OperandType.InlineType: // TypeDef, TypeRef, TypeSpec
                        tok = UnsafeOperations.CastMetaTokenToEntityHandle(Reader.ReadUInt32());
                        switch (tok.Kind)
                        {
                            case HandleKind.TypeDefinition:
                                type = Assembly.GetTypeDefinition((TypeDefinitionHandle)tok);
                                break;
                            case HandleKind.TypeReference:
                                type = Assembly.GetTypeReference((TypeReferenceHandle)tok);
                                break;
                            case HandleKind.TypeSpecification:
                                type = Assembly.GetTypeSpecification((TypeSpecificationHandle)tok, Method);
                                break;

                            default:
                                throw new NotSupportedException($"unexpected Token for InlineType Operand {OpCode.Name}");
                        }
                        if (type == null)
                            throw new Exception("Failed to Resolve Type");
                        IL.Add(new PreContainer(offset, new EmitContainer_Type(OpCode, type)));
                        break;


                    case OperandType.InlineTok: // FieldRef, FieldDef, MethodRef, MethodDef, MethodSpec, TypeRef, TypeDef, TypeSpec

                        // Field, Method, Type REF
                        tok = UnsafeOperations.CastMetaTokenToEntityHandle(Reader.ReadUInt32());
                        switch (tok.Kind)
                        {
                            case HandleKind.TypeDefinition:
                                type = Assembly.GetTypeDefinition((TypeDefinitionHandle)tok);
                                goto type;
                            case HandleKind.TypeReference:
                                type = Assembly.GetTypeReference((TypeReferenceHandle)tok);
                                goto type;
                            case HandleKind.TypeSpecification:
                                type = Assembly.GetTypeSpecification((TypeSpecificationHandle)tok, Method);
                                goto type;
                            case HandleKind.MethodDefinition:
                                method = Assembly.GetMethodDefinition((MethodDefinitionHandle)tok, null);
                                goto method;
                            case HandleKind.MethodSpecification:
                                method = Assembly.GetMethodSpecification((MethodSpecificationHandle)tok, Method);
                                goto method;
                            case HandleKind.FieldDefinition:
                                field = Assembly.GetFieldDefinition((FieldDefinitionHandle)tok);
                                goto field;
                            case HandleKind.MemberReference:
                                member = Assembly.GetMemberReference((MemberReferenceHandle)tok, Method) as IMethod;
                                if (member is IMethod m)
                                {
                                    method = m;
                                    goto method;
                                }
                                else if (member is IField f)
                                {
                                    field = f;
                                    goto field;
                                }
                                throw new Exception("Unable to Resolve Member Reference");
                            default:
                                throw new NotSupportedException($"unexpected Token for InlineType Operand {OpCode.Name}");
                        }

                    field:
                        IL.Add(new PreContainer(offset, new EmitContainer_Field(OpCode, field)));
                        break;
                    method:
                        IL.Add(new PreContainer(offset, new EmitContainer_Method(OpCode, method)));
                        break;
                    type:
                        IL.Add(new PreContainer(offset, new EmitContainer_Type(OpCode, type)));
                        break;



                    case OperandType.InlineVar:
                        locId = Reader.ReadInt32();
                        IL.Add(new PreContainer(offset, new EmitContainer_LocalVar(OpCode, locId)));
                        break;

                    case OperandType.ShortInlineVar:
                        locId = Reader.ReadByte();
                        IL.Add(new PreContainer(offset, new EmitContainer_LocalVar(OpCode, locId)));
                        break;

                    case OperandType.InlineString:
                        tok = UnsafeOperations.CastMetaTokenToEntityHandle(Reader.ReadUInt32());
                        if (tok.Kind != HandleKind.UserString)
                            throw new Exception($"Illegal Token, Expect 'UserString' Got '{tok.Kind}'");
                        IL.Add(new PreContainer(offset, new EmitContainer_InlineString(OpCode, Assembly.MD.GetUserString((UserStringHandle)(Handle)tok))));
                        break;

                    case OperandType.ShortInlineI:
                        I = Reader.ReadByte();
                        IL.Add(new PreContainer(offset, new EmitContainer_InlineI(OpCode, I)));
                        break;

                    case OperandType.InlineI:
                        I = Reader.ReadInt32();
                        IL.Add(new PreContainer(offset, new EmitContainer_InlineI(OpCode, I)));
                        break;

                    case OperandType.InlineI8:
                        I8 = Reader.ReadInt64();
                        IL.Add(new PreContainer(offset, new EmitContainer_InlineI8(OpCode, I8)));
                        break;

                    case OperandType.ShortInlineR:
                        ShortR = Reader.ReadSingle();
                        IL.Add(new PreContainer(offset, new EmitContainer_InlineR(OpCode, ShortR)));
                        break;

                    case OperandType.InlineR:
                        R = Reader.ReadDouble();
                        IL.Add(new PreContainer(offset, new EmitContainer_InlineR(OpCode, R)));
                        break;

                    case OperandType.InlineNone:
                        if (OpCode == OpCodes.Endfilter || OpCode == OpCodes.Endfinally)
                            IL.Add(new PreContainer(offset, new EmitContainer_NoData(OpCodes.Nop)));
                        else
                            IL.Add(new PreContainer(offset, new EmitContainer_NoData(OpCode)));
                        break;
                }
            }

            List<ExceptionBlock> protectedMap = new List<ExceptionBlock>();
            foreach (ExceptionRegion region in MethodBody.ExceptionRegions)
            {
                ExceptionBlock newBlock = new ExceptionBlock()
                {
                    tryOffset = region.TryOffset,
                    handlerOffset = region.HandlerOffset,
                    handlerEndOffset = region.HandlerOffset + region.HandlerLength,
                    shouldEmitEnd = true,
                };

                switch (region.Kind)
                {
                    case ExceptionRegionKind.Catch:
                        switch (region.CatchType.Kind)
                        {
                            case HandleKind.TypeReference:
                                newBlock.handler = new EmitContainer_BeginCatchBlock(Assembly.GetTypeReference((TypeReferenceHandle)region.CatchType));
                                break;
                            case HandleKind.TypeSpecification:
                                newBlock.handler = new EmitContainer_BeginCatchBlock(Assembly.GetTypeSpecification((TypeSpecificationHandle)region.CatchType, Method));
                                break;
                            case HandleKind.TypeDefinition:
                                newBlock.handler = new EmitContainer_BeginCatchBlock(Assembly.GetTypeDefinition((TypeDefinitionHandle)region.CatchType));
                                break;
                            default:
                                throw new NotSupportedException($"{region.CatchType.Kind} not supported for CatchType");
                        }
                        break;

                    case ExceptionRegionKind.Filter:
                        newBlock.handler = new EmitContainer_BeginFilterBlock();
                        break;

                    case ExceptionRegionKind.Finally:
                        newBlock.handler = new EmitContainer_BeginFinallyBlock();
                        break;

                    case ExceptionRegionKind.Fault:
                        newBlock.handler = new EmitContainer_BeginFaultBlock();
                        break;
                }

                foreach (ExceptionBlock block in protectedMap)
                {
                    if (block.shouldEmitEnd)
                    {
                        if (block.tryOffset == newBlock.tryOffset)
                        {
                            if (block.handlerEndOffset > newBlock.handlerOffset)
                                newBlock.shouldEmitEnd = false;
                            else
                                block.shouldEmitEnd = false;
                        }
                    }
                }
                protectedMap.Add(newBlock);
            }


            Labels = BranchList.Count;
            List<IEmitContainer> pIL = new List<IEmitContainer>();
            List<BranchContainer> toRemove = new List<BranchContainer>();
            foreach (PreContainer il in IL)
            {
                foreach (ExceptionBlock region in protectedMap)
                {
                    if (region.shouldEmitEnd && region.tryOffset == il.offset)
                        pIL.Add(new EmitContainer_BeginExceptionBlock());
                    else if (region.handlerOffset == il.offset)
                        pIL.Add(region.handler);
                    else if (region.shouldEmitEnd && region.handlerEndOffset == il.offset)
                        pIL.Add(new EmitContainer_EndExceptionBlock());
                }

                foreach (BranchContainer brC in BranchList)
                    if (brC._offset == il.offset)
                    {
                        pIL.Add(new EmitContainer_EmitLabel(brC._id));
                        toRemove.Add(brC);
                    }
                    else if (brC._offset > il.offset)
                        break;

                if (toRemove.Count > 0)
                {
                    foreach (BranchContainer brC in toRemove)
                        BranchList.Remove(brC);
                    toRemove.Clear();
                }
                pIL.Add(il.container);
            }

            if (BranchList.Count != 0)
                throw new Exception("Branch Target did not match a begining to an IL Instruction");

            ILc = pIL.ToArray();
        }
        internal static OpCode ReadOpCode(ref BlobReader Reader)
        {
            switch (Reader.ReadByte())
            {
                case 0x00: return OpCodes.Nop;
                case 0x01: return OpCodes.Break;
                case 0x02: return OpCodes.Ldarg_0;
                case 0x03: return OpCodes.Ldarg_1;
                case 0x04: return OpCodes.Ldarg_2;
                case 0x05: return OpCodes.Ldarg_3;
                case 0x06: return OpCodes.Ldloc_0;
                case 0x07: return OpCodes.Ldloc_1;
                case 0x08: return OpCodes.Ldloc_2;
                case 0x09: return OpCodes.Ldloc_3;
                case 0x0A: return OpCodes.Stloc_0;
                case 0x0B: return OpCodes.Stloc_1;
                case 0x0C: return OpCodes.Stloc_2;
                case 0x0D: return OpCodes.Stloc_3;
                case 0x0E: return OpCodes.Ldarg_S;
                case 0x0F: return OpCodes.Ldarga_S;
                case 0x10: return OpCodes.Starg_S;
                case 0x11: return OpCodes.Ldloc_S;
                case 0x12: return OpCodes.Ldloca_S;
                case 0x13: return OpCodes.Stloc_S;
                case 0x14: return OpCodes.Ldnull;
                case 0x15: return OpCodes.Ldc_I4_M1;
                case 0x16: return OpCodes.Ldc_I4_0;
                case 0x17: return OpCodes.Ldc_I4_1;
                case 0x18: return OpCodes.Ldc_I4_2;
                case 0x19: return OpCodes.Ldc_I4_3;
                case 0x1A: return OpCodes.Ldc_I4_4;
                case 0x1B: return OpCodes.Ldc_I4_5;
                case 0x1C: return OpCodes.Ldc_I4_6;
                case 0x1D: return OpCodes.Ldc_I4_7;
                case 0x1E: return OpCodes.Ldc_I4_8;
                case 0x1F: return OpCodes.Ldc_I4_S;
                case 0x20: return OpCodes.Ldc_I4;
                case 0x21: return OpCodes.Ldc_I8;
                case 0x22: return OpCodes.Ldc_R4;
                case 0x23: return OpCodes.Ldc_R8;
                case 0x25: return OpCodes.Dup;
                case 0x26: return OpCodes.Pop;
                case 0x27: return OpCodes.Jmp;
                case 0x28: return OpCodes.Call;
                case 0x29: return OpCodes.Calli; // Not Supported!
                case 0x2A: return OpCodes.Ret;
                case 0x2B: return OpCodes.Br_S;
                case 0x2C: return OpCodes.Brfalse_S;
                case 0x2D: return OpCodes.Brtrue_S;
                case 0x2E: return OpCodes.Beq_S;
                case 0x2F: return OpCodes.Bge_S;
                case 0x30: return OpCodes.Bgt_S;
                case 0x31: return OpCodes.Ble_S;
                case 0x32: return OpCodes.Blt_S;
                case 0x33: return OpCodes.Bne_Un_S;
                case 0x34: return OpCodes.Bge_Un_S;
                case 0x35: return OpCodes.Bgt_Un_S;
                case 0x36: return OpCodes.Ble_Un_S;
                case 0x37: return OpCodes.Blt_Un_S;
                case 0x38: return OpCodes.Br;
                case 0x39: return OpCodes.Brfalse;
                case 0x3A: return OpCodes.Brtrue;
                case 0x3B: return OpCodes.Beq;
                case 0x3C: return OpCodes.Bge;
                case 0x3D: return OpCodes.Bgt;
                case 0x3E: return OpCodes.Ble;
                case 0x3F: return OpCodes.Blt;
                case 0x40: return OpCodes.Bne_Un;
                case 0x41: return OpCodes.Bge_Un;
                case 0x42: return OpCodes.Bgt_Un;
                case 0x43: return OpCodes.Ble_Un;
                case 0x44: return OpCodes.Blt_Un;
                case 0x45: return OpCodes.Switch;
                case 0x46: return OpCodes.Ldind_I1;
                case 0x47: return OpCodes.Ldind_U1;
                case 0x48: return OpCodes.Ldind_I2;
                case 0x49: return OpCodes.Ldind_U2;
                case 0x4A: return OpCodes.Ldind_I4;
                case 0x4B: return OpCodes.Ldind_U4;
                case 0x4C: return OpCodes.Ldind_I8;
                case 0x4D: return OpCodes.Ldind_I;
                case 0x4E: return OpCodes.Ldind_R4;
                case 0x4F: return OpCodes.Ldind_R8;
                case 0x50: return OpCodes.Ldind_Ref;
                case 0x51: return OpCodes.Stind_Ref;
                case 0x52: return OpCodes.Stind_I1;
                case 0x53: return OpCodes.Stind_I2;
                case 0x54: return OpCodes.Stind_I4;
                case 0x55: return OpCodes.Stind_I8;
                case 0x56: return OpCodes.Stind_R4;
                case 0x57: return OpCodes.Stind_R8;
                case 0x58: return OpCodes.Add;
                case 0x59: return OpCodes.Sub;
                case 0x5A: return OpCodes.Mul;
                case 0x5B: return OpCodes.Div;
                case 0x5C: return OpCodes.Div_Un;
                case 0x5D: return OpCodes.Rem;
                case 0x5E: return OpCodes.Rem_Un;
                case 0x5F: return OpCodes.And;
                case 0x60: return OpCodes.Or;
                case 0x61: return OpCodes.Xor;
                case 0x62: return OpCodes.Shl;
                case 0x63: return OpCodes.Shr;
                case 0x64: return OpCodes.Shr_Un;
                case 0x65: return OpCodes.Neg;
                case 0x66: return OpCodes.Not;
                case 0x67: return OpCodes.Conv_I1;
                case 0x68: return OpCodes.Conv_I2;
                case 0x69: return OpCodes.Conv_I4;
                case 0x6A: return OpCodes.Conv_I8;
                case 0x6B: return OpCodes.Conv_R4;
                case 0x6C: return OpCodes.Conv_R8;
                case 0x6D: return OpCodes.Conv_U4;
                case 0x6E: return OpCodes.Conv_U8;
                case 0x6F: return OpCodes.Callvirt;
                case 0x70: return OpCodes.Cpobj;
                case 0x71: return OpCodes.Ldobj;
                case 0x72: return OpCodes.Ldstr;
                case 0x73: return OpCodes.Newobj;
                case 0x74: return OpCodes.Castclass;
                case 0x75: return OpCodes.Isinst;
                case 0x76: return OpCodes.Conv_R_Un;
                case 0x79: return OpCodes.Unbox;
                case 0x7A: return OpCodes.Throw;
                case 0x7B: return OpCodes.Ldfld;
                case 0x7C: return OpCodes.Ldflda;
                case 0x7D: return OpCodes.Stfld;
                case 0x7E: return OpCodes.Ldsfld;
                case 0x7F: return OpCodes.Ldsflda;
                case 0x80: return OpCodes.Stsfld;
                case 0x81: return OpCodes.Stobj;
                case 0x82: return OpCodes.Conv_Ovf_I1_Un;
                case 0x83: return OpCodes.Conv_Ovf_I2_Un;
                case 0x84: return OpCodes.Conv_Ovf_I4_Un;
                case 0x85: return OpCodes.Conv_Ovf_I8_Un;
                case 0x86: return OpCodes.Conv_Ovf_U1_Un;
                case 0x87: return OpCodes.Conv_Ovf_U2_Un;
                case 0x88: return OpCodes.Conv_Ovf_U4_Un;
                case 0x89: return OpCodes.Conv_Ovf_U8_Un;
                case 0x8A: return OpCodes.Conv_Ovf_I_Un;
                case 0x8B: return OpCodes.Conv_Ovf_U_Un;
                case 0x8C: return OpCodes.Box;
                case 0x8D: return OpCodes.Newarr;
                case 0x8E: return OpCodes.Ldlen;
                case 0x8F: return OpCodes.Ldelema;
                case 0x90: return OpCodes.Ldelem_I1;
                case 0x91: return OpCodes.Ldelem_U1;
                case 0x92: return OpCodes.Ldelem_I2;
                case 0x93: return OpCodes.Ldelem_U2;
                case 0x94: return OpCodes.Ldelem_I4;
                case 0x95: return OpCodes.Ldelem_U4;
                case 0x96: return OpCodes.Ldelem_I8;
                case 0x97: return OpCodes.Ldelem_I;
                case 0x98: return OpCodes.Ldelem_R4;
                case 0x99: return OpCodes.Ldelem_R8;
                case 0x9A: return OpCodes.Ldelem_Ref;
                case 0x9B: return OpCodes.Stelem_I;
                case 0x9C: return OpCodes.Stelem_I1;
                case 0x9D: return OpCodes.Stelem_I2;
                case 0x9E: return OpCodes.Stelem_I4;
                case 0x9F: return OpCodes.Stelem_I8;
                case 0xA0: return OpCodes.Stelem_R4;
                case 0xA1: return OpCodes.Stelem_R8;
                case 0xA2: return OpCodes.Stelem_Ref;
                case 0xA3: return OpCodes.Ldelem;
                case 0xA4: return OpCodes.Stelem;
                case 0xA5: return OpCodes.Unbox_Any;
                case 0xB3: return OpCodes.Conv_Ovf_I1;
                case 0xB4: return OpCodes.Conv_Ovf_U1;
                case 0xB5: return OpCodes.Conv_Ovf_I2;
                case 0xB6: return OpCodes.Conv_Ovf_U2;
                case 0xB7: return OpCodes.Conv_Ovf_I4;
                case 0xB8: return OpCodes.Conv_Ovf_U4;
                case 0xB9: return OpCodes.Conv_Ovf_I8;
                case 0xBA: return OpCodes.Conv_Ovf_U8;
                case 0xC2: return OpCodes.Refanyval;
                case 0xC3: return OpCodes.Ckfinite;
                case 0xC6: return OpCodes.Mkrefany;
                case 0xD0: return OpCodes.Ldtoken;
                case 0xD1: return OpCodes.Conv_U2;
                case 0xD2: return OpCodes.Conv_U1;
                case 0xD3: return OpCodes.Conv_I;
                case 0xD4: return OpCodes.Conv_Ovf_I;
                case 0xD5: return OpCodes.Conv_Ovf_U;
                case 0xD6: return OpCodes.Add_Ovf;
                case 0xD7: return OpCodes.Add_Ovf_Un;
                case 0xD8: return OpCodes.Mul_Ovf;
                case 0xD9: return OpCodes.Mul_Ovf_Un;
                case 0xDA: return OpCodes.Sub_Ovf;
                case 0xDB: return OpCodes.Sub_Ovf_Un;
                case 0xDC: return OpCodes.Endfinally;
                case 0xDD: return OpCodes.Leave;
                case 0xDE: return OpCodes.Leave_S;
                case 0xDF: return OpCodes.Stind_I;
                case 0xE0: return OpCodes.Conv_U;
                case 0xF8: throw new NotSupportedException("Illegal OpCode [Prefix7]"); //return OpCodes.Prefix7;
                case 0xF9: throw new NotSupportedException("Illegal OpCode [Prefix6]"); //return OpCodes.Prefix6;
                case 0xFA: throw new NotSupportedException("Illegal OpCode [Prefix5]"); //return OpCodes.Prefix5;
                case 0xFB: throw new NotSupportedException("Illegal OpCode [Prefix4]"); //return OpCodes.Prefix4;
                case 0xFC: throw new NotSupportedException("Illegal OpCode [Prefix3]"); //return OpCodes.Prefix3;
                case 0xFD: throw new NotSupportedException("Illegal OpCode [Prefix2]"); //return OpCodes.Prefix2;
                case 0xFF: return OpCodes.Prefixref;
                case 0xFE:
                    switch (Reader.ReadByte())
                    {
                        case 0x00: return OpCodes.Arglist;
                        case 0x01: return OpCodes.Ceq;
                        case 0x02: return OpCodes.Cgt;
                        case 0x03: return OpCodes.Cgt_Un;
                        case 0x04: return OpCodes.Clt;
                        case 0x05: return OpCodes.Clt_Un;
                        case 0x06: return OpCodes.Ldftn;
                        case 0x07: return OpCodes.Ldvirtftn;
                        case 0x09: return OpCodes.Ldarg;
                        case 0x0A: return OpCodes.Ldarga;
                        case 0x0B: return OpCodes.Starg;
                        case 0x0C: return OpCodes.Ldloc;
                        case 0x0D: return OpCodes.Ldloca;
                        case 0x0E: return OpCodes.Stloc;
                        case 0x0F: return OpCodes.Localloc;
                        case 0x11: return OpCodes.Endfilter;
                        case 0x12: return OpCodes.Unaligned;
                        case 0x13: return OpCodes.Volatile;
                        case 0x14: return OpCodes.Tailcall;
                        case 0x15: return OpCodes.Initobj;
                        case 0x16: return OpCodes.Constrained;
                        case 0x17: return OpCodes.Cpblk;
                        case 0x18: return OpCodes.Initblk;
                        case 0x1A: return OpCodes.Rethrow;
                        case 0x1C: return OpCodes.Sizeof;
                        case 0x1D: return OpCodes.Refanytype;
                        case 0x1E: return OpCodes.Readonly;
                    }
                    break;
            }
            throw new NotSupportedException("Unknown OpCode");
        }
    }
}
