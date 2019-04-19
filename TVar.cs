using System;
using TVarId = System.UInt32;

namespace STM
{
    struct TVar<T>
    {
        string name;
        TVarId id;
    }
}
