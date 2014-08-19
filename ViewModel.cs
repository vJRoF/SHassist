using System;
using Android.Webkit;
using Android.App;
using Android.Widget;
using Android.Views;
using Android.Views.InputMethods;

namespace SHassist {
	public class ViewModel	{
		Activity mainActivity;
		InputMethodManager inputManager;

		WebView webView;

		AutoCompleteTextView fieldKey;
		ArrayAdapter<string> fieldKeyAdapter;

		AutoCompleteTextView brainKey;
		ArrayAdapter<string> brainKeyAdapter;

		TextView brainKeyOkLabel;

		Button button;

		public delegate void PageAcked(string fieldKey, string brainKey);
		public event PageAcked OnPageAcked;

		public ViewModel (Activity MainActivity)	{
			inputManager = (InputMethodManager) MainActivity.GetSystemService(Activity.InputMethodService);

			mainActivity = MainActivity;

			webView = mainActivity.FindViewById<WebView> (Resource.Id.webview);

			fieldKey = mainActivity.FindViewById<AutoCompleteTextView> (Resource.Id.fieldKey);
			fieldKey.FocusChange += AddSH;

			brainKey = mainActivity.FindViewById<AutoCompleteTextView> (Resource.Id.brainKey);
			brainKey.FocusChange += AddSH;

			brainKeyOkLabel = mainActivity.FindViewById<TextView> (Resource.Id.brainKeyOkLabel);

			button = mainActivity.FindViewById<Button> (Resource.Id.Submit);
			button.Click += OnSubmitClick;
		}

		void OnSubmitClick (object sender, EventArgs e)	{
			if (OnPageAcked != null)
				OnPageAcked (fieldKey.Text, brainKey.Text);
		}

		void AddSH (object sender, EventArgs e) {
			AutoCompleteTextView currentTextField = (AutoCompleteTextView)sender;
			if (currentTextField.Text == string.Empty)
				mainActivity.RunOnUiThread (() => currentTextField.Text = "SH");
			currentTextField.SetSelection (currentTextField.Text.Length);
		}

		public void SetPage(Page newPage){
			try {
				webView.LoadDataWithBaseURL ("http://www.shvatka.ru/",  newPage.html, "text/html", "UTF-8", "<html><body>Load error</body></html>");

				mainActivity.RunOnUiThread(() => fieldKey.Visibility = ViewStates.Visible);
				mainActivity.RunOnUiThread(() => fieldKeyAdapter = new ArrayAdapter<string>(mainActivity, Resource.Layout.dropdown, newPage.fieldKeys));
				mainActivity.RunOnUiThread(() => fieldKey.Adapter = fieldKeyAdapter);
				mainActivity.RunOnUiThread(() => inputManager.HideSoftInputFromWindow(fieldKey.WindowToken, 0));
				mainActivity.RunOnUiThread(() => fieldKey.ClearFocus());
				mainActivity.RunOnUiThread(() => fieldKey.Text = string.Empty);

				if (newPage.hasBrainKey) {
					mainActivity.RunOnUiThread(() => brainKey.Visibility = ViewStates.Visible);
					mainActivity.RunOnUiThread(() => brainKey.ClearFocus());

					if (newPage.brainKeyCorrect) {
						mainActivity.RunOnUiThread(() => brainKeyOkLabel.Visibility = ViewStates.Visible);
						mainActivity.RunOnUiThread(() => brainKey.Enabled = false);
						mainActivity.RunOnUiThread(() => brainKey.Text = newPage.brainKey);
					} else {
						mainActivity.RunOnUiThread(() => brainKeyOkLabel.Visibility = ViewStates.Gone);
						mainActivity.RunOnUiThread(() => brainKey.Enabled = true);
						mainActivity.RunOnUiThread(() => brainKey.Text = string.Empty);

						mainActivity.RunOnUiThread(() => brainKeyAdapter = new ArrayAdapter<string>(mainActivity, Resource.Layout.dropdown, newPage.brainKeys));
						mainActivity.RunOnUiThread(() => brainKey.Adapter = brainKeyAdapter);
					}

				} else {
					mainActivity.RunOnUiThread(() => brainKey.Visibility = ViewStates.Gone);
					mainActivity.RunOnUiThread(() => brainKeyOkLabel.Visibility = ViewStates.Gone);
				}
			}
			catch (Exception ex) {
				System.Diagnostics.Debug.Print (ex.Message);
			}
		}
	}
}

public class NotificationUtils {
	//private static const string TAG = NotificationUtils.
	private static NotificationUtils instance;
}

