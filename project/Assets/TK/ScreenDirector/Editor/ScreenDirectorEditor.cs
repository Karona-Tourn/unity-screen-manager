using UnityEngine;
using UnityEditor;

namespace TK.ScreenDirectors
{

	[CustomEditor(typeof(ScreenDirector))]
	public class ScreenDirectorEditor : Editor
	{
		public override void OnInspectorGUI ()
		{
			base.OnInspectorGUI ();

			EditorGUILayout.BeginVertical (GUI.skin.box);

			EditorGUILayout.LabelField ( "Live Screens" );

			ScreenDirector realTarget = (ScreenDirector)target;
			var liveScreenNames = realTarget.LiveScreenNames;
			for ( int i = 0; i < liveScreenNames.Count; i++ )
			{
				EditorGUILayout.BeginHorizontal (GUI.skin.button);
				EditorGUILayout.LabelField ( liveScreenNames[i] );
				EditorGUILayout.EndHorizontal ();
			}

			EditorGUILayout.EndVertical ();
		}

	}

}