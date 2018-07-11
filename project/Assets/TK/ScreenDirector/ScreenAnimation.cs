using UnityEngine;

namespace TK.ScreenDirectors
{
	public abstract class ScreenAnimation : MonoBehaviour
	{
		public abstract void Prepare ();

		public abstract void Play ();

		public abstract bool IsEnded ();
	}
}
