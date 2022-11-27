using System;
using System.Collections.Generic;
using System.Reflection;

namespace EmitLoader
{
    /// <summary>
    /// Built in Helpers for The AssemblyLoader Type System
    /// </summary>
    public static class AssemblyLoaderHelpers
    {
        /// <summary>
        /// Validates the Provided Generic Parameter Arguments
        /// </summary>
        /// <param name="Arguments">Generic Arguments</param>
        /// <param name="Parameters">Generic Parameters</param>
        public static Boolean ValidateGenericParameterConstraints(IType[] Arguments, IGenericParameter[] Parameters)
        {
            if (Arguments.Length != Parameters.Length)
                return false;

            for (int x = 0; x < Arguments.Length; x++)
            {
                IGenericParameter Param = Parameters[x];
                IType Arg = Arguments[x];

                if ((Param.GenericParameterAttributes & GenericParameterAttributes.SpecialConstraintMask) != GenericParameterAttributes.None)
                {
                    if (Param.GenericParameterAttributes.HasFlag(GenericParameterAttributes.NotNullableValueTypeConstraint))
                        if (!Arg.IsValueType && !Arg.IsEnum)
                            return false;

                    if (Param.GenericParameterAttributes.HasFlag(GenericParameterAttributes.DefaultConstructorConstraint))
                        if (Arg.FindConstructor(Array.Empty<IType>()) == null)
                            return false;

                    if (Param.GenericParameterAttributes.HasFlag(GenericParameterAttributes.ReferenceTypeConstraint))
                        if (Arg.IsValueType || Arg.IsEnum)
                            return false;
                }


                for (int y = 0; y < Param.Constraints.Length; y++)
                {
                    IGenericParameterConstraint constraint = Param.Constraints[y];
                    if (!Arg.IsCastableTo(constraint.ConstrainType))
                        return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Validates that the Provided Type(<paramref name="self"/>) can be cast to <paramref name="type"/>
        /// </summary>
        /// <param name="self"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public static Boolean IsCastableTo(IType self, IType type)
        {
            if (type.IsArray)
                return self.IsArray && IsCastableTo(self.GetElementType(), type.GetElementType());
            else if (type.IsInterface)
            {
                Queue<IType> queue = new Queue<IType>();
                foreach (IType @interface in self.Interfaces)
                    queue.Enqueue(@interface);

                while (queue.Count > 0)
                {
                    self = queue.Dequeue();

                    if (self == type)
                        return true;

                    foreach (IType @interface in self.Interfaces)
                        queue.Enqueue(@interface);
                }
            }
            else
            {
                do
                {
                    if (self.BaseType == type)
                        return true;
                    self = self.BaseType;
                }
                while (self != null);
            }
            return false;
        }
    }
}
