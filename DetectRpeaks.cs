using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System; // needed for Array.Sort
using System.IO; // needed for StreamWriter

public class DetectRpeaks : MonoBehaviour {

    public bool heartbeat;
    float input;
    ECG ecg1;

    // Use this for initialization
    void Start () {

        string path = "C:/Users/...myfolder.../data.csv";
        // get ecg at 250Hz sample rate. Record data for 5sec untill thresholds (97th-percentiles for voltage and voltage change) are determined. 
        // Then keep checking if both thresholds are reached within 0.02sec, but don't allow two r peaks within 0.196sec. Write down everything to path. 
        ecg1 = new ECG(250, 5, 0.97f, 0.97f, 0.02f, 0.196f, path);

    }
	
	// Update is called once per frame
	void Update () {

        // input = Some_ECG_Streamed_Into_Unity; // Streaming Data into Unity is not part of this script.
        heartbeat = processECG(input, ecg1); // determine heartbeat. Call this function in the same void your data is streamed in, typically not in Update (which runs at 60Hz). 

    }

    // contains and processes all information that an ecg real-time recording needs, and stores data as .csv file
    public class ECG
    {
        public float samplingrate;
        public int counter; // simply counts the number of entries this ECG has received so far
        public float currentTimestamp;

        public float currentVolt; // current numbers to be written down immediately
        public float lastVoltage;
        public float currentDiff;

        // variables to determine thresholds
        public int timepointsTillThreshold; // after how many measurement units should the threshold be determined

        public float thresholdVolt;
        public float thresholdVoltPercentile; // e.g. 0.95 for a 95%-rule for determining R-peaks
        public float[] firstMeasurementsVolt;
        public int VoltageThresholdReached; // notes when threshold was reached. Not simply a boolean, because by setting it to e.g. 10 and slowly move down to 0 from there, it can...

        public float thresholdDiff;
        public float thresholdDiffPercentile;
        public float[] firstMeasurementsDiff;
        public int VoltageDiffThresholdReached; // be checked with some time difference if both values have surpassed the threshold (typically, the diff threshold is reached first)

        // coefficients to determine r peak
        public int counterSinceLastPeak; // counts time since last r peak
        public int blockTime; // time after each r peak in which no new r peak may be detected
        public int timeDelay; // timeDelay (in measurement units) allowed between both thresholds are reached and still heartbeat is detected

        /*
        //if all data are to be stored inside Unity, and/or some analyses performed a few hundred ms post-hoc are to be performed, store all data in Lists.
        public List<float> voltage; // simply the measured voltage at each time point
        public List<float> voltage_diff; // the difference to the last datapoint
        public List<float> timestamp; //
        public List<bool> r_peak_realtime; // decide in real-time, for each timepoint, if it is "the" r-peak. Only be true once during each R-peak
        public List<bool> r_peak_hindsight; // allow for retrospective decision. If no R-peak was found for a certain time, threshold is lowered in retrospection
        public List<float> heartrate_realtime; // heartrate for each timepoint. Of course, all timepoints between two peaks are assigned the same heartrate
        public List<float> heartrate_hindsight; // heartrate for each timepoint, after looking back a few hundred ms and perhaps adjusting thresholds
        */

        // for writing down
        public TextWriter sw;
        public string line;

        public ECG(float samplingf, float waitSecs, float percentileVolt, float percentileDiff, float maxTimeDelay, float block, string writepath)
        {
            samplingrate = samplingf;
            counter = 0;
            currentTimestamp = 0;

            currentVolt = 0;
            lastVoltage = 0;
            currentDiff = 0;

            timepointsTillThreshold = (int)Mathf.Floor(waitSecs * samplingrate);

            thresholdVolt = 0;
            thresholdVoltPercentile = percentileVolt;
            firstMeasurementsVolt = new float[timepointsTillThreshold + 1];
            VoltageThresholdReached = 0;

            thresholdDiff = 0;
            thresholdDiffPercentile = percentileDiff;
            firstMeasurementsDiff = new float[timepointsTillThreshold + 1];
            VoltageDiffThresholdReached = 0;

            counterSinceLastPeak = 0;
            blockTime = (int)(block * samplingrate);
            timeDelay = (int)(maxTimeDelay * samplingrate);

            /*
            voltage = new List<float>();
            voltage_diff = new List<float>();
            timestamp = new List<float>();
            r_peak_realtime = new List<bool>();
            r_peak_hindsight = new List<bool>();
            heartrate_realtime = new List<float>();
            heartrate_hindsight = new List<float>();
            */
            sw = new StreamWriter(writepath);
            line = "Counter;Voltage;VoltageDiff;Timestamp;thresholdVoltReached;thresholdDiffReached;rPeak";// ;Heartrate";
            sw.WriteLine(line);
        }
    }

