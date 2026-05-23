using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpiderManSwing : SpiderMan
{
    [SerializeField] private GameObject swingArea;
    [SerializeField] private Transform bob, leftWebPoint, rightWebPoint;
    [SerializeField] private AudioClip[] webSwings;
    [SerializeField] private AudioClip swingEnd;

    LineRenderer lineRenderer;
    private int frameCount = 0;
    private bool swingCompleted = false;
    private float currentSwingAnimationValue;

    private Transform currentWebPoint;
    private bool lastSwingWasLeft = false;
    private Vector3 currentGrapplePosition;

    private void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();

        if (lineRenderer != null)
        {
            lineRenderer.startWidth = 0.1f;
            lineRenderer.endWidth = 0.1f;
            lineRenderer.positionCount = 2;
        }
    }

    private void Update()
    {
        if (!isGrounded && !isSwinging && Input.GetMouseButtonDown(0))
        {
            StartSwinging();
        }

        if (isSwinging)
        {
            UpdateSwinging();
        }

        if (swingCompleted)
        {
            StopSwinging();
            swingCompleted = false;
        }
    }

    private void StartSwinging()
    {
        isSwinging = true;

        SelectSwingSide();
        SelectRandomSwingAnimation();
        StartCoroutine(SetSwingingAnimationWithDelay());

        if (swingArea != null)
        {
            SwingArea swingAreaScript = swingArea.GetComponent<SwingArea>();
            if (swingAreaScript != null)
            {
                swingAreaScript.SetSpiderManSwing(this);
            }

            swingArea.transform.rotation = Quaternion.Euler(new Vector3(0, transform.eulerAngles.y + 90, 0));
            swingArea.SetActive(true);
        }

        if (lineRenderer != null)
        {
            lineRenderer.enabled = true;

            if (currentWebPoint != null)
            {
                currentGrapplePosition = currentWebPoint.position;
                lineRenderer.SetPosition(0, currentWebPoint.position);
                lineRenderer.SetPosition(1, currentGrapplePosition);
            }

            if (audioSource != null && webSwings != null)
            {
                var index = Random.Range(0, webSwings.Length);
                audioSource.PlayOneShot(webSwings[index], 0.7f);
            }
        }

        jumpVelocity = Vector3.zero;
        swingCompleted = false;
    }

    private void SelectSwingSide()
    {
        if (lastSwingWasLeft)
        {
            currentWebPoint = rightWebPoint;
            lastSwingWasLeft = false;
        }
        else
        {
            currentWebPoint = leftWebPoint;
            lastSwingWasLeft = true;
        }

        if (_animator != null)
        {
            _animator.SetBool("Mirror", currentWebPoint == leftWebPoint);
        }
    }

    private void SelectRandomSwingAnimation()
    {
        int randomPercent = Random.Range(0, 100);

        if (randomPercent < 50) // 50% шанс
        {
            currentSwingAnimationValue = 1f;
        }
        else if (randomPercent < 80) // 30% шанс (50-80)
        {
            currentSwingAnimationValue = 2f;
        }
        else // 20% шанс (80-100)
        {
            currentSwingAnimationValue = 3f;
        }
    }

    private IEnumerator SetSwingingAnimationWithDelay()
    {
        yield return new WaitForEndOfFrame();
        if (_animator != null)
        {
            _animator.SetBool("Swinging", true);
            _animator.SetFloat("SwingAnim", currentSwingAnimationValue);
        }
    }

    private void UpdateSwinging()
    {
        if (bob != null)
        {
            transform.position = bob.position;
        }

        DrawWebLine();

        if (Input.GetMouseButtonUp(0))
        {
            StopSwinging();
        }
    }

    private void DrawWebLine()
    {
        if (!lineRenderer || !lineRenderer.enabled || currentWebPoint == null || swingArea == null)
            return;

        currentGrapplePosition = Vector3.Lerp(currentGrapplePosition, swingArea.transform.position, Time.deltaTime);

        lineRenderer.SetPosition(0, currentWebPoint.position);
        lineRenderer.SetPosition(1, currentGrapplePosition);
    }

    public void StopSwinging()
    {
        isSwinging = false;

        if (swingArea != null)
        {
            swingArea.SetActive(false);
        }
        if (lineRenderer != null)
        {
            lineRenderer.enabled = false;
        }
        if (audioSource != null && swingEnd != null)
        {
            audioSource.PlayOneShot(swingEnd, 0.8f);
        }
        if (_animator != null)
        {
            _animator.SetBool("Swinging", false);
            _animator.SetBool("Mirror", false);
        }

        currentGrapplePosition = Vector3.zero;
    }


    public void OnSwingCompleted()
    {
        swingCompleted = true;
    }
}