namespace TK.ScreenManagement
{
	using UnityEngine.SceneManagement;
	using System.Collections.Generic;
	using Callback = UnityEngine.Events.UnityAction<Screen>;

	static public class SceneLoader
	{
		static private Dictionary<string, Callback> onLoads = new Dictionary<string, Callback>();
		static private Dictionary<string, Callback> onActives = new Dictionary<string, Callback>();
		static private Dictionary<string, Callback> onDeactives = new Dictionary<string, Callback>();

		static public void NotifyCallback ( Screen screen, Dictionary<string, Callback> callbacks )
		{
			Callback callback = null;

			string name = screen.name;

			callbacks.TryGetValue ( name, out callback );

			if ( callback != null )
			{
				callback ( screen );
			}

			callbacks.Remove ( name );
		}

		static public void NotifyOnLoad ( Screen screen )
		{
			NotifyCallback ( screen, onLoads );
		}

		static public void NotifyOnActive ( Screen screen )
		{
			NotifyCallback ( screen, onActives );
		}

		static public void NotifyOnDeactive ( Screen screen )
		{
			NotifyCallback ( screen, onDeactives );
		}

		static public void SetOnActive ( string sceneName, Callback callback )
		{
			if ( callback == null ) { return; }

			onActives.Add ( sceneName, callback );
		}

		static public void SetOnDeactive ( string sceneName, Callback callback )
		{
			if ( callback == null ) { return; }

			onDeactives.Add ( sceneName, callback );
		}

		static public void Load ( string sceneName, bool additive, Callback onLoad, Callback onActive = null, Callback onDeactive = null )
		{
			onLoads.Add ( sceneName, onLoad );
			SetOnActive ( sceneName, onActive );
			SetOnDeactive ( sceneName, onDeactive );
			SceneManager.LoadScene ( sceneName, additive ? LoadSceneMode.Additive : LoadSceneMode.Single );
		}
	}

}