using TMPro;
using UnityEngine;

public class Game : MonoBehaviour
{
    [SerializeField] private TMP_Text timerText;

    private float _elapsed;

    void Update()
    {
        _elapsed += Time.deltaTime;
        timerText.text = _elapsed.ToString("F2");
    }
}
