using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace TK.Http
{
	public abstract class BaseHttpRequester : MonoBehaviour
	{
		public enum HttpMethod
		{
			Get,
			Post,
			Put,
			Delete,
			Head
		}

		public enum ResponseType
		{
			None,
			Texture,
			ReadableTexture,
			Text
		}

        public enum HttpCodeStatus
        {
            // 2xx Success
            OK = 200,
            Created = 201,
            Accepted = 202,
            NonAuthoritativeInformation = 203,
            NoContent = 204,
            ResetContent = 205,
            PartialContent = 206,
            MultiStatus = 207,
            AlreadyReported = 208,
            IMUsed = 226,

            // 3×× Redirection
            MultipleChoices = 300,
            MovedPermanently = 301,
            Found = 302,
            SeeOther = 303,
            NotModified = 304,
            UseProxy = 305,
            TemporaryRedirect = 307,
            PermanentRedirect = 308,

            // 4×× Client Error
            BadRequest = 400,
            Unauthorized = 401,
            PaymentRequired = 402,
            Forbidden = 403,
            NotFound = 404,
            MethodNotAllowed = 405,
            NotAcceptable = 406,
            ProxyAuthenticationRequired = 407,
            RequestTimeout = 408,
            Conflict = 409,
            Gone = 410,
            LengthRequired = 411,
            PreconditionFailed = 412,
            PayloadTooLarge = 413,
            RequestURITooLong = 414,
            UnsupportedMediaType = 415,
            RequestedRangeNotSatisfiable = 416,
            ExpectationFailed = 417,
            I_am_a_teapot = 418,
            MisdirectedRequest = 421,
            UnprocessableEntity = 422, // Validation failed
            Locked = 423,
            FailedDependency = 424,
            UpgradeRequired = 426,
            PreconditionRequired = 428,
            TooManyRequests = 429,
            RequestHeaderFieldsTooLarge = 431,
            ConnectionClosedWithoutResponse = 444,
            UnavailableForLegalReasons = 451,
            ClientClosedRequest = 499,

            // 5×× Server Error
            InternalServerError = 500,
            NotImplemented = 501,
            BadGateway = 502,
            ServiceUnavailable = 503,
            GatewayTimeout = 504,
            HTTPVersionNotSupported = 505,
            VariantAlsoNegotiates = 506,
            InsufficientStorage = 507,
            LoopDetected = 508,
            NotExtended = 510,
            NetworkAuthenticationRequired = 511,
            NetworkConnectTimeoutError = 599
        }

		public abstract class HttpRequestNode
		{
			private bool disposed = false;

			public abstract string ResponseError { get; }
			public abstract HttpCodeStatus ResponseStatusCode { get; }
			public abstract int ResponseCode { get; }
			public abstract string ResponseText { get; }
			public abstract Texture2D ResponseTexture { get; }
			public abstract bool IsSuccess { get; }
			public abstract bool IsDone { get; }
			public abstract bool IsError { get; }
            public abstract bool IsNetworkError { get; }
            public abstract bool IsHttpError { get; }
			public abstract Dictionary<string, string> ResponseHeaders { get; }
			public abstract void Setup (IHttpRequestData requestData);
			public abstract IEnumerator Send ();
			public abstract void Destroy ();
		}

		public class HttpRequest
		{
			public IHttpRequestData requestData = null;
			public UnityAction<HttpRequestNode> onSuccess = null;
			public UnityAction<HttpRequestNode> onFailure = null;
			public UnityAction<HttpRequestNode> onError = null;
			public UnityAction<HttpRequestNode> onFinish = null;
		}

        public delegate HttpRequest PriorityRequestForStatusCode ( HttpRequestNode requestNode );

        static public PriorityRequestForStatusCode priorityRequestForStatusCode = null;

        private Stack<HttpRequest> priorityRequests = new Stack<HttpRequest>();

		private UnityAction<HttpRequestNode> onSuccess = null;
		private UnityAction<HttpRequestNode> onFailure = null;
		private UnityAction<HttpRequestNode> onError = null;
		private UnityAction<HttpRequestNode> onFinishEachRequest = null;
		private UnityAction onFinish = null;

        private Queue<HttpRequest> requestQueue = new Queue<HttpRequest> ();
		private HttpRequestNode requestNode = null;
		private bool isDestroying = false;
		private bool isPending = false;
		private bool destroyWhenDone = false;
		private Coroutine pendingCoroutine = null;

		protected abstract HttpRequestNode CreateRequestNode (IHttpRequestData requestData);

		public bool IsBusy
		{
			get { return isPending || isDestroying; }
		}

		/// <summary>
		/// Set event on success.
		/// </summary>
		/// <returns>Instance of this class.</returns>
		/// <param name="onSuccess">Delegate on success.</param>
		public BaseHttpRequester SetOnSuccess (UnityAction<HttpRequestNode> onSuccess)
		{
			this.onSuccess = onSuccess;
			return this;
		}

		/// <summary>
		/// Set event on failure.
		/// </summary>
		/// <returns>Instance of this class.</returns>
		/// <param name="onFailure">Delegate on failure.</param>
		public BaseHttpRequester SetOnFailure (UnityAction<HttpRequestNode> onFailure)
		{
			this.onFailure = onFailure;
			return this;
		}

		/// <summary>
		/// Set event on error.
		/// </summary>
		/// <returns>Instance of this class.</returns>
		/// <param name="onError">Delegate on error.</param>
		public BaseHttpRequester SetOnError (UnityAction<HttpRequestNode> onError)
		{
			this.onError = onError;
			return this;
		}

		public BaseHttpRequester SetOnFinish (UnityAction onFinish)
		{
			this.onFinish = onFinish;
			return this;
		}

		public BaseHttpRequester SetOnFinishEachRequest (UnityAction<HttpRequestNode> onFinishEachRequest)
		{
			this.onFinishEachRequest = onFinishEachRequest;
			return this;
		}

		public BaseHttpRequester SetDestroyWhenDone ( bool destroy )
		{
			destroyWhenDone = destroy;
			return this;
		}

		public BaseHttpRequester AddRequest ( params HttpRequest[] requests )
		{
			// Exit function if requester is being destroyed or being busy to send
			if ( isPending || isDestroying )
			{
				Debug.LogWarning ( "The http requester is pending so you cannot add another request." );
				return this;
			}

			int length = requests.Length;

			for ( int i = 0; i < length; i++ )
			{
				requestQueue.Enqueue ( requests[i] );
			}

			return this;
		}

		/// <summary>
		/// Adds the request.
		/// </summary>
		/// <returns>The request.</returns>
		/// <param name="requests">Requests.</param>
		public BaseHttpRequester AddRequest (params IHttpRequestData[] requests)
		{
			// Exit function if requester is being destroyed or being busy to send
			if ( isPending || isDestroying )
			{
				Debug.LogWarning ( "The http requester is pending so you cannot add another request." );
				return this;
			}

			int length = requests.Length;

			for (int i = 0; i < length; i++)
			{
				requestQueue.Enqueue ( new HttpRequest () { requestData = requests[i] } );
			}

			return this;
		}

		/// <summary>
		/// Adds the request.
		/// </summary>
		/// <returns>The request.</returns>
		/// <param name="requests">Requests.</param>
		public BaseHttpRequester AddRequest (List<IHttpRequestData> requests)
		{
			// Exit function if requester is being destroyed or being busy to send
			if ( isPending || isDestroying )
			{
				Debug.LogWarning ( "The http requester is pending so you cannot add another request." );
				return this;
			}

			int length = requests.Count;

			for ( int i = 0; i < length; i++ )
			{
				requestQueue.Enqueue ( new HttpRequest () { requestData = requests[i] } );
			}

			return this;
		}

        private HttpRequestNode CreateReq(IHttpRequestData reqData)
        {
            HttpRequestNode req = CreateRequestNode(reqData);
            req.Setup(reqData);
            return req;
        }

        private IEnumerator Coro_ExecutePriorityRequest( HttpRequest newReq )
        {
			HttpRequest httpReq = null;
			HttpRequest pHttpReq = null;

			priorityRequests.Push(newReq);

            while (priorityRequests.Count > 0)
            {
				httpReq = priorityRequests.Pop();
                requestNode = CreateReq(httpReq.requestData);

				yield return requestNode.Send ();

				if ( priorityRequestForStatusCode != null )
				{
					pHttpReq = priorityRequestForStatusCode ( requestNode );

					if ( pHttpReq != null && pHttpReq.requestData != null )
					{
						if ( requestNode != null )
						{
							requestNode.Destroy ();
							requestNode = null;
						}

						priorityRequests.Push ( pHttpReq );
						priorityRequests.Push ( httpReq );

						httpReq = null;
						pHttpReq = null;
						continue;
					}

					pHttpReq = null;
				}

				if ( requestNode.IsNetworkError )
				{
					if ( httpReq.onError != null )
					{
						httpReq.onError ( requestNode );
					}
				}
				else
				{
					if ( requestNode.IsSuccess )
					{
						if ( httpReq.onSuccess != null )
						{
							httpReq.onSuccess ( requestNode );
						}
					}
					else
					{
						if ( httpReq.onFailure != null )
						{
							httpReq.onFailure ( requestNode );
						}
					}
				}

				if ( httpReq.onFinish != null )
				{
					httpReq.onFinish ( requestNode );
				}

				if ( requestNode != null )
				{
					requestNode.Destroy ();
					requestNode = null;
				}

				httpReq = null;
				pHttpReq = null;
			}
        }

		private IEnumerator Coro_Send ()
		{
			HttpRequest httpReq = null;
			HttpRequest pHttpReq = null;

			while (requestQueue.Count > 0)
			{
				httpReq = requestQueue.Dequeue();
				requestNode = CreateReq(httpReq.requestData);

				// Send request to server.
				yield return requestNode.Send ();

				// TODO: This handles to execute agent priority requests based on verified status code
				// Example: when request token is expired, so there must be a priority request to refresh token to be executed first
				// and then continue the other requests again that has issue with expired token
				if ( priorityRequestForStatusCode != null )
				{
					pHttpReq = priorityRequestForStatusCode ( requestNode );

					if ( pHttpReq != null && pHttpReq.requestData != null )
					{
						if ( requestNode != null )
						{
							requestNode.Destroy ();
							requestNode = null;
						}

						HttpRequest[] leftHttpReqs = requestQueue.ToArray();
						requestQueue.Clear ();

						// Add the request that has problem based on checked status code above 
						requestQueue.Enqueue ( httpReq );

						for ( int i = 0; i < leftHttpReqs.Length; i++ )
						{
							requestQueue.Enqueue ( leftHttpReqs[i] );
						}

						yield return Coro_ExecutePriorityRequest ( pHttpReq );

						leftHttpReqs = null;
						httpReq = null;
						pHttpReq = null;

						continue;
					}

					pHttpReq = null;
				}

				if ( requestNode.IsNetworkError )
				{
					if ( onError != null )
					{
						onError ( requestNode );
					}

					if ( httpReq.onError != null )
					{
						httpReq.onError ( requestNode );
					}
				}
				else
				{
					if ( requestNode.IsSuccess )
					{
						if ( onSuccess != null )
						{
							onSuccess ( requestNode );
						}

						if ( httpReq.onSuccess != null )
						{
							httpReq.onSuccess ( requestNode );
						}
					}
					else
					{
						if ( onFailure != null )
						{
							onFailure ( requestNode );
						}

						if ( httpReq.onFailure != null )
						{
							httpReq.onFailure ( requestNode );
						}
					}
				}

				if ( onFinishEachRequest != null )
				{
					onFinishEachRequest ( requestNode );
				}

				if ( httpReq.onFinish != null )
				{
					httpReq.onFinish ( requestNode );
				}

				if( requestNode != null )
				{
					requestNode.Destroy ();
					requestNode = null;
				}

				httpReq = null;
				pHttpReq = null;
			}

			if ( onFinish != null )
			{
				onFinish ();
			}

			if ( destroyWhenDone )
			{
				// Tell that the instance of this class is being destroyed.
				isDestroying = true;
				isPending = false;

				yield return new WaitForEndOfFrame ();

				// Destroy in 0.01 sec
				Destroy ( gameObject, 0.01f );
			}
			else
			{
				isPending = false;
			}

			pendingCoroutine = null;
		}

		/// <summary>
		/// Abort request
		/// </summary>
		[Obsolete( "Use AbortRequest instead." )]
		public void Abort ()
		{
			if ( isDestroying )
			{
				return;
			}

			if (isPending)
			{
                SetOnSuccess(null);
                SetOnFailure(null);
                SetOnError(null);
                SetOnFinish(null);
                SetOnFinishEachRequest(null);

				StopCoroutine ( pendingCoroutine );
				pendingCoroutine = null;

				if (requestNode != null)
				{
					requestNode.Destroy ();
					requestNode = null;
				}
			}

			Destroy (gameObject);
		}

		public void AbortRequest ()
		{
			if ( isDestroying )
			{
				return;
			}

			if ( isPending )
			{
				StopCoroutine ( pendingCoroutine );
				pendingCoroutine = null;

				if ( requestNode != null )
				{
					requestNode.Destroy ();
					requestNode = null;
				}

				requestQueue.Clear ();
			}
		}

		public void Destroy ()
		{
			SetOnSuccess ( null );
			SetOnFailure ( null );
			SetOnError ( null );
			SetOnFinish ( null );
			SetOnFinishEachRequest ( null );

			if ( isDestroying )
			{
				return;
			}

			AbortRequest ();

			Destroy ( gameObject );
		}

		public BaseHttpRequester Send ()
		{
			// Exit function if requester is being destroyed or being busy to send
			if ( IsBusy )
			{
				return this;
			}

			isPending = true;
			pendingCoroutine = StartCoroutine ( Coro_Send ());

			return this;
		}

		static public T Create<T> ( bool dontDestroyOnLoad = false ) where T : BaseHttpRequester
		{
			GameObject go = new GameObject ("UWR");

			if ( dontDestroyOnLoad )
			{
				DontDestroyOnLoad ( go );
			}

			return go.AddComponent<T> ();
		}
	}
}