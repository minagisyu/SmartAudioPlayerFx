﻿using System;
using System.Linq.Expressions;
using System.Linq;
using System.Collections;
using System.ComponentModel;
using System.Diagnostics.Contracts;
using Codeplex.Reactive.Extensions;
#if WINDOWS_PHONE
using Microsoft.Phone.Reactive;
using SerialDisposable = Microsoft.Phone.Reactive.MutableDisposable;
#else
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Disposables;
using System.Reactive.Concurrency;
#endif
#if !WP_COMMON
using System.ComponentModel.DataAnnotations;
#endif

namespace Codeplex.Reactive
{
    internal class SingletonPropertyChangedEventArgs
    {
        public static readonly PropertyChangedEventArgs Value = new PropertyChangedEventArgs("Value");
    }
#if SILVERLIGHT
    internal class SingletonDataErrorsChangedEventArgs
    {
        public static readonly DataErrorsChangedEventArgs Value = new DataErrorsChangedEventArgs("Value");
    }
#endif

    [Flags]
    public enum ReactivePropertyMode
    {
        None = 0x00,
        /// <summary>If next value is same as current, not set and not notify.</summary>
        DistinctUntilChanged = 0x01,
        /// <summary>Push notify on instance created and subscribed.</summary>
        RaiseLatestValueOnSubscribe = 0x02
    }

    // for EventToReactive and Serialization
    public interface IReactiveProperty
    {
        object Value { get; set; }
        string ForceValidate();
        IObservable<object> ObserveErrorChanged { get; }
    }

