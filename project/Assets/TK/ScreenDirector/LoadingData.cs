using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TK.Http;
using UnityEngine;
using UnityEngine.Events;

namespace TK.ScreenDirectors
{
	public enum TaskStatus
	{
		Waiting,
		Pending,
		Ready
	}

	public class ResourceLoadTask
	{
		public string name = "";
		public string path = "";
		public Type type = null;
		public UnityEngine.Object loadedObj = null;
		public TaskStatus status = TaskStatus.Waiting;
	}

	public class HttpRequestTask
	{
		/// <summary>
		/// Abort all next request if failed or error
		/// </summary>
		public bool abortIfFail = false;
		public HttpRequestData req = null;
		public BaseHttpRequester.HttpRequestNode res = null;
		public string resData = "";
		public Texture2D resTex = null;
		public string resFail = "";
		public string resError = "";
		public int resCode = 200;
		public UnityAction<HttpRequestTask> onSuccess = null;
		public UnityAction<string> onFailed = null;
		public TaskStatus status = TaskStatus.Waiting;

		public bool IsSuccess { get { return resCode == 200; } }

		public string Err { get { return string.IsNullOrEmpty ( resFail ) ? resError : resFail; } }
	}

	public class ParallelHttp
	{
		private Hashtable https = new Hashtable();

		public Hashtable Https { get { return https; } }

		public ParallelHttp AddHttp ( string key, HttpRequestTask req )
		{
			https.Add ( key, req );
			return this;
		}

		public T GetReq<T> ( string key ) where T : HttpRequestData
		{
			if ( https.ContainsKey ( key ) == false )
			{
				return null;
			}

			HttpRequestTask task = (HttpRequestTask)https[key];
			return (T)task.req;
		}

		public HttpRequestTask GetReqTask ( string key )
		{
			if ( https.ContainsKey ( key ) == false )
			{
				return null;
			}

			return (HttpRequestTask)https[key];
		}
	}

	public class ParallelHttpGroup
	{
		private Hashtable groups = new Hashtable();

		public Hashtable Groups { get { return groups; } }

		public void AddGroup ( string key, ParallelHttp http )
		{
			groups.Add ( key, http );
		}

		public T GetReq<T> ( string groupKey, string reqKey ) where T : HttpRequestData
		{
			if ( groups.ContainsKey ( groupKey ) == false )
			{
				return null;
			}

			ParallelHttp https = (ParallelHttp)groups[groupKey];

			var req = https.GetReq<T>(reqKey);
			return req;
		}

		public HttpRequestTask GetReqTask ( string groupKey, string reqKey )
		{
			if ( groups.ContainsKey ( groupKey ) == false )
			{
				return null;
			}

			ParallelHttp https = (ParallelHttp)groups[groupKey];

			return https.GetReqTask ( reqKey );
		}
	}

	public class LoadData
	{
		/// <summary>
		/// Extra data
		/// </summary>
		public Hashtable extraData = new Hashtable();

		/// <summary>
		/// Tasks to load resources
		/// </summary>
		public List<ResourceLoadTask> resources = new List<ResourceLoadTask>();

		public Dictionary<string, HttpRequestTask> https = new Dictionary<string, HttpRequestTask>();

		public ParallelHttpGroup parallelHttpGroup = new ParallelHttpGroup();

		public bool HasHttpErr
		{
			get
			{
				foreach ( var http in https )
				{
					if ( string.IsNullOrEmpty ( http.Value.Err ) ) { continue; }
					return true;
				}
				return false;
			}
		}

		public string[] HttpErrs
		{
			get
			{
				string[] errs = null;
				errs = https
					.Select ( e => e.Value.Err )
					.Where ( e => !string.IsNullOrEmpty ( e ) ).ToArray ();
				return errs;
			}
		}

		public T GetExtraData<T> ( string key )
		{
			return (T)extraData[key];
		}

		public T GetReq<T> ( string taskKey ) where T : HttpRequestData
		{
			if ( https.ContainsKey ( taskKey ) )
			{
				HttpRequestTask task = (HttpRequestTask)https[taskKey];
				return (T)task.req;
			}
			else
			{
				return null;
			}
		}

		/// <summary>
		/// Get resource load task by the given name
		/// </summary>
		/// <returns>The resource task.</returns>
		/// <param name="name">Name of resource to load</param>
		public ResourceLoadTask GetResourceTask ( string name )
		{
			return resources.FirstOrDefault ( e => e.name == name );
		}
	}

	public class LoadingData : LoadData
	{
		// First waits before doing next tasks
		public List<IEnumerator> coroutines = new List<IEnumerator>();

		// Coroutines that execute after every other category of tasks is completed
		public List<IEnumerator> lateCoroutines = new List<IEnumerator>();

		/// <summary>
		/// Next screen
		/// </summary>
		public string nextScreen = "";

		/// <summary>
		/// If true, the loaded screen will be cached
		/// </summary>
		public bool isNextScreenCached = false;

		public bool additiveScreen = false;

		public bool forceClose = false;

		public bool isLoadingScreenCached = true;
	}
}