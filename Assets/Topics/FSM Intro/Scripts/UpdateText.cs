using TMPro;
using UnityEngine;

public class UpdateText : MonoBehaviour
{
    public SimpleAgent agent;
    public TMP_Text textElement;

    void Update()
    {
        textElement.text = agent?.FSM.Current.Name ?? "No state - error";
    }
}
