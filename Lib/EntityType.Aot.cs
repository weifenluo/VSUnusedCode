using System;

namespace VSUnusedCode
{
    partial class EntityType
    {
        protected static void RegisterInitializer<T>(Action<T> initializer)
            where T : EntityType
        {
        }
    }
}
