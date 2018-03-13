using System.Collections.Generic;
using UnityEngine.SceneManagement;

namespace TK.ScreenManagement
{
	public class UiScreenSceneLoader
	{
		public delegate void OnScreenLoaded (UiScreen screen);

		private static Dictionary<string, OnScreenLoaded> callbacks = new Dictionary<string, OnScreenLoaded> ();

		public static void InvokeOnScreenLoaded (UiScreen screen)
		{
			callbacks[screen.name].Invoke (screen);
		}

		public static void LoadScene (string sceneName, OnScreenLoaded onScreenLoaded, bool single = true)
		{
			callbacks.Add (sceneName, onScreenLoaded);
			SceneManager.LoadScene (sceneName, single ? LoadSceneMode.Single : LoadSceneMode.Additive);
		}

		public static void UnloadScene (string sceneName)
		{
			callbacks.Remove (sceneName);
			SceneManager.UnloadSceneAsync (sceneName);
		}
	}
}
