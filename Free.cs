using System;
using System.Threading;
using LaYumba.Functional;
using static LaYumba.Functional.F;
using Unit = System.ValueTuple;

namespace STM
{
    public interface STML<A>
    {
        STML<B> Bind<B>(Func<A, STML<B>> f);
    }

    public sealed class Return<A> : STML<A>
    {
        public readonly A Result;
        public Return(A a) => Result = a;

        public STML<B> Bind<B>(Func<A, STML<B>> f) => f(Result);
    }

    public class STML<I, O, A> : STML<A>
    {
        public readonly I Input;
        public readonly Func<O, STML<A>> Next;
        public STML(I input, Func<O, STML<A>> next) => (Input, Next) = (input, next);

        public STML<B> Bind<B>(Func<A, STML<B>> f) => new STML<I, O, B>(Input, r => Next(r).Bind(f));
    }

    public static class STMLMonad
    {
        public static STML<A> Lift<A>(this A a) =>
            new Return<A>(a);

        public static STML<B> Select<A, B>(this STML<A> m, Func<A, B> f) =>
            m.Bind(a => f(a).Lift());

        public static STML<C> SelectMany<A, B, C>(this STML<A> m, Func<A, STML<B>> f, Func<A, B, C> project) =>
            m.Bind(a => f(a).Bind(b => project(a, b).Lift()));
    }

    public static class STMLMonadSugar
    {
        public static STML<R> ToSTML<I, R>(this I input) => new STML<I, R, R>(input, STMLMonad.Lift);
        public static STML<Unit> ToSTML<I>(this I input) => input.ToSTML<I, Unit>();

        public static STML<A> Ignore<I, A>(this STML<I, Unit, A> x) => x.Next(Unit());

        public static STML<A> As<I, O, A>(this STML<I, O, A> x, Func<I, O> process) => x.Next(process(x.Input));
        public static STML<A> As<I, A>(this STML<I, Unit, A> x, Action<I> process)
        {
            process(x.Input);
            return x.Ignore();
        }
    }

    readonly struct NewTVar<T>
    {
        public readonly T Value;
        public NewTVar(T value)
        {
            this.Value = value;
        }
    }

    readonly struct ReadTVar<T>
    {
        public readonly TVar<T> TVar;
        public ReadTVar(TVar<T> tvar)
        {
            this.TVar = tvar;
        }
    }

    readonly struct WriteTVar<T>
    {
        public readonly TVar<T> TVar;
        public readonly T Value;
        public WriteTVar(TVar<T> tvar, T value)
        {
            this.TVar = tvar;
            this.Value = value;
        }
    }

    static class Operations
    {
        public static STML<TVar<T>> NewTVar<T>(T value) =>
            new NewTVar<T>(value).ToSTML<NewTVar<T>, TVar<T>>();

        public static STML<T> ReadTVar<T>(TVar<T> tvar) =>
            new ReadTVar<T>(tvar).ToSTML<ReadTVar<T>, T>();

        public static STML<Unit> WriteTVar<T>(TVar<T> tvar, T value) =>
            new WriteTVar<T>(tvar, value).ToSTML<WriteTVar<T>, Unit>();

    }
}
