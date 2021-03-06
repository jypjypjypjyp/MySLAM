﻿using Org.Opencv.Calib3d;
using Org.Opencv.Core;
using Org.Opencv.Imgproc;
using System.Collections.Generic;
using Size = Org.Opencv.Core.Size;

namespace MySLAM.Xamarin.Helpers.Calibrator
{
    public class CameraCalibrator
    {
        public bool IsCalibrated { get; set; }
        public Mat CameraMatrix { get; set; }
        public Mat DistortionCoefficients { get; set; }
        public int CornersBufferSize { get => cornersBuffer.Count; }

        private bool patternWasFound = false;
        private MatOfPoint2f corners = new MatOfPoint2f();
        private List<Mat> cornersBuffer = new List<Mat>();

        private readonly Size patternSize = new Size(4, 11);
        private readonly int cornersSize;
        private readonly int flags;
        private readonly double squareSize = 0.0181;
        private readonly Size imageSize;

        public CameraCalibrator(int width, int height)
        {
            //Init public preporty
            IsCalibrated = false;
            imageSize = new Size(width, height);
            flags = Calib3d.CalibFixPrincipalPoint +
                     Calib3d.CalibZeroTangentDist +
                     Calib3d.CalibFixAspectRatio +
                     Calib3d.CalibFixK4 +
                     Calib3d.CalibFixK5;
            CameraMatrix = new Mat();
            DistortionCoefficients = new Mat();

            Mat.Eye(3, 3, CvType.Cv64fc1).CopyTo(CameraMatrix);
            CameraMatrix.Put(0, 0, 1.0);
            Mat.Zeros(5, 1, CvType.Cv64fc1).CopyTo(DistortionCoefficients);
            cornersSize = (int)(patternSize.Width * patternSize.Height);
        }

        public void ProcessFrame(Mat grayFrame, Mat rgbaFrame)
        {
            FindPattern(grayFrame);
            RenderFrame(rgbaFrame);
        }

        public void Calibrate()
        {
            var rvecs = new List<Mat>();
            var tvecs = new List<Mat>();
            var reprojectionErrors = new Mat();
            var objectPoints = new List<Mat>
            {
                Mat.Zeros(cornersSize, 1, CvType.Cv32fc3)
            };
            CalcBoardCornerPositions(objectPoints[0]);
            for (int i = 1; i < cornersBuffer.Count; i++)
            {
                objectPoints.Add(objectPoints[0]);
            }

            Calib3d.CalibrateCamera(objectPoints, cornersBuffer, imageSize,
                    CameraMatrix, DistortionCoefficients, rvecs, tvecs, flags);

            IsCalibrated = Core.CheckRange(CameraMatrix)
                    && Core.CheckRange(DistortionCoefficients);
        }

        public void ClearCorners()
        {
            cornersBuffer.Clear();
        }

        private void CalcBoardCornerPositions(Mat corners)
        {
            const int cn = 3;
            float[] positions = new float[cornersSize * cn];

            for (int i = 0; i < patternSize.Height; i++)
            {
                for (int j = 0; j < patternSize.Width * cn; j += cn)
                {
                    positions[(int)(i * patternSize.Width * cn + j + 0)] =
                            (2 * (j / cn) + i % 2) * (float)squareSize;
                    positions[(int)(i * patternSize.Width * cn + j + 1)] =
                            i * (float)squareSize;
                    positions[(int)(i * patternSize.Width * cn + j + 2)] = 0;
                }
            }
            corners.Create(cornersSize, 1, CvType.Cv32fc3);
            corners.Put(0, 0, positions);
        }

        private void FindPattern(Mat grayFrame)
        {
            patternWasFound = Calib3d.FindCirclesGrid(grayFrame, patternSize,
                    corners, Calib3d.CalibCbAsymmetricGrid);
        }

        public bool AddCorners()
        {
            if (patternWasFound)
            {
                cornersBuffer.Add(corners.Clone());
                return true;
            }
            return false;
        }

        private void DrawPoints(Mat rgbaFrame)
        {
            Calib3d.DrawChessboardCorners(rgbaFrame, patternSize, corners, patternWasFound);
        }

        private void RenderFrame(Mat rgbaFrame)
        {
            DrawPoints(rgbaFrame);
            Imgproc.PutText(rgbaFrame, "Captured: " + cornersBuffer.Count, new Point(rgbaFrame.Cols() / 3 * 2, rgbaFrame.Rows() * 0.1),
                    Core.FontHersheySimplex, 1.0, new Scalar(255, 255, 0));
        }
    }
}