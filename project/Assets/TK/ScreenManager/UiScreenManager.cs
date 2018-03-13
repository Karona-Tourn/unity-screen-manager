using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace TK.ScreenManagement
{
	public class UiScreenManager : MonoBehaviour
	{
		#region Singleton Instance
		/// <summary>
		/// The unique instane of the component
		/// </summary>
		private static UiScreenManager instance = null;

		/// <summary>
		/// Get instance of this component
		/// </summary>
		public static UiScreenManager Instance
		{
			get
			{
				if (instance == null)
				{
					instance = GameObject.FindObjectOfType<UiScreenManager> ();
				}
				return instance;
			}
		}
		#endregion

		[SerializeField, Tooltip ("Content transform that will become root parent of loaded screens.")]
		private Transform screenContent = null;

		/// <summary>
		/// Stack to store all loaded pop up screens
		/// </summary>
		private Stack<UiScreen> popupStack = new Stack<UiScreen> ();

		/// <summary>
		/// Stack to store all loaded screens
		/// </summary>
		private List<UiScreen> screens = new List<UiScreen> ();

		private UiScreen screen = null;

		private void Awake ()
		{
			if (instance == null) { instance = this; }

			DontDestroyOnLoad (gameObject);
		}

		private void Update ()
		{
			UpdateScreen ();
			UpdateScreenKeyInput ();
		}

		private void UpdateScreen ()
		{
			lock (popupStack)
			{
				if (popupStack.Count > 0)
				{
					screen = popupStack.Peek ();
					screen.OnUniqueUpdate ();
				}
			}

			lock (screens)
			{
				if (screens.Count > 0)
				{
					for (int i = screens.Count - 1; i >= 0; i--)
					{
						screen = screens[i];
						if (screen.gameObject.activeSelf)
						{
							screen.OnUniqueUpdate ();
						}
					}
				}
			}
		}

		private void UpdateScreenKeyInput ()
		{
			if (Input.GetKeyDown (KeyCode.Escape))
			{
				lock (popupStack)
				{
					lock (screens)
					{
						if (popupStack.Count > 0)
						{
							screen = popupStack.Peek ();
							screen.OnBackPressed ();
						}
						else if (screens.Count > 0)
						{
							for (int i = screens.Count - 1; i >= 0; i--)
							{
								screen = screens[i];
								if (screen.gameObject.activeSelf)
								{
									screen.OnBackPressed ();
									break;
								}
							}
						}
					}
				}
			}
		}

		private void PlaceInContent (UiScreen screen)
		{
			screen.transform.SetParent (screenContent, true);
			screen.transform.localScale = Vector3.one;
		}

		private void AddScreen (UiScreen screen)
		{
			PlaceInContent (screen);
			screens.Add (screen);
		}

		private void CloseScreens ()
		{
			UiScreen screen = null;
			for (int i = screens.Count - 1; i >= 0; i--)
			{
				screen = screens[i];
				if (screen.IsCached)
				{
					screen.gameObject.SetActive (false);
				}
				else
				{
					screens.RemoveAt (i);
					screen.Destroy ();
				}
				screen = null;
			}
		}

		private void AddPopUp (UiScreen screen)
		{
			PlaceInContent (screen);
			popupStack.Push (screen);
		}

		/// <summary>
		/// Load scene
		/// </summary>
		/// <param name="loadData"></param>
		private void LoadScene (SceneLoadData loadData)
		{
			switch (loadData.screenType)
			{
				case ScreenType.PopUp:
					if (popupStack.Count > 0)
					{
						ClosePopUp ();
					}
					break;
				case ScreenType.Screen:
					break;
			}

			UiScreenSceneLoader.LoadScene (loadData.sceneName, (screen) =>
			{
				LoadData ld = loadData.loadData as LoadData;

				screen.onActive += loadData.onActive;
				screen.onDeactive += loadData.onDeactive;

				switch (loadData.screenType)
				{
					case ScreenType.PopUp:
						AddPopUp (screen);
						break;
					case ScreenType.Screen:
						CloseScreens ();
						AddScreen (screen);
						break;
				}

				UiScreenSceneLoader.UnloadScene (screen.name);

				if (ld.isActive)
				{
					screen.OnInitialize (loadData.loadData);
				}
				else
				{
					screen.gameObject.SetActive (false);
				}
			});
		}

		/// <summary>
		/// Load screen
		/// </summary>
		/// <param name="sceneName"></param>
		/// <param name="additive"></param>
		/// <param name="loadData"></param>
		/// <param name="onActive"></param>
		/// <param name="onDeactive"></param>
		public void LoadScreen (string sceneName, bool additive, LoadData loadData = null, ScreenCallback onActive = null, ScreenCallback onDeactive = null)
		{
			ClosePopUp (() =>
			{
				SceneLoadData screenLoadData = new SceneLoadData ();
				screenLoadData.screenType = ScreenType.Screen;
				screenLoadData.sceneName = sceneName;
				screenLoadData.onActive = onActive;
				screenLoadData.onDeactive = onDeactive;
				screenLoadData.additive = additive;
				screenLoadData.loadData = loadData;
				if (screenLoadData.loadData == null)
				{
					screenLoadData.loadData = new LoadData ();
				}
				LoadScene (screenLoadData);
			});
		}

		/// <summary>
		/// Load an additive screen
		/// </summary>
		/// <param name="sceneName"></param>
		/// <param name="additive"></param>
		/// <param name="loadData"></param>
		/// <param name="onActive"></param>
		/// <param name="onDeactive"></param>
		public void LoadAdditiveScreen (string sceneName, bool additive, LoadData loadData = null, ScreenCallback onActive = null, ScreenCallback onDeactive = null)
		{
			LoadScreen (sceneName, true, loadData, onActive, onDeactive);
		}

		/// <summary>
		/// Load screen
		/// </summary>
		/// <param name="sceneName"></param>
		/// <param name="loadData"></param>
		/// <param name="onActive"></param>
		/// <param name="onDeactive"></param>
		public void LoadScreen (string sceneName, LoadData loadData = null, ScreenCallback onActive = null, ScreenCallback onDeactive = null)
		{
			LoadAdditiveScreen (sceneName, false, loadData, onActive, onDeactive);
		}

		/// <summary>
		/// Load pop up screen
		/// </summary>
		/// <param name="sceneName"></param>
		/// <param name="loadData"></param>
		/// <param name="onActive"></param>
		/// <param name="onDeactive"></param>
		/// <param name="message"></param>
		/// <param name="buttons"></param>
		public void LoadPopUp (string sceneName, LoadData loadData = null, ScreenCallback onActive = null, ScreenCallback onDeactive = null, string message = "", string[] buttons = null)
		{
			SceneLoadData screenLoadData = new SceneLoadData ();
			screenLoadData.screenType = ScreenType.PopUp;
			screenLoadData.sceneName = sceneName;
			screenLoadData.onActive = onActive;
			screenLoadData.onDeactive = onDeactive;
			loadData.extraData.Add ("message", message);
			if (buttons != null && buttons.Length > 0)
			{
				for (int i = 1; i <= buttons.Length; i++)
				{
					loadData.extraData.Add ("button" + i, buttons[i]);
				}
			}
			screenLoadData.loadData = loadData;
			LoadScene (screenLoadData);
		}

		/// <summary>
		/// Close all pop up screens
		/// </summary>
		/// <param name="onComplete"></param>
		public void ClosePopUp (UnityAction onComplete = null)
		{
			lock (popupStack)
			{
				if (popupStack.Count > 0)
				{
					UiScreen screen = popupStack.Pop ();
					screen.onDeactive += (s) =>
					{
						if (onComplete != null) { onComplete (); }
					};
					screen.Destroy ();
				}
				else
				{
					if (onComplete != null)
					{
						onComplete ();
					}
				}
			}
		}
	}
}
