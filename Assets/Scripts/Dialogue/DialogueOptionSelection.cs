using TMPro;
using TMPro.Examples;
using UnityEngine;
using UnityEngine.Events;

public class DialogueOptionSelection : MonoBehaviour, IInteractable {
    [SerializeField] private TMP_Text m_optionText;

    private UnityAction<DialogueOption> onOptionSelected;
    private Dialogue_Interactor interactor;
    private DialogueOption option;
    private VertexJitter jitter;

    public void SetupOption(DialogueOption option, Dialogue_Interactor interactor) {
        this.option = option;
        this.interactor = interactor;

        m_optionText.text = option.m_optionText;
        onOptionSelected += OnOptionSelected;

        jitter = m_optionText.GetComponent<VertexJitter>();
    }

    private void OnDestroy() {
        onOptionSelected -= OnOptionSelected;
    }

    public void Interact(Player_InteractionSystem interactor) {
        onOptionSelected?.Invoke(option);
    }

    public void OnHoverOverItem(bool isOnTarget) {
        if (isOnTarget) { 
            jitter.OnDialogueStarted(TalkerMood.Normal);
            m_optionText.color = Color.green;
            return;
        }

        m_optionText.color = Color.white;
        jitter.StopJittering();
    }

    private void OnOptionSelected(DialogueOption option) => interactor.OnOptionSelected(option);
}
