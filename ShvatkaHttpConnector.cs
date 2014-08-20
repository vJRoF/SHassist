#define LOCAL_DEBUG

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Web;



namespace SHassist
{
	internal class ShvatkaHttpConnector {
		public delegate void PageObtained(string newPage);
		public event PageObtained OnPageObtained;

		#if !LOCAL_DEBUG
		Uri baseUri = new Uri("http://www.shvatka.ru");
		const string targetPageRelative = "index.php?act=module&module=shvatka&lofver=1";
		bool useProxy = false;
		#else
		Uri baseUri = new Uri("http://shtest.somee.com/");
		const string targetPageRelative = "index.php?act=module&module=shvatka&lofver=1";
		bool useProxy = false;
		#endif

		int timeout = 5000;

		public bool HasCookies {
			get {
				return _cookieManager.HasCookies;
			}
		}
		CookieManager _cookieManager = new CookieManager();

		private long _actualCreationTime;
		//ManualResetEvent allDone = new ManualResetEvent();

		const string testPageRelative = "index.php?showforum=88";


		public bool Login(string login, string password) {
			HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(new Uri(baseUri, "index.php"));
			webRequest.Method = "POST";
			webRequest.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8";
			webRequest.ContentType = "application/x-www-form-urlencoded";
			webRequest.Timeout = timeout;
			if(useProxy)
				webRequest.Proxy = new WebProxy (new Uri ("http://10.0.2.2:8888/"));
			webRequest.AllowAutoRedirect = false;
			string query =
				string.Format("CookieDate=1&act=Login&CODE=01&s=5aa979b9a58ad0c96c820e979e9aef80&referer=&UserName={0}&PassWord={1}&submit=%C2%F5%EE%E4", login, password);
			byte[] queryBytes = Encoding.GetEncoding (1251).GetBytes (query);
			webRequest.GetRequestStream ().Write (queryBytes, 0, queryBytes.Length);
			HttpWebResponse webResponse = (HttpWebResponse)webRequest.GetResponse();
			var set_cookie = webResponse.Headers.GetValues ("Set-Cookie").Where(value => value.Contains("shvatkamember_id") || value.Contains("shvatkapass_hash")).Select(cookie => cookie.Split(' ')[0]);
			if (set_cookie.Count () == 2) {
				_cookieManager.Add (set_cookie);
				return true;
			} else
				return false;
		}

		private HttpWebRequest CreateRequest() {
			HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(new Uri(baseUri, targetPageRelative));
			webRequest.Timeout = timeout;
			webRequest.Method = "GET";
			if(useProxy)
				webRequest.Proxy = new WebProxy (new Uri ("http://10.0.2.2:8888/"));
			webRequest.AllowAutoRedirect = true;
			return webRequest;
		}

		private HttpWebRequest CreateRequest(string body) {
			HttpWebRequest webRequest = CreateRequest ();
			webRequest.Method = "POST";
			webRequest.ContentType = "application/x-www-form-urlencoded";

			byte[] bodyBytes = Encoding.GetEncoding (1251).GetBytes (body);
			webRequest.GetRequestStream ().Write (bodyBytes, 0, bodyBytes.Length);

			return webRequest;
		}

//		public string GetPage() {
//			HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(new Uri(baseUri, targetPageRelative));
//			webRequest.Headers.Add(HttpRequestHeader.Cookie, _cookieManager.Get("shvatkamember_id") + " " + _cookieManager.Get("shvatkapass_hash"));
//			if(useProxy)
//				webRequest.Proxy = new WebProxy (new Uri ("http://10.0.2.2:8888/"));
//			HttpWebResponse webResponse = (HttpWebResponse)webRequest.GetResponse();
//			string response;
//			using (StreamReader sr = new StreamReader(webResponse.GetResponseStream(), Encoding.GetEncoding(1251))) {
//				response = sr.ReadToEnd ();
//			}
//			return response;
//		}

		public void GetPageAsync(string fieldKey, string brainKey){
			HttpWebRequest webRequest;
			if (string.IsNullOrEmpty (fieldKey) && string.IsNullOrEmpty (brainKey))
				webRequest = CreateRequest ();
			else {
				fieldKey = HttpUtility.UrlEncode (fieldKey, Encoding.GetEncoding(1251));
				brainKey = HttpUtility.UrlEncode (brainKey, Encoding.GetEncoding(1251));
				webRequest = CreateRequest (string.Format ("act=module&module=shvatka&cmd=sh&lofver=1&keyw={0}&b_keyw={1}", fieldKey, brainKey));
			}
			RequestState requestState = new RequestState() { Request = webRequest };
			webRequest.BeginGetResponse(RespCallback, requestState);
		}

		void RespCallback (IAsyncResult ar)	{
			RequestState requestState = (RequestState) ar.AsyncState;
			if(requestState.CreationTime < _actualCreationTime)
				return;
			using ( HttpWebResponse webResponse = (HttpWebResponse) requestState.Request.EndGetResponse(ar)){
				using (StreamReader sr = new StreamReader(webResponse.GetResponseStream(), Encoding.GetEncoding(1251))) {
					string pageHtml = sr.ReadToEnd();
					if (OnPageObtained != null) {
						OnPageObtained(pageHtml);
					}
					_actualCreationTime = requestState.CreationTime;
				}
			}
		}

		private class RequestState {
			public readonly long CreationTime;
			public HttpWebRequest Request;

			public RequestState() {
				CreationTime = DateTime.Now.Ticks;
			}
		}
	}

	class CookieManager {
		private readonly HashSet<string> _cookies;

		public bool HasCookies {
			get {
				return CookieManagerHelper.StoredCookiesExist;
			}
		}

		public CookieManager() {
			_cookies = new HashSet<string>();
			if (CookieManagerHelper.StoredCookiesExist) {
				foreach (string cookie in CookieManagerHelper.GetStored()) {
					_cookies.Add(cookie);
				}
			}
		}

		public void Add(string cookie) {
			_cookies.Add(cookie);
			this.Save();
		}

		public void Add(IEnumerable<string> cookies) {
			foreach (string cookie in cookies) {
				_cookies.Add(cookie);
			}
			this.Save();
		}

		private void Save() {
			CookieManagerHelper.Save(_cookies);
		}

		public string Get(string part) {
			return _cookies.Single(cookie => cookie.Contains(part));
		}
	}

	static class CookieManagerHelper{
		public static bool StoredCookiesExist {
			get { return File.Exists(Filename); }
		}

		private const string Filename = "cookies";
		private static string Filepath {
			get{
				string currentDirectory = System.Environment.GetFolderPath (Environment.SpecialFolder.Personal);
				return Path.Combine (currentDirectory, Filename);
			}
		}

		public static IEnumerable<string> GetStored() {
			return File.ReadAllLines(Filepath);
		}

		public static void Save(IEnumerable<string> cookies) {
			File.WriteAllLines(Filepath, cookies);
		}
	}
}

