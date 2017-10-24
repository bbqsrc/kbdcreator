using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace System.Reactive.Feedback
{
    public struct ObservableSchedulerContext<E> : IObservable<E>
    {
        internal IObservable<E> Source;
        internal IScheduler Scheduler;

        public IDisposable Subscribe(IObserver<E> observer)
        {
            return Source.Subscribe(observer);
        }
    }

    internal static class ObservableExtensions
    {
        internal static IObservable<T> DoOnSubscribe<T>(this IObservable<T> source, Action action)
        {
            return Observable.Defer(() =>
            {
                action();
                return source;
            });
        }
    }

    public static class Feedback
    {
        public static IObservable<TState> System<TState, TEvent>(TState initialState,
            Func<TState, TEvent, TState> reduce,
            IScheduler scheduler,
            params Func<ObservableSchedulerContext<TState>, IObservable<TEvent>>[] scheduledFeedback
        )
        {
            return Observable.Defer(() =>
            {
                var replaySubject = new ReplaySubject<TState>(1);

                IObservable<TEvent> events = Observable.Merge(scheduledFeedback.Select(feedback =>
                {
                    var state = new ObservableSchedulerContext<TState>()
                    {
                        Source = replaySubject.AsObservable(),
                        Scheduler = scheduler
                    };
                    var result = feedback(state);
                    return result.ObserveOn(Scheduler.CurrentThread);
                }));

                return events.Scan(initialState, reduce)
                    .DoOnSubscribe(() => replaySubject.OnNext(initialState))
                    .Do(output => replaySubject.OnNext(output))
                    .SubscribeOn(scheduler)
                    .StartWith(initialState)
                    .ObserveOn(scheduler);
            });
        }

        public static IObservable<TState> System<TState, TEvent>(TState initialState,
            Func<TState, TEvent, TState> reduce,
            params Func<ObservableSchedulerContext<TState>, IObservable<TEvent>>[] scheduledFeedback
        )
        {
            return System(initialState, reduce, Scheduler.CurrentThread, scheduledFeedback);
        }
    }

    public static class FeedbackLoops
    {
        public static Func<ObservableSchedulerContext<TState>, IObservable<TEvent>>
        React<TState, TEvent, TControl>(
            Func<TState, TControl> query,
            Func<TControl, IObservable<TEvent>> effects
        ) where TControl : TEvent, IEquatable<TEvent>
        {
            return state =>
            {
                return state.Select(query)
                    .DistinctUntilChanged()
                    .SelectMany(control =>
                    {
                        if (control == null)
                        {
                            return Observable.Empty<TEvent>();
                        }

                        return effects(control)
                            .ObserveOn(state.Scheduler)
                            .SubscribeOn(state.Scheduler);
                    });
            };
        }
    }

    public class UIBindings<TEvent> : IDisposable
    {
        private IDisposable[] subscriptions;
        internal IObservable<TEvent>[] Events;

        public static UIBindings<TEvent> Create(IDisposable subscription, params IObservable<TEvent>[] events)
        {
            return new UIBindings<TEvent>(new IDisposable[] { subscription }, events);
        }

        public static UIBindings<TEvent> Create(IDisposable[] subscriptions, params IObservable<TEvent>[] events)
        {
            return new UIBindings<TEvent>(subscriptions, events);
        }

        internal UIBindings(IDisposable[] subscriptions, IObservable<TEvent>[] events)
        {
            this.subscriptions = subscriptions;
            Events = events;
        }

        public void Dispose()
        {
            foreach (var subscription in subscriptions)
            {
                subscription.Dispose();
            }
        }
    }

    public struct FeedbackObserverFunc<TState, TEvent>
    {
        public Func<ObservableSchedulerContext<TState>, IObservable<TEvent>> Value;
    }

    public static class UI
    {
        public static FeedbackObserverFunc<TState, TEvent>
        Bind<TState, TEvent>(Func<ObservableSchedulerContext<TState>, UIBindings<TEvent>> bindingsFn)
        {
            Func <ObservableSchedulerContext<TState>, IObservable<TEvent>> func = state =>
            {
                return Observable.Using(() => bindingsFn(state), bindings =>
                {
                    return Observable.Merge(bindings.Events)
                        .ObserveOn(state.Scheduler)
                        .SubscribeOn(state.Scheduler);
                });
            };

            return new FeedbackObserverFunc<TState, TEvent>
            {
                Value = func
            };
        }
    }
}