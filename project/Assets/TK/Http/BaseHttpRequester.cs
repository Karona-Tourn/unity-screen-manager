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

		private UnityAction<HttpRequestNode> onSuccess = null;
		private UnityAction<HttpRequestNode> onFailure = null;
		private UnityAction<HttpRequestNode> onError = null;
		private UnityAction<HttpRequestNode> onFinishEachRequest = null;
		private UnityAction onFinish = null;

		private Queue<HttpRequestNode> requestQueue = new Queue<HttpRequestNode> ();
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
				var r = requests[i];
				var node = CreateRequestNode (r);
				node.Setup (r);
				requestQueue.Enqueue (node);
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

		private IEnumerator Coro_Send ()
		{
			while (requestQueue.Count > 0)
			{
				request = requestQueue.Dequeue ();

				// Send request to server.
				yield return request.Send ();

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

			yield return new WaitForEndOfFrame ();

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
				StopAllCoroutines ();

				if (request != null)
				{
					request.Destroy ();
					request = null;
				}
			}

			while (requestQueue.Count > 0)
			{
				requestQueue.Dequeue ().Destroy ();
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
