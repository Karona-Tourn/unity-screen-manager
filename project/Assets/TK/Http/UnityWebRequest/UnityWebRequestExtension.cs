using UnityEngine.Networking;

namespace TK.Http
{
	public static class UnityWebRequestExtension
	{
		public static bool IsSuccess (this UnityWebRequest request)
		{
			return request.responseCode == 200;
		}

		public static bool IsBadRequest (this UnityWebRequest request)
		{
			return request.responseCode == 400;
		}

		public static bool IsNotAuthenticated (this UnityWebRequest request)
		{
			return request.responseCode == 401;
		}

		public static bool IsNotAuthorized (this UnityWebRequest request)
		{
			return request.responseCode == 403;
		}

		public static bool IsNotFound (this UnityWebRequest request)
		{
			return request.responseCode == 404;
		}

		public static bool IsValidationFailed (this UnityWebRequest request)
		{
			return request.responseCode == 422;
		}

		public static bool IsInternalServerError (this UnityWebRequest request)
		{
			return request.responseCode == 500;
		}
	}

}
