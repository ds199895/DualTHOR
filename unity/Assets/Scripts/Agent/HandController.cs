using System;
using UnityEngine;
using System.IO;
using System.Collections;


namespace Agent
{
    public class HandController : MonoBehaviour
    {
        public ArticulationBody[] leftHandJoints;
        public ArticulationBody[] rightHandJoints;
        
        public ArticulationBody[] hands;
        // assume this is the recorded target angles
        private float[] leftTargetAngles = { 60f, 60f, 60f, 60f, 60f, 60f, 60f, 60f,90f,60f,30f,0f }; // example data
        private float[] rightTargetAngles = { 60f, 60f, 60f, 60f, 60f, 60f, 60f, 60f,90f,60f,30f,0f}; // example data   

        private float hand_initial_angle = 23.3f;
        private float hand_target_angle = 31.1f;

        // interpolation speed
        public float interpolationSpeed = 2.0f;

        private void Start()
        {
            var drive = hands[0].xDrive;
            drive.target = -hand_initial_angle;
            hands[0].xDrive = drive;
            drive = hands[1].xDrive;
            drive.target = hand_initial_angle;
            hands[1].xDrive = drive;
        }

        private void Update()
        {
            // SmoothTransitionLeftHand();
            // SmoothTransitionRightHand();
            if (Input.GetKeyDown(KeyCode.R))
            {
                WriteCurrentAnglesToFile(Path.Combine(Application.dataPath, "Resources", "currentAngles.txt"));
            }
        }
        public void ResetAllHands()
        {

            StartCoroutine(ResetLeftHandCoroutine());

            StartCoroutine(ResetRightHandCoroutine());
        }


        private IEnumerator ResetLeftHandCoroutine()
        {
            bool allJointsReset = false;
            while (!allJointsReset)
            {
                allJointsReset = true;
                for (int i = 0; i < leftHandJoints.Length; i++)
                {
                    float currentAngle = leftHandJoints[i].xDrive.target;
                    float newAngle = Mathf.Lerp(currentAngle, 0, Time.deltaTime * interpolationSpeed);
                    var left_drive = leftHandJoints[i].xDrive;
                    left_drive.target = newAngle;
                    leftHandJoints[i].xDrive = left_drive;

                    if (Mathf.Abs(newAngle) > 0.01f)
                    {
                        allJointsReset = false;
                    }
                }
                yield return null; // Wait for the next frame
            }
        }

        private IEnumerator ResetRightHandCoroutine()
        {
            bool allJointsReset = false;
            while (!allJointsReset)
            {
                allJointsReset = true;
                for (int i = 0; i < rightHandJoints.Length; i++)
                {
                    float currentAngle = rightHandJoints[i].xDrive.target;
                    float newAngle = Mathf.Lerp(currentAngle, 0, Time.deltaTime * interpolationSpeed);
                    var right_drive = rightHandJoints[i].xDrive;
                    right_drive.target = newAngle;
                    rightHandJoints[i].xDrive = right_drive;

                    if (Mathf.Abs(newAngle) > 0.01f)
                    {
                        allJointsReset = false;
                    }
                }
                yield return null; // Wait for the next frame
            }
        }

        public void StartResetHand(bool isLeftHand)
        {
            if (isLeftHand)
            {
                StartCoroutine(ResetLeftHandCoroutine());
            }
            else
            {
                StartCoroutine(ResetRightHandCoroutine());
            }
        }

        private IEnumerator SmoothTransitionLeftHandCoroutine()
        {
            bool allJointsReachedTarget = false;
            while (!allJointsReachedTarget)
            {
                allJointsReachedTarget = true;
                for (int i = 0; i < leftHandJoints.Length; i++)
                {
                    float currentAngle = leftHandJoints[i].xDrive.target;
                    float newAngle = Mathf.Lerp(currentAngle, leftTargetAngles[i], Time.deltaTime * interpolationSpeed);
                    var left_drive = leftHandJoints[i].xDrive;
                    left_drive.target = newAngle;
                    leftHandJoints[i].xDrive = left_drive;

                    if (Mathf.Abs(newAngle - leftTargetAngles[i]) > 0.01f)
                    {
                        allJointsReachedTarget = false;
                    }
                }
                yield return null; // wait for the next frame
            }
        }

        private IEnumerator SmoothTransitionRightHandCoroutine()
        {
            bool allJointsReachedTarget = false;
            while (!allJointsReachedTarget)
            {
                allJointsReachedTarget = true;
                for (int i = 0; i < rightHandJoints.Length; i++)
                {
                    float currentAngle = rightHandJoints[i].xDrive.target;
                    float newAngle = Mathf.Lerp(currentAngle, rightTargetAngles[i], Time.deltaTime * interpolationSpeed);
                    var right_drive = rightHandJoints[i].xDrive;
                    right_drive.target = newAngle;
                    rightHandJoints[i].xDrive = right_drive;

                    if (Mathf.Abs(newAngle - rightTargetAngles[i]) > 0.01f)
                    {
                        allJointsReachedTarget = false;
                    }
                }
                yield return null; // wait for the next frame
            }
        }

        public void StartSmoothTransition(bool isLeftHand)
        {
            if (isLeftHand)
            {
                StartCoroutine(SmoothTransitionLeftHandCoroutine());
            }
            else
            {
                StartCoroutine(SmoothTransitionRightHandCoroutine());
            }
        }

        public void RotateHandBase(bool isLeftArm)
        {
           
            if (isLeftArm)
            {
                var drive = hands[0].xDrive;
                drive.target = hand_target_angle;
                hands[0].xDrive = drive;
            }
            else
            {
                var drive=hands[1].xDrive;
                drive.target = hand_target_angle;
                hands[1].xDrive = drive;
            }
         
        }

        public void ResetHandBase(bool isLeftArm)
        {
            if (isLeftArm)
            {
                var drive = hands[0].xDrive;
                drive.target = 0;
                hands[0].xDrive = drive;
            }
            else
            {
                var drive=hands[1].xDrive;
                drive.target = 0;
                hands[1].xDrive = drive;
            }
        }



        private ArticulationDrive CreateDrive(float target)
        {
            ArticulationDrive drive = new ArticulationDrive();
            drive.target = target;
            drive.stiffness = 10000f;
            drive.damping = 100f;
            drive.forceLimit = 1000f;
            return drive;
        }

        public void WriteCurrentAnglesToFile(string filePath)
        {
            using (StreamWriter writer = new StreamWriter(filePath))
            {
                writer.WriteLine("Left Hand Joints:");
                for (int i = 0; i < leftHandJoints.Length; i++)
                {
                    float currentAngle = leftHandJoints[i].xDrive.target;
                    Debug.Log(currentAngle);
                    writer.WriteLine($"Joint {i}: {currentAngle}");
                }

                writer.WriteLine("Right Hand Joints:");
                for (int i = 0; i < rightHandJoints.Length; i++)
                {
                    float currentAngle = rightHandJoints[i].xDrive.target;
                    writer.WriteLine($"Joint {i}: {currentAngle}");
                }
                Debug.Log("Write Current Angles to File"+filePath);
            }
        }
    }
}