using UnityEngine;

namespace TK.ScreenManagement
{
	public class Screen : MonoBehaviour
	{
		public string Name { get { return name; } }

		protected virtual void Awake ()
		{
			SceneLoader.NotifyOnLoad ( this );
		}

		public virtual void OnInitialize ( object data )
		{
		}

		public virtual void OnStart ()
		{
		}

		public virtual void OnBackPressed ()
		{
		}

		public virtual void OnClosing ()
		{
		}

		public virtual void OnUniqueUpdate ()
		{
		}

		public void Activate ()
		{
			if ( !gameObject.activeSelf ) gameObject.SetActive ( true );

			SceneLoader.NotifyOnActive ( this );
		}

		public void Deactivate ()
		{
			SceneLoader.NotifyOnDeactive ( this );

			if ( gameObject.activeSelf ) gameObject.SetActive ( false );
		}

		public void Destroy ()
		{
			SceneLoader.NotifyOnDeactive ( this );
			Destroy ( gameObject );
		}
	}
}
