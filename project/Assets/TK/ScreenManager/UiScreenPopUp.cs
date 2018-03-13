using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TK.ScreenManagement
{
	public class UiScreenPopUp : UiScreen
	{
		[SerializeField]
		private Text textMessage = null;

		public void SetMessage (string message)
		{
			textMessage.text = message;
		}
	}
}
