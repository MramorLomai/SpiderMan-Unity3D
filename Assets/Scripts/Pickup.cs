using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pickup : MonoBehaviour
{
    [SerializeField] private float rotateSpeed = 10.0f;
    [SerializeField] private float bobSpeed = 2.0f;
    [SerializeField] private float bobHeight = 0.5f;
    [SerializeField] private int currencyValue = 1;
    [SerializeField] private GameObject pickupEffect;
    [SerializeField] private AudioClip pickupSound;

    private float startY;
    private float newY;
    private bool isCollected = false;

    private void Start()
    {
        startY = transform.position.y;
    }

    private void RotateItem()
    {
        transform.Rotate(0, rotateSpeed * Time.deltaTime, 0);
    }

    private void BobItem()
    {
        newY = startY + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
    }

    void Update()
    {
        if (!isCollected)
        {
            RotateItem();
            BobItem();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isCollected && other.CompareTag("Player"))
        {
            SpiderMan spiderMan = other.GetComponent<SpiderMan>();

            if (spiderMan != null)
            {
                Collect(spiderMan);
            }
        }
    }

    private void Collect(SpiderMan spiderMan)
    {
        isCollected = true;
        spiderMan.AddCurrency(currencyValue);
        Vector3 CollectPoint = new Vector3(transform.position.x, newY+0.5f, transform.position.z);

        if (pickupEffect != null)
        {
            Instantiate(pickupEffect, CollectPoint, Quaternion.identity);
        }

        if (pickupSound != null)
        {
            AudioSource.PlayClipAtPoint(pickupSound, CollectPoint);
        }

        MeshRenderer[] renderers = GetComponentsInChildren<MeshRenderer>();
        foreach (var renderer in renderers)
        {
            renderer.enabled = false;
        }

        GetComponent<Collider>().enabled = false;
        Destroy(gameObject, 0.5f);
    }
}


