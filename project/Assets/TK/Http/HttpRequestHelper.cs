using System.Text.RegularExpressions;
using UnityEngine;

namespace TK.Http
{

	public static class HttpRequestHelper
	{
		public static class MimeType
		{
			private const string IMAGE = "image/";
			public const string IMAGE_PNG = IMAGE + "png";
			public const string IMAGE_JPG = IMAGE + "jpg";
			public const string IMAGE_GIF = IMAGE + "gif";
		}

		public static class ContentType
		{
			public const string JSON = "application/json";
			public const string PLAIN_TEXT = "text/plain";
			public const string HTML = "text/html";
			public const string X_WWW_FORM = "application/x-www-form-urlencoded";
			public const string MULTI_PART = "multipart/form-data";
		}

		public static string Latitude { get { return Input.location.lastData.latitude.ToString (); } }

		public static string Longitude { get { return Input.location.lastData.longitude.ToString (); } }

		public static string AppVersion { get { return Application.version; } }

		public static string DeviceName { get { return Regex.Replace (SystemInfo.deviceName, "[^\\w\\._]", " "); } }

        public static string UDID { get { return Regex.Replace (SystemInfo.deviceUniqueIdentifier, "[^\\w\\._]", "-"); } }

		public static string Platform { get { return Regex.Replace (Application.platform.ToString (), "[^\\w\\._]", " "); } }

		public static string Model { get { return Regex.Replace (SystemInfo.deviceModel, "[^\\w\\._]", " "); } }

		public static string OSVersion { get { return Regex.Replace (SystemInfo.operatingSystem, "[^\\w\\._]", " "); } }
	}

}
