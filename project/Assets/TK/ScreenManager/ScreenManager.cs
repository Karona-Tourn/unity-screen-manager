namespace TK.ScreenManagement
{
	using System.Collections;
	using System.Collections.Generic;
	using UnityEngine;

	using ScreenCallback = UnityEngine.Events.UnityAction<Screen>;

	public enum ScreenType
	{
		Screen,
		PopUp,
		Loading,
	}

	public class ScreenManager : MonoBehaviour
	{
		public struct StackMeta
		{
			public string name;
			public ScreenType type;
		}

		public class ScreenMeta
		{
			public Screen screen;
			public bool cached;
		}

		public class ScreenLoadData
		{
			public bool cache;
			public bool active;

			/// <summary>
			/// Screen scene to be load
			/// </summary>
			public string sceneName = "";

			/// <summary>
			/// Screen type
			/// </summary>
			public ScreenType screenType = ScreenType.Screen;

			/// <summary>
			/// Tell whether load a single scene or additive scene
			/// </summary>
			public bool additive = false;

			/// <summary>
			/// Data load job
			/// </summary>
			public LoadData loadData = null;

			/// <summary>
			/// Called when popup screen is active
			/// </summary>
			public ScreenCallback onActive = null;

			/// <summary>
			/// Called when popup screen is deactive
			/// </summary>
			public ScreenCallback onDeactive = null;

			public bool IsPreload { get { return cache && !active; } }
		}

		private class SceneLoadOperation : IEnumerator
		{
			public enum State
			{
				KillingOtherScreens,
				Cleaning,
				LoadingScreen,
				Waiting,
				Ready,
			}

			private State state;
			private ScreenLoadData SLD;
			private ScreenManager director;
			private AsyncOperation async = null;
			private Screen loadedScreen = null;

			public object Current { get { throw new System.NotImplementedException (); } }

			public State Status { get { return state; } }

			public SceneLoadOperation ( ScreenLoadData SLD )
			{
				this.SLD = SLD;
				director = instance;

				if ( SLD.IsPreload )
				{
					state = State.LoadingScreen;
				}
				else
				{
					state = State.KillingOtherScreens;
				}
			}

			public bool Match ( ScreenLoadData SLD )
			{
				return this.SLD.sceneName == SLD.sceneName && this.SLD.screenType == SLD.screenType;
			}

			private void KillOtherScreens ()
			{
				ClosePopUps ();

				if ( SLD.screenType != ScreenType.Screen )
					return;

				bool existInStack = System.Array.Exists(director.screenStack.ToArray(), e => e.name == SLD.sceneName);

				if ( SLD.additive && !existInStack )
					return;

				director.CloseScreensUntil ( SLD.sceneName );
			}

			private bool CleanUnusedAssets ()
			{
				if ( async == null )
				{
					async = Resources.UnloadUnusedAssets ();
				}

				if ( async.isDone )
				{
					async = null;
					return false;
				}
				else
				{
					return true;
				}
			}

			private void LoadScreen ()
			{
				ScreenMeta SM = null;

				if ( director.screens.TryGetValue ( SLD.sceneName, out SM ) )
				{
					loadedScreen = SM.screen;

					director.AddScreen ( loadedScreen, SLD.screenType, SLD.cache, SLD.active );

					SceneLoader.SetOnActive ( SM.screen.Name, SLD.onActive );
					SceneLoader.SetOnDeactive ( SM.screen.Name, SLD.onDeactive );

					FinalizeScreenInit ();

					state = State.Ready;
				}
				else
				{
					state = State.Waiting;

					SceneLoader.Load ( SLD.sceneName, SLD.additive, ( screen ) =>
					  {
						  loadedScreen = screen;
						  director.AddScreen ( screen, SLD.screenType, SLD.cache, SLD.active );

						  FinalizeScreenInit ();

						  state = State.Ready;
					  }, SLD.onActive, SLD.onDeactive );
				}
			}

			private void FinalizeScreenInit ()
			{
				if ( SLD.IsPreload )
					--director.preloadingCount;

				if ( SLD.active )
				{
					loadedScreen.Activate ();

					if ( SLD.screenType == ScreenType.Loading )
						loadedScreen.transform.SetAsFirstSibling ();
					else
						loadedScreen.transform.SetAsLastSibling ();

					loadedScreen.OnInitialize ( SLD.loadData );
					loadedScreen.OnStart ();
				}
				else
				{
					loadedScreen.Deactivate ();
				}
			}

			public bool MoveNext ()
			{
				// TODO: If this operation is PopUp operation, skip it if there is a next operation to be executed
				if ( !SLD.IsPreload )
				{
					if ( state != State.Waiting && state != State.Cleaning && state != State.LoadingScreen )
					{
						if ( SLD.screenType == ScreenType.PopUp )
						{
							if ( director.nextLoads.Count > 0 && !director.nextLoads.Peek ().SLD.IsPreload )
							{
								return false;
							}
						}
					}
				}

				if ( state == State.KillingOtherScreens )
				{
					KillOtherScreens ();
					state = State.Cleaning;
				}

				if ( state == State.Cleaning )
				{
					bool cleaning = CleanUnusedAssets();

					// Move to next state to load screen if cleaning is complete
					if ( cleaning )
						return true;
					else
						state = State.LoadingScreen;
				}

				if ( state == State.LoadingScreen )
					LoadScreen ();

				if ( state == State.Waiting )
					return true;

				return false;
			}

			public void Reset ()
			{
				throw new System.NotImplementedException ();
			}
		}

		static private ScreenManager instance = null;

		private Dictionary<string, ScreenMeta> screens = new Dictionary<string, ScreenMeta>();
		private Stack<StackMeta> screenStack = new Stack<StackMeta>();
		private Stack<StackMeta> popupStack = new Stack<StackMeta>();

		// Store next screen load operations
		private Queue<SceneLoadOperation> nextLoads = new Queue<SceneLoadOperation>();

		private Transform screenContent = null;
		private Transform popupContent = null;

		private int preloadingCount = 0;

		// Running screen loading operation
		private SceneLoadOperation runningLoadOp = null;

		static public bool IsReady { get; private set; }

		static public bool IsPreloadingScreens { get { return instance.preloadingCount > 0; } }

		private void Awake ()
		{
			if ( instance == null )
			{
				instance = this;
				DontDestroyOnLoad ( gameObject );
			}
			else
			{
				Destroy ( gameObject );
			}
		}

		private void Start ()
		{
			CreateContent ( "Screens", out screenContent );
			CreateContent ( "Popups", out popupContent );

			IsReady = true;
		}

		/// <summary>
		/// Create screen parent content
		/// </summary>
		private void CreateContent ( string name, out Transform content )
		{
			var go = new GameObject(name);
			DontDestroyOnLoad ( go );
			content = go.transform;
			content.SetAsLastSibling ();
		}

		private void LateUpdate ()
		{
			if ( nextLoads.Count > 0 || runningLoadOp != null )
				return;

			var stack = popupStack.Count > 0 ? popupStack : screenStack.Count > 0 ? screenStack : null;

			if ( stack == null )
				return;

			var SKM = stack.Peek();

			ScreenMeta SM = null;

			screens.TryGetValue ( SKM.name, out SM );

			if ( SM == null || SM.screen == null )
				return;

			SM.screen.OnUniqueUpdate ();

			if ( Input.GetKeyDown ( KeyCode.Escape ) )
			{
				SM.screen.OnBackPressed ();
			}
		}

		private void Update ()
		{
			if ( runningLoadOp == null )
			{
				if ( nextLoads.Count > 0 )
				{
					runningLoadOp = nextLoads.Dequeue ();
				}
			}

			if ( runningLoadOp != null )
			{
				var run = runningLoadOp.MoveNext();
				if ( !run ) { runningLoadOp = null; }
			}
		}

		private void CloseScreen ( string name )
		{
			ScreenMeta SM = null;

			screens.TryGetValue ( name, out SM );

			if ( SM == null ) { return; }

			SM.screen.OnClosing ();

			if ( SM.cached )
			{
				SM.screen.Deactivate ();
			}
			else
			{
				screens.Remove ( name );
				SM.screen.Destroy ();
			}
		}

		private void _CloseScreen ( Screen screen )
		{
			if ( runningLoadOp != null || nextLoads.Count > 0 )
				return;

			string closedName = screen.name;
			var SKMs = screenStack.ToArray();
			screenStack.Clear ();
			for ( int i = 0; i < SKMs.Length; i++ )
			{
				if ( SKMs[i].name == closedName )
				{
					CloseScreen ( closedName );
					continue;
				}
				screenStack.Push ( SKMs[i] );
			}
		}

		private void _ClosePopUps ()
		{
			while ( popupStack.Count > 0 )
			{
				var each = popupStack.Peek();
				if ( each.type == ScreenType.Loading )
					break;
				popupStack.Pop ();
				CloseScreen ( each.name );
			}
		}

		private void _CloseLoading ()
		{
			var SKMs = popupStack.ToArray();
			popupStack.Clear ();

			for ( int i = 0; i < SKMs.Length; i++ )
			{
				var SKM = SKMs[i];
				if ( SKM.type == ScreenType.Loading )
				{
					CloseScreen ( SKM.name );
					continue;
				}
				popupStack.Push ( SKM );
			}
		}

		private void CloseScreensUntil ( string screenName )
		{
			while ( screenStack.Count > 0 )
			{
				var each = screenStack.Pop();

				if ( each.name == screenName ) { break; }

				CloseScreen ( each.name );
			}
		}

		private void AddScreen ( Screen screen, ScreenType screenType, bool cache, bool active )
		{
			ScreenMeta SM = null;

			if ( screens.TryGetValue ( screen.Name, out SM ) )
			{
				SM.cached = cache;
			}
			else
			{
				var content = screenType == ScreenType.Screen ? screenContent : popupContent;

				if ( content )
				{
					screen.transform.SetParent ( content, true );
					screen.transform.localScale = Vector3.one;
				}
				else
				{
					DontDestroyOnLoad ( screen.gameObject );
				}

				screens.Add ( screen.Name, new ScreenMeta ()
				{
					screen = screen,
					cached = cache
				} );
			}

			if ( !active )
				return;

			Stack<StackMeta> stack = screenType == ScreenType.Screen ? screenStack : popupStack;

			stack.Push ( new StackMeta ()
			{
				name = screen.Name,
				type = screenType
			} );
		}

		private bool LoadScene ( ScreenLoadData SLD )
		{
			if ( !IsReady )
				return false;

			bool reject = System.Array.Exists(nextLoads.ToArray(), o =>
											  (o.Match(SLD) && SLD.IsPreload)
											   || (o.Match(SLD) && SLD.screenType != ScreenType.PopUp));

			if ( runningLoadOp != null )
				reject |= (runningLoadOp.Match ( SLD ) && SLD.IsPreload)
					|| (runningLoadOp.Match ( SLD ) && SLD.screenType != ScreenType.PopUp);

			// Make sure that there must be only one Loading screen loaded
			reject |= System.Array.Exists ( popupStack.ToArray (), e => SLD.screenType == e.type && SLD.screenType == ScreenType.Loading );

			if ( reject )
				return false;

			if ( SLD.IsPreload )
				++preloadingCount;

			nextLoads.Enqueue ( new SceneLoadOperation ( SLD ) );

			return true;
		}

		/// <summary>
		/// Get a loaded screen by name
		/// </summary>
		private Screen _GetScreenByName ( string screenName )
		{
			ScreenMeta SM = null;

			if ( screens.TryGetValue ( screenName, out SM ) == false )
			{
				Debug.LogErrorFormat ( "Screen \"{0}\" not found!", screenName );
			}

			return SM.screen;
		}

		static public Screen GetScreenByName ( string screenName )
		{
			return instance._GetScreenByName ( screenName );
		}

		static private bool LoadScreen ( string name, bool additive, ScreenType type, LoadData loadData, bool cache, bool active, ScreenCallback onActive = null, ScreenCallback onDeactive = null )
		{
			ScreenLoadData SLD = new ScreenLoadData()
			{
				sceneName = name,
				screenType = type,
				additive = additive,
				loadData = loadData == null ? new LoadData() : loadData,
				cache = cache,
				active = active,
				onActive = onActive,
				onDeactive = onDeactive
			};

			return instance.LoadScene ( SLD );
		}

		static public bool LoadScreen ( string name, bool additive, ScreenType type, LoadData loadData, bool cache, ScreenCallback onActive = null, ScreenCallback onDeactive = null )
		{
			return LoadScreen ( name, additive, type, loadData, cache, true, onActive, onDeactive );
		}

		static public bool LoadScreen ( string name, LoadData loadData = null, bool cache = false, ScreenCallback onActive = null, ScreenCallback onDeactive = null )
		{
			return LoadScreen ( name, false, ScreenType.Screen, loadData, cache, onActive, onDeactive );
		}

		static public bool LoadAdditiveScreen ( string name, LoadData loadData = null, bool cache = false, ScreenCallback onActive = null, ScreenCallback onDeactive = null )
		{
			return LoadScreen ( name, true, ScreenType.Screen, loadData, cache, onActive, onDeactive );
		}

		static public bool LoadPopUp ( string name, LoadData loadData = null, bool cache = false, ScreenCallback onActive = null, ScreenCallback onDeactive = null )
		{
			return LoadScreen ( name, false, ScreenType.PopUp, loadData, cache, onActive, onDeactive );
		}

		static public bool LoadLoading ( string name, LoadData loadData, bool cache = false, ScreenCallback onActive = null, ScreenCallback onDeactive = null )
		{
			return LoadScreen ( name, false, ScreenType.Loading, loadData, cache, onActive, onDeactive );
		}

		static public bool PreloadCachedScreen ( string name )
		{
			return LoadScreen ( name, true, ScreenType.Screen, null, true, false );
		}

		static public bool PreloadCachedPopUp ( string name )
		{
			return LoadScreen ( name, true, ScreenType.PopUp, null, true, false );
		}

		static public void ClosePopUps ()
		{
			instance._ClosePopUps ();
		}

		static public void CloseLoading ()
		{
			instance._CloseLoading ();
		}

		static public void CloseScreen ( Screen screen )
		{
			instance._CloseScreen ( screen );
		}

		static public bool HasLoading ()
		{
			return System.Array.Exists ( instance.popupStack.ToArray (), e => e.type == ScreenType.Loading );
		}

		static public bool ExistPopUp ( string name )
		{
			return System.Array.Exists ( instance.popupStack.ToArray (), e => e.name == name && e.type == ScreenType.PopUp );
		}
	}

}