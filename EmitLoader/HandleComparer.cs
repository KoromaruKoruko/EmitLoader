using System;
using System.Collections.Generic;
using System.Reflection.Metadata;

namespace EmitLoader
{
    internal class InterfaceComparer
        : IEqualityComparer<IType>,
        IEqualityComparer<IGeneric>,
        IEqualityComparer<IMethod>,
        IEqualityComparer<IType[]>,
        IEqualityComparer<IGeneric[]>,
        IEqualityComparer<IMethod[]>
    {
        public static readonly InterfaceComparer Instance = new InterfaceComparer();
        private InterfaceComparer() { }

        public bool Equals(IType x, IType y) => Object.ReferenceEquals(x, y);
        public int GetHashCode(IType obj) => obj.GetHashCode();
        public bool Equals(IType[] x, IType[] y)
        {
            if (x.Length != y.Length)
                return false;

            for (int i = 0; i < x.Length; i++)
                if (!Equals(x[i], y[i]))
                    return false;

            return true;
        }
        public int GetHashCode(IType[] obj)
        {
            int x = 0;
            foreach (IType t in obj)
                x ^= t.GetHashCode();
            return x;
        }


        public bool Equals(IGeneric x, IGeneric y) => Object.ReferenceEquals(x, y);
        public int GetHashCode(IGeneric obj) => obj.GetHashCode();
        public bool Equals(IGeneric[] x, IGeneric[] y)
        {
            if (x.Length != y.Length)
                return false;

            for (int i = 0; i < x.Length; i++)
                if (!Equals(x[i], y[i]))
                    return false;

            return true;
        }
        public int GetHashCode(IGeneric[] obj)
        {
            int x = 0;
            foreach (IType t in obj)
                x ^= t.GetHashCode();
            return x;
        }


        public bool Equals(IMethod x, IMethod y) => Object.ReferenceEquals(x, y);
        public int GetHashCode(IMethod obj) => obj.GetHashCode();
        public bool Equals(IMethod[] x, IMethod[] y)
        {
            if (x.Length != y.Length)
                return false;

            for (int i = 0; i < x.Length; i++)
                if (!Equals(x[i], y[i]))
                    return false;

            return true;
        }
        public int GetHashCode(IMethod[] obj)
        {
            int x = 0;
            foreach (IType t in obj)
                x ^= t.GetHashCode();
            return x;
        }
    }
}
