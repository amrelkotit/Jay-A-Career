using UnityEngine;
using Unity.Cinemachine;
using Unity.Cinemachine.TargetTracking;

/// <summary>
/// Runtime Cinemachine rig for first-person play, third-person follow, and dialogue close-ups.
/// The Unity Camera is driven by CinemachineBrain; this component only manages camera targets
/// and priorities.
/// </summary>
public class JayCameraRig : MonoBehaviour
{
    [field: SerializeField] public Transform LookPivot { get; private set; }
    public bool IsThirdPerson { get; private set; }

    [SerializeField] Camera outputCamera;
    [SerializeField] CinemachineCamera firstPersonCamera;
    [SerializeField] CinemachineCamera thirdPersonCamera;
    [SerializeField] CinemachineCamera interactionCamera;
    int normalPriority = 20;
    bool initialized;

    public void Initialize(Camera camera)
    {
        if (initialized || camera == null) return;
        initialized = true;
        outputCamera = camera;
        if (outputCamera.CompareTag("Untagged")) outputCamera.tag = "MainCamera";
        if (LookPivot && firstPersonCamera && thirdPersonCamera && interactionCamera)
        { SetNormalPriorities(); return; }

        Vector3 pivotLocalPosition = outputCamera.transform.localPosition;
        Quaternion pivotLocalRotation = outputCamera.transform.localRotation;
        var pivotObject = new GameObject("JayCameraLookPivot");
        LookPivot = pivotObject.transform;
        LookPivot.SetParent(transform, false);
        LookPivot.localPosition = pivotLocalPosition;
        LookPivot.localRotation = pivotLocalRotation;

        // CinemachineBrain must own an unparented Unity Camera. The player controller continues
        // to rotate LookPivot for the familiar mouse-look behavior.
        outputCamera.transform.SetParent(null, true);
        outputCamera.transform.position = LookPivot.position;
        outputCamera.transform.rotation = LookPivot.rotation;
        if (outputCamera.GetComponent<CinemachineBrain>() == null)
        {
            var brain = outputCamera.gameObject.AddComponent<CinemachineBrain>();
            brain.DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.EaseInOut, 0.45f);
        }

        firstPersonCamera = CreateCamera("CM First Person", LookPivot, 20);
        var firstFollow=firstPersonCamera.gameObject.AddComponent<CinemachineFollow>();firstFollow.FollowOffset=Vector3.zero;
        firstFollow.TrackerSettings=new TrackerSettings{BindingMode=BindingMode.LockToTargetWithWorldUp,PositionDamping=Vector3.zero};
        firstPersonCamera.gameObject.AddComponent<CinemachineRotateWithFollowTarget>();

        thirdPersonCamera = CreateCamera("CM Third Person Follow", LookPivot, 10);
        var thirdPerson = thirdPersonCamera.gameObject.AddComponent<CinemachineThirdPersonFollow>();
        thirdPerson.ShoulderOffset = new Vector3(0.35f, -0.15f, 0f);
        thirdPerson.VerticalArmLength = 0.25f;
        thirdPerson.CameraDistance = 2.8f;
        thirdPerson.Damping = new Vector3(0.12f, 0.16f, 0.12f);
        thirdPerson.AvoidObstacles.Enabled = true;
        thirdPerson.AvoidObstacles.CollisionFilter = ~0;
        thirdPerson.AvoidObstacles.IgnoreTag = "Player";
        thirdPersonCamera.gameObject.AddComponent<CinemachineRotateWithFollowTarget>();

        interactionCamera = CreateCamera("CM Interaction Close Up", LookPivot, 0);
        var interactionFollow = interactionCamera.gameObject.AddComponent<CinemachineFollow>();
        interactionFollow.FollowOffset = new Vector3(0.45f, -0.1f, 1.6f);
        interactionFollow.TrackerSettings = new TrackerSettings
        {
            BindingMode = BindingMode.LockToTargetWithWorldUp,
            PositionDamping = new Vector3(0.18f, 0.18f, 0.18f)
        };
        interactionCamera.gameObject.AddComponent<CinemachineHardLookAt>();

        SetNormalPriorities();
    }

    CinemachineCamera CreateCamera(string cameraName, Transform target, int priority)
    {
        var cameraObject = new GameObject(cameraName);
        var cineCamera = cameraObject.AddComponent<CinemachineCamera>();
        var cameraTarget = cineCamera.Target;
        cameraTarget.TrackingTarget = target;
        cineCamera.Target = cameraTarget;
        cineCamera.Priority = priority;
        var lens=cineCamera.Lens;lens.FieldOfView=65;lens.NearClipPlane=.08f;lens.FarClipPlane=110;cineCamera.Lens=lens;
        return cineCamera;
    }

    public void ToggleView(JayGameManager game)
    {
        if (!initialized) return;
        IsThirdPerson = !IsThirdPerson;
        normalPriority = IsThirdPerson ? 10 : 20;
        SetNormalPriorities();
        if (game != null) game.ShowMessage(IsThirdPerson ? "Third-person camera." : "First-person camera.");
    }

    public void Focus(Transform target)
    {
        if (!initialized || target == null) return;
        var cameraTarget = interactionCamera.Target;
        cameraTarget.TrackingTarget = target;
        cameraTarget.LookAtTarget = target;
        cameraTarget.CustomLookAtTarget = true;
        interactionCamera.Target = cameraTarget;
        interactionCamera.Priority = 30;
    }

    public void EndFocus()
    {
        if (!initialized) return;
        interactionCamera.Priority = 0;
        SetNormalPriorities();
    }

    void SetNormalPriorities()
    {
        if (firstPersonCamera == null || thirdPersonCamera == null) return;
        firstPersonCamera.Priority = IsThirdPerson ? 5 : normalPriority;
        thirdPersonCamera.Priority = IsThirdPerson ? normalPriority : 5;
    }
}
