using Android.App;

namespace FboTextures {
	
	public class DataContainer {

		// Preview view aspect ration.
		public float[] _aspectRatioPreview = new float[2];

		// Filter values.
		public float _brightness, _contrast, _saturation, _cornerRadius;

		// Predefined filter.
		public int _filter;

		// Taken picture data (jpeg).
		public byte[] _imageData;

		// Progress dialog while saving picture.
		public ProgressDialog _imageProgress;

		// Picture capture time.
		public long _imageTime;

		// Device orientation degree.
		public int _orientationDevice;

		// Camera orientation matrix.
		public float[] _orientationM = new float[16];
	}
}