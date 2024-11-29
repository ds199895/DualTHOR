using UnityEngine;

public class GripperController : MonoBehaviour
{
    public ArticulationBody leftArmLeftGripper;
    public ArticulationBody leftArmRightGripper;
    public ArticulationBody rightArmLeftGripper;
    public ArticulationBody rightArmRightGripper;

    public void SetGripper(bool isLeftArm, bool open)
    {
        var leftGripper = isLeftArm ? leftArmLeftGripper : rightArmLeftGripper;
        var rightGripper = isLeftArm ? leftArmRightGripper : rightArmRightGripper;

        var leftDrive = leftGripper.xDrive;
        var rightDrive = rightGripper.xDrive;

        if (open)
        {
            leftDrive.target = isLeftArm ? 11f : -11f; // ×ó±ÛÎª11£¬ÓÒ±ÛÎª-11
            rightDrive.target = isLeftArm ? -11f : 11f; // ×ó±ÛÎª-11£¬ÓÒ±ÛÎª11
        }
        else
        {
            leftDrive.target = isLeftArm ? -3f : 4f;
            rightDrive.target = isLeftArm ? 3f : -4f;
        }

        leftGripper.xDrive = leftDrive;
        rightGripper.xDrive = rightDrive;
    }

    public void ResetGripper(bool isLeftArm)
    {
        var leftGripper = isLeftArm ? leftArmLeftGripper : rightArmLeftGripper;
        var rightGripper = isLeftArm ? leftArmRightGripper : rightArmRightGripper;

        var leftDrive = leftGripper.xDrive;
        var rightDrive = rightGripper.xDrive;

        leftDrive.target = 0f;
        rightDrive.target = 0f;

        leftGripper.xDrive = leftDrive;
        rightGripper.xDrive = rightDrive;
    }
}