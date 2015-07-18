using Java.IO;
using Java.Nio;
using Javax.Microedition.Khronos.Egl;
using Javax.Microedition.Khronos.Opengles;
using Android.Content;
using Android.Graphics;
using Android.Opengl;
using Android.OS;
using Android.Util;
using Android.Widget;

using Java.Lang;

namespace FboTextures {

	// GlSurfaceView renderer with surfacetexture

	public class MyGlSurfaceView : GLSurfaceView, GLSurfaceView.IRenderer, SurfaceTexture.IOnFrameAvailableListener {
		
		private float[] _aspectRatio = new float[2];

		// External OES texture holder, camera preview that is. (Source)
		private FboHelper _fboExternal = new FboHelper(); 

		// Offscreen texture holder for storing camera preview. (Hold FBO Copy)
		private FboHelper _fboOffscreen = new FboHelper();

		// Full view quad vertices.
		private FloatBuffer _fullQuadVertices;

		// Renderer observer.
		private Observer _observer;

		// Shader for copying preview texture into offscreen one.
		private ShaderHelper _shaderCopyOes = new ShaderHelper();

		// Shared data instance.
		private DataContainer _sharedData;

		// One and only SurfaceTexture instance.
		private SurfaceTexture _surfaceTexture;

		// Flag for indicating SurfaceTexture has been updated.
		private bool _surfaceTextureUpdate;

		// SurfaceTexture transform matrix.
		private float[] _transformM = new float[16];

		// View width and height.
		private int _width, _height;

		private ShaderHelper _shaderFilterDefault = new ShaderHelper();

		public MyGlSurfaceView(Context context) : base(context){
			Init();
		}

		public MyGlSurfaceView (Context context, IAttributeSet attrs) : base(context, attrs) {
			Init();
		}


		public MyGlSurfaceView(Context context, IAttributeSet attrs, int defStyle) : base(context, attrs) {
			Init ();
		}

		private void Init() {

			var fullQuadCoords = new float[] { -1, 1, -1, -1, 1, 1, 1, -1 };

			_fullQuadVertices = ByteBuffer.AllocateDirect(fullQuadCoords.Length * 4)
				.Order(ByteOrder.NativeOrder()).AsFloatBuffer();
			
			_fullQuadVertices.Put (fullQuadCoords).Position (0);

			PreserveEGLContextOnPause = true;
			SetEGLContextClientVersion(2);
			SetRenderer(this);
			RenderMode = Android.Opengl.Rendermode.WhenDirty;
		}

		private string LoadRawString(int rawId)  {
			var iStream = Resources.OpenRawResource (rawId);
			var baos = new ByteArrayOutputStream();
			var buf = new byte[1024];
			int oneByte;

			while ((oneByte = iStream.ReadByte()) != -1) {
				baos.Write(oneByte);
			}
			return baos.ToString();
		}

		public void OnDrawFrame(IGL10 unused) {

			// Clear view.
			GLES20.GlClearColor(0.5f, 0.5f, 0.5f, 1.0f);
			GLES20.GlClear(GLES20.GlColorBufferBit);

			// If we have new preview texture.
			if (_surfaceTextureUpdate) {
				// Update surface texture.
				_surfaceTexture.UpdateTexImage ();
				// Update texture transform matrix.
				_surfaceTexture.GetTransformMatrix (_transformM);
				_surfaceTextureUpdate = false;

				// Bind offscreen texture into use.
				_fboOffscreen.Bind ();
				_fboOffscreen.BindTexture (0);

				// Take copy shader into use.
				_shaderCopyOes.UseProgram ();

				// Uniform variables.
				var uOrientationM = _shaderCopyOes.GetHandle ("uOrientationM");
				var uTransformM = _shaderCopyOes.GetHandle ("uTransformM");

				// We're about to transform external texture here already.
				GLES20.GlUniformMatrix4fv (uOrientationM, 1, false,
					_sharedData._orientationM, 0);
				GLES20.GlUniformMatrix4fv (uTransformM, 1, false, _transformM, 0);

				// We're using external OES texture as source.
				GLES20.GlActiveTexture (GLES20.GlTexture0);
				//GLES20.GlBindTexture(GLES20.GlTexture2d, mFboExternal.getTexture(0));

				// Trigger actual rendering.
				renderQuad (_shaderCopyOes.GetHandle ("aPosition"));
			} else {
				System.Diagnostics.Debug.WriteLine ("OK");
			}
			 
			// Bind screen buffer into use.
			GLES20.GlBindFramebuffer(GLES20.GlFramebuffer, 0);
			GLES20.GlViewport(0, 0, _width, _height);

			// Take filter shader into use.
			_shaderFilterDefault.UseProgram();

			// Uniform variables.
			var uBrightness = _shaderFilterDefault.GetHandle("uBrightness");
			var uContrast = _shaderFilterDefault.GetHandle("uContrast");
			var uSaturation = _shaderFilterDefault.GetHandle("uSaturation");
			var uCornerRadius = _shaderFilterDefault.GetHandle("uCornerRadius");

			var uAspectRatio = _shaderFilterDefault.GetHandle("uAspectRatio");
			var uAspectRatioPreview = _shaderFilterDefault.GetHandle("uAspectRatioPreview");

			// Store uniform variables into use.
			GLES20.GlUniform1f(uBrightness, _sharedData._brightness);
			GLES20.GlUniform1f(uContrast, _sharedData._contrast);
			GLES20.GlUniform1f(uSaturation, _sharedData._saturation);
			GLES20.GlUniform1f(uCornerRadius, _sharedData._cornerRadius);

			GLES20.GlUniform2fv(uAspectRatio, 1, _aspectRatio, 0);
			GLES20.GlUniform2fv(uAspectRatioPreview, 1,
				_sharedData._aspectRatioPreview, 0);

			// Use offscreen texture as source.
			GLES20.GlActiveTexture(GLES20.GlTexture1);
			GLES20.GlBindTexture(GLES20.GlTexture2d, _fboOffscreen.GetTexture(0));

			// Trigger actual rendering.
			renderQuad(_shaderCopyOes.GetHandle("aPosition"));
		}

