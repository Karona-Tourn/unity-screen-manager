using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace TK.Http
{

	public class UnityWebRequester : BaseHttpRequester
	{
		public class RequestNode : HttpRequestNode
		{
			private UnityWebRequest request = null;

			public override string ResponseError { get { return request.error; } }

			public override int ResponseCode { get { return (int)request.responseCode; } }

			public override string ResponseText { get { return (request.downloadHandler is DownloadHandlerBuffer ? ((DownloadHandlerBuffer)request.downloadHandler).text : null); } }

            public override Texture2D ResponseTexture { get { return (request.downloadHandler is DownloadHandlerTexture ? DownloadHandlerTexture.GetContent(request) : null); } }

			public override bool IsDone { get { return request.isDone; } }

			public override bool IsError
			{
				get
				{
#if UNITY_2017_1_OR_NEWER
					return request.isNetworkError;
#else
					return request.isError;
#endif
				}
			}

			public override bool IsSuccess
			{
				get
				{
					return request.IsSuccess ();
				}
			}

			public override Dictionary<string, string> ResponseHeaders
			{
				get
				{
					return request.GetResponseHeaders ();
				}
			}

			public RequestNode ()
			{
				request = new UnityWebRequest ();
				request.useHttpContinue = false;
				request.chunkedTransfer = false;
				request.redirectLimit = 0;  // disable redirects
				request.timeout = 60;       // don't make this small, web requests do take some time
				request.disposeDownloadHandlerOnDispose = true;
				request.disposeUploadHandlerOnDispose = true;
			}

			public override void Destroy ()
			{
				if (request.isDone)
				{
					request.Abort ();
				}
				request.Dispose ();
				request = null;
			}

			public override IEnumerator Send ()
			{
#if UNITY_2017_2_OR_NEWER
				yield return request.SendWebRequest ();
#else
				yield return request.Send ();
#endif
			}

			public override void Setup (IHttpRequestData requestData)
			{
				request.url = requestData.URL;

				switch (requestData.Method)
				{
					case HttpMethod.Get:
						request.method = UnityWebRequest.kHttpVerbGET;
						break;
					case HttpMethod.Post:
						request.method = UnityWebRequest.kHttpVerbPOST;
						break;
					case HttpMethod.Put:
						request.method = UnityWebRequest.kHttpVerbPUT;
						break;
					case HttpMethod.Delete:
						request.method = UnityWebRequest.kHttpVerbDELETE;
						break;
				}

				if (requestData.Method != HttpMethod.Get && requestData.Method != HttpMethod.Head)
				{
					var dh = requestData.DataAndHeader;
					request.uploadHandler = new UploadHandlerRaw ((byte[])dh["data"]);
					System.Collections.Generic.Dictionary<string, string> formHeaders = (System.Collections.Generic.Dictionary<string, string>)dh["header"];
					if (formHeaders.ContainsKey ("Content-Type"))
					{
						request.uploadHandler.contentType = formHeaders["Content-Type"];
					}
				}
				else
				{
					request.uploadHandler = null;
				}

				if (requestData.Method != HttpMethod.Head)
				{
					switch (requestData.ResponseType)
					{
						case ResponseType.Text:
							request.downloadHandler = new DownloadHandlerBuffer ();
							break;
						case ResponseType.ReadableTexture:
						case ResponseType.Texture:
							request.downloadHandler = new DownloadHandlerTexture (requestData.ResponseType == ResponseType.ReadableTexture);
							break;
					}
				}

				var headers = new System.Collections.Generic.Dictionary<string, string> (requestData.Headers);
				if (headers != null)
				{
					headers.Remove ("Content-Type");
					foreach (var kv in headers)
					{
						request.SetRequestHeader (kv.Key, kv.Value);
					}
				}
			}
		}

		protected override HttpRequestNode CreateRequestNode (IHttpRequestData requestData)
		{
			return new RequestNode ();
		}

		/// <summary>
		/// Create requester
		/// </summary>
		/// <returns>Return object of UnityWebRequester</returns>
		public static BaseHttpRequester Create ()
		{
			return new GameObject ("UWR" + Guid.NewGuid ().ToString ()).AddComponent<UnityWebRequester> ();
		}
	}

}
