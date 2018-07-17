# Pursuits in VR
Master Thesis Project
User study (N=26) conducted with Unity. We investigated how parameters that are specific to VR settings influence the perfor-
mance of selecting moving targets with gaze using Pursuits. Full paper see here: https://perceptual.mpi-inf.mpg.de/files/2018/04/khamis18_avi.pdf

Software based On
## PupilHMDCalibration

This project provides the calibration process of Pupil HMD hardware and eye gaze data streaming to Unity. 

This works with Pupil Service/ Pupil capture version 0.8+ 
https://github.com/pupil-labs/pupil/releases

#Setup:
1- Run Pupil Capture or Pupil Service in ubuntu/mac 

2- Configure PupilGaze property's Server IP to point to Pupil PC

#Calibration:

Run Unity project, and load Calibration scene. Hit play to start receiving gaze data.

To Calibrate eye gaze, hit "C" on keyboard in Unity, a white target will appear in the HMD. 

To stop calibration, hit "S"
