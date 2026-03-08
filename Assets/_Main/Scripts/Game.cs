using TMPro;
using UnityEngine;

public class Game : MonoBehaviour
{
    [SerializeField] private TMP_Text timerText;

    private float _elapsed;
    private bool _running;

    public void StartGame()
    {
        _elapsed = 0f;
        _running = true;
    }

    void Update()
    {
        if (!_running) return;

        _elapsed += Time.deltaTime;
        timerText.text = _elapsed.ToString("F2");
    }
}
