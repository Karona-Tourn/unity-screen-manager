using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace TK.ScreenDirectors
{
	public class SceneLoader
	{
		static private Dictionary<string, UnityAction<Screen>> onLoadHandlers = new Dictionary<string, UnityAction<Screen>>();

		static public void NotifyOnLoad ( Screen screen )
		{
			UnityAction<Screen> onLoad = null;

			string name = screen.name;

			if ( onLoadHandlers.TryGetValue ( name, out onLoad ) )
			{
				onLoadHandlers.Remove ( name );
				if ( onLoad != null ) onLoad ( screen );
			}
		}

		static public void Load ( string scene, bool additive, UnityAction<Screen> onLoad )
		{
			onLoadHandlers.Add ( scene, onLoad );
			SceneManager.LoadScene ( scene, additive ? LoadSceneMode.Additive : LoadSceneMode.Single );
		}
	}

	public class ScreenDirector : MonoBehaviour
	{
		public enum PriorityLayer
		{
			Low = 1 << 0,
			Popup = 1 << 1,
			High = 1 << 2,
			Alert = 1 << 3
		}

		private class ScreenInfo
		{
			public Screen screen = null;
			public string name = "";
			public bool cached = false;
		}

		private class LiveScreenInfo
		{
			public int id = 0;
			public Screen screen = null;
			public string name = "";

			public bool Match ( LiveScreenInfo other )
			{
				if ( other == null ) return false;
				return name == other.name && screen.Layer == other.screen.Layer;
			}
		}

		private class LoadJobInfo
		{
			public object obj = null;
			public string name = "";
			public bool cached = false;
		}

		private class LoadJob
		{
			protected enum State
			{
				Load,
				Waiting,
				End
			}

			private ScreenDirector _screenDirector = null;
			private State _state = default(State);
			private LoadJobInfo _jobData = null;
			private LiveScreenInfo _nextLiveScreenInfo = null;
			private ScreenInfo _cachedScreenInfo = null;

			public LoadJob ( ScreenDirector screenDirector, LoadJobInfo jobData )
			{
				_screenDirector = screenDirector;
				_jobData = jobData;
				_state = State.Load;
			}

			/// <summary>
			/// Load to display alert screen
			/// </summary>
			private void LoadAlertScreen ()
			{
				_state = State.Waiting;

				var topScreenInfo = _screenDirector.GetTopLiveScreen();
				if ( topScreenInfo != null )
				{
					topScreenInfo.screen.OnLostFocus ();
				}

				// Create instance of next live screen info to be displayed
				_nextLiveScreenInfo = new LiveScreenInfo ()
				{
					id = _screenDirector.NextId ( PriorityLayer.Alert ),
					name = _jobData.name,
					screen = _screenDirector.CloneScreen ( _cachedScreenInfo.screen )
				};

				_screenDirector.AddLiveScreenInfo ( _nextLiveScreenInfo );
				_nextLiveScreenInfo.screen.OnCreated ( _jobData.obj );
				_nextLiveScreenInfo.screen.PrepareAnimateIn ();
				_nextLiveScreenInfo.screen.Open ( true );
				_nextLiveScreenInfo.screen.OnStart ( _jobData.obj );
				_nextLiveScreenInfo.screen.OnFocus ();
				_nextLiveScreenInfo.screen.Open ( false, () =>
				{
					_nextLiveScreenInfo.screen.OnReady ();
					_state = State.End;
				} );
			}

			private void LoadPopupScreen ()
			{
				_state = State.Waiting;

				var topScreenInfo = _screenDirector.GetTopLiveScreen();
				if ( topScreenInfo != null )
				{
					topScreenInfo.screen.OnLostFocus ();
				}

				_screenDirector.CloseScreenAboveLayer ( PriorityLayer.Popup, true, () =>
				{
					_nextLiveScreenInfo = new LiveScreenInfo ()
					{
						id = _screenDirector.NextId ( PriorityLayer.Popup ),
						name = _jobData.name,
						screen = _screenDirector.CloneScreen ( _cachedScreenInfo.screen )
					};

					_screenDirector.AddLiveScreenInfo ( _nextLiveScreenInfo );
					_nextLiveScreenInfo.screen.OnCreated ( _jobData.obj );
					_nextLiveScreenInfo.screen.PrepareAnimateIn ();
					_nextLiveScreenInfo.screen.Open ( true );
					_nextLiveScreenInfo.screen.OnStart ( _jobData.obj );
					_nextLiveScreenInfo.screen.OnFocus ();
					_nextLiveScreenInfo.screen.Open ( false, () =>
					{
						_nextLiveScreenInfo.screen.OnReady ();
						_state = State.End;
					} );
				} );
			}

			private void LoadLowScreen ()
			{
				_state = State.Waiting;

				var topScreenInfo = _screenDirector.GetTopLiveScreen();
				if ( topScreenInfo != null && (topScreenInfo.screen.Layer == PriorityLayer.Alert || topScreenInfo.screen.Layer == PriorityLayer.Popup) )
				{
					topScreenInfo.screen.OnLostFocus ();
				}

				_screenDirector.CloseScreenByLayer ( _screenDirector._liveScreens.Last, PriorityLayer.Alert | PriorityLayer.Popup, true, () =>
				{
					topScreenInfo = _screenDirector.GetTopLiveScreen ();

					if ( topScreenInfo != null )
					{
						if ( topScreenInfo.screen.Layer == PriorityLayer.High )
						{
							topScreenInfo.screen.OnFocus ();

							if ( _screenDirector.IsScreenLive ( _cachedScreenInfo.screen ) )
							{
								_nextLiveScreenInfo.screen.PrepareAnimateIn ();
								_nextLiveScreenInfo.screen.Open ( true );
								
								topScreenInfo.screen.AnimateOut ( () =>
								{
									topScreenInfo.screen.OnLostFocus ();
									CloseScreen ( topScreenInfo.screen, true );

									_nextLiveScreenInfo.screen.OnStart ( _jobData.obj );
									_nextLiveScreenInfo.screen.OnFocus ();
									_nextLiveScreenInfo.screen.Open ( false, () =>
									{
										_nextLiveScreenInfo.screen.OnReady ();
										_state = State.End;
									} );
								} );
							}
							else
							{
								_screenDirector.CloseScreenByLayer ( _screenDirector._liveScreens.Last, PriorityLayer.Low, true, () =>
								{
									_nextLiveScreenInfo = new LiveScreenInfo ()
									{
										id = 1,
										name = _jobData.name,
										screen = _cachedScreenInfo.screen
									};

									_screenDirector.AddLiveScreenInfo ( _nextLiveScreenInfo );
									_nextLiveScreenInfo.screen.OnCreated ( _jobData.obj );
									_nextLiveScreenInfo.screen.PrepareAnimateIn ();
									_nextLiveScreenInfo.screen.Open ( true );

									topScreenInfo.screen.AnimateOut ( () =>
									{
										topScreenInfo.screen.OnLostFocus ();
										CloseScreen ( topScreenInfo.screen, true );

										_nextLiveScreenInfo.screen.OnStart ( _jobData.obj );
										_nextLiveScreenInfo.screen.OnFocus ();
										_nextLiveScreenInfo.screen.Open ( false, () =>
										{
											_nextLiveScreenInfo.screen.OnReady ();
											_state = State.End;
										} );
									} );
								} );
							}
							return;
						}
					}

					if ( _screenDirector.IsScreenLive ( _cachedScreenInfo.screen ) )
					{
						_cachedScreenInfo.screen.PrepareAnimateIn ();
						_cachedScreenInfo.screen.OnStart ( _jobData.obj );
						_cachedScreenInfo.screen.OnFocus ();
						_cachedScreenInfo.screen.Open ( false, () =>
						{
							_nextLiveScreenInfo.screen.OnReady ();
							_state = State.End;
						} );
					}
					else
					{
						_nextLiveScreenInfo = new LiveScreenInfo ()
						{
							id = 1,
							name = _jobData.name,
							screen = _cachedScreenInfo.screen
						};

						_screenDirector.AddLiveScreenInfo ( _nextLiveScreenInfo, false );
						_nextLiveScreenInfo.screen.OnCreated ( _jobData.obj );
						_nextLiveScreenInfo.screen.PrepareAnimateIn ();
						_nextLiveScreenInfo.screen.Open ( true );

						if ( topScreenInfo != null )
						{
							topScreenInfo.screen.AnimateOut ( () =>
							{
								topScreenInfo.screen.OnLostFocus ();
								CloseScreen ( topScreenInfo.screen, true );

								_nextLiveScreenInfo.screen.OnStart ( _jobData.obj );
								_nextLiveScreenInfo.screen.OnFocus ();
								_nextLiveScreenInfo.screen.Open ( false, () =>
								{
									_nextLiveScreenInfo.screen.OnReady ();
									_state = State.End;
								} );
							} );
						}
						else
						{
							_nextLiveScreenInfo.screen.OnStart ( _jobData.obj );
							_nextLiveScreenInfo.screen.OnFocus ();
							_nextLiveScreenInfo.screen.Open ( false, () =>
							{
								_nextLiveScreenInfo.screen.OnReady ();
								_state = State.End;
							} );
						}
					}
				} );
			}

			private void LoadHighScreen ()
			{
				_state = State.Waiting;

				var topScreenInfo = _screenDirector.GetTopLiveScreen();
				if ( topScreenInfo != null )
				{
					topScreenInfo.screen.OnLostFocus ();
				}

				_screenDirector.CloseScreenAboveLayer ( PriorityLayer.Low, true, () =>
				{
					UnityAction<LiveScreenInfo> act = previousScreen =>
					{
						_nextLiveScreenInfo = new LiveScreenInfo ()
						{
							id = 1,
							name = _jobData.name,
							screen = _cachedScreenInfo.screen
						};

						_screenDirector.AddLiveScreenInfo ( _nextLiveScreenInfo );
						_nextLiveScreenInfo.screen.OnCreated ( _jobData.obj );
						_nextLiveScreenInfo.screen.PrepareAnimateIn ();
						_nextLiveScreenInfo.screen.Open ( true );
						_nextLiveScreenInfo.screen.OnStart ( _jobData.obj );
						_nextLiveScreenInfo.screen.OnFocus ();
						_nextLiveScreenInfo.screen.Open ( false, () =>
						{
							_nextLiveScreenInfo.screen.OnReady ();
							_state = State.End;
						} );

						if ( previousScreen != null ) CloseScreen ( previousScreen.screen, true );
					};

					topScreenInfo = _screenDirector.GetTopLiveScreen ();

					if ( topScreenInfo != null )
					{
						topScreenInfo.screen.AnimateOut ( () => act ( topScreenInfo ) );
					}
					else
					{
						act ( topScreenInfo );
					}
				} );
			}

			private void NextLoadProcess ()
			{
				switch ( _cachedScreenInfo.screen.Layer )
				{
					case PriorityLayer.Alert:
						LoadAlertScreen ();
						break;
					case PriorityLayer.High:
						LoadHighScreen ();
						break;
					case PriorityLayer.Popup:
						LoadPopupScreen ();
						break;
					case PriorityLayer.Low:
						LoadLowScreen ();
						break;
				}
			}

			public bool Process ()
			{
				if ( _state == State.Load )
				{
					if ( _screenDirector.TryGetScreen ( _jobData.name, out _cachedScreenInfo ) )
					{
						_cachedScreenInfo.cached = _jobData.cached;
						NextLoadProcess ();
					}
					else
					{
						_state = State.Waiting;
						SceneLoader.Load ( _jobData.name, false, screen =>
						{
							screen.Close ( true );
							_cachedScreenInfo = new ScreenInfo ()
							{
								screen = screen,
								name = _jobData.name,
								cached = _jobData.cached
							};
							_screenDirector.AddScreen ( _cachedScreenInfo );
							NextLoadProcess ();
						} );
					}
				}

				if ( _state == State.End )
				{
					return false;
				}

				return true;
			}
		}

		static private ScreenDirector instance = null;

		[SerializeField]
		private bool _useFixedUpdate = false;

		[SerializeField]
		private Transform _screenContent = null;

		private Dictionary<string, ScreenInfo> _screens = new Dictionary<string, ScreenInfo>();
		private LinkedList<LiveScreenInfo> _liveScreens = new LinkedList<LiveScreenInfo>();
		private Queue<LoadJob> _jobQueue = new Queue<LoadJob>();
		private LoadJob _pendingJob = null;

		static private ScreenDirector Instance
		{
			get
			{
				if ( instance == null )
				{
					instance = FindObjectOfType<ScreenDirector> ();
					DontDestroyOnLoad ( instance.gameObject );
				}
				return instance;
			}
		}

		public List<string> LiveScreenNames
		{
			get
			{
				List<string> list = new List<string>();
				var node = _liveScreens.First;
				while ( node != null )
				{
					list.Add ( node.Value.name );
					node = node.Next;
				}
				return list;
			}
		}

		private void Awake ()
		{
			if ( instance == null )
			{
				instance = this;
				DontDestroyOnLoad ( gameObject );
			}

			if ( _screenContent == null ) _screenContent = transform;
		}

		private void Start ()
		{
			if ( !Equals ( instance ) )
			{
				Destroy ( gameObject );
			}
		}

		private void Update ()
		{
			if ( _useFixedUpdate ) return;

			UpdateJobs ();
		}

		private void FixedUpdate ()
		{
			if ( !_useFixedUpdate ) return;

			UpdateJobs ();
		}

		private void LateUpdate ()
		{
			if ( _jobQueue.Count > 0 || _pendingJob != null ) return;

			if ( _liveScreens.Count > 0 )
			{
				var node = _liveScreens.Last;
				node.Value.screen.OnUpdate ();

				if ( Input.GetKeyDown ( KeyCode.Escape ) )
				{
					node.Value.screen.OnKeyBackPressed ();
				}
			}
		}

		private void UpdateJobs ()
		{
			if ( _jobQueue.Count > 0 )
			{
				if ( _pendingJob == null )
				{
					_pendingJob = _jobQueue.Dequeue ();
				}
			}

			if ( _pendingJob != null )
			{
				var pending = _pendingJob.Process();
				if ( !pending )
				{
					_pendingJob = null;
				}
			}
		}

		/// <summary>
		/// Order screen transforms based on their layer prority
		/// </summary>
		private void OrderScreenSiblings ()
		{
			var node = _liveScreens.First;

			while ( node != null )
			{
				node.Value.screen.rectTransform.SetAsLastSibling ();
				node = node.Next;
			}
		}

		/// <summary>
		/// Generate new ID for screen with a specific priority layer
		/// </summary>
		/// <param name="layer">Priority layer for which a new ID is generated</param>
		/// <returns>New ID</returns>
		private int NextId ( PriorityLayer layer )
		{
			var ids = _liveScreens
				.Where (s => s.screen.Layer == layer)
				.Select (s => s.id)
				.OrderBy (s => s)
				.ToArray ();

			int nextId = 1;

			for ( int i = 0, length = ids.Length; i < length; i++, nextId++ )
			{
				if ( ids[i] != nextId ) break;
			}

			return nextId;
		}

		/// <summary>
		/// Attach a screen as child of screen content
		/// </summary>
		/// <param name="screen">Screen to be attached</param>
		private void AttachScreen ( Screen screen )
		{
			screen.rectTransform.SetParent ( _screenContent );
			screen.rectTransform.localScale = Vector3.one;
			screen.rectTransform.anchorMin = Vector2.zero;
			screen.rectTransform.anchorMax = Vector2.one;
			screen.rectTransform.anchoredPosition3D = Vector3.zero;
			screen.rectTransform.sizeDelta = Vector2.zero;
		}

		/// <summary>
		/// Clone a new screen
		/// </summary>
		/// <param name="screen">Sample screen to be cloned</param>
		/// <returns>New cloned screen</returns>
		private Screen CloneScreen ( Screen screen )
		{
			var newScreen = Instantiate (screen);
			AttachScreen ( newScreen );
			return newScreen;
		}

		/// <summary>
		/// Add a screen info to dictionary
		/// </summary>
		/// <param name="info">New screen info to be added</param>
		private void AddScreen ( ScreenInfo info )
		{
			AttachScreen ( info.screen );
			_screens.Add ( info.name, info );
		}

		private bool TryGetScreen ( string name, out ScreenInfo info )
		{
			return _screens.TryGetValue ( name, out info );
		}

		private LiveScreenInfo GetTopLiveScreen ()
		{
			if ( _liveScreens.Count == 0 ) return null;
			else return _liveScreens.Last.Value;
		}

		private bool IsScreenLive ( Screen screen )
		{
			var node = _liveScreens.First;
			while ( node != null )
			{
				if ( node.Value.screen.Equals ( screen ) )
				{
					return true;
				}
				node = node.Next;
			}
			return false;
		}

		private void AddLiveScreenInfo ( LiveScreenInfo data, bool fromLast = true )
		{
			var node = fromLast ? _liveScreens.Last : _liveScreens.First;

			if ( fromLast )
			{
				while ( true )
				{
					if ( node == null )
					{
						_liveScreens.AddFirst ( data );
						break;
					}
					if ( (int)data.screen.Layer >= (int)node.Value.screen.Layer )
					{
						_liveScreens.AddAfter ( node, data );
						break;
					}
					node = node.Previous;
				}
			}
			else
			{
				while ( true )
				{
					if ( node == null )
					{
						_liveScreens.AddFirst ( data );
						break;
					}
					if ( (int)data.screen.Layer <= (int)node.Value.screen.Layer )
					{
						_liveScreens.AddBefore ( node, data );
						break;
					}
					node = node.Next;
				}
			}

			OrderScreenSiblings ();
		}

		private void CloseScreenAboveLayer ( PriorityLayer layer, bool quick, UnityAction onFinish )
		{
			var node = _liveScreens.Last;

			if ( node == null || node.Value.screen.Layer <= layer )
			{
				if ( onFinish != null ) onFinish ();
			}
			else
			{
				node.Value.screen.Close ( quick, () =>
				{
					var screen = node.Value;
					LiveScreenInfo lowerScreen = null;

					if ( node.Previous != null )
					{
						lowerScreen = node.Previous.Value;
					}

					if ( screen.Match ( lowerScreen ) )
					{
						screen.screen.Destroy ();
					}
					else
					{
						ScreenInfo screenInfo = _screens[screen.name];

						if ( !screen.screen.Equals ( screenInfo.screen ) )
						{
							screen.screen.Destroy ();
						}

						if ( !screenInfo.cached )
						{
							_screens.Remove ( screen.name );
							screen.screen.Destroy ();
						}
					}

					_liveScreens.Remove ( node );

					CloseScreenAboveLayer ( layer, quick, onFinish );
				} );
			}
		}

		private void CloseScreenByLayer ( LinkedListNode<LiveScreenInfo> node, PriorityLayer layer, bool quick, UnityAction onFinish )
		{
			if ( node != null )
			{
				var screen = node.Value.screen;
				if ( (screen.Layer & layer) == 0 )
				{
					CloseScreenByLayer ( node.Previous, layer, quick, onFinish );
				}
				else
				{
					screen.Close ( quick, () =>
					{
						LinkedListNode<LiveScreenInfo> nextNode = null;

						if ( node.Previous != null && node.Previous.Value.Match ( node.Value ) )
						{
							nextNode = node.Previous;
							screen.Destroy ();
							_liveScreens.Remove ( node );
						}
						else
						{
							ScreenInfo screenInfo = _screens[node.Value.name];

							if ( !screenInfo.screen.Equals ( screen ) ) screen.Destroy ();

							if ( !screenInfo.cached )
							{
								screenInfo.screen.Destroy ();
								_screens.Remove ( node.Value.name );
							}

							nextNode = node.Previous;
							_liveScreens.Remove ( node );
						}

						CloseScreenByLayer ( nextNode, layer, quick, onFinish );
					} );
				}
				return;
			}

			if ( onFinish != null ) onFinish ();
		}

		private void AddJob ( LoadJobInfo jobInfo )
		{
			_jobQueue.Enqueue ( new LoadJob ( this, jobInfo ) );
		}

		static public void LoadScreen ( string name, object data, bool cache = true )
		{
			Instance.AddJob ( new LoadJobInfo ()
			{
				obj = data,
				name = name,
				cached = cache
			} );
		}

		/// <summary>
		/// Close a screen
		/// </summary>
		/// <param name="closedScreen">Screen to be closed</param>
		/// <param name="quick">If true, the screen will be closed quickly. Otherwise, it will be animated to close</param>
		/// <param name="onFinish">Callback after closing is finished</param>
		static public void CloseScreen ( Screen closedScreen, bool quick, UnityAction onFinish = null )
		{
			var liveScreens = Instance._liveScreens;
			var node = liveScreens.Last;

			while ( node != null )
			{
				var screen = node.Value;

				if ( screen.screen.Equals ( closedScreen ) )
				{
					screen.screen.Close ( quick, () =>
					{
						LiveScreenInfo lowerScreen = null;

						if ( node.Next == null && node.Previous != null ) lowerScreen = node.Previous.Value;

						screen.screen.OnLostFocus ();

						if ( screen.Match ( lowerScreen ) )
						{
							screen.screen.Destroy ();
						}
						else
						{
							ScreenInfo screenInfo = Instance._screens[screen.name];

							if ( !screen.screen.Equals ( screenInfo.screen ) )
							{
								screen.screen.Destroy ();
							}

							if ( !screenInfo.cached )
							{
								Instance._screens.Remove ( screen.name );
								screen.screen.Destroy ();
							}
						}

						liveScreens.Remove ( node );

						if ( lowerScreen != null ) lowerScreen.screen.OnFocus ();

						if ( onFinish != null ) onFinish ();
					} );
					return;
				}
				node = node.Previous;
			}

			if ( onFinish != null ) onFinish ();
		}

	}

}