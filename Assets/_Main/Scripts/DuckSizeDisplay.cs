using TMPro;
using UnityEngine;

public class DuckSizeDisplay : MonoBehaviour
{
    public Player player;
    public TMP_Text label;
    public string prefix = "Size: ";
    public string format = "F1";

    void Update()
    {
        if (player == null || label == null) return;
        label.text = prefix + player.transform.localScale.x.ToString(format);
    }
}