		public void OnSurfaceChanged(IGL10 unused, int width, int height) {

			// Store width and height.
			_width = width;
			_height = height;

			// Calculate view aspect ratio.
			_aspectRatio[0] = (float) System.Math.Min(_width, _height) / _width;
			_aspectRatio[1] = (float) System.Math.Min(_width, _height) / _height;

			// Initialize textures.
			if (_fboExternal.Width != _width
				|| _fboExternal.Height != _height) {
				_fboExternal.init(_width, _height, 1, true);
			}
			if (_fboOffscreen.Width != _width
				|| _fboOffscreen.Height != _height) {
				_fboOffscreen.init(_width, _height, 1, false);
			}

			// Allocate new SurfaceTexture.
			SurfaceTexture oldSurfaceTexture = _surfaceTexture;
			_surfaceTexture = new SurfaceTexture(_fboExternal.GetTexture(0));
			_surfaceTexture.SetOnFrameAvailableListener(this);

			// TODO: Ici vs OnFrameAvailable
			_surfaceTexture.FrameAvailable += _surfaceTexture_FrameAvailable;
					
			//???? Remove
			if (_observer != null) {
				_observer.OnSurfaceTextureCreated(_surfaceTexture);
			}
			if (oldSurfaceTexture != null) {
				oldSurfaceTexture.Dispose(); // Originally Release. Don't know the Xamarin translation of android java texture disposing.
			}

			RequestRender();
		}

		void _surfaceTexture_FrameAvailable (object sender, SurfaceTexture.FrameAvailableEventArgs e)
		{
			OnFrameAvailable (e.SurfaceTexture);
		}

		public void OnFrameAvailable(SurfaceTexture surfaceTexture) {
			_surfaceTextureUpdate = true;
			RequestRender();	
		}

		public void OnSurfaceCreated(IGL10 unused, EGLConfig config) {
			
			try {
				var vertexSource = LoadRawString(Resource.Raw.copy_oes_vs);
				var fragmentSource = LoadRawString(Resource.Raw.copy_oes_fs);
				_shaderCopyOes.SetProgram(vertexSource, fragmentSource);
			} catch (System.Exception ex) {
				showError (ex.Message.ToString());
			}

			var vertexSourceFilter = LoadRawString(Resource.Raw.filter_vs);
			var fragmentSourceFilter = LoadRawString(Resource.Raw.filter_fs);
			_shaderFilterDefault.SetProgram(vertexSourceFilter, fragmentSourceFilter);

			_fboExternal.Reset();
			_fboOffscreen.Reset();
		}

		/**
		 * Renders fill screen quad using given GLES id/name.
		 */
		private void renderQuad(int aPosition) {
			GLES20.GlVertexAttribPointer(aPosition, 2, GLES20.GlFloat, false, 0,
				_fullQuadVertices);
			GLES20.GlEnableVertexAttribArray(aPosition);
			GLES20.GlDrawArrays(GLES20.GlTriangleStrip, 0, 4);
		}

		/**
		 * Setter for observer.
		 */
		public void SetObserver(Observer observer) {
			_observer = observer;
		}

		/**
		 * Setter for shared data.
		 */
		public void SetSharedData(DataContainer sharedData) {
			_sharedData = sharedData;
			RequestRender();
		}

		/**
		 * Shows Toast on screen with given message.
		 */
		private void showError(string errorMsg) {
			var handler = new Handler(Context.MainLooper);
			handler.Post (() => {
				Toast.MakeText (Context, errorMsg, ToastLength.Long).Show ();
			});
		}

		/**
		 * Observer class for renderer.
		 */
		public interface Observer {
			void OnSurfaceTextureCreated(SurfaceTexture surfaceTexture);
		}

	}
}