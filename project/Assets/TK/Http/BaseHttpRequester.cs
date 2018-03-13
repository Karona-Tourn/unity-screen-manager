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

        public class PriorityRequest
        {
            public IHttpRequestData reqData = null;
            public UnityAction<HttpRequestNode> onSuccess = null;
            public UnityAction<HttpRequestNode> onFailure = null;
            public UnityAction<HttpRequestNode> onError = null;
        }

		public abstract class HttpRequestNode
		{
			public abstract string ResponseError { get; }
			public abstract int ResponseCode { get; }
			public abstract string ResponseText { get; }
			public abstract Texture2D ResponseTexture { get; }
			public abstract bool IsSuccess { get; }
			public abstract bool IsDone { get; }
			public abstract bool IsError { get; }
			public abstract Dictionary<string, string> ResponseHeaders { get; }
			public abstract void Setup (IHttpRequestData requestData);
			public abstract IEnumerator Send ();
			public abstract void Destroy ();
		}

        public delegate PriorityRequest PriorityRequestForStatusCode(int statusCode);

        static public PriorityRequestForStatusCode priorityRequestForStatusCode = null;

        private Stack<PriorityRequest> priorityRequests = new Stack<PriorityRequest>();

		private UnityAction<HttpRequestNode> onSuccess = null;
		private UnityAction<HttpRequestNode> onFailure = null;
		private UnityAction<HttpRequestNode> onError = null;
		private UnityAction<HttpRequestNode> onFinishEachRequest = null;
		private UnityAction onFinish = null;

        private Queue<IHttpRequestData> requestQueue = new Queue<IHttpRequestData> ();
		private HttpRequestNode request = null;
		private bool isDestroying = false;
		private bool isSending = false;

		protected abstract HttpRequestNode CreateRequestNode (IHttpRequestData requestData);

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

		/// <summary>
		/// Adds the request.
		/// </summary>
		/// <returns>The request.</returns>
		/// <param name="requests">Requests.</param>
		public BaseHttpRequester AddRequest (params IHttpRequestData[] requests)
		{
			// Exit function if requester is being destroyed or being busy to send
			if (isSending || isDestroying)
				return this;

			for (int i = 0; i < requests.Length; i++)
			{
                requestQueue.Enqueue(requests[i]);
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
			return AddRequest (requests.ToArray ());
		}

        private HttpRequestNode CreateReq(IHttpRequestData reqData)
        {
            var req = CreateRequestNode(reqData);
            req.Setup(reqData);
            return req;
        }

        private IEnumerator Coro_ExecutePriorityRequest(PriorityRequest newReq)
        {
            priorityRequests.Push(newReq);

            while (priorityRequests.Count > 0)
            {
                var req = priorityRequests.Pop();

                request = CreateReq(req.reqData);

                yield return request.Send();

                if (priorityRequestForStatusCode != null)
                {
                    var pReq = priorityRequestForStatusCode(request.ResponseCode);

                    if (pReq != null && pReq.reqData != null)
                    {
                        if (request != null)
                        {
                            request.Destroy();
                            request = null;
                        }

                        priorityRequests.Push(pReq);
                        priorityRequests.Push(req);

                        continue;
                    }
                }

                if (request.IsError)
                {
                    if (req.onError != null)
                        req.onError(request);
                }
                else
                {
                    if (request.IsSuccess)
                    {
                        if (req.onSuccess != null)
                            req.onSuccess(request);
                    }
                    else
                    {
                        if (req.onFailure != null)
                            req.onFailure(request);
                    }
                }

                if (request != null)
                {
                    request.Destroy();
                    request = null;
                }
            }
        }

		private IEnumerator Coro_Send ()
		{
			while (requestQueue.Count > 0)
			{
                var reqData = requestQueue.Dequeue();

                request = CreateReq(reqData);

				// Send request to server.
				yield return request.Send ();

                // TODO: This handles to execute agent priority requests based on verified status code
                // Example: when request token is expired, so there must be a priority request to refresh token to be executed first
                // and then continue the other requests again that has issue with expired token
                if (priorityRequestForStatusCode != null)
                {
                    var pReq = priorityRequestForStatusCode(request.ResponseCode);

                    if (pReq != null && pReq.reqData != null)
                    {
                        if (request != null)
                        {
                            request.Destroy();
                            request = null;
                        }

                        var leftReqData = requestQueue.ToArray();
                        requestQueue.Clear();

                        // Add the request that has problem based on checked status code above 
                        requestQueue.Enqueue(reqData);

                        for (int i = 0; i < leftReqData.Length; i++)
                            requestQueue.Enqueue(leftReqData[i]);

                        yield return Coro_ExecutePriorityRequest(pReq);

                        continue;
                    }
                }

				if (request.IsError)
				{
					if (onError != null)
						onError (request);
				}
				else
				{
					if (request.IsSuccess)
					{
						if (onSuccess != null)
							onSuccess (request);
					}
					else
					{
						if (onFailure != null)
							onFailure (request);
					}
				}

				if (onFinishEachRequest != null)
					onFinishEachRequest (request);

				if (request != null)
				{
					request.Destroy ();
					request = null;
				}
			}

			if (onFinish != null)
				onFinish ();

			// Tell that the instance of this class is being destroyed.
			isDestroying = true;

            yield return 0;

			// Destroy in 0.01 sec
			Destroy (gameObject, 0.01f);
		}

		/// <summary>
		/// Abort request
		/// </summary>
		public void Abort ()
		{
			if (isDestroying)
				return;

			if (isSending)
			{
                SetOnSuccess(null);
                SetOnFailure(null);
                SetOnError(null);
                SetOnFinish(null);
                SetOnFinishEachRequest(null);

				StopAllCoroutines ();

				if (request != null)
				{
					request.Destroy ();
					request = null;
				}
			}

			Destroy (gameObject);
		}

		public BaseHttpRequester Send ()
		{
			// Exit function if requester is being destroyed or being busy to send
			if (isSending || isDestroying)
				return this;

			isSending = true;
			StartCoroutine (Coro_Send ());

			return this;
		}
	}

}
