using System;
using System.Threading;

namespace STM
{
    static class Runtime
    {
        static T RunSTM<T>(Context context, Func<AtomicRuntime, RunResult<T>> runner)
        {
            var ustamp = context.NewId();
            while (true)
            {
                var snapshot = context.TakeSnapshot();
                var runtime = new AtomicRuntime(context, ustamp, snapshot);
                var runResult = runner(runtime);
                if (runResult.retry)
                {
                    var random = new Random();
                    var t = random.Next(1, 100);
                    Thread.Sleep(t * 10);
                    continue;
                }

                bool success = context.TryCommit(ustamp, runtime.stagedTVars);
                if (success)
                {
                    return runResult.result.Match(
                        () => throw new Exception("Bad optional access"),
                        (value) => value
                    );
                }
            }

        }
    }
}