    public bool processECG(float y0, ECG ecg)
    {
        bool rpeak = false; // first assume there is no heartbeat

        ecg.currentTimestamp = Time.time;
        //ecg.timestamp.Add(ecg.currentTimestamp);

        ecg.currentVolt = y0;
        //ecg.voltage.Add(ecg.currentVoltage);

        ecg.currentDiff = Mathf.Abs(ecg.currentVolt - ecg.lastVoltage);
        //ecg.voltage_diff.Add(ecg.currentDiff);

        if (ecg.counter < ecg.timepointsTillThreshold)
        {
            ecg.firstMeasurementsVolt[ecg.counter] = ecg.currentVolt; // in beginning, also write to additional array that can be sorted
            ecg.firstMeasurementsDiff[ecg.counter] = ecg.currentDiff;
        }

        // Determine threshold at given point in time
        if (ecg.counter == ecg.timepointsTillThreshold)
        {
            Array.Sort(ecg.firstMeasurementsVolt);
            ecg.thresholdVolt = ecg.firstMeasurementsVolt[Mathf.FloorToInt(ecg.firstMeasurementsVolt.Length * ecg.thresholdVoltPercentile)];

            Array.Sort(ecg.firstMeasurementsDiff);
            ecg.thresholdDiff = ecg.firstMeasurementsDiff[Mathf.FloorToInt(ecg.firstMeasurementsDiff.Length * ecg.thresholdDiffPercentile)];

            Debug.Log("voltage threshold was set to " + ecg.thresholdVolt + ", voltage change threshold was set to " + +ecg.thresholdDiff);
        }

        // then start finding R-peaks
        if (ecg.counter > ecg.timepointsTillThreshold)
        {
            if (ecg.currentVolt > ecg.thresholdVolt) ecg.VoltageThresholdReached = ecg.timeDelay;  // if threshold is reached, wave a flag and see if other threshold is reached within timeDelay too
            if (ecg.currentDiff > ecg.thresholdDiff) ecg.VoltageDiffThresholdReached = ecg.timeDelay;
        }

        if (ecg.VoltageThresholdReached > 0 & ecg.VoltageDiffThresholdReached > 0 & ecg.counterSinceLastPeak > ecg.blockTime) // only if both thresholds have been reached will we call this an r peak
        {
            rpeak = true;

            /*
            float bpm = 60 * ecg.samplingrate / ecg.counterSinceLastPeak;
            for (int i = 0; i < ecg.counterSinceLastPeak; i++)
            {
                ecg.heartrate_realtime.Add(bpm);
            }
            */
            ecg.counterSinceLastPeak = -1; // start counting again
        }

        ecg.line = ecg.counter.ToString() + ";" + ecg.currentVolt.ToString() + ";" + ecg.currentDiff.ToString() + ";" + ecg.currentTimestamp.ToString() + ";" + ecg.VoltageThresholdReached.ToString() + ";" + ecg.VoltageDiffThresholdReached.ToString() + ";" + rpeak.ToString(); // + ";" + ecg.heartrate_realtime.ToString();
        ecg.sw.WriteLine(ecg.line);


        if (ecg.VoltageThresholdReached > 0) ecg.VoltageThresholdReached--;
        if (ecg.VoltageDiffThresholdReached > 0) ecg.VoltageDiffThresholdReached--;
        ecg.counter++;
        ecg.counterSinceLastPeak++;
        ecg.lastVoltage = ecg.currentVolt;
        //ecg.r_peak_realtime.Add(rpeak);
        return rpeak;
    }
}
