using System.Collections;

namespace TK.ScreenManagement
{
	public class LoadData
	{
		/// <summary>
		/// Set the screen to be active or deactive when loaded
		/// </summary>
		public bool isActive = true;

		/// <summary>
		/// If true, the loaded screen will be cached
		/// </summary>
		public bool isCached = false;

		/// <summary>
		/// Extra data
		/// </summary>
		public Hashtable extraData = new Hashtable ();
	}
}
