using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TK.Http
{
	public class UnityWWWRequester : BaseHttpRequester
	{
		public class WWWRequetNode : HttpRequestNode
		{
			private WWW www = null;
			private int responseCode = 0;
			private bool isHttpError = false;
			private bool isNetworkError = false;

			public override int ResponseCode
			{
				get
				{
					return responseCode;
				}
			}

			public override string ResponseText { get { return www.text; } }

			public override Texture2D ResponseTexture { get { return www.texture; } }

			public override bool IsDone { get { return www.isDone; } }

			public override bool IsError { get { return isHttpError || isNetworkError; } }

			public override string ResponseError { get { return www.error; } }

			public override bool IsSuccess
			{
				get
				{
					return true;
				}
			}

			public override Dictionary<string, string> ResponseHeaders
			{
				get
				{
					return www.responseHeaders;
				}
			}

            public override bool IsNetworkError
            {
                get
                {
                    return isNetworkError;
                }
            }

            public override bool IsHttpError
            {
                get
                {
                    return isHttpError;
                }
            }

			public override HttpCodeStatus ResponseStatusCode
			{
				get
				{
					return (HttpCodeStatus)ResponseCode;
				}
			}

			public WWWRequetNode (IHttpRequestData req)
			{
				Hashtable dh = req.DataAndHeader;
				Dictionary<string, string> headers = (Dictionary<string, string>)dh["header"];

				foreach(KeyValuePair<string, string> kv in req.Headers)
				{
					if (headers.ContainsKey (kv.Key))
					{
						headers[kv.Key] = kv.Value;
					}
					else
					{
						headers.Add (kv.Key, kv.Value);
					}
				}

				www = new WWW (req.URL, (byte[])dh["data"], headers);
			}

			public override void Destroy ()
			{
				if ( www == null )
				{
					return;
				}

				www.Dispose ();
				www = null;
			}

			public override IEnumerator Send ()
			{
				yield return www;

				string status = null;

				if ( www.responseHeaders.TryGetValue ( "STATUS", out status ) )
				{
					// If OK, status is "HTTP/1.1 200 OK"
					string[] st = status.Split (' ');
					responseCode = int.Parse ( st[1] );
				}

				if ( !string.IsNullOrEmpty ( www.error ) )
				{
					isNetworkError = (www.error == "Cannot resolve destination host");
				}

				isHttpError = ResponseStatusCode != HttpCodeStatus.OK;
			}

			public override void Setup (IHttpRequestData requestData)
			{
			}
		}

		protected override HttpRequestNode CreateRequestNode (IHttpRequestData req)
		{
			return new WWWRequetNode (req);
		}

		/// <summary>
		/// Create requester
		/// </summary>
		/// <returns>Return object of UnityWWWRequester</returns>
		public static BaseHttpRequester Create ( bool dontDestroyOnLoad = false )
		{
			return Create<UnityWWWRequester> ( dontDestroyOnLoad );
		}
	}

}