using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace TK.ScreenDirectors
{
	public class Screen : MonoBehaviour
	{
		[SerializeField]
		private ScreenAnimation _animateIn = null;

		[SerializeField]
		private ScreenAnimation _animateOut = null;

		[SerializeField]
		private ScreenDirector.PriorityLayer _layer = default(ScreenDirector.PriorityLayer);

		private RectTransform _rectTransform = null;

		public ScreenDirector.PriorityLayer Layer
		{
			get
			{
				return _layer;
			}
		}

		public RectTransform rectTransform
		{
			get
			{
				if ( _rectTransform == null ) _rectTransform = GetComponent<RectTransform> ();
				return _rectTransform;
			}
		}

		protected virtual void Awake ()
		{
			SceneLoader.NotifyOnLoad ( this );
		}

		public virtual void OnCreated (object obj)
		{
		}

		public virtual void OnStart ( object obj )
		{
		}

		public virtual void OnFocus ()
		{
		}

		public virtual void OnReady ()
		{
		}

		public virtual void OnUpdate ()
		{
		}

		public virtual void OnKeyBackPressed ()
		{
		}

		public virtual void OnLostFocus ()
		{
		}

		private IEnumerator Coro_WaitAnimateIn ( UnityAction onFinish )
		{
			yield return new WaitUntil ( () => _animateIn.IsEnded () );
			if ( onFinish != null ) onFinish ();
		}

		private IEnumerator Coro_WaitAnimateOut ( UnityAction onFinish )
		{
			yield return new WaitUntil ( () => _animateOut.IsEnded () );
			if ( onFinish != null ) onFinish ();
		}

		public void Open ( bool quick, UnityAction onFinish = null )
		{
			if ( quick || _animateIn == null )
			{
				gameObject.SetActive ( true );
				if ( onFinish != null ) onFinish ();
			}
			else
			{
				gameObject.SetActive ( true );
				AnimateIn ( onFinish );
			}
		}

		public void Close ( bool quick, UnityAction onFinish = null )
		{
			if ( quick || _animateOut == null )
			{
				gameObject.SetActive ( false );
				if ( onFinish != null ) onFinish ();
			}
			else
			{
				AnimateOut ( () =>
				{
					gameObject.SetActive ( false );
					if ( onFinish != null ) onFinish ();
				} );
			}
		}

		public void PrepareAnimateIn ()
		{
			if ( _animateIn != null )
			{
				_animateIn.Prepare ();
			}
		}

		public void PrepareAnimateOut ()
		{
			if ( _animateOut != null )
			{
				_animateOut.Prepare ();
			}
		}

		public void AnimateIn ( UnityAction onFinish = null )
		{
			if ( _animateIn != null )
			{
				_animateIn.Prepare ();
				_animateIn.Play ();
				StartCoroutine ( Coro_WaitAnimateIn ( onFinish ) );
			}
			else
			{
				if ( onFinish != null ) onFinish ();
			}
		}

		public void AnimateOut ( UnityAction onFinish = null )
		{
			if ( _animateOut != null )
			{
				_animateOut.Prepare ();
				_animateOut.Play ();
				StartCoroutine ( Coro_WaitAnimateOut ( onFinish ) );
			}
			else
			{
				if ( onFinish != null ) onFinish ();
			}
		}

		public void Destroy ()
		{
			Destroy ( gameObject );
		}
	}
}
