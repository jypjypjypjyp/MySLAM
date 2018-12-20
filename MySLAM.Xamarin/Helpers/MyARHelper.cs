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

        private readonly Activity owner;

        public static Task<MyARHelper> AsyncBuilder(Activity owner, int width, int height,
                                                            Views.MyDialog.ProgressChanged progressChanged = null)
        {
            return Task.Run(() => new MyARHelper(owner, width, height, progressChanged));
        }

        private MyARHelper(Activity owner, int width, int height,
                                    Views.MyDialog.ProgressChanged progressChanged)
        {
            this.owner = owner;
            CameraCalibrator = new CameraCalibrator(width, height);
            if (CheckSLAMFile(progressChanged))
            {
                CameraCalibrator.IsCalibrated = true;
                FrameRender = new PreviewFrameRender();
            }
            else FrameRender = new CalibrationFrameRender(CameraCalibrator);
        }

        private bool CheckSLAMFile(Views.MyDialog.ProgressChanged progressChanged)
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
                webClient.DownloadFileTaskAsync(new Uri("http://139.199.37.234:2018/ORBvoc.bin"), "/storage/emulated/0/MySLAM/ORBvoc.bin")
                    .Wait();
            }
            if (File.Exists(AppConst.RootPath + "orb_slam2.yaml"))
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

            if (FrameRender is ARFrameRender)
                ((ARFrameRender)FrameRender).Release();

            if (mode == typeof(CalibrationFrameRender))
                FrameRender = new CalibrationFrameRender(CameraCalibrator);
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

        public static void RemoveCache()
        {
            File.Delete("/storage/emulated/0/MySLAM/ORBvoc.bin");
        }
    }
}