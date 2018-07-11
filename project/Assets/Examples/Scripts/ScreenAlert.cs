using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TK.ScreenDirectors;

public class ScreenAlert : TK.ScreenDirectors.Screen
{
	static public void Open ()
	{
		ScreenDirector.LoadScreen ( "Alert", null );
	}

	public void OnOk ()
	{
		ScreenDirector.CloseScreen (this, true);
	}
}
