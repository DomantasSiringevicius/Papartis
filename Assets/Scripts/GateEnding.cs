using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GateEnding : MonoBehaviour
{
    [SerializeField] Transform gate;
    [SerializeField] float requiredOpenAngle = 60f;
    [SerializeField] float finishRadius = 1.5f;
    [SerializeField] float endingDelay = 2f;

    Transform player;
    Quaternion closedRotation;
    bool ending;

    void Start()
    {
        if (gate != null)
            closedRotation = gate.localRotation;

        if (Camera.main != null)
            player = Camera.main.transform;
    }

    void Update()
    {
        if (ending || gate == null || player == null)
            return;

        if (Quaternion.Angle(closedRotation, gate.localRotation) < requiredOpenAngle)
            return;

        Vector3 distance = player.position - transform.position;
        distance.y = 0f;

        if (distance.sqrMagnitude > finishRadius * finishRadius)
            return;

        ending = true;
        StartCoroutine(ShowEnding());
    }

    IEnumerator ShowEnding()
    {
        yield return new WaitForSeconds(endingDelay);
        SceneManager.LoadScene("EndScene");
    }
}