using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TK.ScreenDirectors;
using TK.Http;

namespace TK.ScreenDirectors
{

	public class ScreenLoading : TK.ScreenDirectors.Screen
	{
		private HttpRequesterWrapper httpWrapper = null;
		private Coroutine coroutine = null;

		static public void Open (LoadingData loadingData)
		{
			ScreenDirector.LoadScreen ( "ScreenLoading", loadingData, loadingData.isLoadingScreenCached );
		}

		public override void OnStart ( object obj )
		{
			LoadingData loadingData = (LoadingData)obj;
			coroutine = StartCoroutine ( Coro_ExecuteLoading ( loadingData ) );
		}

		private void OnDisable ()
		{
			if ( coroutine != null )
			{
				StopCoroutine ( coroutine );
				coroutine = null;
			}

			DestroyHttpWrapper ();
		}

		private void DestroyHttpWrapper ()
		{
			if ( httpWrapper != null )
			{
				httpWrapper.Destroy ();
				httpWrapper = null;
			}
		}

		private IEnumerator Coro_ExecuteLoading ( LoadingData load )
		{
			yield return Coro_Progress ( load );

			if ( string.IsNullOrEmpty ( load.nextScreen ) )
			{
				ScreenDirector.CloseScreen ( this, false );
			}
			else
			{
				if ( load.forceClose )
				{
					ScreenDirector.CloseScreen ( this, false );
				}
				else
				{
					ScreenDirector.LoadScreen ( load.nextScreen, load, load.isNextScreenCached );
				}
			}

			coroutine = null;
		}

		private IEnumerator Coro_Progress ( LoadingData load )
		{
			for ( int i = 0; i < load.coroutines.Count; i++ )
			{
				yield return load.coroutines[i];
				if ( load.forceClose )
				{
					yield break;
				}
			}

			if ( load.resources.Count > 0 )
			{
				int srcLoadCount = load.resources.Count;

				foreach ( var task in load.resources )
				{
					task.status = TaskStatus.Pending;
					ResourceManagement.ResourceManager.Load ( new ResourceManagement.ResourceManager.LoadTask ()
					{
						path = System.IO.Path.Combine ( task.path, task.name ),
						type = task.type,
						completed = assets =>
						{
							if ( assets.Length > 0 ) task.loadedObj = assets[0];
							srcLoadCount--;
							task.status = TaskStatus.Ready;
						}
					} );
				}
				yield return new WaitUntil ( () => srcLoadCount == 0 );
			}

			if ( load.parallelHttpGroup.Groups.Count > 0 )
			{
				foreach ( DictionaryEntry e in load.parallelHttpGroup.Groups )
				{
					if ( httpWrapper == null )
					{
						httpWrapper = HttpRequesterWrapper.Create ();
						DontDestroyOnLoad ( httpWrapper.gameObject );
					}

					ParallelHttp https = (ParallelHttp)e.Value;

					foreach ( DictionaryEntry e1 in https.Https )
					{
						var task = (HttpRequestTask)e1.Value;
						task.status = TaskStatus.Pending;
						if ( task.req == null )
						{
							task.status = TaskStatus.Ready;
							continue;
						}
						var req = UnityWebRequester.Create();
						req.SetOnSuccess ( res =>
						{
							task.res = res;
							task.resCode = res.ResponseCode;
							task.resData = res.ResponseText;
							task.resTex = res.ResponseTexture;
							if ( task.onSuccess != null )
							{
								task.onSuccess ( task );
							}
						} );
						req.SetOnFailure ( res =>
						{
							task.res = res;
							task.resCode = res.ResponseCode;
							task.resFail = res.ResponseText;
							if ( task.onFailed != null )
							{
								task.onFailed ( task.resFail );
							}
						} );
						req.SetOnError ( res =>
						{
							task.res = res;
							task.resCode = res.ResponseCode;
							task.resError = res.ResponseError;
							if ( task.onFailed != null )
							{
								task.onFailed ( task.resError );
							}
						} );
						req.SetOnFinish ( () =>
						{
							task.status = TaskStatus.Ready;
						} );
						req.AddRequest ( task.req );
						httpWrapper.SendRequest ( req );
					}

					yield return new WaitUntil ( () => httpWrapper == null || httpWrapper.IsEmpty );

					if ( load.forceClose )
					{
						yield break;
					}

					bool forceBreak = false;

					foreach ( DictionaryEntry e1 in https.Https )
					{
						var task = (HttpRequestTask)e1.Value;
						if ( task.abortIfFail && (string.IsNullOrEmpty ( task.resFail ) == false || string.IsNullOrEmpty ( task.resError ) == false) )
						{
							forceBreak = true;
							break;
						}
					}

					if ( forceBreak )
					{
						break;
					}
				}

				DestroyHttpWrapper ();
			}

			foreach ( var kv in load.https )
			{
				var task = kv.Value;
				task.status = TaskStatus.Pending;
				if ( task.req == null )
				{
					task.status = TaskStatus.Ready;
					continue;
				}
				bool pending = true;
				bool failed = false;
				var reqSender = UnityWebRequester.Create ();
				reqSender.SetOnSuccess ( res =>
				{
					task.res = res;
					task.resCode = res.ResponseCode;
					task.resData = res.ResponseText;
					task.resTex = res.ResponseTexture;
					if ( task.onSuccess != null )
					{
						task.onSuccess ( task );
					}
				} );
				reqSender.SetOnFailure ( res =>
				{
					task.res = res;
					task.resCode = res.ResponseCode;
					task.resFail = res.ResponseText;
					failed = true;
					if ( task.onFailed != null )
					{
						task.onFailed ( task.resFail );
					}
				} );
				reqSender.SetOnError ( res =>
				{
					task.res = res;
					task.resCode = res.ResponseCode;
					task.resError = res.ResponseError;
					failed = true;
					if ( task.onFailed != null )
					{
						task.onFailed ( task.resError );
					}
				} );
				reqSender.SetOnFinish ( () =>
				{
					pending = false;
					task.status = TaskStatus.Ready;
				} );
				reqSender.AddRequest ( task.req );
				reqSender.Send ();

				// Wait request pending...
				yield return new WaitWhile ( () => pending );

				if ( load.forceClose )
				{
					yield break;
				}

				if ( failed && task.abortIfFail )
				{
					break;
				}
			}

			if ( load.forceClose )
			{
				yield break;
			}

			for ( int i = 0; i < load.lateCoroutines.Count; i++ )
			{
				yield return load.lateCoroutines[i];
				if ( load.forceClose )
				{
					yield break;
				}
			}
		}
	}

}