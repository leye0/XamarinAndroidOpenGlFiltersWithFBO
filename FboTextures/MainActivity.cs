using Java.IO;
using Java.Util;
using Android.Animation;
using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Hardware;
using Android.Media;
using Android.OS;
using Android.Provider;
using Android.Views;
using Android.Widget;
using Java.Lang;
using Orientation = Android.Media.Orientation;
using System.Threading.Tasks;

using System.Collections.Generic;

namespace FboTextures
{
	[Activity (Label = "FboTextures", MainLauncher = true)]
	public class MainActivity : Activity
	{
		private CameraHelper _camera = new CameraHelper();

		private RendererObserver mObserverRenderer;
		private SeekBarObserver _observerSeekBar;
		private ISharedPreferences _preferences;
		private MyGlSurfaceView _renderer;
		private DataContainer _sharedData = new DataContainer();

		public override void OnConfigurationChanged (Android.Content.Res.Configuration newConfig)
		{
			base.OnConfigurationChanged(newConfig);
		}

		private bool CameraEnabled = true;

		protected override void OnCreate (Bundle bundle)
		{
			base.OnCreate (bundle);

			RequestWindowFeature (WindowFeatures.NoTitle);

			Window.AddFlags (WindowManagerFlags.Fullscreen);
			Window.ClearFlags (WindowManagerFlags.ForceNotFullscreen);

			if (CameraEnabled) {
				_camera.SetCameraFront (false);
				_camera.SetSharedData (_sharedData);
			}
				
			SetContentView (Resource.Layout.Main);

			_renderer = (MyGlSurfaceView)FindViewById (Resource.Id.myglsurfaceview);

			_renderer.SetSharedData (_sharedData);

			if (CameraEnabled) {
				mObserverRenderer = new RendererObserver (this, _camera);
			}
			_renderer.SetObserver (mObserverRenderer);

			View menu = FindViewById (Resource.Id.menu);

			menu.Visibility = ViewStates.Gone;

			FindViewById (Resource.Id.button_menu).Click += button_clicked;

			_preferences = GetPreferences (FileCreationMode.Private);

			_sharedData._filter = _preferences.GetInt (
				GetString (Resource.String.key_filter), 0);

			var SeekBars = new List<SeekBarInfo>() {
				new SeekBarInfo() {
					ViewId = Resource.Id.seekbar_brightness, 
					StringKey = Resource.String.key_brightness, 
					DefaultValue = 5 
				},
				new SeekBarInfo() {
					ViewId = Resource.Id.seekbar_contrast, 
					StringKey = Resource.String.key_contrast, 
					DefaultValue = 5 
				},
				new SeekBarInfo() {
					ViewId = Resource.Id.seekbar_saturation, 
					StringKey = Resource.String.key_saturation, 
					DefaultValue = 8 
				},
				new SeekBarInfo() {
					ViewId = Resource.Id.seekbar_corner_radius, 
					StringKey = Resource.String.key_corner_radius, 
					DefaultValue = 3 
				}
			};
			// Set SeekBar OnSeekBarChangeListeners and default progress.
			foreach (var seekBar in SeekBars) {
				var seekBarView = (SeekBar)FindViewById (seekBar.ViewId);
				Resource r = new Resource ();
				_observerSeekBar = new SeekBarObserver (_preferences, this, _sharedData, _renderer); 
				seekBarView.SetOnSeekBarChangeListener (_observerSeekBar);
				seekBarView.Progress = _preferences.GetInt (GetString (seekBar.StringKey), seekBar.DefaultValue);

				// SeekBar.setProgress triggers observer only in case its value
				// changes. And we're relying on this trigger to happen.
				if (seekBarView.Progress == 0) {
					seekBarView.Progress = 1;
					seekBarView.Progress = 0;
				}
			}
		}

		public class SeekBarInfo {
			public int ViewId { get; set;}
			public int StringKey {get;set;}
			public int DefaultValue {get;set;}
		}

		void button_clicked(object sender, System.EventArgs e) {
			var v = ((View)sender);
			switch (v.Id) {

			case Resource.Id.button_menu:
				var view = FindViewById (Resource.Id.menu);
				SetMenuVisible (view.Visibility != ViewStates.Visible);
				break;
			}
		}

		protected override void OnPause() {
			base.OnPause();
			_camera.OnPause();
			_renderer.OnPause();
		}

		protected override void OnResume() {
			base.OnResume();
			_camera.OnResume();
			_renderer.OnResume();
		}

		private void SetCameraFront(bool front) {
			_camera.SetCameraFront(front);				
		}

		private void SetMenuVisible(bool visible) {
			var menu = FindViewById(Resource.Id.menu);
			var button = FindViewById(Resource.Id.button_menu);

			if (visible) {
				menu.Visibility = ViewStates.Visible;
			} else {
				FindViewById(Resource.Id.menu).Visibility = ViewStates.Gone;
			}
		}

