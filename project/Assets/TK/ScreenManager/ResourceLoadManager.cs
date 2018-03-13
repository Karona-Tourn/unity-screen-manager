using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace TK.ScreenManagement
{
	public class ResourceLoadLoader : IEnumerator
	{
		private string srcName = "";
		private string srcPath = "";
		private Type srcType = null;

		private UnityAction<ResourceLoadLoader> onComplete = null;
		private ResourceLoadManager manager = null;

		private ResourceRequest req = null;

		public object Current { get { return null; } }

		public UnityEngine.Object LoadedObject { get { return req == null ? null : req.asset; } }

		public bool MoveNext ()
		{
			if (req != null)
			{
				if (req.isDone)
				{
					if (onComplete != null)
					{
						onComplete (this);
					}
				}
				else
				{
					return true;
				}
			}

			Abort ();
			return false;
		}

		private ResourceLoadLoader () { }

		public void Reset ()
		{
			req = Resources.LoadAsync (System.IO.Path.Combine (srcPath, srcName), srcType);
		}

		public void Abort ()
		{
			manager.Abort (this);
		}

		public static ResourceLoadLoader Build (ResourceLoadManager manager, string name, string path, Type type, UnityAction<ResourceLoadLoader> onLoaded)
		{
			return new ResourceLoadLoader ()
			{
				srcName = name,
				srcPath = path,
				srcType = type,
				onComplete = onLoaded,
				manager = manager
			};
		}
	}

	public class ResourceLoadManager : MonoBehaviour
	{
		private static ResourceLoadManager instance = null;
		private List<ResourceLoadLoader> loaders = new List<ResourceLoadLoader> ();

		public static ResourceLoadManager Instance
		{
			get
			{
				if (instance == null)
				{
					instance = new GameObject ("ResourceLoader").AddComponent<ResourceLoadManager> ();
				}
				return instance;
			}
		}

		private void Awake ()
		{
			DontDestroyOnLoad (gameObject);
		}

		public ResourceLoadLoader Load (string name, string path, Type type, UnityAction<ResourceLoadLoader> onLoaded)
		{
			ResourceLoadLoader req = ResourceLoadLoader.Build (this, name, path, type, onLoaded);
			Load (req);
			return req;
		}

		private void Load (ResourceLoadLoader req)
		{
			loaders.Add (req);
			req.Reset ();
			StartCoroutine (req);
		}

		public void AbortAll ()
		{
			while (loaders.Count > 0)
			{
				loaders[0].Abort ();
			}
		}

		public void Abort (ResourceLoadLoader req)
		{
			if (req != null)
			{
				if (loaders.Contains (req))
				{
					StopCoroutine (req);
					loaders.Remove (req);
				}
			}
		}
	}
}
