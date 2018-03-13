namespace TK.ScreenManagement
{
	public enum ScreenType
	{
		PopUp,
		Screen
	}

	public class SceneLoadData
	{
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
		public object loadData = null;

		/// <summary>
		/// Called when popup screen is active
		/// </summary>
		public ScreenCallback onActive = null;

		/// <summary>
		/// Called when popup screen is deactive
		/// </summary>
		public ScreenCallback onDeactive = null;
	}
}
