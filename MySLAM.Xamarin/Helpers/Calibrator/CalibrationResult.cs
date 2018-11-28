using Android.App;
using Org.Opencv.Core;
using System.IO;

namespace MySLAM.Xamarin.MyHelper
{
    public abstract class CalibrationResult
    {

        public static void Save(Activity activity, Mat cameraMatrix)
        {
            if(!File.Exists(AppConst.RootPath + "orb_slam2.yaml"))
            {
                try
                {
                    activity.Assets.Open("orb_slam2_template.yaml")
                                .CopyWholeFile(File.Create(AppConst.RootPath + "orb_slam2.yaml"));
                }
                catch (System.Exception e)
                {

                    throw;
                }
            }
            using (var file = File.Open(AppConst.RootPath + "orb_slam2.yaml",FileMode.Open))
            {
                double[] cameraMatrixArray = new double[3 * 3];
                cameraMatrix.Get(0, 0, cameraMatrixArray);
                file.Edit("Camera.fx", cameraMatrixArray[0].ToString());
                file.Edit("Camera.fy", cameraMatrixArray[4].ToString());
                file.Edit("Camera.cx", cameraMatrixArray[2].ToString());
                file.Edit("Camera.cy", cameraMatrixArray[5].ToString());
            }
        }

        public static bool TryLoad()
        {
            if (File.Exists(AppConst.RootPath + "orb_slam2.yaml"))
            {
                return true;
            }
            else
                return false;
        }
    }
}