using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.Serialization;

[Serializable]
public struct DialogueInitData
{
    public List<string> lines;
    public string npcId;
    public string characterName;
    public VoiceType voice;
    public NpcPortrait portrait;
    public PlayerInput input;
}

public class DialogueUI : MonoBehaviour
{
    public static event System.Action<string> OnEndDialogue;
    
    [FormerlySerializedAs("panelDialogo")]
    [SerializeField] private GameObject dialoguePanel;
    [FormerlySerializedAs("textoDialogo")]
    [SerializeField] private TMP_Text dialogueText;
    [FormerlySerializedAs("nombreNPC")]
    [SerializeField] private TMP_Text npcNameText;
    [SerializeField] private NPCBlinkAnimation avatar;

    [Header("Efecto de escritura")]
    [FormerlySerializedAs("velocidadEscritura")]
    [SerializeField] private float typewriterDelay = 0.03f;
    [FormerlySerializedAs("sfxHablaMasculina")]
    [SerializeField] private List<SFXDefinition> masculineTalkSfx;
    [FormerlySerializedAs("sfxHablaFemenina")]
    [SerializeField] private List<SFXDefinition> feminineTalkSfx;
    [FormerlySerializedAs("sfxHablaMonstruo")]
    [SerializeField] private List<SFXDefinition> monsterTalkSfx;

    private string currentNpcId;
    private VoiceType currentVoice;
    private Queue<string> lineQueue = new Queue<string>();
    private PlayerInput currentPlayerInput;
    private Coroutine typewriterCoroutine;
    private bool isTyping = false;
    private string currentLine = "";
    private AudioClip LastClip;
    private AudioSource LastAudioSource;

    private bool InDialogue = false;

    private void OnEnable()
    {
        IConversation.OnDialogueInit += DialogueInit;
    }

    private void OnDisable()
    {
        IConversation.OnDialogueInit -= DialogueInit;
    }

    private void Update()
    {
        avatar.SetTalking(LastAudioSource != null && LastAudioSource.isPlaying);
        if (!InDialogue || currentPlayerInput == null) return;

        InputAction interactAction = currentPlayerInput.currentActionMap.FindAction("Interact");
        if (interactAction != null && interactAction.WasPressedThisFrame())
        {
            if (isTyping)
            {
                CompleteLineInstantly();
            }
            else
            {
                ShowNextLine();
            }
        }
    }

    private void DialogueInit(DialogueInitData data)
    {
        InDialogue = true;
        ShowDialogue(data.lines, data.npcId, data.characterName, data.voice, data.portrait, data.input);
    }


    private void ShowDialogue(List<string> lines , string npcId, string characterName, VoiceType voice, NpcPortrait portrait, PlayerInput input = null)
    {

        lineQueue = new Queue<string>(lines);
        currentPlayerInput = input;
        npcNameText.text = characterName;
        currentVoice = voice;
        currentNpcId = npcId;
        //if (retratoNPC != null)
          //  retratoNPC.sprite = retrato;
        avatar.SetPortrait(portrait);

        dialoguePanel.SetActive(true);
        ShowNextLine();
    }

    public void ShowNextLine()
    {
        if (lineQueue.Count == 0)
        {
            CloseDialogue();
            return;
        }

        currentLine = lineQueue.Dequeue();

        if (typewriterCoroutine != null)
            StopCoroutine(typewriterCoroutine);

        typewriterCoroutine = StartCoroutine(TypeText(currentLine));
    }

    private IEnumerator TypeText(string line)
    {
        isTyping = true;
        dialogueText.text = "";
        
        

        SFXDefinition voiceClip = GetClipForVoice();
        if (voiceClip != null)
        {
            LastAudioSource = SoundFXManager.instance.PlaySoundRandomPitch(voiceClip.sfx, transform, voiceClip.volume , voiceClip.minPitch, voiceClip.maxPitch);
            LastClip = voiceClip.sfx;
        }

        foreach (char ch in line)
        {
            dialogueText.text += ch;
            yield return new WaitForSeconds(typewriterDelay);
        }

        isTyping = false;
    }

    private SFXDefinition GetClipForVoice()
    {
        List<SFXDefinition> voiceClips = currentVoice switch
        {
            VoiceType.Masculine => masculineTalkSfx,
            VoiceType.Feminine => feminineTalkSfx,
            VoiceType.Monster => monsterTalkSfx,
            _ => masculineTalkSfx
        };

        if (voiceClips == null || voiceClips.Count == 0) return null;

        return voiceClips[UnityEngine.Random.Range(0, voiceClips.Count)];
    }

    private void CompleteLineInstantly()
    {
        if (typewriterCoroutine != null)
            StopCoroutine(typewriterCoroutine);
        
        if(LastClip != null && LastAudioSource != null)
        {
            SoundFXManager.instance.StopSound(LastClip, LastAudioSource);
        }

        dialogueText.text = currentLine;
        isTyping = false;
    }

    private void CloseDialogue()
    {
        dialoguePanel.SetActive(false);
        
        if(LastClip != null && LastAudioSource != null)
        {
            SoundFXManager.instance.StopSound(LastClip, LastAudioSource);
        }
        
        LastClip = null;
        LastAudioSource = null;
        InDialogue = false;
        if (currentPlayerInput != null)
            currentPlayerInput.SwitchCurrentActionMap("Player");
        OnEndDialogue?.Invoke(currentNpcId);
    }
}