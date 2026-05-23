using UnityEngine;
using UnityEngine.UI;

public class FPS_Displayer : MonoBehaviour
{
    float deltaTime = 0.0f;
    public Text fpsText;

    void Start()
    {
        this.hideFlags = HideFlags.HideInHierarchy;
        gameObject.hideFlags = HideFlags.HideInHierarchy;
    }

    void Update()
    {
        deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;

        float msec = deltaTime * 1000.0f;
        float fps = 1.0f / deltaTime;
        fpsText.text = string.Format("{0:0.0} ms ({1:0.} fps)", msec, fps);
    }
}