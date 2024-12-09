using System;
using System.Reflection;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class AgentMovement : MonoBehaviour
{
    public SceneManager sceneManager;
    public float moveSpeed = 1.0f;
    public float rotationSpeed = 90.0f;  // ÿ����ת�ĽǶ�
    public GripperController gripperController; 
    public ArticulationBody[] articulationChain;
    public float stiffness = 10000f;    // �ն�
    public float damping = 100f;       // ����
    public float forceLimit = 1000f;  // ������
    public float speed = 30f;         // �ٶȣ���λ����/��
    public float torque = 100f;       // Ť�أ���λ��Nm
    public float acceleration = 10f;  // ���ٶ�

    public Transform target;
    private Vector3 lastTargetPosition;
    private float positionChangeThreshold = 0.0001f; // λ�ñ仯��ֵ
    public IKClient ikClient;  
    public Transform pickPositionL;  // ��ȡλ��
    public Transform placePositionL; // ����λ��
    public Transform pickPositionR;  // ��ȡλ��
    public Transform placePositionR; // ����λ��
    public List<float> targetJointAngles = new List<float> { 0, 0, 0, 0, 0, 0 }; // ��ʼֵ
    public List<ArticulationBody> leftArmJoints;  // ��۹ؽ�
    public List<ArticulationBody> rightArmJoints; // �ұ۹ؽ�

    private bool hasMovedToPosition = false; // toggle�����б���Ƿ��Ѿ�����Ŀ��λ��
    private bool isTargetAnglesUpdated = false;



    private bool isManualControlEnabled = false;
    private float manualMoveSpeed = 5.0f;
    private float manualRotateSpeed = 60.0f;
    private float sprintMultiplier = 3f; // ���ٱ���
    private float mouseSensitivity = 2.0f;
    private float verticalRotation = 0f;
    private Transform cameraTransform; // ���Transform
    private float maxVerticalAngle = 80f; // ������Ƕ�
    private bool isMouseUnlocked = false; // ����Ƿ�����ESC�Խ������

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
    public List<JointAdjustment> rightAdjustments = new List<JointAdjustment>
    {
        new JointAdjustment { angle = 0f, axis = Vector3.up },
        new JointAdjustment { angle = 90f, axis = Vector3.right },
        new JointAdjustment { angle = 0f, axis = Vector3.right },
        new JointAdjustment { angle = 90f, axis = Vector3.right },
        new JointAdjustment { angle = 0f, axis = Vector3.up },
        new JointAdjustment { angle = 0f, axis = Vector3.right }
    };
    private List<Vector3> rightDefaultRotations = new List<Vector3>
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
        initGame();
        cameraTransform = Camera.main.transform;
        InitializeAdjustments(true);
        InitializeAdjustments(false);
        ikClient.OnTargetJointAnglesUpdated += UpdateTargetJointAngles;

    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            StartCoroutine(ResetJoint(true)); // ͬ����true��ʾʹ����ۣ�false��ʾʹ���ұ�
        }
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            string targetObjectID = "Kitchen_Cup_01"; // �滻ΪĿ����Ʒ�� ID
            TP(targetObjectID);
        }
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            StartCoroutine(Pick("Kitchen_Cup_01", true)); // true��ʾʹ����ۣ�false��ʾʹ���ұ�
        }
        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            StartCoroutine(Place("Kitchen_Cup_01", true)); // ͬ����true��ʾʹ����ۣ�false��ʾʹ���ұ�
        }
        if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            StartCoroutine(Pick("Kitchen_Cup_01", false)); // true��ʾʹ����ۣ�false��ʾʹ���ұ�
        }
        if (Input.GetKeyDown(KeyCode.Alpha6))
        {
            StartCoroutine(Place("Kitchen_Cup_01", false)); // ͬ����true��ʾʹ����ۣ�false��ʾʹ���ұ�
        }


        if (Input.GetKeyDown(KeyCode.Alpha7))
        {
            string targetObjectID = "Kitchen_Faucet_01"; // �滻ΪĿ����Ʒ�� ID
            TP(targetObjectID);
        }
        if (Input.GetKeyDown(KeyCode.Alpha8))
        {
            StartCoroutine(Toggle("Kitchen_Faucet_01", true)); //
        }
        if (Input.GetKeyDown(KeyCode.Alpha9))
        {
            StartCoroutine(Toggle("Kitchen_Faucet_01", false)); //
        }

        if (Input.GetKeyDown(KeyCode.J))
        {
            string targetObjectID = "Kitchen_Fridge_01"; // �滻ΪĿ����Ʒ�� ID
            TP(targetObjectID);
        }
        if (Input.GetKeyDown(KeyCode.K))
        {
            StartCoroutine(Open("Kitchen_Fridge_01", true)); // 
        }
        if (Input.GetKeyDown(KeyCode.L))
        {
            StartCoroutine(Open("Kitchen_Fridge_01", false)); // 
        }
        //if (Input.GetKeyDown(KeyCode.Alpha5))
        //{
        //    string targetObjectID = "Kitchen_StoveKnob_01"; // �滻ΪĿ����Ʒ�� ID
        //    TP(targetObjectID);
        //}
        //if (Input.GetKeyDown(KeyCode.Alpha6))
        //{
        //    StartCoroutine(Toggle("Kitchen_StoveKnob_01")); // 
        //}
        //if (Input.GetKeyDown(KeyCode.Alpha7))
        //{
        //    string targetObjectID = "Kitchen_Cabinet_02"; // �滻ΪĿ����Ʒ�� ID
        //    TP(targetObjectID);
        //}
        //if (Input.GetKeyDown(KeyCode.Alpha8))
        //{
        //    StartCoroutine(Open("Kitchen_Cabinet_02")); // ͬ����
        //}
        //if (Input.GetKeyDown(KeyCode.Alpha0))
        //{
        //    StartCoroutine(ResetJoint(false)); // ͬ����true��ʾʹ����ۣ�false��ʾʹ���ұ�
        //}


        if (Input.GetKeyDown(KeyCode.Z))        // Z���л��ֶ�����
        {
            isManualControlEnabled = !isManualControlEnabled;
            Cursor.visible = !isManualControlEnabled;
            Cursor.lockState = isManualControlEnabled ? CursorLockMode.Locked : CursorLockMode.None;

            if (isManualControlEnabled)
            {
                DisableArticulationBodies();
                isMouseUnlocked = false; // ȷ���������ģʽʱ�������
            }
            else
            {
                EnableArticulationBodies();
            }
        }
        // ESC���˳��ֶ����Ƶ����ָ�ArticulationBodies
        if (isManualControlEnabled && Input.GetKeyDown(KeyCode.Escape))
        {
            isMouseUnlocked = true;
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
        // �����Ļ���½����ֶ�����
        if (isMouseUnlocked && Input.GetMouseButtonDown(0))
        {
            isMouseUnlocked = false;
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
        // �ֶ�����
        if (isManualControlEnabled && !isMouseUnlocked) ManualControl();
    }
    public void initGame()
    {
        articulationChain = GetComponentsInChildren<ArticulationBody>();

        if (articulationChain == null || articulationChain.Length == 0)
        {
            Debug.LogError("δ�ҵ��κιؽڣ���ȷ���ö������ ArticulationBody �����");
            return;
        }

        foreach (ArticulationBody joint in articulationChain)
        {
            ArticulationDrive drive = joint.xDrive;
            drive.stiffness = stiffness;
            drive.damping = damping;
            drive.forceLimit = forceLimit;
            joint.xDrive = drive;
        }

    }

    public void ExecuteActionWithCallback(UnityClient.ActionData actionData, Action callback)
    {
        // ��ȡ����
        MethodInfo method = typeof(AgentMovement).GetMethod(actionData.action, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);

        // ������������ڣ�ִ�лص�������
        if (method == null)
        {
            Debug.LogWarning($"Unknown action: {actionData.action}");
            callback?.Invoke();
            return;
        }

        // ���� lastAction
        sceneManager?.UpdateLastAction(actionData.action);

        // ʹ�ô���� successRate ��ִ�и����ж�
        bool isSuccessful = Probability(actionData.successRate);
        sceneManager?.UpdateLastActionSuccess(isSuccessful, actionData.action);

        if (!isSuccessful)
        {
            Debug.LogWarning($"Action {actionData.action} failed due to random chance.");
            callback?.Invoke();
            return;
        }

        try
        {
            // ���������ִ�з���
            object[] args = ConstructArguments(method.GetParameters(), actionData);

            if (method.ReturnType == typeof(IEnumerator))
            {
                // �����Э�̷���������Э�̲��ڽ���ʱ���ûص�
                StartCoroutine(ExecuteCoroutineAction(method, args, callback));
            }
            else
            {
                // ��Э�̷������������ò������ص�
                method.Invoke(this, args);
                callback?.Invoke();
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error executing action {actionData.action}: {ex.Message}");
            callback?.Invoke();
        }
    }

    private IEnumerator ExecuteCoroutineAction(MethodInfo method, object[] args, Action callback)
    {
        Debug.Log($"Executing coroutine: {method.Name} with arguments: {string.Join(", ", args ?? new object[0])}");

        IEnumerator coroutine = null;

        try
        {
            coroutine = (IEnumerator)method.Invoke(this, args); // ����Э�̷���
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error invoking coroutine {method.Name}: {ex.Message}");
        }

        if (coroutine != null)
        {
            yield return StartCoroutine(coroutine); // �ȴ�Э��ִ�����
        }
        else
        {
            Debug.LogError($"Coroutine method returned null: {method.Name}");
        }

        callback?.Invoke(); // Э�̽����󴥷��ص�
    }
    private object[] ConstructArguments(ParameterInfo[] parameters, UnityClient.ActionData actionData)
    {
        if (parameters.Length == 0) return null;

        List<object> args = new List<object>();

        foreach (var param in parameters)
        {
            // ���ݲ���������ʽӳ��
            switch (param.Name.ToLower())
            {
                case "stateid":
                    args.Add(actionData.stateID); // ӳ�䵽 stateID
                    break;
                case "objectid":
                    args.Add(actionData.objectID); // ӳ�䵽 objectID
                    break;
                case "isleftarm":
                    args.Add(actionData.arm.Equals("left", StringComparison.OrdinalIgnoreCase)); // ӳ�䵽 arm
                    break;
                case "magnitude":
                    args.Add(actionData.Magnitude); // ӳ�䵽 Magnitude
                    break;
                default:
                    Debug.LogWarning($"Unsupported parameter: {param.Name} of type {param.ParameterType.Name}");
                    args.Add(null); // Ĭ��ֵ
                    break;
            }
        }

        return args.ToArray();
    }


    public IEnumerator Toggle(string objectID, bool isLeftArm)
    {
        // ��ȡĿ�꽻����� Toggle �ű�
        Transform interactPoint = SceneManager.GetInteractablePoint(objectID);
        CanToggleOnOff toggleScript = interactPoint?.GetComponentInParent<CanToggleOnOff>();

        // �����δ����Ŀ��λ�ã�ִ���ƶ�
        if (!hasMovedToPosition && interactPoint != null)
        {
            yield return StartCoroutine(MoveToPosition(interactPoint.position, isLeftArm));
            yield return new WaitForSeconds(1f);
            hasMovedToPosition = true; // ���Ϊ�ѵ���
        }

        // �л�����
        toggleScript?.Toggle();
        yield return new WaitForSeconds(1f);
    }

    public IEnumerator Open(string objectID, bool isLeftArm)
    {
        // ��ȡĿ�꽻����� Open �ű�
        Transform interactPoint = SceneManager.GetInteractablePoint(objectID);
        CanOpen_Object openScript = interactPoint?.GetComponentInParent<CanOpen_Object>();

        // �����δ����Ŀ��λ�ã�ִ���ƶ�
        if (!hasMovedToPosition && interactPoint != null)
        {
            yield return StartCoroutine(MoveToPosition(interactPoint.position, isLeftArm));
            yield return new WaitForSeconds(1f);
            hasMovedToPosition = true; // ���Ϊ�ѵ���
        }

        // �򿪶���
        openScript?.Interact();
        yield return new WaitForSeconds(1f);
    }

    public void TP(string objectID)
    {
        // ������Ʒ�� TransferPoint
        Transform transferPoint = SceneManager.GetTransferPointByObjectID(objectID);

        if (transferPoint == null)
        {
            Debug.LogError($"TP action failed: objectID '{objectID}' not found.");
            return;
        }

        // ���û��������йؽڵ� ArticulationBody
        DisableArticulationBodies();

        // ֱ���޸Ļ����˵�λ�ú���ת
        Debug.Log($"Robot directly transported to {objectID}'s TransferPoint: {transferPoint.position}");
        transform.position = transferPoint.position;
        transform.rotation = transferPoint.rotation;

        // ���û��������йؽڵ� ArticulationBody
        EnableArticulationBodies();

        Debug.Log($"Robot successfully transported to {objectID}'s TransferPoint");
    }

    public IEnumerator Pick(string objectID, bool isLeftArm)
    {
        Vector3 offset = new Vector3(0, 0.1f, 0);
        Transform pickPosition = SceneManager.GetInteractablePoint(objectID);

        if (pickPosition == null)
        {
            Debug.LogError($"δ�ҵ�IDΪ {objectID} ����Ʒ��Ĭ�Ͻ����㣬�޷�ִ��Pick����");
            yield break;
        }

        Vector3 abovePickPosition = pickPosition.position + offset;

        // �ƶ�����ȡλ���Ϸ�
        Debug.Log($"�ƶ���{(isLeftArm ? "���" : "�ұ�")}��ȡλ���Ϸ�: {abovePickPosition}");
        yield return StartCoroutine(MoveToPosition(abovePickPosition, isLeftArm));
        yield return new WaitForSeconds(1f);

        // �򿪼�צ׼����ȡ
        Debug.Log($"��{(isLeftArm ? "���" : "�ұ�")}��צ׼����ȡ");
        gripperController.SetGripper(isLeftArm, true);
        yield return new WaitForSeconds(1f);

        // �½�����ȡλ��
        Debug.Log($"�½���{(isLeftArm ? "���" : "�ұ�")}��ȡλ��: {pickPosition.position}");
        yield return StartCoroutine(MoveToPosition(pickPosition.position, isLeftArm));
        yield return new WaitForSeconds(1f);

        // �н�����
        Debug.Log($"{(isLeftArm ? "���" : "�ұ�")}�н�����");
        gripperController.SetGripper(isLeftArm, false);
        yield return new WaitForSeconds(1f);

        Debug.Log($"�ƶ���{(isLeftArm ? "���" : "�ұ�")}��ȡλ���Ϸ�: {abovePickPosition}");
        yield return StartCoroutine(MoveToPosition(abovePickPosition, isLeftArm));
        yield return new WaitForSeconds(1f);
    }

    public IEnumerator Place(string objectID, bool isLeftArm)
    {
        // �����Ƿ����������ƫ����
        Vector3 offset = isLeftArm ? new Vector3(0, 0.1f, -0.13f) : new Vector3(0, 0.1f, 0.13f);
        Transform pickPosition = SceneManager.GetInteractablePoint(objectID);

        if (pickPosition == null)
        {
            Debug.LogError($"δ�ҵ�IDΪ {objectID} ����Ʒ��Ĭ�Ͻ����㣬�޷�ִ��Place����");
            yield break;
        }

        Vector3 placePosition = pickPosition.position + offset; // ����Pick��λ��ƫ��

        // �ƶ�������λ��
        Debug.Log($"�ƶ���{(isLeftArm ? "���" : "�ұ�")}����λ��: {placePosition}");
        yield return MoveToPosition(placePosition, isLeftArm);
        yield return new WaitForSeconds(1f);

        // �򿪼�צ��������
        Debug.Log($"��{(isLeftArm ? "���" : "�ұ�")}��צ��������");
        gripperController.SetGripper(isLeftArm, true);
        yield return new WaitForSeconds(1f);
    }
    public IEnumerator ResetJoint(bool isLeftArm)
    {
        Debug.Log($"{(isLeftArm ? "���" : "�ұ�")}�ؽ��������õ���ʼλ��...");

        List<float> initialAngles = new List<float>();
        var adjustments = isLeftArm ? this.adjustments : rightAdjustments;
        var joints = isLeftArm ? leftArmJoints : rightArmJoints;

        for (int i = 0; i < joints.Count; i++)
        {
            float initialAngle = (i == 0 || i == 4 || i == 3) ? -adjustments[i].angle : adjustments[i].angle;
            initialAngles.Add(initialAngle);
        }

        yield return StartCoroutine(SmoothUpdateJointAngles(initialAngles, 2f, isLeftArm));

        Debug.Log($"{(isLeftArm ? "���" : "�ұ�")}�ؽ��ѳɹ����ã�");

        hasMovedToPosition = false; // ���Ϊ�ѵ���

    }

    private void InitializeAdjustments(bool isLeftArm)
    {
        var joints = isLeftArm ? leftArmJoints : rightArmJoints;
        var defaultRotations = isLeftArm ? this.defaultRotations : rightDefaultRotations;
        var adjustments = isLeftArm ? this.adjustments : rightAdjustments;

        string logMessage = $"{(isLeftArm ? "���" : "�ұ�")}��ʼ���ؽڵ�����Ϣ��\nĬ��ֵ:\n";

        for (int i = 0; i < joints.Count; i++)
        {
            var joint = joints[i];
            Vector3 initialRotation = joint.transform.localRotation.eulerAngles;

            // �淶����ȡ���ĽǶ�
            initialRotation.x = NormalizeAngle(initialRotation.x);
            initialRotation.y = NormalizeAngle(initialRotation.y);
            initialRotation.z = NormalizeAngle(initialRotation.z);

            Vector3 defaultRotation = defaultRotations[i];
            float adjustmentAngle = (i == 0 || i == 4)
                ? defaultRotation.y - initialRotation.y
                : defaultRotation.x - initialRotation.x;

            adjustmentAngle = NormalizeAngle(adjustmentAngle);
            adjustments[i].angle = adjustmentAngle;

            logMessage += $"�ؽ� {i + 1} Ĭ����ת: {defaultRotation}\n" +
                          $"�ؽ� {i + 1} ��ʼ��ת: {initialRotation}\n" +
                          $"�ؽ� {i + 1} �����Ƕ�: {adjustmentAngle}\n";
        }

        Debug.Log(logMessage);
    }


    private void UpdateTargetJointAngles(List<float> updatedAngles)//�¼�
    {
        targetJointAngles = updatedAngles;
        isTargetAnglesUpdated = true; // ��ǽǶ��Ѹ���
    }

    private IEnumerator MoveToPosition(Vector3 position, bool isLeftArm)
    {
        // �������Ŀ��Ƕ�
        ikClient.ProcessTargetPosition(position, isLeftArm);

        // �ȴ�Ŀ��Ƕȸ���
        yield return new WaitUntil(() => isTargetAnglesUpdated);

        //Debug.Log("MoveToPosition �е�Ŀ��Ƕ�: " + string.Join(", ", targetJointAngles));

        // ���ñ��
        isTargetAnglesUpdated = false;

        yield return StartCoroutine(SmoothUpdateJointAngles(targetJointAngles, 2f, isLeftArm));

        // ����״̬����ֹ����������Ӱ��
        targetJointAngles.Clear();
    }

    public IEnumerator SmoothUpdateJointAngles(List<float> targetJointAngles, float duration, bool isLeftArm)
    {
        //Debug.Log("���� SmoothUpdateJointAngles ʱ��Ŀ��Ƕ�: " + string.Join(", ", targetJointAngles));

        List<float> startAngles = new List<float>();
        var joints = isLeftArm ? leftArmJoints : rightArmJoints;
        var adjustments = isLeftArm ? this.adjustments : rightAdjustments;

        foreach (var joint in joints)
        {
            startAngles.Add(NormalizeAngle(joint.xDrive.target));
        }

        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / duration);

            for (int i = 0; i < joints.Count; i++)
            {
                var joint = joints[i];
                var drive = joint.xDrive;
                //�ؽ�1��5��Y�ᣬ�ҷ������
                float adjustedAngle = NormalizeAngle(targetJointAngles[i] + ((i == 0 || i == 4) ? -adjustments[i].angle : adjustments[i].angle));

                float interpolatedAngle = Mathf.Lerp(startAngles[i], adjustedAngle, t);

                drive.target = NormalizeAngle(interpolatedAngle);
                joint.xDrive = drive;

                //Debug.Log($"��ֵ��: �ؽ� {i + 1}, ��ʼ={startAngles[i]}, ������Ŀ��={adjustedAngle}, ��ֵ={interpolatedAngle}, xDrive={drive.target}");
            }

            yield return null;
        }

        for (int i = 0; i < joints.Count; i++)
        {
            var joint = joints[i];
            var drive = joint.xDrive;

            float finalAdjustedAngle = NormalizeAngle(targetJointAngles[i] + ((i == 0 || i == 4) ? -adjustments[i].angle : adjustments[i].angle));

            drive.target = finalAdjustedAngle;
            joint.xDrive = drive;

            //Debug.Log($"�ؽ� {i + 1} ����Ŀ��Ƕ� (��): {finalAdjustedAngle}");
        }
    }


    // ƽ���ƶ���Э�̣���Ϊʹ�þֲ�����ϵ�ķ���
    private IEnumerator SmoothMove(Vector3 localDirection, float magnitude, float duration)
    {
        DisableArticulationBodies();

        Vector3 startPosition = transform.position;
        Vector3 targetPosition = startPosition + transform.TransformDirection(localDirection) * moveSpeed * magnitude;

        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            transform.position = Vector3.Lerp(startPosition, targetPosition, elapsedTime / duration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        transform.position = targetPosition; // ȷ������Ŀ��λ��
        EnableArticulationBodies();
    }

    // ƽ����ת��Э�̣�ʹ�þֲ�����ϵ�ķ���
    private IEnumerator SmoothRotate(Vector3 rotationAxis, float magnitude, float duration)
    {
        DisableArticulationBodies();

        // ʹ����ת�Ƕ����ۼӷ�ʽ
        float targetAngle = rotationSpeed * magnitude;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            float stepAngle = (targetAngle / duration) * Time.deltaTime; // ��ÿ֡����������ת�Ƕ�
            transform.Rotate(rotationAxis, stepAngle, Space.Self);

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        EnableArticulationBodies();
    }

    public IEnumerator MoveAhead(float Magnitude, Action callback = null)
    {
        yield return SmoothMove(Vector3.forward, Magnitude, 1.0f); 
        callback?.Invoke(); 
    }

    public IEnumerator MoveRight(float Magnitude, Action callback = null)
    {
        yield return SmoothMove(Vector3.right, Magnitude, 1.0f);
        callback?.Invoke();
    }

    public IEnumerator MoveBack(float Magnitude, Action callback = null)
    {
        yield return SmoothMove(Vector3.back, Magnitude, 1.0f);
        callback?.Invoke();
    }

    public IEnumerator MoveLeft(float Magnitude, Action callback = null)
    {
        yield return SmoothMove(Vector3.left, Magnitude, 1.0f);
        callback?.Invoke();
    }
    public IEnumerator MoveUp(float Magnitude, Action callback = null)
    {
        yield return SmoothMove(Vector3.up, Magnitude * 0.1f, 1.0f);
        callback?.Invoke();
    }

    public IEnumerator MoveDown(float Magnitude, Action callback = null)
    {
        yield return SmoothMove(Vector3.down, Magnitude * 0.1f, 1.0f);
        callback?.Invoke();
    }
    public IEnumerator RotateRight(float Magnitude, Action callback = null)
    {
        yield return SmoothRotate(Vector3.up, Mathf.Abs(Magnitude), 1.0f);
        callback?.Invoke();
    }

    public IEnumerator RotateLeft(float Magnitude, Action callback = null)
    {
        yield return SmoothRotate(Vector3.up, -Mathf.Abs(Magnitude), 1.0f);
        callback?.Invoke();
    }
    public void Undo()
    {
        DisableArticulationBodies();
        sceneManager.Undo();
        EnableArticulationBodies();
    }
    public void Redo()
    {
        DisableArticulationBodies();
        sceneManager.Redo();
        EnableArticulationBodies();
    }

    // ���� SceneManager �� LoadStateByIndex ����
    public void LoadState(string stateID)
    {
        Debug.Log($"Attempting to load scene state with ID: {stateID}");
        DisableArticulationBodies();
        sceneManager.LoadStateByIndex(stateID);
        EnableArticulationBodies();
    }
    public void DisableArticulationBodies()
    {
        foreach (ArticulationBody body in articulationChain)
        {
            body.enabled = false;
        }
    }
    public void EnableArticulationBodies()
    {
        foreach (ArticulationBody body in articulationChain)
        {
            body.enabled = true;
        }
    }
    public float NormalizeAngle(float angle)
    {
        while (angle > 180) angle -= 360;
        while (angle < -180) angle += 360;
        return angle;
    }
    private bool Probability(float successRate)
    {
        return UnityEngine.Random.value < successRate; // ʹ�ô���� successRate �����ж�
    }
    private void ManualControl()
    {
        // ALT������ʱ��ʾ�������UI����
        if (Input.GetKey(KeyCode.LeftAlt))
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
        else if (!isMouseUnlocked) // ALT�ͷź������������
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }

        // ��ȡ�����ƶ�����
        Vector3 moveDirection = new Vector3(
            Input.GetKey(KeyCode.D) ? 1 : Input.GetKey(KeyCode.A) ? -1 : 0,
            0,
            Input.GetKey(KeyCode.W) ? 1 : Input.GetKey(KeyCode.S) ? -1 : 0
        );

        // ����ʵ���ƶ��ٶȣ���סShift���٣�
        float currentSpeed = manualMoveSpeed * (Input.GetKey(KeyCode.LeftShift) ? sprintMultiplier : 1.0f);

        // Ӧ���ƶ�
        if (moveDirection != Vector3.zero)
            transform.position += transform.TransformDirection(moveDirection.normalized) * currentSpeed * Time.deltaTime;

        // ��ȡ�������
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        // ���´�ֱ��ת�Ƕ�
        verticalRotation -= mouseY; // ע�������Ǽ��ţ���ΪUnity������ϵͳ��������������
        verticalRotation = Mathf.Clamp(verticalRotation, -maxVerticalAngle, maxVerticalAngle);

        // Ӧ����ת
        transform.Rotate(Vector3.up * mouseX, Space.World); // ���������ˮƽ��ת
        cameraTransform.localRotation = Quaternion.Euler(verticalRotation, 0, 0); // ֻ��ת����������ӽ�
    }
}