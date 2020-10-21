using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum Target
{
    RoadBlock,
    WaterTank,
    TrashBin,
    Telephone,
    Tree,
    None
};

public class GameManager : MonoBehaviour
{
    private Target target;
    public GameObject controller;
    public DistanceMeasure distanceMeasure;
    public ControllerHandler controllerHandler;
    public GameObject[] pathTargets;
    public GameObject[] pathWaypoints;
    public AudioSource audioPlayer;
    public AudioClip[] pathInstructions;
    private int instruction = 0;
    private bool triggerPressed = false;
    private int targetNo = 1;
    private bool pointUp;
    public bool pointingTask;
    private bool distanceTask;
    private bool distancePrompt;
    private bool pointToTarget;

    // Start is called before the first frame update
    void Start()
    {
        audioPlayer = GetComponent<AudioSource>();
        target = Target.None;
        StartCoroutine(Updater());
        pathTargets[0].SetActive(true);
        pointUp = true;
        pointingTask = true;
        distanceTask = false;
        pointToTarget = false;
    }

    // Update is called once per frame
    IEnumerator Updater()
    {
        while (Application.isPlaying)
        {
            // Pointing task manager
            if (targetNo < 6)
            {
                if (pointingTask)
                {
                    if (!audioPlayer.isPlaying)
                    {
                        if (pointUp && instruction < targetNo + 1)
                        {
                            Debug.Log("Waiting to point Target");
                            if (Mathf.Abs(controller.transform.eulerAngles.x) > 330 || Mathf.Abs(controller.transform.eulerAngles.x) < 20 || instruction == 0)
                            {
                                instruction++;
                                audioPlayer.clip = pathInstructions[0];
                                audioPlayer.Play();
                                pointUp = false;
                                pointToTarget = true;
                                Debug.Log("1. Point up prompt + Instruction: " + instruction + " + Pointing Task: " + pointingTask + " + pointUp: " + pointUp + " + targetNo: " + targetNo);
                            }
                        }
                        if (pointToTarget && instruction < targetNo + 1)
                        {
                            Debug.Log("Waiting to point up");
                            if (Mathf.Abs(controller.transform.eulerAngles.x) > 250 && Mathf.Abs(controller.transform.eulerAngles.x) < 290)
                            {
                                pointToTarget = false;
                                yield return new WaitForSeconds(.5f);                                
                                audioPlayer.clip = pathInstructions[targetNo + 1 - instruction];
                                audioPlayer.Play();
                                pointUp = true;
                                Debug.Log("2. Target Prompt + Instruction: " + instruction + " + Pointing Task: " + pointingTask + " + pointUp: " + pointUp + " + targetNo: " + targetNo);
                            }
                        }
                        else if (instruction == targetNo + 1)
                        {
                            pointingTask = false;
                            distanceTask = true;
                            instruction = 0;
                            distancePrompt = true;
                            pointUp = true;
                            pointToTarget = false;
                            Debug.Log("3. Pointing Task Done + Instruction: " + instruction + " + Pointing Task: " + pointingTask + " + pointUp: " + pointUp+ " + targetNo: " + targetNo);
                        }
                    }
                }
                // Distance measurement manager
                if (distanceTask)
                {
                    if (!audioPlayer.isPlaying)
                    {
                        if (instruction == 0)
                        {
                            yield return new WaitForSeconds(.5f);
                            audioPlayer.clip = pathInstructions[6];
                            audioPlayer.Play();
                            instruction++;
                        }
                        else if (instruction < targetNo + 1)
                        {
                            if (distancePrompt)
                            {
                                yield return new WaitForSeconds(.5f);
                                audioPlayer.clip = pathInstructions[targetNo + 1 - instruction];
                                audioPlayer.Play();
                                distancePrompt = false;
                                Debug.Log("4. Target Distance + Instruction: " + instruction + " + Pointing Task: " + pointingTask + " + pointUp: " + pointUp+ " + targetNo: " + targetNo);
                                Debug.Log("5. Target Distance + Instruction: " + instruction + " + Distance Prompt: " + distancePrompt + " + targetNo: " + targetNo);
                            }
                            else
                            {
                                Debug.Log("Waiting to measure distance:");
                                if (distanceMeasure.distanceMeasurementStart)
                                {
                                    triggerPressed = true;
                                }
                                if (triggerPressed)
                                {
                                    Debug.Log("Waiting to release trigger:");
                                    if (!distanceMeasure.distanceMeasurementStart)
                                    {
                                        instruction++;
                                        distancePrompt = true;
                                        triggerPressed = false;
                                    }
                                }
                            }
                            
                        }
                        else
                        {
                            distanceTask = false;
                            instruction = 0;
                            targetNo++;
                            Debug.Log("6. New Target + Instruction: " + instruction + " + Pointing Task: " + pointingTask + " + pointUp: " + pointUp+ " + targetNo: " + targetNo);
                        }
                    }
                }
                
            }
            else
            {
                Application.Quit();
            }    
            yield return null;
        }
    }
}
