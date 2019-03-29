using Android.App;
using MySLAM.Xamarin.Helpers.Calibrator;
using Org.Opencv.Core;
using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace MySLAM.Xamarin.Helpers
{
    public class MyARHelper
    {
        public static int TOTAL_VOC_SIZE = 44365017;

        public FrameRender FrameRender { get; set; }
        public CameraCalibrator CameraCalibrator { get; set; }
        public IMUCalibrator IMUCalibrator { get; set; }

        private readonly Activity owner;

        public bool IsCalibrated
        {
            get
            {
                return CameraCalibrator.IsCalibrated && IMUCalibrator.IsCalibrated;
            }
        }

        public static Task<MyARHelper> AsyncBuilder(Activity owner, int width, int height,
                                                            Views.MyDialog.ProgressChanged progressChanged = null)
        {
            return Task.Run(() => new MyARHelper(owner, width, height, progressChanged));
        }

        private MyARHelper(Activity owner, int width, int height,
                                    Views.MyDialog.ProgressChanged progressChanged)
        {
            this.owner = owner;
            IMUCalibrator = new IMUCalibrator();
            CameraCalibrator = new CameraCalibrator(width, height);
            if (CheckFile(progressChanged))
            {
                CameraCalibrator.IsCalibrated = true;
                IMUCalibrator.IsCalibrated = true;
                FrameRender = new PreviewFrameRender();
            }
            else FrameRender = new CalibrationFrameRender(CameraCalibrator);
        }

        private bool CheckFile(Views.MyDialog.ProgressChanged progressChanged)
        {
            if (!File.Exists(AppConst.RootPath + "ORBvoc.bin"))
            {
                var webClient = new WebClient();
                if (progressChanged != null)
                    webClient.DownloadProgressChanged +=
                        (o, e) =>
                        {
                            progressChanged((int)e.BytesReceived * 100 / TOTAL_VOC_SIZE);
                        };
                webClient.DownloadFileTaskAsync(new Uri("http://39.97.38.181:2018/ORBvoc.bin"), AppConst.RootPath + "ORBvoc.bin").Wait();
            }
            if (File.Exists(AppConst.RootPath + "orb_slam2.yaml") && File.Exists(AppConst.RootPath + "imu.yaml"))
            {
                return true;
            }
            else
                return false;
        }

        public void ChangeRenderMode<T>() where T : FrameRender
        {
            var mode = typeof(T);
            if (FrameRender.GetType() == mode) return;

            (FrameRender as AR2FrameRender)?.Dispose();
            (FrameRender as AR1FrameRender)?.Dispose();

            if (mode == typeof(CalibrationFrameRender))
                FrameRender = new CalibrationFrameRender(CameraCalibrator);
            else if (mode == typeof(AR2FrameRender))
                FrameRender = new AR2FrameRender();
            else if (mode == typeof(AR1FrameRender))
                FrameRender = new AR1FrameRender();
            else FrameRender = new PreviewFrameRender();
        }

        public void Save(Mat cameraMat, Mat imuMat)
        {
            if (!File.Exists(AppConst.RootPath + "orb_slam2.yaml"))
                owner.Assets.Open("settings_template.yaml")
                            .CopyWholeFile(File.OpenWrite(AppConst.RootPath + "orb_slam2.yaml"));
            double[] cameraMatrArray = new double[3 * 3];
            cameraMat.Get(0, 0, cameraMatrArray);

            string readAll = File.ReadAllText(AppConst.RootPath + "orb_slam2.yaml");

            readAll = readAll.Edit("Camera.fx", cameraMatrArray[0].ToString())
                            .Edit("Camera.fy", cameraMatrArray[4].ToString())
                            .Edit("Camera.cx", cameraMatrArray[2].ToString())
                            .Edit("Camera.cy", cameraMatrArray[5].ToString());

            File.WriteAllText(AppConst.RootPath + "orb_slam2.yaml", readAll);

            string file = AppConst.RootPath + "imu.yaml";

            // cv::FileStorage have some defects. It's difficult to use.
            File.Create(file);
            YamlExtension.WriteIMUSettings(file, imuMat.RowRange(0, 3), imuMat.RowRange(3, 4));
        }

        public static void RemoveCache()
        {
            File.Delete(AppConst.RootPath + "ORBvoc.bin");
        }
    }
}