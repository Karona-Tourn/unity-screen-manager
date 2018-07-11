using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TK.ScreenDirectors;

public class ScreenMain : TK.ScreenDirectors.Screen
{
	static public void Open ()
	{
		ScreenDirector.LoadScreen ( "Main", null, true );
	}

	public void OpenAlert ()
	{
		ScreenAlert.Open ();
		ScreenAlert.Open ();
		Invoke ( "OpenPlay", 2f );
	}

	private void OpenPlay ()
	{
		//ScreenPlay.Open ();
		LoadingData loadingData = new LoadingData();
		loadingData.nextScreen = "Play";
		loadingData.isNextScreenCached = true;
		loadingData.coroutines.Add ( Wait () );
		ScreenLoading.Open ( loadingData );
	}

	private static IEnumerator Wait ()
	{
		yield return new WaitForSeconds ( 2 );
	}
}
