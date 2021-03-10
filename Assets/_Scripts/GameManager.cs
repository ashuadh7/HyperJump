using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Security.Cryptography;
using UnityEngine.UI;


public class GameManager : MonoBehaviour
{
    // Toggle writing data | Turn off while testing to not create unnecessary csv files.
    [SerializeField]
    private bool writeData;

    //==================reference to other GameObjects and AudioClips=============================//

    [SerializeField]
    private GameObject vrCamera;
    [SerializeField]
    private GameObject controller;
     [SerializeField]
    private GameObject[] pathTargets;
    [SerializeField]
    private GameObject[] pointers;
    [SerializeField]
    private GameObject[] pathWaypoints;
    private GameObject thinPointLaser, measurementCone;
    [SerializeField]
    private GameObject pointerAnimationObject, m_shotPrefab;
     [SerializeField]
    private GameObject SSQpointer;
    [SerializeField]
    private GameObject SSQbar;
    [SerializeField]
    private GameObject SSQUI;
    [SerializeField]
    private AudioSource audioPlayer;
    [SerializeField]
    private AudioSource controllerAudioPlayer;
    [SerializeField]
    private AudioClip[] pathInstructions;
    [SerializeField]
    private AudioClip pointUpDing;
    [SerializeField]
    private AudioClip pointedTargetDing, distanceMeasurementDing;    
    private Text txt;
    



    //=================================reference to other scripts=============================//

    [SerializeField]
    private HyperJump hyperJump; // use for get and set brake
    [SerializeField]
    private DistanceMeasurement distanceMeasurement; // use for get and set brake
    private LeaningInputAdapter leaningInputAdapter;



    //=====================================variables for file input and output======================//

    private StreamReader inputDataFile;
    private StreamWriter participantDataFile;
    private StreamWriter pointingDataFile;
    private StreamWriter pointingAverageDataFile;
    private StreamWriter controllerInfoWhileAdjustingDistance;
    private StreamWriter waypointDataFile;
    private StreamWriter pointingAveragePathDataFile;
    private bool participantDataFileisOpen = true;


    //================user and trial Info==================================//

    string participantID;
    string trialNo;
    string interfaceName;
    string pathID;
    string dateAndTime;
    private string jumpNoJump;
    private string leaningGamepad;
    private bool hyperJumpOn, hyperJumpRotationOn;
    

    //==================================variables for behavioral data===========================//

    private Vector3 previousVirtualPosition, previousVirtualOrientation, previousVirtualTranslationalVelocity, previousVirtualRotationalVelocity, previousVirtualTranslationalAcceleration, previousVirtualRotationalAcceleration, previousVirtualTranslationalJerk, previousVirtualRotationalJerk, previousPhysicalPosition, previousPhysicalOrientation, previousPhysicalTranslationalVelocity, previousPhysicalRotationalVelocity, previousPhysicalTranslationalAcceleration, previousPhysicalRotationalAcceleration, previousPhysicalTranslationalJerk, previousPhysicalRotationalJerk;
	private Vector3 currentVirtualPosition, currentVirtualOrientation, currentVirtualTranslationalVelocity, currentVirtualRotationalVelocity, currentVirtualTranslationalAcceleration, currentVirtualRotationalAcceleration, currentVirtualTranslationalJerk, currentVirtualRotationalJerk, currentPhysicalPosition, currentPhysicalOrientation, currentPhysicalTranslationalVelocity, currentPhysicalRotationalVelocity, currentPhysicalTranslationalAcceleration, currentPhysicalRotationalAcceleration, currentPhysicalTranslationalJerk, currentPhysicalRotationalJerk;
    private Vector3 currentCameraRigPosition, previousCameraRigPosition;
    float distanceTravelledframe;
    private float experimentTime = 0f;
    private List<float>[] pointingErrorListYaw = new List<float>[3];
    private List<float>[] pointingErrorListPitch = new List<float>[3];
    private List<float>[] pointingTimeList = new List<float>[3];
    private float lastWayPointTime = 0, totalJumpDistanceBetweenWaypoints = 0, totalAngularJumpBetweenWaypoints = 0;
    private float timeToPoint;
    private float FMSValue;
    private int noOfTranslationalJumpsBetweenWaypoints = 0, noOfRotationalJumpsBetweenWaypoints = 0;
    private Vector3 lastWayPointLocation, sumOfVelocityBetweenWaypointsWithoutJump = Vector3.zero;    
    private float wayPointshortestDistance, wayPointTime;
    private Vector3 wayPointLocation, playerPositionwhileCrossingWaypoint;
    private float controllerVelocity = 0f;
    private float[] controllerVelocitys; 

    //===================variables for state machine========================//
    private int instruction = 0;
    private bool triggerPressed = false;
    private int totalTargetInstance = 2;             
    private int noOfPointings = 1;
    private bool pointUp = true;
    private bool pointToTarget = false;
    private bool _pointingTask = false;
    private bool distanceTask = false;
    private bool _showDistanceMeasurement;
    public bool showDistanceMeasurement;
    private bool pointingTask;
    private bool firstTime = true;
    private bool shufflePointingList = true;
    private bool distancePrompt = false;
    private bool measure2ndPointing = false;
    private bool motionSicknessPrompt = false;

    //=======================variables for waypoint data========================//
    private bool _writeWayPointData = false;
    public bool writeWayPointData
    {
        set {_writeWayPointData = value;}
    }
    private bool blueWaypoint;
    private string wayPointName;
    private int waypointID = 0;
    private float timeSpentHighSpeed = 0, distanceTravelledHighSpeed = 0, totalDistanceTravelledBetweenWaypoints = 0, averageVelocityHighSpeed = 0, averageVelocityNormalSpeed = 0, frameHighSpeed = 0, frameNormalSpeed = 0;
    private float travelLookAngle = 0;
    private int CONTROLLERVELOCITYAVERAGEFRAME = 2;
   
    void Start()
    {
        audioPlayer = GetComponent<AudioSource>();
        pointers[0].SetActive(true);
        dateAndTime = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
        Vector3 targetDirection = convertToSpherical(pathTargets[0].transform.position - vrCamera.transform.position);
        this.gameObject.transform.eulerAngles = new Vector3 (this.gameObject.transform.eulerAngles.x, targetDirection.y, this.gameObject.transform.eulerAngles.z);
        controllerVelocitys = new float[CONTROLLERVELOCITYAVERAGEFRAME];
        hyperJump.SetBreak(true);

        // Load and create file for writing data
        loadParticipantData();
        createFileForPointingData();
        createFileForWayPointData();

        // Reset variables at the start
        lastWayPointLocation = vrCamera.transform.position;
        pointerAnimationObject.SetActive(false);
        txt = SSQUI.GetComponent<Text>();
        for (int i = 0; i < 3; i++)
        {
            pointingErrorListYaw[i] = new List<float>();
            pointingErrorListPitch[i] = new List<float>();
            pointingTimeList[i] = new List<float>();
        }
        StartCoroutine(Updater());
        StartCoroutine(ControllerVeloctiy());        
    }

