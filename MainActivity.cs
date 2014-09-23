using System;
using System.Net;

using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using Android.Webkit;

namespace SHassist
{
	[Activity (Label = "SHassist", MainLauncher = true, Icon = "@drawable/icon")]
	public class MainActivity : Activity
	{
		ShvatkaHttpConnector shvatkaHttpConnector = new ShvatkaHttpConnector();
		bool hasCookies = false;

		GameEngine gameEngine;
		ViewModel viewModel;

		protected override void OnCreate (Bundle bundle)
		{
			base.OnCreate (bundle);

			// Set our view from the "main" layout resource
			SetContentView (Resource.Layout.Main);


			//TODO: Проверить, что куки доступны, иначе отобразить форму логина

			if (!(shvatkaHttpConnector.HasCookies||hasCookies)) {
				ShowLoginDialog ();
			}


			gameEngine = new GameEngine (shvatkaHttpConnector) { Period = 1000 };
			viewModel = new ViewModel (this);

			viewModel.OnPageAcked += gameEngine.AckPage;
			gameEngine.OnNewPageObtained += viewModel.SetPage;
		}

		void ShowLoginDialog(){
			var customView = LayoutInflater.Inflate (Resource.Layout.LoginDialog, null);
			var builder = new AlertDialog.Builder (this);
			builder.SetView (customView);
			builder.SetPositiveButton (Resource.String.Login, OnLoginClicked);
			builder.SetNegativeButton (Resource.String.CancelLogin, (s, e) => {	this.Finish(); });
			builder.Create ().Show();
		}

		void OnLoginClicked(object sender, DialogClickEventArgs args) {
			AlertDialog dialog = (AlertDialog)sender;
			EditText username = dialog.FindViewById<EditText> (Resource.Id.username);
			EditText password = dialog.FindViewById<EditText> (Resource.Id.password);

			bool serverFailed = false;
			try {
				hasCookies = shvatkaHttpConnector.Login (username.Text, password.Text);
				if (hasCookies)
					Toast.MakeText (this, "Вход выполнен.", ToastLength.Short).Show ();
			}
			catch(WebException){
				Toast.MakeText (this, "Нет связи с сервером.", ToastLength.Short).Show ();
				serverFailed = true;
			}
			finally{
				if (!hasCookies || serverFailed)
					ShowLoginDialog ();
				if (!hasCookies && !serverFailed)
					Toast.MakeText (this, "Логин или пароль не верен.", ToastLength.Short).Show ();
			}
		}
	}
}


