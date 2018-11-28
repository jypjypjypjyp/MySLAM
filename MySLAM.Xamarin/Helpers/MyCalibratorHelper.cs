using Android.App;

namespace MySLAM.Xamarin.MyHelper
{
    public class MyCalibratorHelper
    {
        public FrameRender FrameRender { get; set; }
        public CameraCalibrator CameraCalibrator { get; set; }

        private readonly Activity owner;
        private readonly int width;
        private readonly int height;

        public MyCalibratorHelper(Activity owner, int width, int height)
        {
            this.width = width;
            this.height = height;
            CameraCalibrator = new CameraCalibrator(width, height);
            if (CalibrationResult.TryLoad())
            {
                CameraCalibrator.IsCalibrated = true;
                FrameRender = new PreviewFrameRender();
            }
            else FrameRender = new CalibrationFrameRender(CameraCalibrator);
            this.owner = owner;
        }

        public void ChangeRenderMode<T>() where T : FrameRender
        {
            var mode = typeof(T);
            if (FrameRender.GetType() == mode) return;

            if (FrameRender is ARFrameRender)
                ((ARFrameRender)FrameRender).Dispose();

            if (mode == typeof(CalibrationFrameRender))
                FrameRender = new CalibrationFrameRender(CameraCalibrator);
            else if (mode == typeof(ComparisonFrameRender))
                FrameRender = new ComparisonFrameRender(CameraCalibrator, width, height, owner.Resources);
            else if (mode == typeof(UndistortionFrameRender))
                FrameRender = new UndistortionFrameRender(CameraCalibrator);
            else if (mode == typeof(ARFrameRender))
                FrameRender = new ARFrameRender(width, height);
            else FrameRender = new PreviewFrameRender();
        }
    }
}