    IEnumerator Updater()
    {
        while (Application.isPlaying)
        {
            float controllerVelocityAverage = calculateControllerVelocityAverage();
            if (firstTime)
            {
                if (leaningInputAdapter.IsCalibrated())
                {
                    _pointingTask = true;
                    firstTime = false;
                    Debug.Log("After Calibration");
                }
            }

            // Pointing task Manager
            if (totalTargetInstance < 7)
            {
                // "Waiting for pointingTask to be true"
                // Triggered by the last waypoint collision
                if (_pointingTask)
                {
                    if (shufflePointingList)
                    {
                        ShufflePointingListFunction();
                        shufflePointingList = false;
                    }
                    if (instruction == 0)
                    {
                        if (totalTargetInstance - 3 >= 0 && totalTargetInstance!=6 && totalTargetInstance!=7)
                        {
                            pathWaypoints[totalTargetInstance - 3].SetActive(false);
                        }
                    }
                    if (!audioPlayer.isPlaying)
                    {
                        if (pointUp && ((totalTargetInstance != 6 && instruction < totalTargetInstance + 1)|| (totalTargetInstance == 6 && instruction < totalTargetInstance)))
                        {
                            timeToPoint += Time.deltaTime;
                            // Debug.Log("Waiting to point Target");
                            if (((Mathf.Abs(controller.transform.eulerAngles.x) > 320 || Mathf.Abs(controller.transform.eulerAngles.x) < 45)) || instruction == 0)
                            {
                                thinPointLaser.SetActive(true);
                                if (controllerVelocityAverage < .05f)
                                {
                                    if (instruction != 0)
                                    {
                                        shootRay();
                                        pointerAnimationObject.SetActive(false);
                                        controllerAudioPlayer.clip = pointedTargetDing;
                                        controllerAudioPlayer.Play();

                                        writePointingData(0);
                                        timeToPoint = 0;
                                        noOfPointings++;
                                    }
                                    thinPointLaser.SetActive(false);
                                    instruction++;
                                    // Debug.Log("Instruction No: " + instruction + ", Target No: " + totalTargetInstance);
                                    audioPlayer.clip = pathInstructions[0];
                                    if ((totalTargetInstance != 6 && instruction < totalTargetInstance + 1) || (totalTargetInstance == 6 && instruction < totalTargetInstance)) audioPlayer.Play();
                                    
                                    pointUp = false;
                                    pointToTarget = true;
                                }
                                // Debug.Log("1. Point up prompt + Instruction: " + instruction + " + Pointing Task: " + _pointingTask + " + pointUp: " + pointUp + " + totalTargetInstance: " + totalTargetInstance);
                            }

                        }
                        else if (((Mathf.Abs(controller.transform.eulerAngles.x) < 320 && Mathf.Abs(controller.transform.eulerAngles.x) > 45)))
                        {
                            thinPointLaser.SetActive(false);
                        }
                    }
                    if (pointToTarget && ((totalTargetInstance != 6 && instruction < totalTargetInstance + 1) || (totalTargetInstance == 6 && instruction < totalTargetInstance)))
                    {
                        // Debug.Log("Waiting to point up");
                        if (Mathf.Abs(controller.transform.eulerAngles.x) > 250 && Mathf.Abs(controller.transform.eulerAngles.x) < 290)
                        {
                            StartCoroutine(pointUpAnimation());
                            controllerAudioPlayer.clip = pointUpDing;
                            controllerAudioPlayer.Play();
                            pointToTarget = false;
                            yield return new WaitForSeconds(.5f);                                
                            audioPlayer.clip = pathInstructions[instruction];
                            audioPlayer.Play();
                            pointUp = true;
                            // Debug.Log("2. Target Prompt + Instruction: " + instruction + " + Pointing Task: " + _pointingTask + " + pointUp: " + pointUp + " + totalTargetInstance: " + totalTargetInstance + + pathInstructions[totalTargetInstance + 1 - instruction]);
                        }
                    }
                    else if (((instruction == totalTargetInstance + 1) && (totalTargetInstance != 6))|| ((instruction == totalTargetInstance) && (totalTargetInstance == 6)))
                    {
                        _pointingTask = false;
                        distanceTask = true;
                        instruction = 0;
                        distancePrompt = true;
                        pointUp = true;
                        pointToTarget = false;
                        // Debug.Log("3. Pointing Task Done + Instruction: " + instruction + " + Pointing Task: " + _pointingTask + " + pointUp: " + pointUp+ " + totalTargetInstance: " + totalTargetInstance);
                    }
                }
                if (distanceTask)
                {
                    // Debug.Log("Instruction: " + instruction + ", Target No:" + totalTargetInstance);
                    if (!audioPlayer.isPlaying)
                    {
                        if(instruction == 0)
                        {
                            yield return new WaitForSeconds(.5f);
                            audioPlayer.clip = pathInstructions[6];
                            audioPlayer.Play();
                            instruction++;
                        }
                        else if (((instruction < totalTargetInstance + 1) && (totalTargetInstance != 6)) || ((instruction < totalTargetInstance) && (totalTargetInstance == 6)))
                        {
                            _showDistanceMeasurement = true;
                            if (distancePrompt)
                            {
                                yield return new WaitForSeconds(.5f);
                                audioPlayer.clip = pathInstructions[instruction];
                                audioPlayer.Play();
                                distancePrompt = false;
                                measure2ndPointing = true;
                            }
                            else
                            {
                                timeToPoint += Time.deltaTime;
                                if (((Mathf.Abs(controller.transform.eulerAngles.x) > 320 || Mathf.Abs(controller.transform.eulerAngles.x) < 30)))
                                {
                                    thinPointLaser.SetActive(true);
                                }
                                else
                                {
                                    thinPointLaser.SetActive(false);
                                }
                                if (distanceMeasurement.distanceMeasurementStart)
                                {
                                    if (measure2ndPointing)
                                    {
                                        writePointingData(1);
                                        noOfPointings++;
                                        measure2ndPointing = false;
                                    }
                                    measurementCone.SetActive(true);
                                    triggerPressed = true;
                                    recordControllerInfoWhilePointing();
                                }
                                if (triggerPressed)
                                {
                                    if (!distanceMeasurement.distanceMeasurementStart)
                                    {
                                        controllerAudioPlayer.clip = distanceMeasurementDing;
                                        controllerAudioPlayer.Play();
                                        writePointingData(2);
                                        noOfPointings++;
                                        timeToPoint = 0;
                                        instruction++;
                                        distancePrompt = true;
                                        triggerPressed = false;
                                        motionSicknessPrompt = true;
                                        measurementCone.SetActive(false);
                                        thinPointLaser.SetActive(false);
                                    }
                                }
                            }
                        } 
                        else if  (((instruction == totalTargetInstance + 1) && (totalTargetInstance != 6)) || ((instruction == totalTargetInstance) && (totalTargetInstance == 6)))
                        {
                            thinPointLaser.SetActive(false);
                            _showDistanceMeasurement = false;
                            if (motionSicknessPrompt)
                            {
                                audioPlayer.clip = pathInstructions[8];
                                audioPlayer.Play();
                                motionSicknessPrompt = false;
                            }
                            SSQbar.SetActive(true);
                            Vector2 cameraForward = new Vector2(vrCamera.transform.forward.x, vrCamera.transform.forward.z);
                            Vector2 controllerForward = new Vector2(controller.transform.forward.x, controller.transform.forward.z);
                            float forwardAngle = Vector2.SignedAngle(cameraForward, controllerForward);
                            float scaleFMS = (forwardAngle + 30)/30  - 1;
                            if (scaleFMS > 1) scaleFMS = 1;
                            if (scaleFMS < -1) scaleFMS = -1;
                            SSQpointer.transform.localPosition = new Vector3(0,scaleFMS,0);   
                            FMSValue = 100 - 50 * (scaleFMS + 1);    
                            txt.text = (FMSValue).ToString("F0");
                            // if (distanceMeasurement.interactWithUI != null && distanceMeasurement.interactWithUI.GetState(distanceMeasurement.pose.inputSource))
                            {
                                SSQbar.SetActive(false);
                                instruction++;
                            }
                        }
                        else
                        {
                            if (totalTargetInstance != 6)
                            {
                                audioPlayer.clip = pathInstructions[7];
                                audioPlayer.Play();
                            }
                            writePointDataAverageFile();
                            // restart locomotion
                            distanceTask = false;
                            shufflePointingList = true;
                            instruction = 0;
                            if (totalTargetInstance - 2 < pathWaypoints.Length)
                            {
                                pathWaypoints[totalTargetInstance - 2].SetActive(true);
                                pointers[totalTargetInstance - 2].SetActive(false);
                                pointers[totalTargetInstance - 1].SetActive(true);
                                pointers[0].SetActive(false);
                            }
                            totalTargetInstance++;
                            hyperJump.SetBreak(false);

                            //reset values for waypoints
                            lastWayPointTime = Time.time;
                            lastWayPointLocation = vrCamera.transform.position;
                            noOfRotationalJumpsBetweenWaypoints = 0;
                            noOfTranslationalJumpsBetweenWaypoints = 0;
                            totalJumpDistanceBetweenWaypoints = 0;
                            totalAngularJumpBetweenWaypoints = 0;
                            sumOfVelocityBetweenWaypointsWithoutJump = Vector3.zero;
                            waypointID = 0;

                            // Reset Pointing Error List
                            for (int i = 0; i < 3; i++)
                            {
                                pointingErrorListYaw[i] = new List<float>();
                                pointingErrorListPitch[i] = new List<float>();
                                pointingTimeList[i] = new List<float>();

                            }
                        }
            
                    }
                
                }
            
            }
            else
            {
                if (writeData)
                {
                    pointingDataFile.Close();
                    participantDataFile.Close();
                }
                participantDataFileisOpen = false;
                Application.Quit();
            }

            yield return null; 
        }
        pointingDataFile.Close();
        participantDataFile.Close();
        participantDataFileisOpen = false;

    }

