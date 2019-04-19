using System;
using System.Collections.Generic;
using LaYumba.Functional;
using System.Threading;
using TVarId = System.Int32;

namespace STM
{
    using TVars = Dictionary<TVarId, TVarHandle>;
    using UStamp = System.Int32;

    struct TVarHandle
    {
        public UStamp ustamp;
        public Object data;
        public bool modified;
    }

    class Context
    {
        private static Int32 _idCounter;
        private Int32 _id = 0;
        TVars _tvars;
        object _lockObj = new Object();

        public bool TryCommit(UStamp ustamp, TVars stagedTvars)
        {
            lock(_lockObj)
            {
                bool conflict = false;
                foreach (var it in stagedTvars)
                {
                    TVarId stagedTVarId = it.Key;
                    var isFound = _tvars.TryGetValue(stagedTVarId, out var found);
                    if (!isFound) continue;
                    if (it.Value.modified && (it.Value.ustamp != found.ustamp))
                    {
                        conflict = true;
                        break;
                    }
                }
                if (!conflict)
                {
                    foreach (var it in stagedTvars)
                    {
                        var handle = new TVarHandle();
                        handle.data = it.Value.data;
                        handle.ustamp = it.Value.ustamp;
                        handle.modified = it.Value.modified;
                        _tvars[it.Key] = handle;
                    }
                }
                return !conflict;
            }
        }

        public Int32 NewId()
        {
            return Interlocked.Increment(ref _idCounter);
        }

        public TVars TakeSnapshot()
        {
            lock(_lockObj)
            {
                TVars tvars = _tvars;
                return _tvars;
            }
        }
    }

    class AtomicRuntime
    {
        Context context { get; }
        public UStamp ustamp { get; }
        public TVars stagedTVars { get; }

        public AtomicRuntime(Context context, UStamp ustamp, TVars tvars)
        {
            this.context = context;
            this.ustamp = ustamp;
            this.stagedTVars = tvars;
        }

        public TVarId NewId()
        {
            return this.context.NewId();
        }

        public void AddTVarHandle(TVarId tvarId, TVarHandle tvarHandle)
        {
            var isFound = this.stagedTVars.TryGetValue(tvarId, out var found);
            if (isFound)
            {
                throw new Exception("TVar is not unique!");
            }
            this.stagedTVars[tvarId] = tvarHandle;
        }

        public TVarHandle GetTVarHandle(TVarId tvarId)
        {
            var isFound = this.stagedTVars.TryGetValue(tvarId, out var found);
            if (!isFound)
            {
                throw new Exception("TVar not found: " + tvarId);
            }
            return found;
        }

        public void SetTVarHandleData(TVarId tvarId, Object data)
        {
            var handle = new TVarHandle();
            handle.data = data;
            handle.ustamp =  this.stagedTVars[tvarId].ustamp;
            handle.modified = true;
            this.stagedTVars[tvarId] = handle;
        }
    }

    struct RunResult<T>
    {
        public bool retry;
        public Option<T> result;
    }
}
