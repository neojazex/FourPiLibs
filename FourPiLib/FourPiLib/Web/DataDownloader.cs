using System;
using System.IO;
using System.Text;
using System.Runtime.InteropServices;
using MonoTouch.Foundation;
using MonoTouch.CoreFoundation;

namespace FourPiLib.Web
{
	public class DataDownloader
	{
		// Events, only used with Async calls
		public event EventHandler<AsyncDownloadComplete_EventArgs> AsyncDownloadComplete = null;
		public event EventHandler<AsyncDownloadProgressChanged_EventArgs> AsyncDownloadProgressChanged = null;
		public event EventHandler<AsyncDownloadFailed_EventArgs> AsyncDownloadFailed = null;


		private NSUrlConnection _connection = null;

		private string _url = string.Empty;
		public string URL
		{
			get { return _url; }
		}

		// Can probably be replaced with just a regular string, due to the lack of having to append this in an old implementation
		private StringBuilder _stringBuilder = new StringBuilder();
		public String Text
		{
			get { return _stringBuilder.ToString(); }
		}

		// Exception to write too if need be.		
		private Exception _exception = null;
		public Exception Exception
		{
			get { return _exception; }
		}

		// POST data		
		private string _post = String.Empty;
		public string POST
		{
			get { return _post; }
			set { _post = value; }
		}

		// Connection timeout.
		private double _timeout = 120;
		public double Timeout
		{
			get { return _timeout; }
			set { _timeout = value; }
		}

		// Constructor takes a URL
		public DataDownloader(string url)
		{
			_url =  Uri.EscapeUriString(url);
		}

		// Call this when wanting to stop Async downloads
		public void CancelAsync()
		{
			if (_connection != null)
			{
				_connection.Cancel();
			}
		}

		// Begin an async data download
		public void AsyncDownload()
		{
			Download(true, String.Empty);
		}

		// Begin an async data download to a file
		public void AsyndDownloadToFile(string path)
		{
			Download(true, path);
		}

		// Begin a sync data download
		public void SyncDownload()
		{
			Download(false, String.Empty);
		}

		// Begin a sync data download to file
		public void SyndDownloadToFile(string path)
		{
			Download(false, path);
		}

		// Preform the download
		private void Download(bool isAsync, string path)
		{
			_stringBuilder.Clear();
			
			NSMutableUrlRequest request = null;
			NSError error = null;
			
			try
			{
#if DEBUG				
				DateTime start = DateTime.Now;
#endif
				request = new NSMutableUrlRequest(new NSUrl(_url), NSUrlRequestCachePolicy.ReloadIgnoringLocalAndRemoteCacheData, _timeout);
				
				if (_post != String.Empty)
				{
					request.HttpMethod = "POST";
					request["Content-Length"] = _post.Length.ToString();;
					request["Content-Type"] = "application/x-www-form-urlencoded";
					request.Body = _post;
				}
				
				if (isAsync)
				{
					DataDownloader_NSUrlConnectionDelegate connectionDelegate = new DataDownloader_NSUrlConnectionDelegate();
					
					connectionDelegate.AsyncDownloadComplete += delegate(object sender, AsyncDownloadComplete_EventArgs e) {
						if (path != String.Empty)
						{
							((DataDownloader_NSUrlConnectionDelegate)sender).WriteResultToFile(path);
						}
						
						if (AsyncDownloadComplete != null)
						{
							AsyncDownloadComplete(this, e);
						}
					};					
					
					
					if (AsyncDownloadProgressChanged != null)
					{
						connectionDelegate.AsyncDownloadProgressChanged += delegate(object sender, AsyncDownloadProgressChanged_EventArgs e) {
							AsyncDownloadProgressChanged(this, e);
						};
					}
					

					connectionDelegate.AsyncDownloadFailed += delegate(object sender, AsyncDownloadFailed_EventArgs e) {
						_exception = new Exception(e.Error.LocalizedDescription);

						if (AsyncDownloadFailed != null)
						{
							AsyncDownloadFailed(this, e);
						}
					};
					
					// Not using matching functions, but I'll live.
					//NSUrlConnection.SendAsynchronousRequest(request, new NSOperationQueue(), connectionDelegate);
					_connection = new NSUrlConnection(request, connectionDelegate, true);

				}
				else
				{					
					NSUrlResponse response = null;
					
					NSData output = NSUrlConnection.SendSynchronousRequest(request, out response, out error);
					
					if (output == null)
					{
						if (error != null)
						{
							_exception = new Exception(error.LocalizedDescription);
						}
						else if (response == null)
						{
							_exception = new Exception("Could not get a response from the server.");
						}
						else
						{
							_exception = new Exception("Could not get data from server.");
						}
					}
					
					if (path != String.Empty)
					{
						File.WriteAllText(path, output.ToString());
					}
					
					_stringBuilder = new StringBuilder(output.ToString());
					Console.WriteLine(_stringBuilder.ToString());
				}
#if DEBUG
				Console.WriteLine("NSData.FromUrl: " + (DateTime.Now - start).TotalMilliseconds);
				start = DateTime.Now;
#endif
			}
			catch (Exception err)
			{
				_exception = err;
				Console.WriteLine("StreamLoading Error: " + err.Message);
			}
			finally
			{
				
			}
		}
		