    //=====================Update Info============================//
    void LateUpdate()
    {
        Vector3 targetDirection = convertToSpherical(pathTargets[0].transform.position - vrCamera.transform.position);
        Vector3 facingDirection = convertToSpherical(vrCamera.transform.forward);
        // Debug.Log("Target: " + targetDirection.y + "," + "Facing: " + facingDirection.y);  
        experimentTime += Time.deltaTime;
        calculateSpeed();
        string teleportTranslational = "";
        string teleportRotational = "";
        totalDistanceTravelledBetweenWaypoints += distanceTravelledframe;
        travelLookAngle += Mathf.Abs(Vector3.Angle(currentVirtualTranslationalVelocity, vrCamera.transform.forward));
    
        // jumping frame or not
        if (hyperJumpOn)
        {
            if (hyperJump._STATE_jumpedThisFrame)
            {
                teleportTranslational = "Teleport";
                noOfTranslationalJumpsBetweenWaypoints++;
                totalJumpDistanceBetweenWaypoints += hyperJump._STATE_distanceLastJump;
                frameHighSpeed++;
            }
            else
            {
                teleportTranslational = "Continuous";
                averageVelocityNormalSpeed += currentVirtualTranslationalVelocity.magnitude;
                frameNormalSpeed++;
            }
        }
        else
        {
            if (currentVirtualTranslationalVelocity.magnitude > 7)
            {
                teleportTranslational = "HighSpeed";
                timeSpentHighSpeed += Time.deltaTime;
                distanceTravelledHighSpeed += distanceTravelledframe;
                averageVelocityHighSpeed += currentVirtualTranslationalVelocity.magnitude;
                frameHighSpeed++;
            }
            else
            {
                teleportTranslational = "NormalSpeed";
                averageVelocityNormalSpeed += currentVirtualTranslationalVelocity.magnitude;
                frameNormalSpeed++;
            }
        }

        if (hyperJump._STATE_rotationalJumpThisFrame)
        {
            teleportRotational = "Teleport";
            noOfRotationalJumpsBetweenWaypoints++;
            totalAngularJumpBetweenWaypoints += hyperJump._STATE_angleOfVirtualRotationThisFrame;

        }
        else
        {
            teleportRotational = "Continuous";
        }

        // track participant movement
        if (participantDataFileisOpen && writeData)
        {
           writeTrackingData(teleportTranslational, teleportRotational);
        }

        // track data for waypoint
        if (_writeWayPointData && writeData)
        {
           writeWayPointDataFile(teleportTranslational, teleportRotational);
        }
    }
    public void updateWayPointInfo(bool blueWaypoint, float wayPointshortestDistance, float wayPointTime, Vector3 wayPointLocation, Vector3 playerPosition, string wayPointName)
    {
        this.blueWaypoint = blueWaypoint;
        this.wayPointshortestDistance = wayPointshortestDistance;
        this.wayPointTime = wayPointTime;
        this.wayPointLocation = wayPointLocation;
        this.playerPositionwhileCrossingWaypoint = playerPosition;
        this.wayPointName = wayPointName;
    }
    //=====================Update Info============================//

