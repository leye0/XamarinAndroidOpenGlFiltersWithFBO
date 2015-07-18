using Android.Opengl;

namespace FboTextures { 
	
	public class FboHelper {

		// FBO handle.
		private int _frameBufferHandle = -1;
		// Generated texture handles.
		private int[] _textureHandles = {};
		// FBO textures and depth buffer size.
		private int _width, _height;

		/**
		 * Binds this FBO into use and adjusts viewport to FBO size.
		 */
		public void Bind() {
			GLES20.GlBindFramebuffer(GLES20.GlFramebuffer, _frameBufferHandle);
			GLES20.GlViewport(0, 0, _width, _height);
		}

		/**
		 * Bind certain texture into target texture. This method should be called
		 * only after call to bind().
		 * 
		 * @param index
		 *            Index of texture to bind.
		 */
		public void BindTexture(int index) {
			GLES20.GlFramebufferTexture2D(GLES20.GlFramebuffer,
				GLES20.GlColorAttachment0, GLES20.GlTexture2d,
				_textureHandles[index], 0);
		}

		/**
		 * Getter for FBO height.
		 * 
		 * @return FBO height in pixels.
		 */
		public int Height {
			get { 
				return _height;
			}
		}

		/**
		 * Getter for texture ids.
		 * 
		 * @param index
		 *            Index of texture.
		 * @return Texture id.
		 */
		public int GetTexture(int index) {
			return _textureHandles[index];
		}

		/**
		 * Getter for FBO width.
		 * 
		 * @return FBO width in pixels.
		 */
		public int Width {
			get { 
				return _width;
			}
		}

		/**
		 * Initializes FBO with given parameters. Width and height are used to
		 * generate textures out of which all are sized same to this FBO. If you
		 * give genRenderBuffer a value 'true', depth buffer will be generated also.
		 * 
		 * @param width
		 *            FBO width in pixels
		 * @param height
		 *            FBO height in pixels
		 * @param textureCount
		 *            Number of textures to generate
		 * @param genDepthBuffer
		 *            If true, depth buffer is allocated for this FBO @ param
		 *            genStencilBuffer If true, stencil buffer is allocated for this
		 *            FBO
		 */
		public void init(int width, int height, int textureCount,
			bool textureExternalOES) {

			// Just in case.
			Reset();

			// Store FBO size.
			_width = width;
			_height = height;

			// Genereta FBO.
			var handle = new int[] { 0 };
			GLES20.GlGenFramebuffers(1, handle, 0);
			_frameBufferHandle = handle[0];
			GLES20.GlBindFramebuffer(GLES20.GlFramebuffer, _frameBufferHandle);

			// Generate textures.
			_textureHandles = new int[textureCount];
			GLES20.GlGenTextures(textureCount, _textureHandles, 0);
			var target = textureExternalOES ? GLES11Ext.GlTextureExternalOes
				: GLES20.GlTexture2d;
			foreach (var texture in _textureHandles) {
				GLES20.GlBindTexture(target, texture);
				GLES20.GlTexParameteri(GLES20.GlTexture2d,
					GLES20.GlTextureWrapS, GLES20.GlClampToEdge);
				GLES20.GlTexParameteri(target, GLES20.GlTextureWrapT,
					GLES20.GlClampToEdge);
				GLES20.GlTexParameteri(target, GLES20.GlTextureMinFilter,
					GLES20.GlNearest);
				GLES20.GlTexParameteri(target, GLES20.GlTextureMagFilter,
					GLES20.GlLinear);
				if (target == GLES20.GlTexture2d) {
					GLES20.GlTexImage2D(GLES20.GlTexture2d, 0, GLES20.GlRgba,
						_width, _height, 0, GLES20.GlRgba,
						GLES20.GlUnsignedByte, null);
				}
			}
		}

		/**
		 * Resets this FBO into its initial state, releasing all resources that were
		 * allocated during a call to init.
		 */
		public void Reset() {
			int[] handle = { _frameBufferHandle };
			GLES20.GlDeleteFramebuffers(1, handle, 0);
			GLES20.GlDeleteTextures(_textureHandles.Length, _textureHandles, 0);
			_frameBufferHandle = -1;
			_textureHandles = new int[0];
			_width = _height = 0;
		}
	}
}