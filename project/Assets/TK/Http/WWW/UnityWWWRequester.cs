using System;
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

			public override int ResponseCode
			{
				get
				{
					string status = null;
					if (www.responseHeaders.TryGetValue ("STATUS", out status))
					{
						// If OK, status is "HTTP/1.1 200 OK"
						string[] st = status.Split (' ');
						return int.Parse (st[1]);
					}
					return 0;
				}
			}

			public override string ResponseText { get { return www.text; } }

			public override Texture2D ResponseTexture { get { return www.texture; } }

			public override bool IsDone { get { return www.isDone; } }

			public override bool IsError { get { return string.IsNullOrEmpty (www.error) == false; } }

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

			public WWWRequetNode (IHttpRequestData req)
			{
				var dh = req.DataAndHeader;
				var headers = (System.Collections.Generic.Dictionary<string, string>)dh["header"];
				foreach(var kv in req.Headers)
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
				www.Dispose ();
				www = null;
				GC.Collect ();
			}

			public override IEnumerator Send ()
			{
				yield return www;
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
		public static BaseHttpRequester Create ()
		{
			return new GameObject ("UWR" + Guid.NewGuid ().ToString ()).AddComponent<UnityWWWRequester> ();
		}
	}

}