    /// <summary>
    /// Two-way bindable IObserable&lt;T&gt;
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ReactiveProperty<T> : IObservable<T>, IDisposable, INotifyPropertyChanged, IReactiveProperty, IDataErrorInfo
#if SILVERLIGHT
, INotifyDataErrorInfo
#endif
    {
        public event PropertyChangedEventHandler PropertyChanged;

        T latestValue;
        bool isDisposed = false;
        readonly IScheduler raiseEventScheduler;
        readonly IObservable<T> source;
        readonly Subject<T> anotherTrigger = new Subject<T>();
        readonly IDisposable sourceDisposable;
        readonly IDisposable raiseSubscription;

        // for Validation
        bool isValueChanged = false;
        readonly SerialDisposable validateNotifyErrorSubscription = new SerialDisposable();
        readonly Subject<object> errorsTrigger = new Subject<object>();

        /// <summary>PropertyChanged raise on UIDispatcherScheduler</summary>
        public ReactiveProperty(T initialValue = default(T), ReactivePropertyMode mode = ReactivePropertyMode.DistinctUntilChanged|ReactivePropertyMode.RaiseLatestValueOnSubscribe)
            : this(UIDispatcherScheduler.Default, initialValue, mode)
        { }

        /// <summary>PropertyChanged raise on selected scheduler</summary>
        public ReactiveProperty(IScheduler raiseEventScheduler, T initialValue = default(T), ReactivePropertyMode mode = ReactivePropertyMode.DistinctUntilChanged|ReactivePropertyMode.RaiseLatestValueOnSubscribe)
            : this(Observable.Never<T>(), raiseEventScheduler, initialValue, mode)
        {
        }

        // ToReactiveProperty Only
        internal ReactiveProperty(IObservable<T> source, T initialValue = default(T), ReactivePropertyMode mode = ReactivePropertyMode.DistinctUntilChanged|ReactivePropertyMode.RaiseLatestValueOnSubscribe)
            : this(source, UIDispatcherScheduler.Default, initialValue, mode)
        {
        }

        internal ReactiveProperty(IObservable<T> source, IScheduler raiseEventScheduler, T initialValue = default(T), ReactivePropertyMode mode = ReactivePropertyMode.DistinctUntilChanged|ReactivePropertyMode.RaiseLatestValueOnSubscribe)
        {
            this.latestValue = initialValue;
            this.raiseEventScheduler = raiseEventScheduler;

            // create source
            var merge = source.Merge(anotherTrigger);
            if (mode.HasFlag(ReactivePropertyMode.DistinctUntilChanged)) merge = merge.DistinctUntilChanged();
            merge = merge.Do(x =>
            {
                // setvalue immediately
                if (!isValueChanged) isValueChanged = true;
                latestValue = x;
            });

            // publish observable
            var connectable = (mode.HasFlag(ReactivePropertyMode.RaiseLatestValueOnSubscribe))
                ? merge.Publish(initialValue)
                : merge.Publish();
            this.source = connectable.AsObservable();

            // raise notification
            this.raiseSubscription = connectable
                .ObserveOn(raiseEventScheduler)
                .Subscribe(x =>
                {
                    var handler = PropertyChanged;
                    if (handler != null) PropertyChanged(this, SingletonPropertyChangedEventArgs.Value);
                });

            // start source
            this.sourceDisposable = connectable.Connect();
        }

        /// <summary>
        /// Get latestValue or push(set) value.
        /// </summary>
        public T Value
        {
            get { return latestValue; }
            set { anotherTrigger.OnNext(value); }
        }

        object IReactiveProperty.Value
        {
            get { return (T)Value; }
            set { Value = (T)value; }
        }

        /// <summary>
        /// Subscribe source.
        /// </summary>
        public IDisposable Subscribe(IObserver<T> observer)
        {
            return source.Subscribe(observer);
        }

        /// <summary>
        /// Unsubcribe all subscription.
        /// </summary>
        public void Dispose()
        {
            if (isDisposed) return;

            isDisposed = true;
            anotherTrigger.Dispose();
            raiseSubscription.Dispose();
            sourceDisposable.Dispose();
            validateNotifyErrorSubscription.Dispose();
            errorsTrigger.OnCompleted();
            errorsTrigger.Dispose();
        }

        public override string ToString()
        {
            return (latestValue == null)
                ? "null"
                : "{" + latestValue.GetType().Name + ":" + latestValue.ToString() + "}";
        }

        // Validations

        /// <summary>
        /// <para>Checked validation, raised value. If success return value is null.</para>
        /// <para>From Attribute is Exception, from IDataErrorInfo is string, from IDataNotifyErrorInfo is Enumerable.</para>
        /// <para>If you want to assort type, please choose OfType. For example: ErrorsChanged.OfType&lt;string&gt;().</para>
        /// </summary>
        public IObservable<object> ObserveErrorChanged
        {
            get { return errorsTrigger.AsObservable(); }
        }

        // Exception

        /// <summary>
        /// Trigger IDataErrorInfo validation.
        /// </summary>
        /// <returns>If success return null, failure return string(ErrorMessage).</returns>
        public string ForceValidate()
        {
            return ((IDataErrorInfo)this)["Value"];
        }

#if! WP_COMMON
        ValidationContext validationContext;
        ValidationAttribute[] attributes;

        /// <summary>
        /// <para>Set DataAnnotaion's validation, return value is self.</para>
        /// <para>Note:This validation check by IDataErrorInfo. Please turn on XAML's ValidatesOnDataErrors</para>
        /// </summary>
        /// <param name="selfSelector">Self selector. For example: () =&gt; this.MyProperty</param>
        public ReactiveProperty<T> SetValidateAttribute(Expression<Func<ReactiveProperty<T>>> selfSelector)
        {
            this.attributes = ((MemberExpression)selfSelector.Body).Member
                .GetCustomAttributes(typeof(ValidationAttribute), true)
                .Cast<ValidationAttribute>()
                .ToArray();
            return this;
        }

        Exception ValidateException()
        {
            try
            {
                if (validationContext == null)
                {
                    validationContext = new ValidationContext(this, null, null) { MemberName = "Value" };
                }

                foreach (var item in attributes)
                {
                    item.Validate(latestValue, validationContext);
                }

                return null;
            }
            catch (Exception ex)
            {
                return ex;
            }
        }
#endif

        // IDataErrorInfo

        Func<T, string> dataErrorInfoValidate;
        string currentError;

        /// <summary>
        /// <para>Set IDataErrorInfo's validation, return value is self.</para>
        /// </summary>
        /// <param name="validate">If success return null, failure return string(ErrorMessage).</param>
        public ReactiveProperty<T> SetValidateError(Func<T, string> validate)
        {
            this.dataErrorInfoValidate = validate;
            return this;
        }

        /// <summary>Get IDataErrorInfo's error store</summary>
        public string Error
        {
            get { return currentError; }
        }

        string IDataErrorInfo.this[string columnName]
        {
            get
            {
                if (columnName != "Value") return null;
                if (!isValueChanged) return null;

#if! WP_COMMON
                if (attributes != null)
                {
                    var exceptionResult = ValidateException();
                    if (exceptionResult != null)
                    {
                        currentError = exceptionResult.Message;
                        errorsTrigger.OnNext(currentError);
                        return currentError;
                    }
                }
#endif

                var handler = dataErrorInfoValidate;
                if (handler != null)
                {
                    currentError = handler(latestValue);
                    errorsTrigger.OnNext(currentError);
                    return currentError;
                }

                errorsTrigger.OnNext(null);
                return null;
            }
        }

#if SILVERLIGHT
        // INotifyDataErrorInfo

        IEnumerable currentErrors;
        public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;

        /// <summary>
        /// <para>Set INotifyDataErrorInfo's asynchronous validation, return value is self.</para>
        /// </summary>
        /// <param name="validate">Argument is self. If success return IO&lt;null&gt;, failure return IO&lt;IEnumerable&gt;(Errors).</param>
        public ReactiveProperty<T> SetValidateNotifyError(Func<IObservable<T>, IObservable<IEnumerable>> validate)
        {
            validateNotifyErrorSubscription.Disposable = validate(source)
                .Subscribe(xs =>
                {
                    currentErrors = xs;
                    var handler = ErrorsChanged;
                    if (handler != null)
                    {
                        raiseEventScheduler.Schedule(() =>
                            handler(this, SingletonDataErrorsChangedEventArgs.Value));
                    }
                    errorsTrigger.OnNext(currentErrors);
                });

            return this;
        }

        /// <summary>Get INotifyDataErrorInfo's error store</summary>
        public System.Collections.IEnumerable GetErrors(string propertyName)
        {
            return currentErrors;
        }

        /// <summary>Get INotifyDataErrorInfo's error store</summary>
        public bool HasErrors
        {
            get { return currentErrors != null; }
        }

#endif
    }

