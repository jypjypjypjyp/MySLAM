using Android.App;
using MySLAM.Xamarin.Helpers.Calibrator;
using Org.Opencv.Core;
using System.IO;
using System.Threading.Tasks;

namespace MySLAM.Xamarin.Helpers
{
    public class MyCalibratorHelper
    {
        public FrameRender FrameRender { get; set; }
        public CameraCalibrator CameraCalibrator { get; set; }

        private readonly Activity owner;
        private readonly int width;
        private readonly int height;

        public static Task<MyCalibratorHelper> Builder(Activity owner, int width, int height)
        {
            return Task.Run(() => new MyCalibratorHelper(owner, width, height));
        }

        private MyCalibratorHelper(Activity owner, int width, int height)
        {
            this.width = width;
            this.height = height;
            this.owner = owner;
            CameraCalibrator = new CameraCalibrator(width, height);
            if (TryLoad())
            {
                CameraCalibrator.IsCalibrated = true;
                FrameRender = new PreviewFrameRender();
            }
            else FrameRender = new CalibrationFrameRender(CameraCalibrator);
        }

        public void ChangeRenderMode<T>() where T : FrameRender
        {
            var mode = typeof(T);
            if (FrameRender.GetType() == mode) return;

            if (FrameRender is ARFrameRender)
                ((ARFrameRender)FrameRender).Release();

            if (mode == typeof(CalibrationFrameRender))
                FrameRender = new CalibrationFrameRender(CameraCalibrator);
            else if (mode == typeof(ComparisonFrameRender))
                FrameRender = new ComparisonFrameRender(CameraCalibrator, width, height, owner.Resources);
            else if (mode == typeof(UndistortionFrameRender))
                FrameRender = new UndistortionFrameRender(CameraCalibrator);
            else if (mode == typeof(ARFrameRender))
                FrameRender = new ARFrameRender();
            else FrameRender = new PreviewFrameRender();
        }

        public void Save(Mat cameraMatrix)
        {
            if (!File.Exists(AppConst.RootPath + "orb_slam2.yaml"))
            {
                owner.Assets.Open("orb_slam2_template.yaml")
                            .CopyWholeFile(File.Create(AppConst.RootPath + "orb_slam2.yaml"));

            }
            double[] cameraMatrixArray = new double[3 * 3];
            cameraMatrix.Get(0, 0, cameraMatrixArray);

            string readAll = File.ReadAllText(AppConst.RootPath + "orb_slam2.yaml");

            readAll = readAll.Edit("Camera.fx", cameraMatrixArray[0].ToString())
                            .Edit("Camera.fy", cameraMatrixArray[4].ToString())
                            .Edit("Camera.cx", cameraMatrixArray[2].ToString())
                            .Edit("Camera.cy", cameraMatrixArray[5].ToString());

            File.WriteAllText(AppConst.RootPath + "orb_slam2.yaml", readAll);
        }

        public bool TryLoad()
        {
            if (!File.Exists(AppConst.RootPath + "ORBvoc.txt"))
            {
                owner.Assets.Open("ORBvoc.txt")
                    .CopyWholeFile(File.Create(AppConst.RootPath + "ORBvoc.txt"));
            }
            if (File.Exists(AppConst.RootPath + "orb_slam2.yaml"))
            {
                return true;
            }
            else
                return false;
        }
    }
}