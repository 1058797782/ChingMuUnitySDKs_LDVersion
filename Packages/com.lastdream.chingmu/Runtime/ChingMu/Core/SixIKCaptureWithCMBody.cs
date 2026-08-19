using UnityEngine;

public class SixIKCaptureWithCMBody : MonoBehaviour
{
    [Range(-1f, 1f)]
    public float RealHuamnMassOffset;
    public Transform CharacterHipTrans;
    public Transform CharacterHeadTrans;
    public Transform CharacterLeftFootTrans;
    public Transform CharacterRightFootTrans;

    private Animator animator;
    private float characterHipHeight;
    private bool IsSacle;
    private bool configurationReady;
    private string serverAddress;
    private CMUTrackerPreset<int> preset;

    private void Start()
    {
        animator = GetComponent<Animator>();
        preset = Config.Instance.CMTrackPreset;
        serverAddress = Config.Instance.ServerIP;
        configurationReady = animator != null && CharacterHipTrans != null &&
                             preset != null && preset.Bodies != null && preset.Bodies.Count >= 6 &&
                             !string.IsNullOrWhiteSpace(ChingMuAddress.Host(serverAddress));

        if (!configurationReady)
        {
            Debug.LogWarning("ChingMu six-point capture requires an Animator, a hip transform, and six body IDs in Config.json.", this);
            enabled = false;
            return;
        }

        characterHipHeight = CharacterHipTrans.position.y;
        if (Mathf.Abs(characterHipHeight) <= Mathf.Epsilon)
        {
            Debug.LogWarning("ChingMu six-point capture cannot scale a character with zero hip height.", this);
            enabled = false;
        }
    }

    private void Update()
    {
        if (!IsSacle)
        {
            IsSacle = ScaleCharacter(preset.Bodies[1]);
        }
    }

    private bool ScaleCharacter(int hipBodyId)
    {
        float humanHipHeight = CMVrpn.CMPos(serverAddress, hipBodyId).y;
        if (humanHipHeight <= 0.6f)
        {
            return false;
        }

        float scaleFactor = humanHipHeight / characterHipHeight;
        transform.localScale = Vector3.one * scaleFactor;
        return true;
    }

    private void OnAnimatorIK(int layerIndex)
    {
        if (!configurationReady || !IsSacle)
        {
            return;
        }

        Vector3 hipPosition = CMVrpn.CMPos(serverAddress, preset.Bodies[1]);
        Quaternion hipRotation = CMVrpn.CMQuat(serverAddress, preset.Bodies[1]);
        animator.bodyPosition = new Vector3(
            hipPosition.x,
            hipPosition.y - RealHuamnMassOffset,
            hipPosition.z);
        animator.bodyRotation = hipRotation;

        Quaternion headRotation = CMVrpn.CMQuat(serverAddress, preset.Bodies[0]);
        animator.SetBoneLocalRotation(HumanBodyBones.Head, Quaternion.Inverse(hipRotation) * headRotation);

        SetIkPosition(AvatarIKGoal.LeftHand, CMVrpn.CMPos(serverAddress, preset.Bodies[2]), false);
        SetIkPosition(AvatarIKGoal.RightHand, CMVrpn.CMPos(serverAddress, preset.Bodies[3]), false);
        SetIkPosition(AvatarIKGoal.LeftFoot, CMVrpn.CMPos(serverAddress, preset.Bodies[4]), true);
        SetIkPosition(AvatarIKGoal.RightFoot, CMVrpn.CMPos(serverAddress, preset.Bodies[5]), true);
    }

    private void SetIkPosition(AvatarIKGoal goal, Vector3 position, bool applyMassOffset)
    {
        if (applyMassOffset)
        {
            position.y -= RealHuamnMassOffset;
        }

        animator.SetIKPosition(goal, position);
        animator.SetIKPositionWeight(goal, 1f);
    }
}
