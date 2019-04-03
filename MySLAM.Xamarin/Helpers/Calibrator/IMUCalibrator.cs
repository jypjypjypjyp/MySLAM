using Org.Opencv.Core;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MySLAM.Xamarin.Helpers.Calibrator
{
    public class IMUCalibrator
    {
        public bool IsCalibrated { get; set; }
        public Mat IMUMatrix { get; set; }

        public IMUCalibrator()
        {
            IsCalibrated = false;
        }

        public void Calibrate()
        {
            List<float[]> _IMURecordList = new List<float[]>();
            ManualResetEvent finishSignal = new ManualResetEvent(false);
            HelperManager.IMUHelper.Register(MySensorHelper.ModeType.Calibrate,
                (object data) =>
                {
                    _IMURecordList.Add((float[])data);
                    if (_IMURecordList.Count >= 1 << 12)
                    {
                        HelperManager.IMUHelper.UnRegister();
                        finishSignal.Set();
                    }
                });
            finishSignal.WaitOne();
            Mat samples = new Mat(_IMURecordList.Count, 3, CvType.Cv32f);
            samples.Put(0, 0, _IMURecordList.Aggregate((cat, next) => cat.Concat(next).ToArray()));
            Mat mean = new Mat();
            Mat covar = new Mat();
            Core.CalcCovarMatrix(samples, covar, mean, Core.CovarNormal | Core.CovarRows);
            covar.Push_back(mean);
            IMUMatrix = covar;
            IsCalibrated = true;
        }
    }
}