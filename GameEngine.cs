using System;
using System.Threading;
using System.Xml;
using System.Xml.Linq;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace SHassist {
	internal class GameEngine {
		ShvatkaHttpConnector _shvatkaHttpConnector;
		Timer _timer;

		public delegate void NewPageObtained (Page newPage);
		public event NewPageObtained OnNewPageObtained;

		public int Period { set; get; }

		private List<string> fieldKeysList = new List<string> ();
		private List<string> brainKeysList = new List<string> ();

		public delegate void NewLevel ();
		public event NewLevel OnNewLevel;
		private string _currentLevel;
		public string CurrentLevel {
			set {
				if (value != _currentLevel) {
					_currentLevel = value;
					fieldKeysList = new List<string> ();
					brainKeysList = new List<string> ();
					if (OnNewLevel != null)
						OnNewLevel ();
				}
			}
			get {
				return _currentLevel;
			}
		}

		private Page _actualPage;
		public Page ActualPage{
			set{
				if (value != _actualPage) {
					_actualPage = value;
					CurrentLevel = _actualPage.levelNumber;
					_actualPage.fieldKeys = fieldKeysList.ToArray ();
					_actualPage.brainKeys = brainKeysList.ToArray ();
					if (OnNewPageObtained != null)
						OnNewPageObtained (_actualPage);
				}
			}
			get{ 
				return _actualPage;
			}
		}
			
		public GameEngine (ShvatkaHttpConnector shvatkaHttpConnector) {
			_shvatkaHttpConnector = shvatkaHttpConnector;
			_shvatkaHttpConnector.OnPageObtained += pageHtml => {
				ActualPage = new Page(pageHtml);
			};
//			_shvatkaHttpConnector.GetPageAsync ("SHПОЛЕВОЙ", "SHМОЗГОВОЙ");
//			_timer = new Timer (_shvatkaHttpConnector.GetPage, null, 0, Period);
		}

		/// <summary>
		/// Acks the page.
		/// При необходимости добавляет ключи в списки автодополнения и запрашивает страницу
		/// </summary>
		/// <param name="fieldKey">Field key.</param>
		/// <param name="brainKey">Brain key.</param>
		public void AckPage(string fieldKey, string brainKey){
			if (!string.IsNullOrEmpty (fieldKey))
				if (!fieldKeysList.Contains (fieldKey))
					fieldKeysList.Add (fieldKey);
			if (!string.IsNullOrEmpty (brainKey))
				if (!brainKeysList.Contains (brainKey))
					brainKeysList.Add (brainKey);
			_shvatkaHttpConnector.GetPageAsync (fieldKey, brainKey);
		}

	}

	public class Page{
		const string brainKeyStart = "Мозговой ключ: <input type=text name=\"b_keyw\" SIZE=50 value=\"";
		const string brainKeyEnd = "\">";

		public Page (string pageHtml) {
			{
				Regex redundant = new Regex ("<div id=\"userlinks\">[\\s\\S]*?</div>");
				pageHtml = redundant.Replace (pageHtml, string.Empty);
			}
			{
				Regex redundant = new Regex ("<center>(?!(?![\\s\\S]*?</center>)[\\s\\S]*?<center>)[\\s\\S]*?Название игры[\\s\\S]*?</center>");
				pageHtml = redundant.Replace (pageHtml, string.Empty);
			}
			{
				Regex redundant = new Regex ("<center>(?!(?![\\s\\S]*?</center>)[\\s\\S]*?<center>)[\\s\\S]*?Схватка с оформлением[\\s\\S]*?</center>");
				pageHtml = redundant.Replace (pageHtml, string.Empty);
			}

			string levelNumPattern = @"<b>Уровень \d+";
			levelNumber = Regex.Match(pageHtml, levelNumPattern, RegexOptions.IgnoreCase).Value.Replace("<b>Уровень ", string.Empty);

			string formPattern = "<Form[\\s\\S]*?</form>";
			string formString = Regex.Match (pageHtml, formPattern, RegexOptions.IgnoreCase).Value;
			if (formString.Contains ("Мозговой ключ")) {
				hasBrainKey = true;
				brainKey = Regex.Match (formString, brainKeyStart + "[\\s\\S]*?" + brainKeyEnd, RegexOptions.IgnoreCase).Value;
				brainKey = brainKey.Replace (brainKeyStart, string.Empty);
				brainKey = brainKey.Replace (brainKeyEnd, string.Empty);
				if (brainKey.Length > 0) {
					brainKeyCorrect = true;
				} else {
					brainKeyCorrect = false;
				}
			} else {
				hasBrainKey = false;
			}

			html = pageHtml.Replace(formString, string.Empty);
		}

		public string[] fieldKeys { set; get; }
		public string[] brainKeys { set; get; }

		public string levelNumber { private set; get; }
		public string html { private set; get; }
		public bool hasBrainKey { private set; get; }     //Отображать поле ввода мозгового ключа
		public string brainKey { private set; get; }      //Сам мозговой ключ
		public bool brainKeyCorrect { private set; get; } //Был введён правильный мозговой ключ
		public bool hasButton{ private set; get; }
	}
}

