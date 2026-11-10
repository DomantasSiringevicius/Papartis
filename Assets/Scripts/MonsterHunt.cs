using System.Collections;
using MimicSpace;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MonsterHunt : MonoBehaviour
{
    [SerializeField] float huntSpeed = 7f;
    [SerializeField] float spawnDistance = 15f;
    [SerializeField] float killDistance = 1.2f;
    [SerializeField] string gameOverScene = "Opening screen";
    [SerializeField] AudioClip scream;
    [SerializeField] Material fadeMaterial;
    [SerializeField] float fadeDuration = 1.5f;
    [SerializeField] float blackScreenTime = 1f;

    Movement monster;
    TorchFireLightFlicker torch;
    Transform player;
    float normalSpeed;
    bool torchWasLit;
    bool hunting;
    bool playerDead;

    void Start()
    {
        monster = FindFirstObjectByType<Movement>(FindObjectsInactive.Include);
        torch = FindFirstObjectByType<TorchFireLightFlicker>(FindObjectsInactive.Include);
        if (Camera.main != null)
            player = Camera.main.transform;
        if (monster != null)
            normalSpeed = monster.speed;
    }

    void Update()
    {
        if (monster == null || torch == null || player == null || playerDead)
            return;

        if (torch.IsLit)
        {
            torchWasLit = true;
            if (hunting)
                StopHunt();
            return;
        }

        if (!torchWasLit)
            return;

        if (!hunting)
            StartHunt();

        Vector3 toPlayer = player.position - monster.transform.position;
        toPlayer.y = 0f;
        if (toPlayer.magnitude <= killDistance)
            KillPlayer();
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
        monster.StopChase();
        monster.speed = normalSpeed;
        monster.gameObject.SetActive(false);
    }

    void KillPlayer()
    {
        playerDead = true;
        monster.StopChase();
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
