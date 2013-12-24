using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Diagnostics.Contracts;
using Codeplex.Reactive.Notifiers;
#if WINDOWS_PHONE
using Microsoft.Phone.Reactive;
#else
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Threading.Tasks;
#endif

namespace Codeplex.Reactive.Asynchronous
{
    public static class WebRequestExtensions
    {
        static IObservable<TResult> SafeFromAsyncPattern<TResult>(Func<AsyncCallback, object, IAsyncResult> begin, Func<IAsyncResult, TResult> end, Action cancelAsync)
        {
            var result = Observable.Create<TResult>(observer =>
            {
                var isCompleted = false;
                Observable.FromAsync(() => Task.Factory.FromAsync<TResult>(begin,
                    ar =>
                    {
                        try
                        {
                            isCompleted = true;
                            return end(ar);
                        }
                        catch (WebException ex)
                        {
                            if (ex.Status == WebExceptionStatus.RequestCanceled) return default(TResult);
                            throw;
                        }
                    }, null))
                    .Subscribe(observer);
                return () =>
                {
                    if (!isCompleted) cancelAsync();
                };
            });

            return result;
        }

        /// <summary>
        /// <para>Get WebResponse async.</para>
        /// <para>Run deferred, Length of return value is always 1.</para>
        /// </summary>
        public static IObservable<WebResponse> GetResponseObservableAsync(this WebRequest request)
        {
            return SafeFromAsyncPattern(request.BeginGetResponse, request.EndGetResponse, request.Abort);
        }

        /// <summary>
        /// <para>Get HttpWebResponse async.</para>
        /// <para>Run deferred, Length of return value is always 1.</para>
        /// </summary>
        public static IObservable<HttpWebResponse> GetResponseObservableAsync(this HttpWebRequest request)
        {
            return SafeFromAsyncPattern(request.BeginGetResponse, ar => (HttpWebResponse)request.EndGetResponse(ar), request.Abort);
        }

        /// <summary>
        /// <para>Get RequestStream async.</para>
        /// <para>Run deferred, Length of return value is always 1.</para>
        /// </summary>
        public static IObservable<Stream> GetRequestStreamObservableAsync(this WebRequest request)
        {
            return SafeFromAsyncPattern(request.BeginGetRequestStream, request.EndGetRequestStream, request.Abort);
        }

        /// <summary>
        /// <para>Download data async.</para>
        /// <para>Run deferred, Length of return value is always 1.</para>
        /// </summary>
        public static IObservable<byte[]> DownloadDataAsync(this WebRequest request)
        {
            var result = request.GetResponseObservableAsync().SelectMany(r => r.DownloadDataAsync());

            return result;
        }

        /// <summary>
        /// <para>Download data async.</para>
        /// <para>Run deferred, Length of return value is always 1.</para>
        /// </summary>
        /// <param name="progressReporter">Reporter of progress(such as ScheduledNotifier).</param>
        public static IObservable<byte[]> DownloadDataAsync(this WebRequest request, IProgress<ProgressStatus> progressReporter)
        {
            var result = request.GetResponseObservableAsync().SelectMany(r => r.DownloadDataAsync(progressReporter));

            return result;
        }

        /// <summary>
        /// <para>Download string async.</para>
        /// <para>Run deferred, Length of return value is always 1.</para>
        /// </summary>
        public static IObservable<string> DownloadStringAsync(this WebRequest request)
        {
            return DownloadStringAsync(request, Encoding.UTF8);
        }

        /// <summary>
        /// <para>Download string async.</para>
        /// <para>Run deferred, Length of return value is always 1.</para>
        /// </summary>
        public static IObservable<string> DownloadStringAsync(this WebRequest request, Encoding encoding)
        {
            var result = request.GetResponseObservableAsync().SelectMany(r => r.DownloadStringAsync(encoding));

            return result;
        }

        /// <summary>
        /// <para>Download string async.</para>
        /// <para>Run deferred, Length of return value is always 1.</para>
        /// </summary>
        /// <param name="progressReporter">Reporter of progress(such as ScheduledNotifier).</param>
        public static IObservable<string> DownloadStringAsync(this WebRequest request, IProgress<ProgressStatus> progressReporter)
        {
            return DownloadStringAsync(request, Encoding.UTF8, progressReporter);
        }

        /// <summary>
        /// <para>Download string async.</para>
        /// <para>Run deferred, Length of return value is always 1.</para>
        /// </summary>
        /// <param name="progressReporter">Reporter of progress(such as ScheduledNotifier).</param>
        public static IObservable<string> DownloadStringAsync(this WebRequest request, Encoding encoding, IProgress<ProgressStatus> progressReporter)
        {
            var result = request.GetResponseObservableAsync().SelectMany(r => r.DownloadStringAsync(encoding, progressReporter));

            return result;
        }

        /// <summary>
        /// <para>Download string lines async.</para>
        /// <para>Run deferred, Length of return value is lines row count.</para>
        /// </summary>
        public static IObservable<string> DownloadStringLineAsync(this WebRequest request)
        {
            return DownloadStringLineAsync(request, Encoding.UTF8);
        }

        /// <summary>
        /// <para>Download string lines async.</para>
        /// <para>Run deferred, Length of return value is lines row count.</para>
        /// </summary>
        public static IObservable<string> DownloadStringLineAsync(this WebRequest request, Encoding encoding)
        {
            var result = request.GetResponseObservableAsync().SelectMany(r => r.DownloadStringLineAsync(encoding));

            return result;
        }