    //=============File Read and Write Operations================//
    private void loadParticipantData()
    {
        inputDataFile = new StreamReader ("input.txt");
        inputDataFile.ReadLine ();
        string participantRecord =  inputDataFile.ReadLine();
        string[] participantFields = participantRecord.Split(',');
        participantID = participantFields[0];
        trialNo = participantFields[0];
        interfaceName = participantFields[0];
        pathID = participantFields[0];
        
        switch (interfaceName)
        {
            case "Controller":
            hyperJumpOn = false;
            break;
            case "ControllerTeleport":
            hyperJumpOn = true;
            break;
            case "Leaning":
            hyperJumpOn = false;
            break;
            case "LeaningTeleport":
            hyperJumpOn = true;
            break;
            default:
            throw new UnityException ("Mistake in interface name | Check input.txt");
        }
        if (writeData)
        {
            try
            {
                StreamReader ifOutputFileExists = new StreamReader ("Results\\" + dateAndTime + "_" +  participantID  + "_" + interfaceName + "_" + "Behavior" + ".csv");
                ifOutputFileExists.Close ();
                participantDataFile = new StreamWriter ("Results\\" + dateAndTime + "_" +  participantID + "_" + interfaceName + "_" + "Behavior" + ".csv", true, System.Text.Encoding.UTF8, 1024 * 3);
            }
            catch
            {
                 participantDataFile = new StreamWriter ("Results\\" + dateAndTime + "_" +  participantID + "_" + interfaceName + "_" + "Behavior" +".csv", true, System.Text.Encoding.UTF8, 1024 * 3);
                string nextLine = "Participant,TimeOfDay,TimeSinceStartOfExperiment,JumpNoJump,LeaningGamepad,Interface,TrialNo,PathID,LocationAlongPath,NavigationOrPointing,TeleportORHighSpeedTranslation,TeleportORContinuousRotation";
                    nextLine += ",Virtual Translational Velocity Magnitude,Virtual Translational Accelerational Magnitude,Virtual Rotational Velocity Magnitude,Virtual Rotational Acceleration Magnitude";
                    nextLine += ",Input, Input X, Input Y";
                    nextLine += ",Virtual Position X,Virtual Position Y,Virtual Position Z";
                    nextLine += ",Virtual Orientation X,Virtual Orientation Y,Virtual Orientation Z";
                    nextLine += ",Physical Position X,Physical Position Y,Physical Position Z";
                    nextLine += ",Physical Orientation X,Physical Orientation Y,Physical Orientation Z";
                    nextLine += ",Virtual Translational Velocity X,Virtual Translational Velocity Y,Virtual Translational Velocity Z";
                    nextLine += ",Virtual Rotational Velocity X,Virtual Rotational Velocity Y,Virtual Rotational Velocity Z";
                    nextLine += ",Physical Translational Velocity X,Physical Translational Velocity Y,Physical Translational Velocity Z";
                    nextLine += ",Physical Rotational Velocity X,Physical Rotational Velocity Y,Physical Rotational Velocity Z";
                    nextLine += ",Virtual Translational Acceleration X,Virtual Translational Acceleration Y,Virtual Translational Acceleration Z";
                    nextLine += ",Virtual Rotational Acceleration X,Virtual Rotational Acceleration Y,Virtual Rotational Acceleration Z";
                    nextLine += ",Physical Translational Acceleration X,Physical Translational Acceleration Y,Physical Translational Acceleration Z";
                    nextLine += ",Physical Rotational Acceleration X,Physical Rotational Acceleration Y,Physical Rotational Acceleration Z";
                    nextLine += ",Virtual Translational Jerk X,Virtual Translational Jerk Y,Virtual Translational Jerk Z";
                    nextLine += ",Virtual Rotational Jerk X,Virtual Rotational Jerk Y,Virtual Rotational Jerk Z";
                    nextLine += ",Physical Translational Jerk X,Physical Translational Jerk Y,Physical Translational Jerk Z";
                    nextLine += ",Physical Rotational Jerk X,Physical Rotational Jerk Y,Physical Rotational Jerk Z";
                participantDataFile.WriteLine (nextLine);
                participantDataFile.Close();

            }
        }
        inputDataFile.Close();

    }
    private void createFileForPointingData()
    {
        if (writeData)
        {
            try
            {
                // Debug.Log("From try");
                StreamReader ifOutputFileExists = new StreamReader ("Results\\" + dateAndTime + "_" +  participantID +  "_" + interfaceName + "_" + "Pointing" + ".csv");
                ifOutputFileExists.Close ();
                pointingDataFile = new StreamWriter ("Results\\" + dateAndTime + "_" +  participantID +  "_" + interfaceName + "_" + "Pointing" + ".csv", true, System.Text.Encoding.UTF8, 1024 * 3);
                ifOutputFileExists = new StreamReader ("Results\\" + dateAndTime + "_" +  participantID +  "_" + interfaceName + "_" + "PointingAverage" + ".csv");
                ifOutputFileExists.Close ();
                pointingAverageDataFile = new StreamWriter ("Results\\" + dateAndTime + "_" +  participantID +  "_" + interfaceName + "_" + "PointingAverage" + ".csv", true, System.Text.Encoding.UTF8, 1024 * 3);
            }
            catch
            {
                // Debug.Log("From Catch");
                pointingDataFile = new StreamWriter ("Results\\" + dateAndTime + "_" +  participantID +  "_" + interfaceName + "_" + "Pointing" +".csv", true, System.Text.Encoding.UTF8, 1024 * 3);
                string nextLine = "Participant,TimeOfDay,TimeSinceStartOfExperiment,JumpNoJump,LeaningGamepad,Interface,TrialNo,PathID,LocationALongPath,TotalPointing,PointingCounterFromLocation,PointingPhase,Trivial";
                    nextLine += ",TargetID";
                    nextLine += ",Controller Position X,Controller Position Y,Controller Position Z";
                    nextLine += ",Target Position X,Target Position Y,Target Position Z";
                    nextLine += ",Target Yaw,Target Pitch, Pointing Yaw, Pointing Pitch";
                    nextLine += ",Signed Pointing Error Yaw, Signed Pointing Error Pitch";
                    nextLine += ",Absolute Pointing Error Yaw, Absolute Pointing Error Pitch";
                    nextLine += ",Pointing Time";
                    nextLine += ",Correct distance in 3D";
                    nextLine += ",Distance Estimate";
                    nextLine += ",Signed Distance Estimate Error";
                    nextLine += ",Absolute Distance Estimate Error";
                    // Debug.Log("Next Line: " + nextLine);
                    pointingDataFile.WriteLine(nextLine);
                    pointingDataFile.Close();
                pointingAverageDataFile = new StreamWriter ("Results\\" + dateAndTime + "_" +  participantID +  "_" + interfaceName + "_" + "PointingAverage" + ".csv", true, System.Text.Encoding.UTF8, 1024 * 3);
                nextLine = "Participant,TimeOfDay,TimeSinceStartOfExperiment,JumpNoJump,LeaningGamepad,Interface,TrialNo,PathID,LocationALongPath, PointingPhase";
                    nextLine += ",Signed Average Pointing Error Yaw,Signed Average Pointing Error Pitch";
                    nextLine += ",Average Absoulte Pointing Error Yaw,Average Absolute Pointing Error Pitch";
                    nextLine += ",Configuration Error Yaw,Configuration Error Pitch,Average Response Time,FMSValue";
                pointingAverageDataFile.WriteLine(nextLine);
                pointingAverageDataFile.Close();
                controllerInfoWhileAdjustingDistance  = new StreamWriter ("Results\\" + dateAndTime + "_" +  participantID +  "_" + interfaceName + "_" + "ControllerWhilePointing" + ".csv", true, System.Text.Encoding.UTF8, 1024 * 3);
                nextLine = "Participant,TimeOfDay,TimeSinceStartOfExperiment,JumpNoJump,LeaningGamepad,Interface,TrialNo,PathID,LocationALongPath,TotalPointing,PointingCounterFromLocation,Trivial";
                    nextLine += ",TargetID";
                    nextLine += ",Controller Position X,Controller Position Y,Controller Position Z";
                    nextLine += ",Target Position X,Target Position Y,Target Position Z";
                    nextLine += ",Target Yaw,Target Pitch, Pointing Yaw, Pointing Pitch";
                    nextLine += ",Signed Pointing Error Yaw, Signed Pointing Error Pitch";
                    nextLine += ",Absolute Pointing Error Yaw, Absolute Pointing Error Pitch";
                    nextLine += ",Pointing Time";
                    nextLine += ",Correct distance in 3D";
                    nextLine += ",Distance Estimate";
                    nextLine += ",Signed Distance Estimate Error";
                    nextLine += ",Absolute Distance Estimate Error";
                controllerInfoWhileAdjustingDistance.WriteLine(nextLine);
                controllerInfoWhileAdjustingDistance.Close();
            }
        }
        
    }
    private void createFileForWayPointData()
    {
        if (writeData)
        {
            try
            {
                // Debug.Log("From try");
                StreamReader ifOutputFileExists = new StreamReader ("Results\\" + dateAndTime + "_" +  participantID + "_" + interfaceName + "_" + "WayPoint" + ".csv");
                ifOutputFileExists.Close ();
                waypointDataFile = new StreamWriter ("Results\\" + dateAndTime + "_" +  participantID + "_" + interfaceName + "_" + "WayPoint" + ".csv", true, System.Text.Encoding.UTF8, 1024 * 3);
                }
            catch
            {
                // Debug.Log("From Catch");
                waypointDataFile = new StreamWriter ("Results\\" + dateAndTime + "_" +  participantID + "_" + interfaceName + "_" + "WayPoint" +".csv", true, System.Text.Encoding.UTF8, 1024 * 3);
                string nextLine = "Participant,TimeOfDay,TimeSinceStartOfExperiment,JumpNoJump,LeaningGamepad,Interface,TrialNo,PathID,LocationALongPath";
                    nextLine += ",WaypointID,BlueOrRed,WaypointName";
                    nextLine += ",WaypointPositionX,WaypointPositionY";
                    nextLine += ",UserPositionX,UserPositionY";
                    nextLine += ",AbsoluteWaypointError";
                    nextLine += ",TimeOfCrossingTheWaypoint";
                    nextLine += ",CurrentVelocityMagnitude";
                    nextLine += ",TeleportingOrHighSpeedTranslational,TeleportingOrNotRotational";
                    nextLine += ",TimeFromLastWaypoint";
                    nextLine += ",DistanceFromLastWaypoint";
                    nextLine += ",DistanceTravelledFromLastWaypoint";
                    nextLine += ",NoOfJumpsBetweenWayPoints";
                    nextLine += ",AverageJumpDistance";
                    nextLine += ",TimeSpentInHighSpeed";
                    nextLine += ",PercentofTimeSpentInHighSpeed";
                    nextLine += ",AverageVelocityNoJumpORHighSpeed";
                    nextLine += ",AverageVelocityIncludingJumpORHighSpeed";
                    nextLine += ",TotalDistancePowerMode";
                    nextLine += ",PercentofTravelasPowerDistance";
                    nextLine += ",TotalJumpAnglefromLastWaypoint";
                    nextLine += ",averageTravelLookAngleDifference";
                    // Debug.Log("Next Line: " + nextLine);
                waypointDataFile.WriteLine(nextLine);
                waypointDataFile.Close();
            }
        }
        

    }
    private void writeTrackingData(string teleportTranslational, string teleportRotational)
    {
        if (writeData)
        {
            string navigationOrPointing = "";
            string location = "";

            // Conditions of the experiment
            
            // navigating or Pointing
            if (!pointingTask && !distanceTask && totalTargetInstance !=2)
            {
                navigationOrPointing = "Navigation"; 
                location = (totalTargetInstance-2) + "__" + (totalTargetInstance -1); 
            }
            else if (pointingTask || distanceTask)
            {
                navigationOrPointing = "Pointing";
                location = (totalTargetInstance - 1).ToString();
            }
            else if (!pointingTask && !distanceTask && totalTargetInstance == 2)
            {
                navigationOrPointing = "Before Calibration";
                location = (totalTargetInstance-1).ToString();
            }
            participantDataFile = new StreamWriter ("Results\\" + dateAndTime + "_" +  participantID + "_" + interfaceName + "_" + "Behavior" +".csv", true, System.Text.Encoding.UTF8, 1024 * 3);
            string nextLine = participantID + "," + System.DateTime.Now + "," + experimentTime + "," + jumpNoJump + "," + leaningGamepad + "," + interfaceName + "," + trialNo + "," + pathID + "," + location  + "," + navigationOrPointing + "," + teleportTranslational + "," + teleportRotational + ",";
                nextLine += currentVirtualTranslationalVelocity.magnitude + "," + currentVirtualTranslationalAcceleration.magnitude + "," + currentVirtualRotationalVelocity.magnitude + "," + currentVirtualRotationalAcceleration.magnitude + ",";
                nextLine += currentVirtualPosition.x + "," + currentVirtualPosition.y + "," + currentVirtualPosition.z + ",";
                nextLine += currentVirtualOrientation.x + "," + currentVirtualOrientation.y + "," + currentVirtualOrientation.z + ",";
                nextLine += currentPhysicalPosition.x + "," + currentPhysicalPosition.y + "," + currentPhysicalPosition.z + ",";
                nextLine += currentPhysicalOrientation.x + "," + currentPhysicalOrientation.y + "," + currentPhysicalOrientation.z + ",";
                nextLine += currentVirtualTranslationalVelocity.x + "," + currentVirtualTranslationalVelocity.y + "," + currentVirtualTranslationalVelocity.z + ",";
                nextLine += currentVirtualRotationalVelocity.x + "," + currentVirtualRotationalVelocity.y + "," + currentVirtualRotationalVelocity.z + ",";
                nextLine += currentPhysicalTranslationalVelocity.x + "," + currentPhysicalTranslationalVelocity.y + "," + currentPhysicalTranslationalVelocity.z + ",";
                nextLine += currentPhysicalRotationalVelocity.x + "," + currentPhysicalRotationalVelocity.y + "," + currentPhysicalRotationalVelocity.z + ",";
                nextLine += currentVirtualTranslationalAcceleration.x + "," + currentVirtualTranslationalAcceleration.y + "," + currentVirtualTranslationalAcceleration.z + ",";
                nextLine += currentVirtualRotationalAcceleration.x + "," + currentVirtualRotationalAcceleration.y + "," + currentVirtualRotationalAcceleration.z + ",";
                nextLine += currentPhysicalTranslationalAcceleration.x + "," + currentPhysicalTranslationalAcceleration.y + "," + currentPhysicalTranslationalAcceleration.z + ",";
                nextLine += currentPhysicalRotationalAcceleration.x + "," + currentPhysicalRotationalAcceleration.y + "," + currentPhysicalRotationalAcceleration.z + ",";
                nextLine += currentVirtualTranslationalJerk.x + "," + currentVirtualTranslationalJerk.y + "," + currentVirtualTranslationalJerk.z + ",";
                nextLine += currentVirtualRotationalJerk.x + "," + currentVirtualRotationalJerk.y + "," + currentVirtualRotationalJerk.z + ",";
                nextLine += currentPhysicalTranslationalJerk.x + "," + currentPhysicalTranslationalJerk.y + "," + currentPhysicalTranslationalJerk.z + ",";
                nextLine += previousPhysicalRotationalJerk.x + "," + previousPhysicalRotationalJerk.y + "," + previousPhysicalRotationalJerk.z;
            participantDataFile.WriteLine (nextLine);
            participantDataFile.Close();  
        }
    }
    private void writePointingData(int pointingPhaseNo)
    {
        if (writeData)
        {
            Vector3 controllerForwardOrientation = convertToSpherical(controller.transform.forward);
            Vector3 userTargetOrientation = convertToSpherical(pathTargets[instruction - 1].transform.position - controller.transform.position);
            // Debug.Log(pathTargets[instruction - 1]);
            float pointingErrorYaw = controllerForwardOrientation.y - userTargetOrientation.y;
            if (pointingErrorYaw > 180) pointingErrorYaw -= 360;
            if (pointingErrorYaw < -180) pointingErrorYaw += 360;
            float pointingErrorPitch = userTargetOrientation.z - controllerForwardOrientation.z;
            float pointingErrorYawAbsolute = Mathf.Abs(pointingErrorYaw);
            float pointingErrorPitchAbsolute = Mathf.Abs(pointingErrorPitch);
            if (instruction != totalTargetInstance)
            {
                pointingErrorListYaw[pointingPhaseNo].Add(pointingErrorYaw);
                pointingErrorListPitch[pointingPhaseNo].Add(pointingErrorPitch);
                pointingTimeList[pointingPhaseNo].Add(timeToPoint);
            }
            string pointingPhase = "";
            if (pointingPhaseNo == 0) pointingPhase = "rapid1stPointing";
            else if (pointingPhaseNo == 1) pointingPhase = "rapid2ndPointing";
            else pointingPhase = "distanceJudgmentAnd2ndPointing";
            string trivial = "No";
            if (instruction == totalTargetInstance) trivial = "Yes";
            if (totalTargetInstance == 2) trivial = "Yes";
            pointingDataFile = new StreamWriter ("Results\\" + dateAndTime + "_" +  participantID + "_" + interfaceName + "_" + "Pointing" +".csv", true, System.Text.Encoding.UTF8, 1024 * 3);
            string nextLine = participantID + "," + System.DateTime.Now + "," + experimentTime + "," + jumpNoJump + "," + leaningGamepad + "," + interfaceName + "," + trialNo + "," + pathID + "," + (totalTargetInstance - 1) + "," + noOfPointings + "," + instruction + "," + pointingPhase + "," + trivial + ",";
                    nextLine += pathInstructions[instruction] + ",";
                    nextLine += controller.transform.position.x + "," +  controller.transform.position.y + "," + controller.transform.position.z + ",";
                    nextLine += pathTargets[instruction - 1].transform.position.x + "," +  pathTargets[instruction - 1].transform.position.y + "," + pathTargets[instruction - 1].transform.position.z + ",";
                    nextLine += userTargetOrientation.y + "," + userTargetOrientation.z + "," + controllerForwardOrientation.y + "," + controllerForwardOrientation.z + ",";
                    nextLine += pointingErrorYaw + "," + pointingErrorPitch + ",";
                    nextLine += pointingErrorYawAbsolute + "," + pointingErrorPitchAbsolute + ",";
                    nextLine += timeToPoint + ",";
                if (pointingPhaseNo == 2)
                {
                    float distanceError = userTargetOrientation.x - distanceMeasurement.distanceEstimate;
                    nextLine += userTargetOrientation.x + ",";
                    nextLine += distanceMeasurement.distanceEstimate + ",";
                    nextLine += distanceError + ",";
                    nextLine += Mathf.Abs(distanceError);   
                }                                     
            pointingDataFile.WriteLine (nextLine);
            pointingDataFile.Close();           
            

        }
    }
    private void writePointDataAverageFile()
    {
        if (writeData)
        {
            if (totalTargetInstance > 2)
            {
                float[] averagePointingErrorYaw = new float[3];
                float[] averagePointingErrorPitch = new float[3];
                float[] averagePointingTime = new float[3];
                float[] sdPointingErrorYaw = new float[3];
                float[] sdPointingErrorPitch = new float[3];
                
                for (int i = 0; i < 3; i++)
                {
                    averagePointingErrorYaw[i] = circularAverage(pointingErrorListYaw[i]);
                    averagePointingErrorPitch[i] = circularAverage(pointingErrorListPitch[i]);
                    averagePointingTime[i] = floatListAverage(pointingTimeList[i]);
                    sdPointingErrorYaw[i] = circularStandardDeviation (pointingErrorListYaw[i]);
                    sdPointingErrorPitch[i] = circularStandardDeviation (pointingErrorListPitch[i]);
                }

                pointingAverageDataFile = new StreamWriter ("Results\\" + dateAndTime + "_" +  participantID + "_" + interfaceName + "_" + "PointingAverage" + ".csv", true, System.Text.Encoding.UTF8, 1024 * 3);
                for (int i = 0; i < 3; i++)
                {
                    string pointingPhase = "";
                    if (i == 0) pointingPhase = "rapid1stPointing";
                    else if (i == 1) pointingPhase = "rapid2ndPointing";
                    else pointingPhase = "distanceJudgmentAnd2ndPointing";

                    string nextLine = participantID + "," + System.DateTime.Now + "," + experimentTime + "," + jumpNoJump + "," + leaningGamepad + "," + interfaceName + "," + trialNo + "," + pathID + "," + (totalTargetInstance-1) + "," + pointingPhase + ",";
                    nextLine += averagePointingErrorYaw[i] + "," + averagePointingErrorPitch[i] + ",";
                    nextLine += Mathf.Abs(averagePointingErrorYaw[i]) + "," + Mathf.Abs(averagePointingErrorPitch[i]) + ",";
                    nextLine += sdPointingErrorYaw[i] + "," + sdPointingErrorPitch[i] + ",";
                    nextLine += averagePointingTime[i];
                    if (i == 2)  nextLine += "," + FMSValue;
                    pointingAverageDataFile.WriteLine(nextLine);

                }
                pointingAverageDataFile.Close();
            }
            else
            {
                pointingAverageDataFile = new StreamWriter ("Results\\" + dateAndTime + "_" +  participantID + "_" + interfaceName + "_" + "PointingAverage" + ".csv", true, System.Text.Encoding.UTF8, 1024 * 3);
                string nextLine = participantID + "," + System.DateTime.Now + "," + experimentTime + "," + jumpNoJump + "," + leaningGamepad + "," + interfaceName + "," + trialNo + "," + pathID + "," + (totalTargetInstance-1) + "," + "" + ",";
                nextLine += "" + "," + "" + ",";
                    nextLine += ""+ "," + "" + ",";
                    nextLine += "" + "," + "" + ",";
                    nextLine += "";
                    nextLine += "," + FMSValue;
                pointingAverageDataFile.WriteLine(nextLine);
                pointingAverageDataFile.Close();    
            }
        }
        
    }
    private void recordControllerInfoWhilePointing()
    {  
        if (writeData)
        {
            Vector3 controllerForwardOrientation = convertToSpherical(controller.transform.forward);
            Vector3 userTargetOrientation = convertToSpherical(pathTargets[instruction - 1].transform.position - controller.transform.position);
            float pointingErrorYaw = controllerForwardOrientation.y - userTargetOrientation.y;
            if (pointingErrorYaw > 180) pointingErrorYaw -= 360;
            if (pointingErrorYaw < -180) pointingErrorYaw += 360;
            float pointingErrorPitch = userTargetOrientation.z - controllerForwardOrientation.z;
            float pointingErrorYawAbsolute = Mathf.Abs(pointingErrorYaw);
            float pointingErrorPitchAbsolute = Mathf.Abs(pointingErrorPitch);
            float distanceError = userTargetOrientation.x - distanceMeasurement.distanceEstimate;
            string trivial = "no";
            if (totalTargetInstance == instruction) trivial = "yes";
            if (totalTargetInstance < 3) trivial = "yes";
            controllerInfoWhileAdjustingDistance = new StreamWriter ("Results\\" + dateAndTime + "_" +  participantID + "_" + interfaceName + "_" + "ControllerWhilePointing" +".csv", true, System.Text.Encoding.UTF8, 1024 * 3);
            string nextLine = participantID + "," + System.DateTime.Now + "," + experimentTime + "," + jumpNoJump + "," + leaningGamepad + "," + interfaceName + "," + trialNo + "," + pathID + "," + (totalTargetInstance - 1) + "," + noOfPointings + "," + instruction + "," + trivial + ",";
                nextLine += pathInstructions[instruction] + ",";
                nextLine += controller.transform.position.x + "," +  controller.transform.position.y + "," + controller.transform.position.z + ",";
                nextLine += pathTargets[instruction - 1].transform.position.x + "," +  pathTargets[instruction - 1].transform.position.y + "," + pathTargets[instruction - 1].transform.position.z + ",";
                nextLine += userTargetOrientation.y + "," + userTargetOrientation.z + "," + controllerForwardOrientation.y + "," + controllerForwardOrientation.z + ",";
                nextLine += pointingErrorYaw + "," + pointingErrorPitch + ",";
                nextLine += pointingErrorYawAbsolute + "," + pointingErrorPitchAbsolute + ",";
                nextLine += timeToPoint + ",";                                        
                nextLine += userTargetOrientation.x + ",";
                nextLine += distanceMeasurement.distanceEstimate + ",";
                nextLine += distanceError + ",";
                nextLine += Mathf.Abs(distanceError);
            controllerInfoWhileAdjustingDistance.WriteLine (nextLine);
            controllerInfoWhileAdjustingDistance.Close();
        }
    }
    private void writeWayPointDataFile(string teleportTranslational, string teleportRotational)
    {
        if (writeData)
        {
            string location = (totalTargetInstance - 2) + "__" + (totalTargetInstance - 1);
            // totalDistanceTravelledBetweenWaypoints /= (frameHighSpeed + frameNormalSpeed);
            waypointDataFile = new StreamWriter ("Results\\" + dateAndTime + "_" +  participantID + "_" + interfaceName + "_" + "WayPoint" +".csv", true, System.Text.Encoding.UTF8, 1024 * 3);
            string nextLine = participantID + "," + System.DateTime.Now + "," + experimentTime + "," + jumpNoJump + "," + leaningGamepad + "," + interfaceName + "," + trialNo + "," + pathID + "," + location + ",";
                if (blueWaypoint)
                {
                    nextLine += waypointID + "," + "blue" + "," + wayPointName + ",";
                }
                else
                {
                    nextLine += waypointID + "," + "red" + "," + wayPointName + ",";
                }
                    nextLine += wayPointLocation.x + "," + wayPointLocation.z + ",";
                    nextLine += playerPositionwhileCrossingWaypoint.x + "," + playerPositionwhileCrossingWaypoint.z + ",";
                    nextLine += wayPointshortestDistance + ",";
                    nextLine += wayPointTime + ",";
                    nextLine += currentVirtualTranslationalVelocity.magnitude + ",";
                    nextLine += teleportTranslational + "," + teleportRotational + ",";
                    nextLine += wayPointTime - lastWayPointTime + ",";
                    float distanceBetweenWaypoints = (lastWayPointLocation - wayPointLocation).magnitude; // Mathf.Sqrt((lastWayPointLocation.x - wayPointLocation.x)*(lastWayPointLocation.x - wayPointLocation.x)+(lastWayPointLocation.z - wayPointLocation.z)*(lastWayPointLocation.z - wayPointLocation.z));
                    nextLine += distanceBetweenWaypoints + ",";
                    nextLine += totalDistanceTravelledBetweenWaypoints + ",";
                    nextLine += noOfTranslationalJumpsBetweenWaypoints + ",";
                    float averageJumpDistance = 0;
                if (noOfTranslationalJumpsBetweenWaypoints != 0)
                {
                    averageJumpDistance = totalJumpDistanceBetweenWaypoints/noOfTranslationalJumpsBetweenWaypoints;
                }
                    nextLine += averageJumpDistance + ",";
                    nextLine += timeSpentHighSpeed + ",";
                    nextLine += timeSpentHighSpeed/ (wayPointTime - lastWayPointTime) * 100 + ",";
                if (frameNormalSpeed !=0 ) nextLine += averageVelocityNormalSpeed/frameNormalSpeed + ",";
                else nextLine += 0 + ",";
                if (hyperJumpOn)
                {
                    nextLine += totalDistanceTravelledBetweenWaypoints/ (wayPointTime - lastWayPointTime) + ",";
                    nextLine += totalJumpDistanceBetweenWaypoints + ",";
                    if (totalDistanceTravelledBetweenWaypoints !=0 ) nextLine += totalJumpDistanceBetweenWaypoints/totalDistanceTravelledBetweenWaypoints * 100 + ",";
                    else nextLine += "" + ",";
                }
                else
                {
                   if (frameHighSpeed !=0 || frameNormalSpeed != 0) nextLine += (averageVelocityHighSpeed + averageVelocityNormalSpeed)/(frameHighSpeed + frameNormalSpeed) + ",";
                   else nextLine += "," ;
                   nextLine += distanceTravelledHighSpeed + ",";
                   if (totalDistanceTravelledBetweenWaypoints != 0) nextLine += distanceTravelledHighSpeed/totalDistanceTravelledBetweenWaypoints * 100 + ",";
                   else nextLine += "" + ",";
                }
                    nextLine += totalAngularJumpBetweenWaypoints + ",";
                    nextLine += travelLookAngle/ (frameHighSpeed + frameNormalSpeed);
            waypointDataFile.WriteLine(nextLine);
            waypointDataFile.Close();

            // change the values for next waypoint
            lastWayPointTime = wayPointTime;
            lastWayPointLocation = wayPointLocation;
            noOfTranslationalJumpsBetweenWaypoints = 0;
            totalJumpDistanceBetweenWaypoints = 0;
            totalAngularJumpBetweenWaypoints = 0;
            distanceTravelledHighSpeed = 0;
            averageVelocityHighSpeed = 0;
            frameHighSpeed = 0;
            frameNormalSpeed = 0;
            totalDistanceTravelledBetweenWaypoints = 0;
            averageVelocityNormalSpeed = 0;
            timeSpentHighSpeed = 0;
            travelLookAngle = 0;
            sumOfVelocityBetweenWaypointsWithoutJump = Vector3.zero;
            _writeWayPointData = false;
            waypointID++;
        }
    }
    //=============File Read and Write Operations================//

