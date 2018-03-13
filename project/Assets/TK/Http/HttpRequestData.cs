using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Text;

namespace TK.Http
{
	[AttributeUsage (AttributeTargets.Field, AllowMultiple = false)]
	public class HttpField : Attribute
	{
		public string CustomName { get; set; }
	}

	public struct HttpBinaryData
	{
		public string filename;
		public byte[] contents;
		public string mimeType;
	}

	/// <summary>
	/// Interface of request data to be sent as request to server.
	/// </summary>
	public interface IHttpRequestData
	{
		string URL { get; }

		Dictionary<string, string> Headers { get; }

		BaseHttpRequester.HttpMethod Method { get; }

		BaseHttpRequester.ResponseType ResponseType { get; }

		Hashtable DataAndHeader { get; }
	}

	public abstract class HttpRequestData : IHttpRequestData
	{
		public abstract string URL
		{
			get;
		}

		public abstract BaseHttpRequester.ResponseType ResponseType
		{
			get;
		}

		public virtual Dictionary<string, string> Headers
		{
			get
			{
				return new Dictionary<string, string> ();
			}
		}

		protected virtual string ContentType { get { return ""; } }

		protected virtual bool SerializeFormInToUrl { get { return false; } }

		private bool IsFormData
		{
			get
			{
				return ContentType != HttpRequestHelper.ContentType.JSON;
			}
		}

		public Hashtable DataAndHeader
		{
			get
			{
				Hashtable ht = new Hashtable ();
				if (IsFormData)
				{
					if (SerializeFormInToUrl)
					{
						ht.Add ("data", SerializeSimpleForm);
						ht.Add ("header", new Dictionary<string, string> () { { "Content-Type", HttpRequestHelper.ContentType.X_WWW_FORM } });
					}
					else
					{
						var form = UploadForm;
						ht.Add ("data", form.data.Length == 0 ? null : form.data);
						ht.Add ("header", form.headers);
					}
				}
				else
				{
					//string s = WWWTranscoder.URLEncode (JsonData, Encoding.UTF8);
					ht.Add ("data", Encoding.UTF8.GetBytes (JsonData));
					ht.Add ("header", new Dictionary<string, string> () { { "Content-Type", HttpRequestHelper.ContentType.JSON } });
				}
				return ht;
			}
		}

		private byte[] SerializeSimpleForm
		{
			get
			{
				FieldInfo[] fields = GetType ()
					.GetFields (BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
					.Where (f => f.GetCustomAttributes (typeof (HttpField), false).Length > 0)
					.ToArray ();

				int fieldCount = fields.Length;

				Dictionary<string, string> formFields = new Dictionary<string, string> ();

				for (int i = 0; i < fieldCount; i++)
				{
					var f = fields[i];

					var attr = (HttpField)f.GetCustomAttributes (typeof (HttpField), false)[0];

					string fieldName = string.IsNullOrEmpty (attr.CustomName) ? f.Name : attr.CustomName;
					formFields.Add (fieldName, f.GetValue (this) == null ? "" : (string)f.GetValue (this));
				}

				return UnityEngine.Networking.UnityWebRequest.SerializeSimpleForm (formFields);
			}
		}

		private WWWForm UploadForm
		{
			get
			{
				FieldInfo[] fields = GetType ()
					.GetFields (BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
					.Where (f => f.GetCustomAttributes (typeof (HttpField), false).Length > 0)
					.ToArray ();

				int fieldCount = fields.Length;

				WWWForm form = new WWWForm ();

				for (int i = 0; i < fieldCount; i++)
				{
					var f = fields[i];

					var attr = (HttpField)f.GetCustomAttributes (typeof (HttpField), false)[0];

					string fieldName = string.IsNullOrEmpty (attr.CustomName) ? f.Name : attr.CustomName;

					if (f.FieldType == typeof (string))
					{
						form.AddField (fieldName, f.GetValue (this) == null ? "" : (string)f.GetValue (this));
					}
					else if (f.FieldType == typeof (float)
						|| f.FieldType == typeof (double)
						|| f.FieldType == typeof (char))
					{
						form.AddField (fieldName, f.GetValue (this).ToString ());
					}
					else if (f.FieldType == typeof (int)
							 || f.FieldType == typeof (long)
							 || f.FieldType == typeof (byte)
							 || f.FieldType == typeof (bool))
					{
						form.AddField (fieldName, Convert.ToInt32 (f.GetValue (this)));
					}
					else if (f.FieldType == typeof (HttpBinaryData))
					{
						HttpBinaryData value = (HttpBinaryData)f.GetValue (this);
						form.AddBinaryData ("file", value.contents, value.filename, value.mimeType);
					}
				}

				return form;
			}
		}

		private string JsonData { get { return JsonUtility.ToJson (this); } }

		public virtual BaseHttpRequester.HttpMethod Method
		{
			get
			{
				return BaseHttpRequester.HttpMethod.Post;
			}
		}
	}
}
