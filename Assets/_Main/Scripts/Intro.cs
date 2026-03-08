using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class Intro : MonoBehaviour
{
    public GameObject sadNode;
    public GameObject superNode;
    public double switchTime = 3.0;
    public RawImage videoDisplay;
    public float fadeLeadTime = 2f;

    private VideoPlayer videoPlayer;
    private bool switched = false;
    private bool fading = false;

    void Start()
    {
        videoPlayer = transform.Find("Intro").GetComponent<VideoPlayer>();
        videoPlayer.Play();

        sadNode.SetActive(true);
        superNode.SetActive(false);
    }

    void Update()
    {
        if (!videoPlayer.isPrepared || videoPlayer.frameCount == 0)
            return;

        double duration = (double)videoPlayer.frameCount / videoPlayer.frameRate;
        double remaining = duration - videoPlayer.time;

        if (!switched && videoPlayer.time >= switchTime)
        {
            sadNode.SetActive(false);
            superNode.SetActive(true);
            switched = true;
        }

        Debug.Log($"Intro remaining: {remaining:F2}s");

        if (!fading && remaining <= fadeLeadTime)
            fading = true;

        if (fading)
        {
            float alpha = Mathf.Clamp01((float)(remaining / fadeLeadTime));
            Color c = videoDisplay.color;
            c.a = alpha;
            videoDisplay.color = c;
        }


        if (remaining <= 0.05)
        {
            Debug.Log("Intro finished, destroying.");
            Destroy(gameObject);
        }
    }
}
