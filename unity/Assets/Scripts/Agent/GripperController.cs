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
            leftDrive.target = isLeftArm ? 11f : 11f; //左夹爪
            rightDrive.target = isLeftArm ? -11f : -11f; // 右夹爪
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