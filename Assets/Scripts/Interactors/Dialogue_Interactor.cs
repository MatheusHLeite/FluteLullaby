using DG.Tweening;
using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using TMPro.Examples;
using UnityEngine;

public class Dialogue_Interactor : MonoBehaviour, IInteractable {
    [Header("Dialogue")]
    [SerializeField] private string m_screenShowcaseName;
    [SerializeField] private Dialogue_SO m_dialogue;
    [SerializeField] private Transform m_upperHead;

    [Header("Visuals")]
    [SerializeField] private Renderer[] m_itemVisual;
    
    [Header("Options UI")]
    [FoldoutGroup("Setup")][SerializeField] private DialogueOptionSelection m_optionButtonPrefab;
    [FoldoutGroup("Setup")][SerializeField] private Transform m_optionContainer;

    [Header("Transforms")]
    [FoldoutGroup("Setup")][SerializeField] private GameObject m_base;
    [Tooltip("The camera will zoom and focus at this Transform when starting the dialogue")]
    [FoldoutGroup("Setup")][SerializeField] private Transform m_focusPoint;
    [FoldoutGroup("Setup")][SerializeField] private Transform m_quickTextHolder;

    private AudioSource audioSource;

    private InputHandler input;
    private Dialogue_SO actualDialogue;
    private VerticalLayout3D m_verticalLayout;
    private TMP_Text quickText;
    private Coroutine typingCoroutine;

    private bool onDialogue;
    private bool dialogueRunning;
    private int dialogueIndex;
    private float cooldownTime;

    private MaterialPropertyBlock propBlock;
    private Material outlineInstance;
    private float outlineWidth;

    private float speakAmount;
    private bool disabled;

    private List<GameObject> optionsCreated = new List<GameObject>();

    private Camera playerCamera;

    protected virtual void Awake() {
        Singleton.Instance.GameEvents.OnPlayerLoaded.AddListener(i => playerCamera = i.GetPlayerCamera());

        quickText = m_quickTextHolder.transform.GetComponentInChildren<TMP_Text>();
        m_verticalLayout = m_optionContainer.GetComponent<VerticalLayout3D>();
        input = Singleton.Instance.InputHandler;

        m_quickTextHolder.gameObject.SetActive(false);

        SetMaterials();
    }

    protected virtual void OnDestroy() {
        Singleton.Instance.GameEvents.OnPlayerLoaded.RemoveListener(i => playerCamera = i.GetPlayerCamera());

        if (outlineInstance) Destroy(outlineInstance);
    }

    private void Start() {
        audioSource = Singleton.Instance.DialogueManager.GetAudioSource();
        speakAmount = -90;
    }

    private void SetMaterials() {
        propBlock = new MaterialPropertyBlock();

        if (m_itemVisual.Length <= 0) {
            m_itemVisual = GetComponentsInChildren<Renderer>();
        }

        outlineWidth = 1.05f;
        Material material = Singleton.Instance.DialogueManager.GetOutlineMaterial();

        for (int v = 0; v < m_itemVisual.Length; v++) {
            Material[] currentMaterials = m_itemVisual[v].materials;
            if (!System.Array.Exists(currentMaterials, m => m.name.Contains(material.name))) {
                Material[] newMats = new Material[currentMaterials.Length + 1];
                for (int i = 0; i < currentMaterials.Length; i++)
                    newMats[i] = currentMaterials[i];

                if (!outlineInstance)
                    outlineInstance = new Material(material);
                newMats[^1] = outlineInstance;

                m_itemVisual[v].materials = newMats;
            }

            m_itemVisual[v].GetPropertyBlock(propBlock);
            propBlock.SetFloat("_OutlineScale", 0);
            m_itemVisual[v].SetPropertyBlock(propBlock);
        }
    }

    public void StopImmediately() {
        OnHoverOverItem(false);
        Destroy(m_base);

        disabled = true;

        StopAllCoroutines();
        ResetInteraction();        
    }

    public virtual void OnHoverOverItem(bool isOnTarget) {
        if (onDialogue || disabled) return;

        Singleton.Instance.GameEvents.OnHoverOverItem?.Invoke(isOnTarget ? m_screenShowcaseName : "");

        propBlock.SetFloat("_OutlineScale", isOnTarget ? outlineWidth : 0);
        for (int i = 0; i < m_itemVisual.Length; i++)
            m_itemVisual[i].SetPropertyBlock(propBlock);
    }

    public virtual void Interact(Player_InteractionSystem interactor) {
        if (onDialogue || disabled) return;

        Singleton.Instance.GameEvents.OnHoverOverItem?.Invoke("");
        Singleton.Instance.GameEvents.OnSlotSelected?.Invoke(interactor.ActualSlotSelected, false);

        actualDialogue = m_dialogue;

        TriggerDialogue();
    }

    private void ResetInteraction() {
        onDialogue = false;
        Singleton.Instance.GameEvents.OnInteractionReset?.Invoke();        
    }

    public void TriggerDialogue() {
        dialogueIndex = 0;
        onDialogue = true;

        Vector3 rot = playerCamera.transform.position - quickText.transform.position;
        m_quickTextHolder.transform.rotation = Quaternion.LookRotation(rot);
        m_optionContainer.transform.rotation = Quaternion.LookRotation(rot);

        NextDialogue();
    }

