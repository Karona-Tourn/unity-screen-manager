using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TK.ScreenManagement
{
	public class UiScreen : MonoBehaviour
	{
		private bool isCached = false;

		public ScreenCallback onActive = null;

		public ScreenCallback onDeactive = null;

		public bool IsCached { get { return isCached; } }

		private void InvokeOnActive ()
		{
			if (onActive != null)
			{
				onActive (this);
			}
		}

		private void InvokeOnDeactive ()
		{
			if (onDeactive != null)
			{
				onDeactive (this);
			}
		}

		private void Awake ()
		{
			UiScreenSceneLoader.InvokeOnScreenLoaded (this);
		}

		public virtual void OnInitialize (object data)
		{
		}

		// Use this for initialization
		protected virtual void Start ()
		{
			InvokeOnActive ();
		}

		protected virtual void OnDisable ()
		{
			InvokeOnDeactive ();
		}

		public void Destroy ()
		{
			Destroy (gameObject);
		}

		public virtual void OnUniqueUpdate ()
		{
		}

		/// <summary>
		/// Invoke when key Esc/Back is pressed on device
		/// </summary>
		public virtual void OnBackPressed ()
		{
		}
	}
}