    //==========Animations=============//
    void shootRay()
    {
        Ray ray = new Ray(controller.transform.position, controller.transform.forward);
        GameObject laser = GameObject.Instantiate(m_shotPrefab, controller.transform.position, controller.transform.rotation) as GameObject;
        GameObject.Destroy(laser, 2f);
    }
    private IEnumerator pointUpAnimation()
    {
        float z = 0;
        pointerAnimationObject.transform.localScale = new Vector3 (pointerAnimationObject.transform.localScale.x, pointerAnimationObject.transform.localScale.y, 0);
        pointerAnimationObject.SetActive(true);
        while(z < .008f)
        {
            z += .001f;
            pointerAnimationObject.transform.localScale = new Vector3 (pointerAnimationObject.transform.localScale.x, pointerAnimationObject.transform.localScale.y, z);
            yield return new WaitForEndOfFrame();
        }
    }
    //==========Animations=============//

    //=============Mathmatical Calculations================//
    private Vector3 convertToSpherical(Vector3 cartesian)
    {
        if (cartesian.x == 0) {cartesian.x = Mathf.Epsilon;}
        float radius = Mathf.Sqrt((cartesian.x * cartesian.x )+(cartesian.y * cartesian.y)+(cartesian.z * cartesian.z));
        float polar = Mathf.Atan (cartesian.z /cartesian.x);
        if (cartesian.x < 0) polar += Mathf.PI;
        polar = polar * 180 / Mathf.PI;
        if (polar < 0) polar += 360;
        float elevation = Mathf.Asin(cartesian.y / radius);
        elevation = elevation * 180 / Mathf.PI;
        return new Vector3 (radius, polar, elevation);
    }
    private float floatListAverage(List<float> list)
    {
        float average = 0;
        int counter = 0;
        foreach (float data in list)
        {
            average += data;
            counter++;
        }
        average /= counter;
        return average;
    }
    // https://en.wikipedia.org/wiki/Mean_of_circular_quantities
    private float circularAverage(List<float> pointingError)
    {
        float averagePointingErrorSin = 0;
        float averagePointingErrorCos = 0;
        int counter = 0;
        float averagePointError = 0;

        foreach (float data in pointingError)
        {
            averagePointingErrorSin += Mathf.Sin(data * Mathf.PI/180);
            averagePointingErrorCos += Mathf.Cos(data * Mathf.PI/180);
            counter++;
        }
        averagePointingErrorSin /= counter;
        averagePointingErrorCos /= counter;
        if (averagePointingErrorCos > 0)
        {
            averagePointError = Mathf.Atan(averagePointingErrorSin/averagePointingErrorCos)* 180/Mathf.PI;
        }
        else
        {
            averagePointError = Mathf.Atan(averagePointingErrorSin/averagePointingErrorCos)* 180/Mathf.PI + 180;
            if (averagePointError > 180) averagePointError -=360;
        }
        return averagePointError;
    }
    // https://stackoverflow.com/questions/13928404/calculating-standard-deviation-of-angles
    private float circularStandardDeviation(List<float> pointingError)
    {
        float sinError = 0;
        float cosError = 0;
        foreach(float data in pointingError)
        {
            sinError += Mathf.Sin(data * Mathf.PI / 180);
            cosError += Mathf.Cos(data * Mathf.PI / 180);
        }

        sinError /= pointingError.Count;
        cosError /= pointingError.Count;

        float standardDeviation = Mathf.Sqrt(-Mathf.Log(sinError * sinError + cosError * cosError));
        return standardDeviation * 180 / Mathf.PI;
    }
    private void calculateSpeed()
    {
        //calculating virtual and physical position & rotation of the user
		currentVirtualPosition = vrCamera.transform.position;
		currentVirtualOrientation = vrCamera.transform.rotation.eulerAngles;
		currentPhysicalPosition = vrCamera.transform.localPosition;
		currentPhysicalOrientation = vrCamera.transform.localRotation.eulerAngles;
        currentCameraRigPosition = this.transform.position;
        if (previousCameraRigPosition != null) 
        {
            distanceTravelledframe = (currentCameraRigPosition - previousCameraRigPosition).magnitude;
            if (float.IsNaN(distanceTravelledframe)) distanceTravelledframe = 0;
        }


		//calculating virtual/physical positional and rotational velocity of the user
		if (previousVirtualPosition != null) { // If there are previous positional data for the user

			currentVirtualTranslationalVelocity = (currentVirtualPosition - previousVirtualPosition) / Time.deltaTime;
			currentPhysicalTranslationalVelocity = (currentPhysicalPosition - previousPhysicalPosition) / Time.deltaTime;

			//Calculate current virtual velocity
			currentVirtualRotationalVelocity = new Vector3 ();
			if (currentVirtualOrientation.x - previousVirtualOrientation.x > 180)
				currentVirtualRotationalVelocity.x = (currentVirtualOrientation.x - previousVirtualOrientation.x - 360) / Time.deltaTime;
			else if (currentVirtualOrientation.x - previousVirtualOrientation.x < -180)
				currentVirtualRotationalVelocity.x = (currentVirtualOrientation.x - previousVirtualOrientation.x + 360) / Time.deltaTime;
			else
				currentVirtualRotationalVelocity.x = (currentVirtualOrientation.x - previousVirtualOrientation.x) / Time.deltaTime;

			if (currentVirtualOrientation.y - previousVirtualOrientation.y > 180)
				currentVirtualRotationalVelocity.y = (currentVirtualOrientation.y - previousVirtualOrientation.y - 360) / Time.deltaTime;
			else if (currentVirtualOrientation.y - previousVirtualOrientation.y < -180)
				currentVirtualRotationalVelocity.y = (currentVirtualOrientation.y - previousVirtualOrientation.y + 360) / Time.deltaTime;
			else
				currentVirtualRotationalVelocity.y = (currentVirtualOrientation.y - previousVirtualOrientation.y) / Time.deltaTime;

			if (currentVirtualOrientation.z - previousVirtualOrientation.z > 180)
				currentVirtualRotationalVelocity.z = (currentVirtualOrientation.z - previousVirtualOrientation.z - 360) / Time.deltaTime;
			else if (currentVirtualOrientation.z - previousVirtualOrientation.z < -180)
				currentVirtualRotationalVelocity.z = (currentVirtualOrientation.z - previousVirtualOrientation.z + 360) / Time.deltaTime;
			else
				currentVirtualRotationalVelocity.z = (currentVirtualOrientation.z - previousVirtualOrientation.z) / Time.deltaTime;

			//Calculate current physical velocity
			currentPhysicalRotationalVelocity = new Vector3 ();
			if (currentPhysicalOrientation.x - previousPhysicalOrientation.x > 180)
				currentPhysicalRotationalVelocity.x = (currentPhysicalOrientation.x - previousPhysicalOrientation.x - 360) / Time.deltaTime;
			else if (currentPhysicalOrientation.x - previousPhysicalOrientation.x < -180)
				currentPhysicalRotationalVelocity.x = (currentPhysicalOrientation.x - previousPhysicalOrientation.x + 360) / Time.deltaTime;
			else
				currentPhysicalRotationalVelocity.x = (currentPhysicalOrientation.x - previousPhysicalOrientation.x) / Time.deltaTime;

			if (currentPhysicalOrientation.y - previousPhysicalOrientation.y > 180)
				currentPhysicalRotationalVelocity.y = (currentPhysicalOrientation.y - previousPhysicalOrientation.y - 360) / Time.deltaTime;
			else if (currentPhysicalOrientation.y - previousPhysicalOrientation.y < -180)
				currentPhysicalRotationalVelocity.y = (currentPhysicalOrientation.y - previousPhysicalOrientation.y + 360) / Time.deltaTime;
			else
				currentPhysicalRotationalVelocity.y = (currentPhysicalOrientation.y - previousPhysicalOrientation.y) / Time.deltaTime;

			if (currentPhysicalOrientation.z - previousPhysicalOrientation.z > 180)
				currentPhysicalRotationalVelocity.z = (currentPhysicalOrientation.z - previousPhysicalOrientation.z - 360) / Time.deltaTime;
			else if (currentPhysicalOrientation.z - previousPhysicalOrientation.z < -180)
				currentPhysicalRotationalVelocity.z = (currentPhysicalOrientation.z - previousPhysicalOrientation.z + 360) / Time.deltaTime;
			else
				currentPhysicalRotationalVelocity.z = (currentPhysicalOrientation.z - previousPhysicalOrientation.z) / Time.deltaTime;
		}

		//calculating virtual/physical positional and Orientational acceleration of the user
		if (previousVirtualTranslationalVelocity != null) { // If there are previous velocity data for the user
			currentVirtualTranslationalAcceleration = (currentVirtualTranslationalVelocity - previousVirtualTranslationalVelocity) / Time.deltaTime;
			currentVirtualRotationalAcceleration = (currentVirtualRotationalVelocity - previousVirtualRotationalVelocity) / Time.deltaTime;
			currentPhysicalTranslationalAcceleration = (currentPhysicalTranslationalVelocity - previousPhysicalTranslationalVelocity) / Time.deltaTime;
			currentPhysicalRotationalAcceleration = (currentPhysicalRotationalVelocity - previousPhysicalRotationalVelocity) / Time.deltaTime;
		}

		//calculating virtual/physical positional and orientational jerk of the user
		if (previousVirtualTranslationalAcceleration != null) { // If there are previous acceleration data for the user
			currentVirtualTranslationalJerk = (currentVirtualTranslationalAcceleration - previousVirtualTranslationalAcceleration) / Time.deltaTime;
			currentVirtualRotationalJerk = (currentVirtualRotationalAcceleration - previousVirtualRotationalAcceleration) / Time.deltaTime;
			currentPhysicalTranslationalJerk = (currentPhysicalTranslationalAcceleration - previousPhysicalTranslationalAcceleration) / Time.deltaTime;
			currentPhysicalRotationalJerk = (currentPhysicalRotationalAcceleration - previousPhysicalRotationalAcceleration) / Time.deltaTime;
		}

		//current physical/virtual data (i.e., position, velocity, accelration, jerk) should be stored as the previous data
        if (float.IsNaN(currentVirtualPosition.x)||float.IsNaN(currentVirtualPosition.y)||float.IsNaN(currentVirtualPosition.z)) currentVirtualPosition = previousVirtualPosition;
        if (float.IsNaN(currentVirtualOrientation.x)||float.IsNaN(currentVirtualOrientation.y)||float.IsNaN(currentVirtualOrientation.z)) currentVirtualOrientation = previousVirtualOrientation;
        if (float.IsNaN(currentPhysicalPosition.x)||float.IsNaN(currentPhysicalPosition.y)||float.IsNaN(currentPhysicalPosition.z)) currentPhysicalPosition = previousPhysicalPosition;
        if (float.IsNaN(currentPhysicalOrientation.x)||float.IsNaN(currentPhysicalOrientation.y)||float.IsNaN(currentPhysicalOrientation.z)) currentPhysicalOrientation = previousPhysicalOrientation;
        if (float.IsNaN(currentVirtualTranslationalVelocity.x)||float.IsNaN(currentVirtualTranslationalVelocity.y)||float.IsNaN(currentVirtualTranslationalVelocity.z)) currentVirtualTranslationalVelocity = new Vector3 (0, 0, 0);
        if (float.IsNaN(currentVirtualRotationalVelocity.x)||float.IsNaN(currentVirtualRotationalVelocity.y)||float.IsNaN(currentVirtualRotationalVelocity.z)) currentVirtualRotationalVelocity = new Vector3 (0, 0, 0);
        if (float.IsNaN(currentPhysicalTranslationalVelocity.x)||float.IsNaN(currentPhysicalTranslationalVelocity.y)||float.IsNaN(currentPhysicalTranslationalVelocity.z)) currentPhysicalTranslationalVelocity = new Vector3 (0, 0, 0);
        if (float.IsNaN(currentPhysicalRotationalVelocity.x)||float.IsNaN(currentPhysicalRotationalVelocity.y)||float.IsNaN(currentPhysicalRotationalVelocity.z)) currentPhysicalRotationalVelocity = new Vector3 (0, 0, 0);
        if (float.IsNaN(currentVirtualTranslationalAcceleration.x)||float.IsNaN(currentVirtualTranslationalAcceleration.y)||float.IsNaN(currentVirtualTranslationalAcceleration.z)) currentVirtualTranslationalAcceleration = new Vector3 (0, 0, 0);
        if (float.IsNaN(currentVirtualRotationalAcceleration.x)||float.IsNaN(currentVirtualRotationalAcceleration.y)||float.IsNaN(currentVirtualRotationalAcceleration.z)) currentVirtualRotationalAcceleration = new Vector3 (0, 0, 0);
        if (float.IsNaN(currentPhysicalTranslationalAcceleration.x)||float.IsNaN(currentPhysicalTranslationalAcceleration.y)||float.IsNaN(currentPhysicalTranslationalAcceleration.z)) currentPhysicalTranslationalAcceleration = new Vector3 (0, 0, 0);
        if (float.IsNaN(currentPhysicalRotationalAcceleration.x)||float.IsNaN(currentPhysicalRotationalAcceleration.y)||float.IsNaN(currentPhysicalRotationalAcceleration.z)) currentPhysicalRotationalAcceleration = new Vector3 (0, 0, 0);
        if (float.IsNaN(currentVirtualTranslationalJerk.x)||float.IsNaN(currentVirtualTranslationalJerk.y)||float.IsNaN(currentVirtualTranslationalJerk.z)) currentVirtualTranslationalJerk = new Vector3 (0, 0, 0);
        if (float.IsNaN(currentVirtualRotationalJerk.x)||float.IsNaN(currentVirtualRotationalJerk.y)||float.IsNaN(currentVirtualRotationalJerk.z)) currentVirtualRotationalJerk = new Vector3 (0, 0, 0);
        if (float.IsNaN(currentPhysicalTranslationalJerk.x)||float.IsNaN(currentPhysicalTranslationalJerk.y)||float.IsNaN(currentPhysicalTranslationalJerk.z)) currentPhysicalTranslationalJerk = new Vector3 (0, 0, 0);
        if (float.IsNaN(currentPhysicalRotationalJerk.x)||float.IsNaN(currentPhysicalRotationalJerk.y)||float.IsNaN(currentPhysicalRotationalJerk.z)) currentPhysicalRotationalJerk = new Vector3 (0, 0, 0);
        
		previousVirtualPosition = currentVirtualPosition;
		previousVirtualOrientation = currentVirtualOrientation;
		previousPhysicalPosition = currentPhysicalPosition;
		previousPhysicalOrientation = currentPhysicalOrientation;
		previousVirtualTranslationalVelocity = currentVirtualTranslationalVelocity;
		previousVirtualRotationalVelocity = currentVirtualRotationalVelocity;
		previousPhysicalTranslationalVelocity = currentPhysicalTranslationalVelocity;
		previousPhysicalRotationalVelocity = currentPhysicalRotationalVelocity;
		previousVirtualTranslationalAcceleration = currentVirtualTranslationalAcceleration;
		previousVirtualRotationalAcceleration = currentVirtualRotationalAcceleration;
		previousPhysicalTranslationalAcceleration = currentPhysicalTranslationalAcceleration;
		previousPhysicalRotationalAcceleration = currentPhysicalRotationalAcceleration;
		previousVirtualTranslationalJerk = currentVirtualTranslationalJerk;
		previousVirtualRotationalJerk = currentVirtualRotationalJerk;
		previousPhysicalTranslationalJerk = currentPhysicalTranslationalJerk;
		previousPhysicalRotationalJerk = currentPhysicalRotationalJerk;
        previousCameraRigPosition = currentCameraRigPosition;
    }
    private IEnumerator ControllerVeloctiy()
    {
        while(Application.isPlaying)
        {
            Vector3 prevPos = controller.transform.position;
            yield return new WaitForEndOfFrame();
            controllerVelocity = (prevPos - controller.transform.position).magnitude/Time.deltaTime;
        }
    }
    private float calculateControllerVelocityAverage()
    {
        float controllerVelocityAverage = 0f;

        // length of the array determines how quick or stable the pointing needs to be (higher number = higher frames)
        for (int i = 0; i < controllerVelocitys.Length - 1; i++)
        {
            controllerVelocitys[i] = controllerVelocitys[i + 1];
            if (!float.IsNaN(controllerVelocitys[i]))
            {
                controllerVelocityAverage += controllerVelocitys[i];
            }
        }
        controllerVelocitys[controllerVelocitys.Length - 1] = controllerVelocity;
        controllerVelocityAverage += controllerVelocitys[controllerVelocitys.Length - 1];
        controllerVelocityAverage /= controllerVelocitys.Length;
        return controllerVelocityAverage;
    }
    private void ShufflePointingListFunction()
    {
        RNGCryptoServiceProvider provider = new RNGCryptoServiceProvider();
        int n = totalTargetInstance - 1;
        AudioClip tempAudio;
        GameObject tempGameObject;
        while(n > 1)
        {
            byte[] box = new byte[1];
            do provider.GetBytes(box);
            while (!(box[0] < n * (byte.MaxValue /n)));
            int k = (box[0] % n);
            n--;
            tempAudio = pathInstructions[k + 1];
            tempGameObject = pathTargets[k];
            pathInstructions[k + 1] = pathInstructions[n + 1];
            pathInstructions[n + 1] = tempAudio;
            pathTargets[k] = pathTargets[n];
            pathTargets[n] = tempGameObject;
        }
    }
    //=============Mathmatical Calculations================//

}
