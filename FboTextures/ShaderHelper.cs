using Java.Util;
using Android.Opengl;
using System.Collections.Generic;
using Android.Util;
using Java.Lang;

namespace FboTextures {
	
	public class ShaderHelper {

		private int _program = 0;
		private int _shaderFragment = 0;

		private Dictionary<string, int> mShaderHandleMap = new Dictionary<string, int>();
		private int _shaderVertex = 0;

		/**
		 * Deletes program and shaders associated with it.
		 */
		public void DeleteProgram() {
			GLES20.GlDeleteShader(_shaderFragment);
			GLES20.GlDeleteShader(_shaderVertex);
			GLES20.GlDeleteProgram(_program);
			_program = _shaderVertex = _shaderFragment = 0;
		}

		/**
		 * Get id for given handle name. This method checks for both attribute and
		 * uniform handles.
		 * 
		 * @param name
		 *            Name of handle.
		 * @return Id for given handle or -1 if none found.
		 */
		public int GetHandle(string name) {
			if (mShaderHandleMap.ContainsKey(name)) {
				return mShaderHandleMap [name];
			}
			var handle = GLES20.GlGetAttribLocation(_program, name);
			if (handle == -1) {
				handle = GLES20.GlGetUniformLocation(_program, name);
			}
			if (handle == -1) {

				// One should never leave log messages but am not going to follow
				// this rule. This line comes handy if you see repeating 'not found'
				// messages on LogCat - usually for typos otherwise annoying to
				// spot from shader code.
				Log.Debug("GlslShader", "Could not get attrib location for " + name);
			} else {
				mShaderHandleMap.Add(name, handle);
			}
			return handle;
		}

		/**
		 * Get array of ids with given names. Returned array is sized to given
		 * amount name elements.
		 * 
		 * @param names
		 *            List of handle names.
		 * @return array of handle ids.
		 */
		public int[] GetHandles(string[] names) {
			var res = new int[names.Length];
			for (var i = 0; i < names.Length; ++i) {
				res[i] = GetHandle(names[i]);
			}
			return res;
		}

		/**
		 * Helper method for compiling a shader.
		 * 
		 * @param shaderType
		 *            Type of shader to compile
		 * @param source
		 *            String presentation for shader
		 * @return id for compiled shader
		 */
		private int LoadShader(int shaderType, string source) {
			var shader = GLES20.GlCreateShader(shaderType);
			if (shader != 0) {
				GLES20.GlShaderSource(shader, source);
				GLES20.GlCompileShader(shader);
				int[] compiled = new int[1];
				GLES20.GlGetShaderiv(shader, GLES20.GlCompileStatus, compiled, 0);
				if (compiled[0] == 0) {
					var error = GLES20.GlGetShaderInfoLog(shader);
					GLES20.GlDeleteShader(shader);
					throw new Exception(error);
				}
			}
			return shader;
		}

		/**
		 * Compiles vertex and fragment shaders and links them into a program one
		 * can use for rendering. Once OpenGL context is lost and onSurfaceCreated
		 * is called, there is no need to reset existing GlslShader objects but one
		 * can simply reload shader.
		 * 
		 * @param vertexSource
		 *            String presentation for vertex shader
		 * @param fragmentSource
		 *            String presentation for fragment shader
		 */
		public void SetProgram(string vertexSource, string fragmentSource) {

			_shaderVertex = LoadShader(GLES20.GlVertexShader, vertexSource);
			_shaderFragment = LoadShader(GLES20.GlFragmentShader, fragmentSource);

			var program = GLES20.GlCreateProgram();
			if (program != 0) {
				GLES20.GlAttachShader(program, _shaderVertex);
				GLES20.GlAttachShader(program, _shaderFragment);
				GLES20.GlLinkProgram(program);
				int[] linkStatus = new int[1];
				GLES20.GlGetProgramiv(program, GLES20.GlLinkStatus, linkStatus, 0);
				if (linkStatus[0] != GLES20.GlTrue) {
					var error = GLES20.GlGetProgramInfoLog(program);
					DeleteProgram();
					throw new Exception(error);
				}
			}
			_program = program;
			mShaderHandleMap.Clear();
		}

		/**
		 * Activates this shader program.
		 */
		public void UseProgram() {
			GLES20.GlUseProgram(_program);
		}

	}
}