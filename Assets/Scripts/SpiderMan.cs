using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public abstract class SpiderMan : MonoBehaviour
{
    protected Vector3 moveVelocity;
    protected static Vector3 jumpVelocity = Vector3.zero;
    protected Vector3 startingPosition;
    protected Animator _animator;
    protected Rigidbody rBody;
    protected Collider collider;
    public AudioSource audioSource;

    protected float yRot, horizontalAxis, verticalAxis;
    protected static bool isGrounded, isSwinging;

    protected int currentCurrency = 0;
    public System.Action<int> OnCurrencyChanged;

    public void Awake()
    {
        isGrounded = true;
        startingPosition = transform.position;
        audioSource = GetComponent<AudioSource>();
        collider = GetComponent<Collider>();
        rBody = GetComponent<Rigidbody>();
        _animator = GetComponent<Animator>();

        currentCurrency = 0;
    }

    public void AddCurrency(int amount)
    {
        currentCurrency += amount;
        OnCurrencyChanged?.Invoke(currentCurrency);
    }
    public int GetCurrency()
    {
        return currentCurrency;
    }
    public bool SpendCurrency(int amount)
    {
        if (currentCurrency >= amount)
        {
            currentCurrency -= amount;
            OnCurrencyChanged?.Invoke(currentCurrency);
            return true;
        }
        return false;
    }
}