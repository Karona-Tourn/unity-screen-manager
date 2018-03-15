namespace TK.ScreenManagement
{
    using System.Collections;
    using UnityEngine;
	using TK.Http;
    using ScreenCallback = UnityEngine.Events.UnityAction<Screen>;

    public class ScreenLoad : Screen
	{
		public const string SceneName = "ScreenLoad";

		[SerializeField]
		private float clearBackgroundAlpha = 0.5f;

		[SerializeField]
		private CanvasGroup canvas = null;

        private HttpRequesterWrapper httpWrapper = null;
        private Coroutine coroutine = null;

        public static void Open (LoadData load, bool clearBackground = false, ScreenCallback onActive = null, ScreenCallback onDeactive = null)
		{
            bool noNextScreen = string.IsNullOrEmpty(load.nextScreen);

			if ( noNextScreen && ScreenManager.HasLoading () ) return;

			load.extraData.Add ("clear-bg", clearBackground);

            if (noNextScreen)
			{
				ScreenManager.LoadPopUp ( SceneName, load, true, onActive, onDeactive);
			}
			else
			{
                ScreenManager.LoadLoading ( SceneName, load, true, onActive, onDeactive);
			}
		}

        public override void OnInitialize(object data)
        {
            LoadData load = data as LoadData;
            bool clearBg = (bool)load.extraData["clear-bg"];
            canvas.alpha = clearBg ? clearBackgroundAlpha : 1f;
            coroutine = StartCoroutine(Coro_ExecuteLoading(load));
        }

        private void DestroyHttpWrapper()
        {
            if (httpWrapper != null)
            {
                httpWrapper.Destroy();
                httpWrapper = null;
            }
        }

        public override void OnClosing()
        {
            if (coroutine != null)
            {
                StopCoroutine(coroutine);
            }
            DestroyHttpWrapper();
        }

        private IEnumerator Coro_ExecuteLoading(LoadData load)
		{
			yield return Coro_Progress (load);

            if (string.IsNullOrEmpty(load.nextScreen))
            {
                ScreenManager.ClosePopUps();
            }
            else
            {
                if (load.forceClose)
                {
                    ScreenManager.CloseLoading();
                }
                else
                {
                    if (load.additiveScreen)
                        ScreenManager.LoadAdditiveScreen(load.nextScreen, load, false, screen =>
                        {
                            ScreenManager.CloseLoading();
                        });
                    else
                        ScreenManager.LoadScreen(load.nextScreen, load, false, screen =>
                        {
                            ScreenManager.CloseLoading();
                        });
                }
            }

            coroutine = null;
		}

		private IEnumerator Coro_Progress (LoadData load)
		{
			//yield return new WaitForEndOfFrame();

			for (int i = 0; i < load.coroutines.Count; i++)
			{
				yield return load.coroutines [i];
				if (load.forceClose)
				{
					yield break;
				}
			}

			if (load.resources.Count > 0)
			{
				int srcLoadCount = load.resources.Count;

				foreach (var task in load.resources)
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
				yield return new WaitUntil (() => srcLoadCount == 0);
			}

            if (load.parallelHttpGroup.Groups.Count > 0)
            {
                foreach (DictionaryEntry e in load.parallelHttpGroup.Groups)
                {
                    if (httpWrapper == null)
                    {
                        httpWrapper = HttpRequesterWrapper.Create();
                        DontDestroyOnLoad(httpWrapper.gameObject);
                    }

                    ParallelHttp https = (ParallelHttp)e.Value;

                    foreach (DictionaryEntry e1 in https.Https)
                    {
                        var task = (HttpRequestTask)e1.Value;
                        task.status = TaskStatus.Pending;
                        if (task.req == null)
                        {
                            task.status = TaskStatus.Ready;
                            continue;
                        }
                        var req = UnityWebRequester.Create();
                        req.SetOnSuccess(res =>
                        {
                            task.res = res;
                            task.resCode = res.ResponseCode;
                            task.resData = res.ResponseText;
                            task.resTex = res.ResponseTexture;
                            if (task.onSuccess != null)
                            {
                                task.onSuccess(task);
                            }
                            //Log (res.ResponseText);
                        });
                        req.SetOnFailure(res =>
                        {
                            task.res = res;
                            task.resCode = res.ResponseCode;
                            task.resFail = res.ResponseText;
                            if (task.onFailed != null)
                            {
                                task.onFailed(task.resFail);
                            }
                            //Log (task.resFail);
                        });
                        req.SetOnError(res =>
                        {
                            task.res = res;
                            task.resCode = res.ResponseCode;
                            task.resError = res.ResponseError;
                            if (task.onFailed != null)
                            {
                                task.onFailed(task.resError);
                            }
                            //Log (task.resError);
                        });
                        req.SetOnFinish(() =>
                        {
                            task.status = TaskStatus.Ready;
                        });
                        req.AddRequest(task.req);
                        httpWrapper.SendRequest(req);
                    }

                    yield return new WaitUntil(() => httpWrapper == null || httpWrapper.IsEmpty);

                    if (load.forceClose)
                    {
                        yield break;
                    }

                    bool forceBreak = false;

                    foreach (DictionaryEntry e1 in https.Https)
                    {
                        var task = (HttpRequestTask)e1.Value;
                        if (task.abortIfFail && (string.IsNullOrEmpty(task.resFail) == false || string.IsNullOrEmpty(task.resError) == false))
                        {
                            forceBreak = true;
                            break;
                        }
                    }

                    if (forceBreak)
                    {
                        break;
                    }
                }

                DestroyHttpWrapper();
            }

			foreach (var kv in load.https)
			{
				var task = kv.Value;
				task.status = TaskStatus.Pending;
				if (task.req == null)
				{
					task.status = TaskStatus.Ready;
					continue;
				}
				bool pending = true;
				bool failed = false;
				var reqSender = UnityWebRequester.Create ();
				reqSender.SetOnSuccess (res =>
					{
						task.res = res;
						task.resCode = res.ResponseCode;
						task.resData = res.ResponseText;
						task.resTex = res.ResponseTexture;
						if (task.onSuccess != null)
						{
							task.onSuccess (task);
						}
						//Log (res.ResponseText);
					});
				reqSender.SetOnFailure (res =>
					{
						task.res = res;
						task.resCode = res.ResponseCode;
						task.resFail = res.ResponseText;
						failed = true;
						if (task.onFailed != null)
						{
							task.onFailed (task.resFail);
						}
						//Log (task.resFail);
					});
				reqSender.SetOnError (res =>
					{
						task.res = res;
						task.resCode = res.ResponseCode;
						task.resError = res.ResponseError;
						failed = true;
						if (task.onFailed != null)
						{
							task.onFailed (task.resError);
						}
						//Log (task.resError);
					});
				reqSender.SetOnFinish (() =>
					{
						pending = false;
						task.status = TaskStatus.Ready;
					});
				reqSender.AddRequest (task.req);
				reqSender.Send ();

				// Wait request pending...
				yield return new WaitWhile (() => pending);

				if (load.forceClose)
				{
					yield break;
				}

				if (failed && task.abortIfFail)
				{
					break;
				}
			}

			if (load.forceClose)
			{
				yield break;
			}

			for (int i = 0; i < load.lateCoroutines.Count; i++)
			{
				yield return load.lateCoroutines [i];
				if (load.forceClose)
				{
					yield break;
				}
			}
		}
	}
}
