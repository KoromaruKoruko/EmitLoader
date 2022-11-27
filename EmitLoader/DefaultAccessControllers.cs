using System;
using System.Reflection;

namespace EmitLoader
{
    /// <summary>
    /// The Default Prebuilt Access Controllers
    /// </summary>
    public static class DefaultAccessControllers
    {
        /// <summary>
        /// No Restriction / Full Access Controller
        /// </summary>
        public static readonly IAccessController NoRestrictions = new FullAccess();
        private class FullAccess : IAccessController
        {
            public AccessKind CanAccess(Assembly assembly) => AccessKind.Full;
            public AccessKind CanAccess(string @namespace) => AccessKind.Full;
            public AccessKind CanAccess(Type type) => AccessKind.Full;
            public AccessKind CanAccess(MethodInfo method) => AccessKind.Full;
            public AccessKind CanAccess(ConstructorInfo constructor) => AccessKind.Full;
            public AccessKind CanAccess(FieldInfo field) => AccessKind.Full;

            public bool CanGet(FieldInfo field) => true;
            public bool CanSet(FieldInfo field) => true;
        }
    }
}
