using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIBridge : MonoBehaviour
{
    public TMP_Text Title;
    public TMP_Text Body;
    public Button NextButton;
    public Button RestartButton;
    public TMP_Text Message;
    public Image Image;
    public Image LargeImage;
    
    // --- INSTRUCTIONS SCENE ---
    public void NextInstruction()
    {
        InstructionManager im = FindObjectOfType<InstructionManager>();
        if (im != null)
        {
            im.OnNextPressed();
        }
    }

    // --- INSTRUCTIONS and FinishedCogTask ---
    public void RestartInstruction()
    {
        InstructionManager im = FindObjectOfType<InstructionManager>();
        if (im != null)
        {
            im.OnRestartPressed();
        }
    }

    // --- QUIZ SCENE ---
    public void ToggleAnswer(Toggle toggle)
    {
        // We only care about this if it's the specific template toggle 
        // generated toggles handle their own logic via code
        InstructionManager im = FindObjectOfType<InstructionManager>();
        if (im != null)
        {
            im.CheckAnswer(toggle);
        }
    }

    // --- COG TASK HOME SCENE ---
    // Note: You must drag the Button itself into the parameter slot in the Inspector
    public void SelectNextTask(Button button)
    {
        if (GameManager.instance != null)
            GameManager.instance.Next(button);
    }

    // --- CONFIDENCE SCENE ---
    public void SelectConfidence(Button button)
    {
        if (GameManager.instance != null)
            GameManager.instance.ConfidenceSelection(button);
    }

    // ---  FinishedCogTask SCENE ---
    public void EndNext()
    {
        if (GameManager.instance != null)
            GameManager.instance.EndNext();
    }
}