    /// <summary>
    /// Static methods and extension methods of ReactiveProperty&lt;T&gt;
    /// </summary>
    public static class ReactiveProperty
    {
        /// <summary>
        /// <para>Convert plain object to ReactiveProperty.</para>
        /// <para>Value is OneWayToSource(ReactiveProperty -> Object) synchronized.</para>
        /// <para>PropertyChanged raise on UIDispatcherScheduler</para>
        /// </summary>
        public static ReactiveProperty<TProperty> FromObject<TTarget, TProperty>(
            TTarget target,
            Expression<Func<TTarget, TProperty>> propertySelector,
            ReactivePropertyMode mode = ReactivePropertyMode.DistinctUntilChanged|ReactivePropertyMode.RaiseLatestValueOnSubscribe)
        {
            return FromObject(target, propertySelector, UIDispatcherScheduler.Default, mode);
        }

        /// <summary>
        /// <para>Convert plain object to ReactiveProperty.</para>
        /// <para>Value is OneWayToSource(ReactiveProperty -> Object) synchronized.</para>
        /// <para>PropertyChanged raise on selected scheduler</para>
        /// </summary>
        public static ReactiveProperty<TProperty> FromObject<TTarget, TProperty>(
            TTarget target,
            Expression<Func<TTarget, TProperty>> propertySelector,
            IScheduler raiseEventScheduler,
            ReactivePropertyMode mode = ReactivePropertyMode.DistinctUntilChanged|ReactivePropertyMode.RaiseLatestValueOnSubscribe)
        {
            string propertyName; // no use
            var getter = AccessorCache<TTarget>.LookupGet(propertySelector, out propertyName);
            var setter = AccessorCache<TTarget>.LookupSet(propertySelector, out propertyName);

            var result = new ReactiveProperty<TProperty>(raiseEventScheduler, initialValue: getter(target), mode: mode);
            result.Subscribe(x => setter(target, x));

            return result;
        }