    private void NextDialogue() {
        if (dialogueIndex >= actualDialogue.m_dialogues.Length) {
            OnDialogueEnd();
            return;
        }

        if (typingCoroutine != null) 
            StopCoroutine(typingCoroutine);        

        typingCoroutine = StartCoroutine(ShowQuickText(actualDialogue.m_dialogues[dialogueIndex]));
        dialogueIndex++;
    }

    private IEnumerator ShowQuickText(Dialogue dialogue) {
        CharacterData characterData = Singleton.Instance.DialogueManager.GetTalker(dialogue);

        dialogueRunning = true;
        m_quickTextHolder.gameObject.SetActive(true);

        quickText.DOKill();
        quickText.text = string.Empty;
        quickText.color = characterData.m_talkerColor;

        var jitter = quickText.GetComponent<VertexJitter>();
        if (jitter != null)        
            jitter.OnDialogueStarted(dialogue.m_speakerMood);

        string fullText = dialogue.m_dialogue;

        for (int i = 0; i < fullText.Length; i++) {
            if (!dialogueRunning) {
                quickText.text = fullText;
                break;
            }

            if (char.IsLetterOrDigit(fullText[i])) {
                audioSource.pitch = Random.Range(characterData.m_pitchRange.x, characterData.m_pitchRange.y);
                audioSource.PlayOneShot(characterData.m_talkerVoice);

                speakAmount -= Time.deltaTime * Random.Range(255, 290);
                yield return null;
            }

            char currentChar = fullText[i];
            quickText.text += currentChar;

            float delay = characterData.m_voiceSpeed;

            if (currentChar == ',' || currentChar == ';') 
                delay += 0.25f;
            else if (currentChar == '.' || currentChar == '!' || currentChar == '?')
                delay += 0.95f;            

            yield return new WaitForSeconds(delay);
        }

        yield return new WaitForSeconds(0.15f);
        CheckDialogueOptions();
    }

    private void OnDialogueEnd() {
        dialogueIndex = 0;

        if (typingCoroutine != null) {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }

        quickText.DOFade(0, .25f).OnComplete(() => {
            quickText.SetText(string.Empty);
            m_quickTextHolder.gameObject.SetActive(false);

            ResetInteraction();
        });  
    }

    private void CheckDialogueOptions() {
        var current = actualDialogue.m_dialogues[dialogueIndex - 1];

        if (current.m_answers.Count > 0) {
            ShowOptions(current);
            return;
        }

        dialogueRunning = false;
    }

    #region Options
    private void ShowOptions(Dialogue question) {
        dialogueRunning = false;

        m_optionContainer.gameObject.SetActive(true);

        foreach (Transform child in m_optionContainer)
            Destroy(child.gameObject);

        foreach (DialogueOption option in question.m_answers) {
            DialogueOptionSelection newOption = Instantiate(m_optionButtonPrefab, m_optionContainer);
            newOption.SetupOption(option, this);

            optionsCreated.Add(newOption.gameObject);
        }

        m_verticalLayout.RebuildLayout();
    }
    
    public void OnOptionSelected(DialogueOption option) {
        for (int i = 0; i < optionsCreated.Count; i++) {
            Destroy(optionsCreated[i]);
        }
        optionsCreated.Clear();

        m_optionContainer.gameObject.SetActive(false);

        option.m_onSelected?.Invoke();

        if (option.m_nextDialogue != null) {
            actualDialogue = option.m_nextDialogue;
            TriggerDialogue();
            return;
        }

        OnDialogueEnd();
    }
    #endregion

    private void HandleDialogueSkipInput() {
        cooldownTime = Time.time + 0.45f;

        if (dialogueRunning) {
            if (typingCoroutine != null) {
                StopCoroutine(typingCoroutine);
                typingCoroutine = null;
            }

            Dialogue current = actualDialogue.m_dialogues[dialogueIndex - 1];
            quickText.text = current.m_dialogue;

            CheckDialogueOptions();            
            return;
        }

        if (actualDialogue.m_dialogues[dialogueIndex - 1].m_answers.Count > 0) return;

        NextDialogue();
    }

    private void HandleDialogueEndCheck() {
        if (input.SkipDialogue && cooldownTime < Time.time) {
            HandleDialogueSkipInput();
        }
    }

    private void HandleTextVisual() {
        if (playerCamera == null) return;

        Vector3 direction = playerCamera.transform.position - quickText.transform.position;
        Quaternion targetRotation = Quaternion.LookRotation(direction);

        Vector3 optionDirection = playerCamera.transform.position - m_optionContainer.transform.position; 
        Quaternion optionTargetRotation = Quaternion.LookRotation(optionDirection);

        m_quickTextHolder.transform.rotation = Quaternion.Lerp(
            m_quickTextHolder.transform.rotation,
            targetRotation,
        Time.deltaTime * 5f
        );

        m_optionContainer.transform.rotation = Quaternion.Lerp(
            m_optionContainer.transform.rotation,
            optionTargetRotation,
        Time.deltaTime * 5f
        );
    }

    private void Update() {
        if (!onDialogue || disabled) return;

        if (speakAmount < -90) speakAmount += Time.deltaTime * 80;
        if (m_upperHead) m_upperHead.localRotation = Quaternion.Euler(speakAmount, 0, 0);

        HandleTextVisual();
        HandleDialogueEndCheck();
    } 
}