        /// <summary>
        /// <para>Upload string async.</para>
        /// <para>Run deferred, Length of return value is always 1.</para>
        /// </summary>
        public static IObservable<WebResponse> UploadStringAsync(this WebRequest request, string data)
        {
            return request.UploadStringAsync(data, Encoding.UTF8);
        }

        /// <summary>
        /// <para>Upload string async.</para>
        /// <para>Run deferred, Length of return value is always 1.</para>
        /// </summary>
        public static IObservable<WebResponse> UploadStringAsync(this WebRequest request, string data, Encoding encoding)
        {
            var bytes = encoding.GetBytes(data);
            return request.UploadDataAsync(bytes);
        }

        /// <summary>
        /// <para>Upload string async.</para>
        /// <para>Run deferred, Length of return value is always 1.</para>
        /// </summary>
        /// <param name="progressReporter">Reporter of progress(such as ScheduledNotifier).</param>
        public static IObservable<WebResponse> UploadStringAsync(this WebRequest request, string data, IProgress<ProgressStatus> progressReporter)
        {
            return UploadStringAsync(request, data, Encoding.UTF8, progressReporter);
        }

        /// <summary>
        /// <para>Upload string async.</para>
        /// <para>Run deferred, Length of return value is always 1.</para>
        /// </summary>
        /// <param name="progressReporter">Reporter of progress(such as ScheduledNotifier).</param>
        public static IObservable<WebResponse> UploadStringAsync(this WebRequest request, string data, Encoding encoding, IProgress<ProgressStatus> progressReporter)
        {
            var bytes = encoding.GetBytes(data);
            return request.UploadDataAsync(bytes, progressReporter);
        }

        /// <summary>
        /// <para>Upload values async.</para>
        /// <para>Run deferred, Length of return value is always 1.</para>
        /// </summary>
        public static IObservable<WebResponse> UploadValuesAsync(this WebRequest request, IDictionary<string, string> parameters)
        {
            return UploadValuesAsync(request, parameters, Encoding.UTF8);
        }

        /// <summary>
        /// <para>Upload values async.</para>
        /// <para>Run deferred, Length of return value is always 1.</para>
        /// </summary>
        public static IObservable<WebResponse> UploadValuesAsync(this WebRequest request, IDictionary<string, string> parameters, Encoding encoding)
        {
            var parameter = string.Join("&", parameters
                .Select(kvp => Uri.EscapeDataString(kvp.Key) + "=" + Uri.EscapeDataString(kvp.Value)).ToArray());
            var bytes = encoding.GetBytes(parameter);

            return request.UploadDataAsync(bytes);
        }

        /// <summary>
        /// <para>Upload values async.</para>
        /// <para>Run deferred, Length of return value is always 1.</para>
        /// </summary>
        /// <param name="progressReporter">Reporter of progress(such as ScheduledNotifier).</param>
        public static IObservable<WebResponse> UploadValuesAsync(this WebRequest request, IDictionary<string, string> parameters, IProgress<ProgressStatus> progressReporter)
        {
            return UploadValuesAsync(request, parameters, Encoding.UTF8, progressReporter);
        }

        /// <summary>
        /// <para>Upload values async.</para>
        /// <para>Run deferred, Length of return value is always 1.</para>
        /// </summary>
        /// <param name="progressReporter">Reporter of progress(such as ScheduledNotifier).</param>
        public static IObservable<WebResponse> UploadValuesAsync(this WebRequest request, IDictionary<string, string> parameters, Encoding encoding, IProgress<ProgressStatus> progressReporter)
        {
            var parameter = string.Join("&", parameters
                .Select(kvp => Uri.EscapeDataString(kvp.Key) + "=" + Uri.EscapeDataString(kvp.Value)).ToArray());
            var bytes = encoding.GetBytes(parameter);

            return request.UploadDataAsync(bytes, progressReporter);
        }

        /// <summary>
        /// <para>Upload data async.</para>
        /// <para>Run deferred, Length of return value is always 1.</para>
        /// </summary>
        public static IObservable<WebResponse> UploadDataAsync(this WebRequest request, byte[] data)
        {
            var result = request.GetRequestStreamObservableAsync()
                .SelectMany(stream => stream.WriteAsObservable(data, 0, data.Length)
                    .Finally(() => { stream.Flush(); stream.Close(); }))
                .TakeLast(1) // call before sequence's finally
                .SelectMany(_ => request.GetResponseObservableAsync());

            return result;
        }

        /// <summary>
        /// <para>Upload data async.</para>
        /// <para>Run deferred, Length of return value is always 1.</para>
        /// </summary>
        /// <param name="progressReporter">Reporter of progress(such as ScheduledNotifier).</param>
        /// <param name="chunkSize">The size of one reading.</param>
        public static IObservable<WebResponse> UploadDataAsync(this WebRequest request, byte[] data, IProgress<ProgressStatus> progressReporter, int chunkSize = 65536)
        {
            var result = request.GetRequestStreamObservableAsync()
                .SelectMany(stream => stream.WriteAsync(data, progressReporter, chunkSize))
                .SelectMany(_ => request.GetResponseObservableAsync());

            return result;
        }
    }
}