using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TK.ScreenDirectors;

public class ScreenPlay : TK.ScreenDirectors.Screen
{
	static public void Open ()
	{
		ScreenDirector.LoadScreen ( "Play", null, true );
	}

	public override void OnKeyBackPressed ()
	{
		ScreenMain.Open ();
	}
}
