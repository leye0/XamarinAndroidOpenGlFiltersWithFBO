using Java.IO;
using Android.Graphics;
using Android.Hardware;
using Android.Opengl;
using Camera = Android.Hardware.Camera;
using Matrix = Android.Opengl.Matrix;
using Java.Lang;

namespace FboTextures {
	
	public class CameraHelper {

		// Current Camera instance.
		public Camera MyCamera;
		// Current Camera Id.
		private int _cameraId;
		// Current Camera CameraInfo.
		private Camera.CameraInfo _cameraInfo = new Camera.CameraInfo();
		// SharedData instance.
		private DataContainer _sharedData;
		// Surface texture instance.
		private SurfaceTexture _surfaceTexture;

		public int GetOrientation() {
				
			if (_cameraInfo == null || _sharedData == null) {
				return 0;
			}
			if (_cameraInfo.Facing == CameraFacing.Front) {
				return (_cameraInfo.Orientation - _sharedData._orientationDevice + 360) % 360;
			} else {
				return (_cameraInfo.Orientation + _sharedData._orientationDevice) % 360;
			}
		}

		public bool IsCameraFront() {
			return _cameraInfo.Facing == CameraFacing.Front;
		}

		/**
		 * Must be called from Activity.onPause(). Stops preview and releases Camera
		 * instance.
		 */
		public void OnPause() {
			_surfaceTexture = null;
			ResetCamera ();
		}

		private bool CameraEnabled = true;

		private void ResetCamera() {
			if (CameraEnabled) {
				if (MyCamera != null) {
					MyCamera.StopPreview();
					MyCamera.Release ();
					MyCamera = null;
				}
			}
		}

		/**
		 * Should be called from Activity.onResume(). Recreates Camera instance.
		 */
		public void OnResume() {
			OpenCamera();
		}

		public void OpenCamera() {
			if (MyCamera != null) {
				MyCamera.StopPreview ();
				MyCamera.Release ();
				MyCamera = null;
			}

			if (_cameraId >= 0) {
				
				Camera.GetCameraInfo (_cameraId, _cameraInfo);

				MyCamera = Camera.Open (_cameraId);

				Camera.Parameters param = MyCamera.GetParameters ();
				param.SetRotation (0);

				MyCamera.SetParameters (param);

				try {
					if (_surfaceTexture != null) {
						
						MyCamera.SetPreviewTexture (_surfaceTexture);
						MyCamera.StartPreview ();
					}
				} catch (Exception) {
				}
			}

			UpdateRotation ();
		}

	/**
	 * Selects either front-facing or back-facing camera.
	 */
	public void SetCameraFront(bool frontFacing) {
			var facing = (int)(frontFacing ? CameraFacing.Front : CameraFacing.Back);

			_cameraId = -1;
			var numberOfCameras = Camera.NumberOfCameras;
			for (var i = 0; i < numberOfCameras; ++i) {
				Camera.GetCameraInfo (i, _cameraInfo);
				if ((int)_cameraInfo.Facing == facing) {
					_cameraId = i;
					break;
				}
			}

			OpenCamera ();
		}

	/**
	 * Simply forwards call to Camera.setPreviewTexture.
	 */
	public void SetPreviewTexture(SurfaceTexture surfaceTexture) {
		_surfaceTexture = surfaceTexture;
		
		MyCamera.SetPreviewTexture(surfaceTexture);
	}

	/**
	 * Setter for storing shared data.
	 */
	public void SetSharedData(DataContainer sharedData) {
		_sharedData = sharedData;
	}

	/**
	 * Simply forwards call to Camera.startPreview.
	 */
	public void StartPreview() {
		MyCamera.StartPreview();
	}

	/**
	 * Simply forwards call to Camera.stopPreview.
	 */
	public void StopPreview() {
			if (MyCamera != null) {
				MyCamera.StopPreview ();	
			}
		}

	/**
	 * Handles picture taking callbacks etc etc.
	 */
	public void TakePicture(Observer observer) {
		MyCamera.AutoFocus(new CameraObserver(observer));
	}

	/**
	 * Updated rotation matrix, aspect ratio etc.
	 */
	public void UpdateRotation() {
			if (MyCamera == null || _sharedData == null) {
				return;
			}

			var orientation = _cameraInfo.Orientation;
			Matrix.SetRotateM (_sharedData._orientationM, 0, orientation, 0f, 0f, 1f);

			Camera.Size size = MyCamera.GetParameters ().PreviewSize;
		
			if (orientation % 90 == 0) {
				var w = size.Width;
				size.Width = size.Height;
				size.Height = w;
			}

			_sharedData._aspectRatioPreview [0] = (float)Math.Min (size.Width,
				size.Height) / size.Width;
			_sharedData._aspectRatioPreview [1] = (float)Math.Min (size.Width,
				size.Height) / size.Height;
		}

	/**
	 * Class for implementing Camera related callbacks.
	 */
		public class CameraObserver : Java.Lang.Object, Camera.IShutterCallback, Camera.IAutoFocusCallback, Camera.IPictureCallback {

			private Camera _camera;

			public CameraObserver(Camera camera) {
				_camera = camera;
			}

			Observer mObserver;

			public CameraObserver(Observer observer) {
				mObserver = observer;
			}

			public void OnAutoFocus(bool success, Camera camera) {
				mObserver.OnAutoFocus (success);
				_camera.TakePicture (this, null, this);
			}

			public void OnPictureTaken(byte[] data, Camera camera) {
				mObserver.OnPictureTaken (data);
			}

			public void OnShutter() {
				mObserver.OnShutter ();
			}
		}

		/**
		 * Interface for observing picture taking process.
		 */
		public interface Observer {

			/**
			 * Called once auto focus is done.
			 */
			void OnAutoFocus(bool success);

			/**
			 * Called once picture has been taken.
			 */
			void OnPictureTaken(byte[] jpeg);

			/**
			 * Called to notify about shutter event.
			 */
			void OnShutter();
		}
	}
}