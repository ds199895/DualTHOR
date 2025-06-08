using UnityEngine;

public class GripperController : MonoBehaviour
{
    public ArticulationBody leftArmLeftGripper;
    public ArticulationBody leftArmRightGripper;
    public ArticulationBody rightArmLeftGripper;
    public ArticulationBody rightArmRightGripper;

    public ArticulationBody h1_leftArmLeftGripper;
    public ArticulationBody h1_leftArmRightGripper;
    public ArticulationBody h1_rightArmLeftGripper;
    public ArticulationBody h1_rightArmRightGripper;

    public ArticulationBody currentLeftLeftGripper ;
    public ArticulationBody currentLeftRightGripper ;
    public ArticulationBody currentRightLeftGripper ;
    public ArticulationBody currentRightRightGripper;



    public void Start()
    {
        AgentMovement movement = this.GetComponent<AgentMovement>();
        Debug.Log("Current Robot Type: " + movement.CurrentRobotType);
        if (movement.CurrentRobotType == RobotType.H1)
        {
            Debug.Log("H1 Gripper");
            currentLeftLeftGripper = h1_leftArmLeftGripper;
            currentLeftRightGripper = h1_leftArmRightGripper;
            currentRightLeftGripper = h1_rightArmLeftGripper;
            currentRightRightGripper = h1_rightArmRightGripper;
        }
        else
        {
            Debug.Log("X1 Gripper");
            currentLeftLeftGripper = leftArmLeftGripper;
            currentLeftRightGripper = leftArmRightGripper;
            currentRightLeftGripper = rightArmLeftGripper;
            currentRightRightGripper = rightArmRightGripper;
        }
    }

    public void InitializeGripper(RobotType type)
    {
        if (type == RobotType.H1)
        {
            currentLeftLeftGripper = h1_leftArmLeftGripper;
            currentLeftRightGripper = h1_leftArmRightGripper;
            currentRightLeftGripper = h1_rightArmLeftGripper;
            currentRightRightGripper = h1_rightArmRightGripper;
        }
        else
        {
            currentLeftLeftGripper = leftArmLeftGripper;
            currentLeftRightGripper = leftArmRightGripper;
            currentRightLeftGripper = rightArmLeftGripper;
            currentRightRightGripper = rightArmRightGripper;
        }
    }



    public void SetRobotGripper(RobotType type,bool isLeftArm, bool open)
    {
        if (type == RobotType.H1)
        {
            currentLeftLeftGripper = h1_leftArmLeftGripper;
            currentLeftRightGripper = h1_leftArmRightGripper;
            currentRightLeftGripper = h1_rightArmLeftGripper;
            currentRightRightGripper = h1_rightArmRightGripper;
        }
        else
        {
            currentLeftLeftGripper = leftArmLeftGripper;
            currentLeftRightGripper = leftArmRightGripper;
            currentRightLeftGripper = rightArmLeftGripper;
            currentRightRightGripper = rightArmRightGripper;
        }
        
        SetGripper(isLeftArm, open);
        
    }
    public void SetGripper(bool isLeftArm, bool open)
    {
        var leftGripper = isLeftArm ? currentLeftLeftGripper : currentRightLeftGripper;
        var rightGripper = isLeftArm ? currentLeftRightGripper : currentRightRightGripper;

        var leftDrive = leftGripper.xDrive;
        var rightDrive = rightGripper.xDrive;

        if (open)
        {
            leftDrive.target = isLeftArm ? 11f : 11f; //Left gripper 
            rightDrive.target = isLeftArm ? -11f : -11f; //Right gripper
        }
        else
        {
            leftDrive.target = isLeftArm ? -3f : -3f;
            rightDrive.target = isLeftArm ? 3f : 3f;
        }

        leftGripper.xDrive = leftDrive;
        rightGripper.xDrive = rightDrive;
    }

    public void ResetGripper(bool isLeftArm)
    {
        var leftGripper = isLeftArm ? currentLeftLeftGripper : currentRightLeftGripper;
        var rightGripper = isLeftArm ? currentLeftRightGripper : currentRightRightGripper;

        var leftDrive = leftGripper.xDrive;
        var rightDrive = rightGripper.xDrive;

        leftDrive.target = 0f;
        rightDrive.target = 0f;

        leftGripper.xDrive = leftDrive;
        rightGripper.xDrive = rightDrive;
    }
}