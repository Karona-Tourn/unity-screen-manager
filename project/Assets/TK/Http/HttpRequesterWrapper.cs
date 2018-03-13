using System.Collections.Generic;
using UnityEngine;

namespace TK.Http
{

	public class HttpRequesterWrapper : MonoBehaviour
	{
		private LinkedList<BaseHttpRequester> requestList = new LinkedList<BaseHttpRequester> ();
		private LinkedListNode<BaseHttpRequester> node = null;

		private void LateUpdate ()
		{
			node = requestList.First;
			while (node != null)
			{
				if (node.Value == null)
					node.List.Remove (node);
				node = node.Next;
			}
		}

		public void AbortAll ()
		{
			node = requestList.First;
			while (node != null)
			{
				if (node.Value != null)
					node.Value.Abort ();
				node = node.Next;
			}
			requestList.Clear ();
		}

		public void Destroy ()
		{
			AbortAll ();
			Destroy (gameObject);
		}

		public HttpRequesterWrapper SendRequest (BaseHttpRequester request)
		{
			request.transform.SetParent (transform);
			requestList.AddLast (request.Send ());
			return this;
		}

		public static HttpRequesterWrapper Create ()
		{
			return new GameObject ("HttpRequesterWrapper").AddComponent<HttpRequesterWrapper> ();
		}
	}

}
