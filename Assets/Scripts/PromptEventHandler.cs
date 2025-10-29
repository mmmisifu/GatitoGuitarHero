using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PromptEventHandler : MonoBehaviour
{
    public GameObject[] glowBtns;       
    public GameObject speechBubble;
    public TextMeshProUGUI lyrics;

    public void OnPrompt(int lane, string lyric)
    {
        // If lyric, turn on speech bubble & display lyrics
        if (lyric.Length != 0)
        {
            speechBubble.SetActive(true);
            if (lyrics) lyrics.text = lyric;
        }

        // Turn on only the correct lane's glowBtn
        for (int i = 0; i < glowBtns.Length; i++)
        {
            glowBtns[i].SetActive(i == lane);
        }
    }

    public void OnPromptExpired()
    {
        // Turn off all glowBtns
        foreach (var obj in glowBtns)
            obj.SetActive(false);
    }
}

