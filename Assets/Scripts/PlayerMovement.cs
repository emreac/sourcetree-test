using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using DG;
using DG.Tweening;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] TrailRenderer trailRenderer;
    [SerializeField] GameObject helperArrow;

    [SerializeField] AudioSource collectSound;
    [SerializeField] AudioSource finalCountSound;
    [SerializeField] AudioSource loseWagonSound; // Add this for the sound effect when losing wagons

    [SerializeField] ParticleSystem wagonCollect;
    [SerializeField] ParticleSystem bloodFx;
    [SerializeField] ParticleSystem win1;
    [SerializeField] ParticleSystem win2;
    [SerializeField] GameObject winScreen;
    [SerializeField] GameObject lostScreen;

    public TMP_Text wagonCountText; // Assign this in the Inspector
    public TMP_Text wagonCountTextFinal; // Assign this in the Inspector

    private int wagonCount = 0; // Counter for the number of added wagons
    private bool grow = false;

    public float speed;
    private float velocity;
    private Camera m_Camera;
    public float roadEndPoint;
    public GameObject skyscraper; // Assign this in the Inspector

    private Transform player;
    private Vector3 firstMousePos, firstPlayerPos;
    private bool moveTheBall;
    public float playerzSpeed = 15f;

    private float camVelocity;
    public float camSpeed = 0.4f;
    public Vector3 offset;

    public GameObject bodyPrefab;
    public int gap = 2;
    public float bodySpeed = 15f;
    public int wagonsRequiredToWin = 10; // Number of wagons required to win

    private List<GameObject> bodyParts = new List<GameObject>();
    private List<int> bodyPartsIndex = new List<int>();
    private List<Vector3> PositionHistory = new List<Vector3>();

    private bool gameFinished = false;

    // Camera zoom variables
    public float initialFOV = 60f;
    public float firstZoomOutFOV = 70f;
    public float secondZoomOutFOV = 120f;
    public int wagonsForFirstZoomOut = 5;
    public int wagonsForSecondZoomOut = 10;

    private void Start()
    {
        trailRenderer = trailRenderer.GetComponent<TrailRenderer>();
        m_Camera = Camera.main;
        m_Camera.fieldOfView = initialFOV; // Set the initial FOV
        player = this.transform;
    }

    private void Update()
    {
        if (gameFinished) return;

        if (Input.GetMouseButtonDown(0))
        {
            moveTheBall = true;
            Plane newPlane = new Plane(Vector3.up, 0.8f);
            Ray ray = m_Camera.ScreenPointToRay(Input.mousePosition);
            if (newPlane.Raycast(ray, out var distance))
            {
                firstMousePos = ray.GetPoint(distance);
                firstPlayerPos = player.position;
            }
        }
        if (moveTheBall)
        {
            Plane newPlane = new Plane(Vector3.up, 0.8f);
            Ray ray = m_Camera.ScreenPointToRay(Input.mousePosition);

            if (newPlane.Raycast(ray, out var distance))
            {
                Vector3 newMousePos = ray.GetPoint(distance) - firstMousePos;
                Vector3 newPlayerPos = newMousePos + firstPlayerPos;
                newPlayerPos.x = Mathf.Clamp(newPlayerPos.x, -roadEndPoint, roadEndPoint);
                player.position = new Vector3(Mathf.SmoothDamp(player.position.x, newPlayerPos.x, ref velocity, speed), player.position.y, player.position.z);
            }
        }

        if (grow)
        {
            wagonCountText.gameObject.SetActive(true);
        }
    }

    private void FixedUpdate()
    {
        if (gameFinished) return;

        player.position += Vector3.forward * playerzSpeed * Time.fixedDeltaTime;

        PositionHistory.Insert(0, transform.position);
        int index = 0;
        foreach (var body in bodyParts)
        {
            Vector3 point = PositionHistory[Mathf.Min(index * gap, PositionHistory.Count - 1)];
            Vector3 moveDir = point - body.transform.position;
            body.transform.position += moveDir * bodySpeed * Time.fixedDeltaTime;
            index++;
            body.transform.LookAt(point);
        }

        // Update the camera zoom based on the number of wagons
        UpdateCameraZoom();
    }

    private void LateUpdate()
    {
        if (gameFinished) return;

        Vector3 newCamPos = m_Camera.transform.position;
        m_Camera.transform.position = new Vector3(Mathf.SmoothDamp(newCamPos.x, player.position.x, ref camVelocity, camSpeed),
            newCamPos.y, player.position.z + offset.z);
    }

    public void GrowBody()
    {
        collectSound.Play();
        grow = true;
        GameObject body = Instantiate(bodyPrefab, transform.position, transform.rotation);
        bodyParts.Add(body);
        int index = 0;
        index++;
        bodyPartsIndex.Add(index);

        if (trailRenderer != null)
        {
            trailRenderer.enabled = true;
        }

        wagonCount++;

        // Update TextMeshPro text
        if (wagonCountText != null)
        {
            wagonCountText.text = "" + wagonCount.ToString();
        }
        wagonCollect.Play();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "CubeObs")
        {
            Destroy(other.gameObject, 0.005F);
            GrowBody();
            helperArrow.SetActive(false);
        }
        else if (other.gameObject.tag == "Obstacle") // Add a tag for obstacles
        {
            loseWagonSound.Play();
            bloodFx.Play();
            LoseWagons(3); // Immediately lose wagons
        }
        else if (other.gameObject.tag == "FinishLine")
        {
            FinishGame();
        }
        else if (other.gameObject.tag == "Up")
        {
            DOTween.Restart("Up");

        }
        else if (other.gameObject.tag == "UpStay")
        {
            DOTween.PlayForward("UpStay");
        }
        else if (other.gameObject.tag == "DownStay")
        {
            DOTween.PlayBackwards("UpStay");
        }
    }

    private void FinishGame()
    {
        gameFinished = true;

        StartCoroutine(StackWagonsOneByOne());
        StartCoroutine(AdjustCameraHeight());
        if (trailRenderer != null)
        {
            trailRenderer.enabled = false;
        }

        // Show final wagon count at the finish line
        if (wagonCountTextFinal != null)
        {
            wagonCountTextFinal.gameObject.SetActive(true);
            StartCoroutine(CountdownFinalWagonCount(wagonCount)); // Start countdown for final wagon count
        }

        HidePlayer(); // Call the method to hide the player

        // Start countdown for decreasing wagon count
        StartCoroutine(CountdownWagonCount(bodyParts.Count));
    }

    private void HidePlayer()
    {
        // Disable the player's renderer and collider to make it invisible and non-interactable
        Renderer playerRenderer = GetComponent<Renderer>();
        Collider playerCollider = GetComponent<Collider>();

        if (playerRenderer != null)
        {
            playerRenderer.enabled = false;
        }

        if (playerCollider != null)
        {
            playerCollider.enabled = false;
        }
    }

    private IEnumerator AdjustCameraHeight()
    {
        float initialCamHeight = m_Camera.transform.position.y;
        float targetCamHeight = initialCamHeight + (bodyParts.Count * 0.8f); // Adjust the multiplier to control the amount of camera lift per wagon
        float liftSpeed = 10f; // Adjust the speed of camera lift

        while (m_Camera.transform.position.y < targetCamHeight)
        {
            Vector3 newCamPos = m_Camera.transform.position;
            newCamPos.y += liftSpeed * Time.deltaTime; // Adjust the speed of camera lift
            m_Camera.transform.position = newCamPos;
            yield return null;
        }
    }

    private IEnumerator StackWagonsOneByOne()
    {
        float skyscraperHeight = skyscraper.transform.position.y;
        float gapBetweenWagons = 2.5f; // Adjust this value to reduce the gap between wagons
        float stackDelay = 0.1f; // Adjust this value to make the stacking faster

        foreach (var body in bodyParts)
        {
            body.transform.position = new Vector3(skyscraper.transform.position.x, skyscraperHeight, skyscraper.transform.position.z);
            skyscraperHeight += gapBetweenWagons; // Use the smaller value for the gap
            yield return new WaitForSeconds(stackDelay); // Wait before stacking the next wagon
        }

        yield return new WaitForSeconds(1f); // Small delay before showing the result
        CheckWinCondition();
    }

    private void CheckWinCondition()
    {
        if (bodyParts.Count >= wagonsRequiredToWin)
        {
            Debug.Log("You Win!");

            win1.Play();
            win2.Play();
            StartCoroutine(ShowWinScreenWithDelay(1f)); // Add any additional win actions or UI here
        }
        else
        {
            Debug.Log("You Lose!");
            lostScreen.SetActive(true); // Display the lost screen immediately
            // Add any additional lose actions or UI here
        }
    }

    private void UpdateCameraZoom()
    {
        int numWagons = bodyParts.Count;
        float targetFOV = initialFOV;

        if (numWagons > wagonsForSecondZoomOut)
        {
            targetFOV = secondZoomOutFOV;
        }
        else if (numWagons > wagonsForFirstZoomOut)
        {
            targetFOV = firstZoomOutFOV;
        }

        // Smoothly transition to the target FOV
        m_Camera.fieldOfView = Mathf.Lerp(m_Camera.fieldOfView, targetFOV, Time.deltaTime * 2f);
    }

    private IEnumerator CountdownWagonCount(int count)
    {
        int startCount = wagonCount;
        int endCount = Mathf.Max(wagonCount - count, 0); // Ensure the count doesn't go negative

        float duration = 2f; // Duration of the countdown in seconds
        float timer = 0f;
        int previousCount = startCount;

        while (timer < duration)
        {
            // Calculate the current count based on interpolation between start and end counts
            int currentCount = (int)Mathf.Lerp(startCount, endCount, timer / duration);

            // Update TextMeshPro text
            if (wagonCountText != null)
            {
                wagonCountText.text = "" + currentCount.ToString();
            }

            if (currentCount != previousCount)
            {
                finalCountSound.Play();
                previousCount = currentCount;
            }

            // Wait for the next frame
            yield return null;

            // Increment the timer
            timer += Time.deltaTime;
        }

        // Ensure the count is set to the end count
        wagonCount = endCount;
        if (wagonCountText != null)
        {
            wagonCountText.text = "" + wagonCount.ToString();
        }
    }

    private IEnumerator ShowWinScreenWithDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        winScreen.SetActive(true);
    }

    private IEnumerator RocketLaunch(int count)
    {
        yield return new WaitForSeconds(count);
        DOTween.PlayForward("Rocket");
    }

    private IEnumerator CountdownFinalWagonCount(int count)
    {
        float duration = 2f; // Duration of the countdown in seconds
        float timer = 0f;
        int currentCount = 0; // Start counting from zero

        while (timer < duration)
        {
            currentCount = (int)Mathf.Lerp(0, count, timer / duration); // Count up from zero to total wagons

            if (wagonCountTextFinal != null)
            {
                wagonCountTextFinal.text = "" + currentCount.ToString();
            }

            if (bodyParts.Count >= wagonsRequiredToWin)
            {
                StartCoroutine(RocketLaunch(1));
                // Add any additional win actions or UI here
            }

            yield return null;

            timer += Time.deltaTime;
        }

        if (wagonCountTextFinal != null)
        {
            wagonCountTextFinal.text = "" + count.ToString(); // Ensure final count matches the total wagons collected
        }

        if (bodyParts.Count >= wagonsRequiredToWin)
        {
            // Add any additional win actions or UI here
            Debug.Log("You Win!");

            StartCoroutine(ShowWinScreenWithDelay(1f));
        }
        else
        {
            lostScreen.SetActive(true); // Show the lost screen immediately
            Debug.Log("You Lose!");
        }
    }

    private void LoseWagons(int count)
    {
        StartCoroutine(LoseWagonsCoroutine(count, 0.1f)); // Adjust the delay as needed
    }

    private IEnumerator LoseWagonsCoroutine(int count, float delay)
    {
        // Ensure we don't try to remove more wagons than we have
        count = Mathf.Min(count, bodyParts.Count);

        for (int i = 0; i < count; i++)
        {
            if (bodyParts.Count > 0)
            {
                // Get the last body part
                GameObject lastBodyPart = bodyParts[bodyParts.Count - 1];

                // Remove it from the list
                bodyParts.RemoveAt(bodyParts.Count - 1);

                // Destroy the body part
                Destroy(lastBodyPart);

                // Update the wagon count
                wagonCount--;

                // Update TextMeshPro text
                if (wagonCountText != null)
                {
                    wagonCountText.text = "" + wagonCount.ToString();
                }

                // If there are no wagons left, ensure the count is zero
                if (wagonCount < 0)
                {
                    wagonCount = 0;
                }

                yield return new WaitForSeconds(delay);
            }
            else
            {
                break; // Exit the loop if there are no more body parts
            }
        }
    }
}