        /// <summary>
        /// <para>Convert plain object to ReactiveProperty.</para>
        /// <para>Value is OneWayToSource(ReactiveProperty -> Object) synchronized.</para>
        /// <para>PropertyChanged raise on UIDispatcherScheduler</para>
        /// </summary>
        public static ReactiveProperty<TResult> FromObject<TTarget, TProperty, TResult>(
            TTarget target,
            Expression<Func<TTarget, TProperty>> propertySelector,
            Func<TProperty, TResult> convert,
            Func<TResult, TProperty> convertBack,
            ReactivePropertyMode mode = ReactivePropertyMode.DistinctUntilChanged|ReactivePropertyMode.RaiseLatestValueOnSubscribe)
        {
            return FromObject(target, propertySelector, convert, convertBack, UIDispatcherScheduler.Default, mode);
        }

        /// <summary>
        /// <para>Convert plain object to ReactiveProperty.</para>
        /// <para>Value is OneWayToSource(ReactiveProperty -> Object) synchronized.</para>
        /// <para>PropertyChanged raise on selected scheduler</para>
        /// </summary>
        public static ReactiveProperty<TResult> FromObject<TTarget, TProperty, TResult>(
            TTarget target,
            Expression<Func<TTarget, TProperty>> propertySelector,
            Func<TProperty, TResult> convert,
            Func<TResult, TProperty> convertBack,
            IScheduler raiseEventScheduler,
            ReactivePropertyMode mode = ReactivePropertyMode.DistinctUntilChanged|ReactivePropertyMode.RaiseLatestValueOnSubscribe)
        {
            string propertyName; // no use
            var getter = AccessorCache<TTarget>.LookupGet(propertySelector, out propertyName);
            var setter = AccessorCache<TTarget>.LookupSet(propertySelector, out propertyName);

            var result = new ReactiveProperty<TResult>(raiseEventScheduler, initialValue: convert(getter(target)), mode: mode);
            result.Select(convertBack).Subscribe(x => setter(target, x));

            return result;
        }


        /// <summary>
        /// <para>Convert to two-way bindable IObservable&lt;T&gt;</para>
        /// <para>PropertyChanged raise on UIDispatcherScheduler</para>
        /// </summary>
        public static ReactiveProperty<T> ToReactiveProperty<T>(this IObservable<T> source,
            T initialValue = default(T),
            ReactivePropertyMode mode = ReactivePropertyMode.DistinctUntilChanged|ReactivePropertyMode.RaiseLatestValueOnSubscribe)
        {
            return new ReactiveProperty<T>(source, initialValue, mode);
        }

        /// <summary>
        /// <para>Convert to two-way bindable IObservable&lt;T&gt;</para>
        /// <para>PropertyChanged raise on selected scheduler</para>
        /// </summary>
        public static ReactiveProperty<T> ToReactiveProperty<T>(this IObservable<T> source,
            IScheduler raiseEventScheduler,
            T initialValue = default(T),
            ReactivePropertyMode mode = ReactivePropertyMode.DistinctUntilChanged|ReactivePropertyMode.RaiseLatestValueOnSubscribe)
        {
            return new ReactiveProperty<T>(source, raiseEventScheduler, initialValue, mode);
        }
    }
}