		// Class used for Async downloads
		public class DataDownloader_NSUrlConnectionDelegate : NSUrlConnectionDelegate
		{
			long bytesReceived = 0;
			long expectedBytes = 0;
			float progress = 0;
			byte [] result;
			
			public event EventHandler<AsyncDownloadComplete_EventArgs> AsyncDownloadComplete = null;
			public event EventHandler<AsyncDownloadProgressChanged_EventArgs> AsyncDownloadProgressChanged = null;			
			public event EventHandler<AsyncDownloadFailed_EventArgs> AsyncDownloadFailed = null;
			
			public DataDownloader_NSUrlConnectionDelegate()
			{
				result = new byte [0];
			}
			
			public override void ReceivedResponse(NSUrlConnection connection, NSUrlResponse response)
			{
				expectedBytes = response.ExpectedContentLength;
			}
			
			public void WriteResultToFile(string path)
			{
				// Try/catch this, just in case.
				File.WriteAllBytes(path, result);
			}
			
			public override void ReceivedData(NSUrlConnection connection, NSData data)
			{
				byte [] nb = new byte [result.Length + data.Length];
				result.CopyTo(nb, 0);
				Marshal.Copy(data.Bytes, nb, result.Length, (int) data.Length);
				result = nb;				
				
				uint receivedLen = data.Length;
				bytesReceived = (bytesReceived + receivedLen);

				progress = (bytesReceived/(float)expectedBytes);
				
				if (AsyncDownloadProgressChanged != null)
				{
					AsyncDownloadProgressChanged(this, new AsyncDownloadProgressChanged_EventArgs(progress));
				}
			}
			
			public override void FinishedLoading(NSUrlConnection connection)
			{
				if (AsyncDownloadComplete != null)
				{
					AsyncDownloadComplete(this, new AsyncDownloadComplete_EventArgs());
				}
			}
			
			public override void FailedWithError(NSUrlConnection connection, NSError error)
			{
				if (AsyncDownloadFailed != null)
				{
					AsyncDownloadFailed(this, new AsyncDownloadFailed_EventArgs(error));
				}
			}
		}
		
		// Various EventArgs
		public class AsyncDownloadComplete_EventArgs : EventArgs
		{
			public AsyncDownloadComplete_EventArgs()
			{
			}
		}
		
		public class AsyncDownloadFailed_EventArgs : EventArgs
		{
			public NSError Error { get; internal set; }
			
			public AsyncDownloadFailed_EventArgs(NSError error)
			{
				this.Error = error;
			}
		}
		
		public class AsyncDownloadProgressChanged_EventArgs : EventArgs
		{
			public float Progress { get; internal set; }
			
			public AsyncDownloadProgressChanged_EventArgs(float progress)
			{
				this.Progress = progress;
			}
		}
	}
}
	

