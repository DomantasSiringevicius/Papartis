using System.Collections;
using System.Linq;
using MimicSpace;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class MonsterHunt : MonoBehaviour
{
    const float HuntCooldown = 10f;

    [SerializeField] float huntSpeed = 7f;
    [SerializeField] float spawnDistance = 15f;
    [SerializeField] float killDistance = 1.2f;
    [SerializeField, Min(0f)] float stunDuration = 5f;
    [SerializeField, Min(0.01f)] float handMovementThreshold = 0.5f;
    [SerializeField] Transform[] safeZones;
    [SerializeField, Min(0f)] float safeRadius = 4f;
    [SerializeField] string gameOverScene = "Opening screen";
    [SerializeField] AudioClip scream;
    [SerializeField] Material fadeMaterial;
    [SerializeField] float fadeDuration = 1.5f;
    [SerializeField] float blackScreenTime = 1f;
    [SerializeField] GameObject stunPrompt;

    Movement monster;
    TorchFireLightFlicker torch;
    Transform player;
    Transform playerOrigin;
    float normalSpeed;
    bool torchWasLit;
    bool hunting;
    bool stunned;
    bool playerDead;
    float stunTimer;
    float nextHuntTime;
    Vector3 leftHandStartPosition;
    Vector3 rightHandStartPosition;
    bool hasLeftHandStartPosition;
    bool hasRightHandStartPosition;
    Vector3 stunOriginPosition;

    void Start()
    {
        if (stunPrompt != null)
            stunPrompt.SetActive(false);
        else
            Debug.LogWarning($"{name} has no stun prompt assigned.", this);

        monster = FindFirstObjectByType<Movement>(FindObjectsInactive.Include);
        torch = FindObjectsByType<TorchFireLightFlicker>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .FirstOrDefault(candidate => candidate.GetComponentInParent<XRGrabInteractable>(true) != null);
        if (Camera.main != null)
        {
            player = Camera.main.transform;
            XROrigin xrOrigin = player.GetComponentInParent<XROrigin>();
            if (xrOrigin != null)
                playerOrigin = xrOrigin.transform;
            else
                Debug.LogError($"{name} cannot lock player movement because the camera is not under an XR Origin.", this);
        }
        if (monster != null)
            normalSpeed = monster.speed;
    }

    void LateUpdate()
    {
        if (stunned && playerOrigin != null)
            playerOrigin.position = stunOriginPosition;
    }

    void Update()
    {
        if (playerDead)
            return;

        if (stunned)
        {
            UpdateStun();
            return;
        }

        if (monster == null || torch == null || player == null)
            return;

        if (torch.IsLit)
        {
            torchWasLit = true;
            if (hunting)
                StopHunt();
            return;
        }

        if (safeZones != null)
        {
            foreach (Transform safeZone in safeZones)
            {
                if (safeZone == null)
                    continue;

                Vector3 offset = player.position - safeZone.position;
                offset.y = 0f;

                if (offset.sqrMagnitude <= safeRadius * safeRadius)
                {
                    if (hunting)
                        StopHunt();
                    return;
                }
            }
        }

        if (!torchWasLit || Time.time < nextHuntTime)
            return;

        if (!hunting)
            StartHunt();

        Vector3 toPlayer = player.position - monster.transform.position;
        toPlayer.y = 0f;
        if (toPlayer.magnitude <= killDistance)
            StunPlayer();
    }

    void StunPlayer()
    {
        stunned = true;
        stunTimer = 0f;
        nextHuntTime = Time.time + HuntCooldown;
        if (playerOrigin != null)
            stunOriginPosition = playerOrigin.position;
        hasLeftHandStartPosition = TryGetHandPosition(XRNode.LeftHand, out leftHandStartPosition);
        hasRightHandStartPosition = TryGetHandPosition(XRNode.RightHand, out rightHandStartPosition);
        StopHunt();
        if (stunPrompt != null)
            stunPrompt.SetActive(true);
    }

    void UpdateStun()
    {
        stunTimer += Time.deltaTime;
        if (stunTimer >= stunDuration)
        {
            KillPlayer();
            return;
        }

        if (HasHandMoved(XRNode.LeftHand, ref leftHandStartPosition, ref hasLeftHandStartPosition)
            || HasHandMoved(XRNode.RightHand, ref rightHandStartPosition, ref hasRightHandStartPosition))
        {
            stunned = false;
            if (stunPrompt != null)
                stunPrompt.SetActive(false);
            hasLeftHandStartPosition = false;
            hasRightHandStartPosition = false;
        }
    }

    bool HasHandMoved(XRNode hand, ref Vector3 startPosition, ref bool hasStartPosition)
    {
        if (!TryGetHandPosition(hand, out Vector3 currentPosition))
            return false;

        if (!hasStartPosition)
        {
            startPosition = currentPosition;
            hasStartPosition = true;
            return false;
        }

        float threshold = Mathf.Max(0.01f, handMovementThreshold);
        return (currentPosition - startPosition).sqrMagnitude >= threshold * threshold;
    }

    static bool TryGetHandPosition(XRNode hand, out Vector3 position)
    {
        InputDevice device = InputDevices.GetDeviceAtXRNode(hand);
        if (device.isValid && device.TryGetFeatureValue(CommonUsages.devicePosition, out position))
            return true;

        position = Vector3.zero;
        return false;
    }

    void StartHunt()
    {
        hunting = true;

        if (!monster.gameObject.activeSelf)
        {
            Vector3 away = Random.insideUnitCircle.normalized * spawnDistance;
            monster.transform.position = player.position + new Vector3(away.x, 0f, away.y);
            monster.gameObject.SetActive(true);
        }

        monster.speed = huntSpeed;
        monster.Chase(player);
    }

    void StopHunt()
    {
        hunting = false;
        if (monster == null)
            return;

        monster.StopChase();
        monster.speed = normalSpeed;
        monster.gameObject.SetActive(false);
    }

    void KillPlayer()
    {
        playerDead = true;
        stunned = false;
        if (stunPrompt != null)
            stunPrompt.SetActive(false);
        StopHunt();
        StartCoroutine(DeathSequence());
    }

    IEnumerator DeathSequence()
    {
        if (scream != null)
            AudioSource.PlayClipAtPoint(scream, player.position);

        Material fade = CreateFadeScreen();
        Color color = Color.black;
        for (float time = 0f; time < fadeDuration; time += Time.deltaTime)
        {
            color.a = time / fadeDuration;
            if (fade != null)
                fade.color = color;
            yield return null;
        }

        color.a = 1f;
        if (fade != null)
            fade.color = color;

        yield return new WaitForSeconds(blackScreenTime);
        SceneManager.LoadScene(gameOverScene);
    }

    Material CreateFadeScreen()
    {
        if (fadeMaterial == null)
            return null;

        GameObject screen = GameObject.CreatePrimitive(PrimitiveType.Quad);
        Destroy(screen.GetComponent<Collider>());
        screen.name = "Death Fade";
        screen.transform.SetParent(player, false);
        screen.transform.localPosition = new Vector3(0f, 0f, 0.15f);
        screen.transform.localRotation = Quaternion.identity;
        screen.transform.localScale = Vector3.one;

        Material material = new Material(fadeMaterial);
        material.color = new Color(0f, 0f, 0f, 0f);
        screen.GetComponent<MeshRenderer>().sharedMaterial = material;
        return material;
    }
}
