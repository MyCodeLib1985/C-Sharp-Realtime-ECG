# C-Sharp-Realtime-ECG
Detect R peaks in ECG data in real-time using C#

* Purpose
In psychological and medical research, ECG data is typically evaluated semi-automatically for maximal accuracy. If your program needs to react to changes in heartrate, ECG has to be evaluated in real-time. Various algorithms have been proposed to perform this, and accuracy is high enough for a wide range of applications. For an introduction I recommend https://de.mathworks.com/help/dsp/examples/real-time-ecg-qrs-detection.html

* Use
** In the attached script, R peaks are determined based on two criteria: Both a threshold in absolute voltage and a threshold in voltage change (determined as percentiles in data recorded at the beginning of each session) need to be surpassed within a defined amount of time to result in an R peak detection. 

* Limitations
** The algorithm is still work in progress and does not yet match any defined in the literature. 
