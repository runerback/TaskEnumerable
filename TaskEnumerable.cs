using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Runerback.Utils
{
    public static class TaskEnumerable
    {
        public static ITaskEnumerable<T> Empty<T>()
        {
            return EmptyTaskEnumerable<T>.Default;
        }

        public static ITaskEnumerable<T> ToTaskEnumerable<T>(this IEnumerable<Task<T>> source)
        {
            if (source == null)
                return Empty<T>();
            return new WrappedTaskEnumerable<T>(source);
        }

        public static async Task ForEach<T>(this ITaskEnumerable<T> source, Action<T, ITaskForEachController> action)
        {
            if (source == null || action == null)
                return;

            var controller = new TaskForEachController();
            using (var iterator = source.GetEnumerator())
            {
                while (iterator.MoveNext())
                {
                    if (controller.CheckContinue())
                        continue;

                    var item = await iterator.Current;
                    action(await iterator.Current, controller);

                    if (controller.CheckBreak())
                        break;
                }
            }
        }

        public static ITaskEnumerable<TResult> Continue<TSource, TResult>(this ITaskEnumerable<TSource> source, Func<Task<TSource>, Task<TResult>> continuation)
        {
            if (source == null || continuation == null)
                return Empty<TResult>();
            return new ContinueTaskEnumerable<TSource, TResult>(source, continuation);
        }

        public static ITaskEnumerable<T> Map<T>(this ITaskEnumerable<ITaskEnumerable<T>> source)
        {
            if (source == null)
                return Empty<T>();
            return new MappedTaskEnumerable<T>(source);
        }

        sealed class TaskForEachController : ITaskForEachController
        {
            private bool _continue = false;
            private bool _break = false;

            public bool CheckContinue()
            {
                if (_continue)
                {
                    _continue = false;
                    return true;
                }
                return false;
            }

            void ITaskForEachController.Continue()
            {
                _continue = true;
            }

            public bool CheckBreak()
            {
                if (_break)
                {
                    _break = false;
                    return true;
                }
                return false;
            }

            void ITaskForEachController.Break()
            {
                _break = true;
            }
        }

        sealed class EmptyTaskEnumerable<T> : ITaskEnumerable<T>
        {
            private EmptyTaskEnumerable() { }

            private static ITaskEnumerable<T> instance;
            public static ITaskEnumerable<T> Default
            {
                get
                {
                    var result = instance;
                    if (result == null)
                    {
                        instance = new EmptyTaskEnumerable<T>();
                        result = instance;
                    }

                    return result;
                }
            }

            public IEnumerator<Task<T>> GetEnumerator()
            {
                yield break;
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        sealed class WrappedTaskEnumerable<T> : ITaskEnumerable<T>
        {
            public WrappedTaskEnumerable(IEnumerable<Task<T>> source)
            {
                this.source = source ?? throw new ArgumentNullException(nameof(source));
            }

            private readonly IEnumerable<Task<T>> source;

            public IEnumerator<Task<T>> GetEnumerator()
            {
                return source.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        sealed class ContinueTaskEnumerable<TSource, TResult> : ITaskEnumerable<TResult>
        {
            public ContinueTaskEnumerable(ITaskEnumerable<TSource> source, Func<Task<TSource>, Task<TResult>> continuation)
            {
                this.source = source ?? throw new ArgumentNullException(nameof(source));
                this.continuation = continuation ?? throw new ArgumentNullException(nameof(continuation));
            }

            private readonly ITaskEnumerable<TSource> source;
            private readonly Func<Task<TSource>, Task<TResult>> continuation;

            public IEnumerator<Task<TResult>> GetEnumerator()
            {
                using (var iterator = source.GetEnumerator())
                {
                    var continuation = this.continuation;
                    while (iterator.MoveNext())
                        yield return continuation(iterator.Current);
                }
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        sealed class MappedTaskEnumerable<T> : ITaskEnumerable<T>
        {
            public MappedTaskEnumerable(ITaskEnumerable<ITaskEnumerable<T>> source)
            {
                this.source = source ?? throw new ArgumentNullException(nameof(source));
            }

            private readonly ITaskEnumerable<ITaskEnumerable<T>> source;

            public IEnumerator<Task<T>> GetEnumerator()
            {
                return source.SelectMany(item => item.Result).GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }
    }
}