		/**
	 * Class for implementing Camera related callbacks.
	 */
		private class CameraObserver : Java.Lang.Object, CameraHelper.Observer {

			private Context _context;
			private DataContainer _sharedData;

			public CameraObserver(Context context, DataContainer sharedData) {
				_context = context;
				_sharedData = sharedData;
			}

			public void OnAutoFocus(bool success) {
				// If auto focus failed show brief notification about it.
				if (!success) {
					Toast.MakeText(_context, "Problem with focus", ToastLength.Short).Show();
				}
			}

			public void OnPictureTaken(byte[] data) {
			}


			public void OnShutter() {
			}

		}

		private class RendererObserver : Java.Lang.Object, MyGlSurfaceView.Observer {

			private CameraHelper _camera;
			private Context _context;
			private bool CameraEnabled = true;

			public RendererObserver(Context context, CameraHelper camera) {
				_context = context;
				_camera = camera;
			}

			public void OnSurfaceTextureCreated(SurfaceTexture surfaceTexture) {
				// Once we have SurfaceTexture try setting it to Camera.
				try {
					if (CameraEnabled) {
						
						if (_camera.MyCamera == null) 
						{
							throw new Exception("Duhhh. This sucks.");
						}

						_camera.StopPreview();
						_camera.SetPreviewTexture(surfaceTexture);	

						if (A(_context).FindViewById(Resource.Id.buttons_shoot).Visibility == ViewStates.Visible)
						{
							_camera.StartPreview();
						}
					}

					// TODO: Media player texture here


				} catch (Exception ex) {
					A (_context).RunOnUiThread (new System.Action (() => {
						Toast.MakeText (_context, ex.Message,
							ToastLength.Long).Show ();
					}));
				}
			}
		}


		/**
	 * Class for implementing SeekBar related callbacks.
	 */
		private class SeekBarObserver : Java.Lang.Object, SeekBar.IOnSeekBarChangeListener {
			
			private Context _context;
			private ISharedPreferences _preferences;
			private DataContainer _sharedData;
			private MyGlSurfaceView _renderer;

			public SeekBarObserver(ISharedPreferences preferences, Context context, DataContainer sharedData, MyGlSurfaceView renderer) {
				_context = context;
				_preferences = preferences;
				_sharedData = sharedData;
				_renderer = renderer;
			}

			public void OnProgressChanged(SeekBar seekBar, int progress,
				bool fromUser) {

				switch (seekBar.Id) {
				// On brightness recalculate shared value and update preferences.
				case Resource.Id.seekbar_brightness: {
						_preferences.Edit()
							.PutInt(_context.Resources.GetString(Resource.String.key_brightness), progress)
							.Commit();
						_sharedData._brightness = (progress - 5f) / 10f;

						TextView textView = (TextView) A(_context).FindViewById(Resource.Id.text_brightness);
						textView.SetText(_context.GetString(Resource.String.seekbar_brightness,
							progress - 5), TextView.BufferType.Normal);
						break;
					}

				case Resource.Id.seekbar_contrast: {
						_preferences.Edit ()
							.PutInt (_context.Resources.GetString (Resource.String.key_contrast), progress)
							.Commit ();
						_sharedData._contrast = (progress - 5) / 10f;
						TextView textView = (TextView) A(_context).FindViewById(Resource.Id.text_contrast);
						textView.SetText (_context.GetString (Resource.String.seekbar_contrast,
							progress - 5), TextView.BufferType.Normal);
						break;
					}

				case Resource.Id.seekbar_saturation: {
						_preferences.Edit()
							.PutInt(_context.GetString(Resource.String.key_saturation), progress)
							.Commit();
						_sharedData._saturation = (progress - 5) / 10f;
						TextView textView = (TextView) A(_context).FindViewById(Resource.Id.text_saturation);
						textView.SetText(_context.Resources.GetString(Resource.String.seekbar_saturation,
							progress - 5), TextView.BufferType.Normal);
						break;
					}

				case Resource.Id.seekbar_corner_radius: {
						_preferences
							.Edit()
							.PutInt(_context.Resources.GetString(Resource.String.key_corner_radius), progress)
							.Commit();
						_sharedData._cornerRadius = progress / 10f;
						TextView textView = (TextView) A(_context).FindViewById(Resource.Id.text_corner_radius);
						textView.SetText(_context.Resources.GetString(Resource.String.seekbar_corner_radius, -progress), TextView.BufferType.Normal);
						break;
					}
				}
				_renderer.RequestRender();
			}

			public void OnStartTrackingTouch(SeekBar seekBar) {
			}

			public void OnStopTrackingTouch(SeekBar seekBar) {
			}
		}

		public static Activity A(Context c) {
			return c as Activity;
		}
	}
}


