using System;
using System.Collections.Generic;
using UnityEngine;

public class UR5Controller : MonoBehaviour
{
    public ArticulationBody[] articulationChain;
    public float stiffness;
    public float damping;
    public float forceLimit;
    public float speed = 30f;
    public float torque = 100f;
    public float acceleration = 10f;

    [System.Serializable]
    public class JointAdjustment
    {
        public float angle;
        public Vector3 axis;
    }

    public List<JointAdjustment> adjustments = new List<JointAdjustment>
    {
        new JointAdjustment { angle = 0f, axis = Vector3.up },
        new JointAdjustment { angle = 90f, axis = Vector3.right },
        new JointAdjustment { angle = 0f, axis = Vector3.right },
        new JointAdjustment { angle = 90f, axis = Vector3.right },
        new JointAdjustment { angle = 0f, axis = Vector3.up },
        new JointAdjustment { angle = 0f, axis = Vector3.right }
    };

    private List<Vector3> defaultRotations = new List<Vector3>
    {
        new Vector3(0, 0, 0),
        new Vector3(90, 0, 0),
        new Vector3(0, 0, 0),
        new Vector3(90, 0, 0),
        new Vector3(0, 0, 0),
        new Vector3(0, 0, 0)
    };

    void Start()
    {
        InitializeArticulationChain();
        InitializeAdjustments();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.O))
        {
            DisplayEndEffectorPose();
        }
    }

    private void InitializeArticulationChain()
    {
        articulationChain = GetComponentsInChildren<ArticulationBody>();
        foreach (ArticulationBody joint in articulationChain)
        {
            var drive = joint.xDrive;
            drive.stiffness = stiffness;
            drive.damping = damping;
            drive.forceLimit = forceLimit;
            joint.xDrive = drive;
        }
    }

    private void InitializeAdjustments()
    {
        int[] jointIndices = { 2, 3, 4, 5, 6, 7 };
        string logMessage = "Initialize joint adjustment information:\nDefault values:\n";

        for (int i = 0; i < jointIndices.Length; i++)
        {
            int index = jointIndices[i];
            if (index >= articulationChain.Length) continue;

            var joint = articulationChain[index];
            Vector3 initialRotation = joint.transform.localRotation.eulerAngles;
            Vector3 defaultRotation = defaultRotations[i];
            float adjustmentAngle = i == 0 || i == 4
                ? defaultRotation.y - initialRotation.y
                : defaultRotation.x - initialRotation.x;

            adjustmentAngle = NormalizeAngle(adjustmentAngle);
            adjustments[i].angle = adjustmentAngle;

            logMessage += $"Joint {i + 1} default rotation: {defaultRotation}\n" +
                          $"Joint {i + 1} initial rotation: {initialRotation}\n" +
                          $"Joint {i + 1} adjustment angle: {adjustmentAngle}\n";
        }

        Debug.Log(logMessage);
    }

    public void UpdateJointAngles(List<float> jointAngles)
    {
        int[] jointIndices = { 2, 3, 4, 5, 6, 7 };
        for (int i = 0; i < jointIndices.Length; i++)
        {
            int index = jointIndices[i];
            if (index >= articulationChain.Length) continue;

            var joint = articulationChain[index];
            var drive = joint.xDrive;
            float adjustedAngle = jointAngles[i] + (i == 0 || i == 4 ? -adjustments[i].angle : adjustments[i].angle);
            drive.target = NormalizeAngle(adjustedAngle);
            joint.xDrive = drive;
        }
    }

    public float NormalizeAngle(float angle)
    {
        while (angle > 180) angle -= 360;
        while (angle < -180) angle += 360;
        return angle;
    }

    public void DisplayEndEffectorPose()
    {
        var endEffector = articulationChain[^1];
        var output = $"End effector name: {endEffector.name}\n" +
                     $"End effector world coordinates: {endEffector.transform.position:F3}\n" +
                     $"End effector world rotation (Euler angles): {endEffector.transform.rotation.eulerAngles:F3}\n" +
                     $"End effector local coordinates: {endEffector.transform.localPosition:F3}\n" +
                     $"End effector local rotation (Euler angles): {endEffector.transform.localRotation.eulerAngles:F3}";

        Debug.Log(output);
    }
}