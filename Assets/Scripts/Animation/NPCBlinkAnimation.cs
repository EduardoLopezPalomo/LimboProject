using System;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public struct NpcPortrait
{
    public Sprite baseSprite;
    public Sprite openEyes;
    public Sprite mediumEyes;
    public Sprite closeEyes;
    public Sprite openMouth;
    public Sprite closeMouth;
}


public class NPCBlinkAnimation : MonoBehaviour
{
    [Header("Images")]
    [SerializeField] private Image baseImg;
    [SerializeField] private Image openEyes;
    [SerializeField] private Image mediumEyes;
    [SerializeField] private Image closeEyes;
    [SerializeField] private Image openMouth;
    [SerializeField] private Image closeMouth;
    [Header("Mouth")]
    [SerializeField] private float timeBetweenMouthShapes = 0.12f;
    [SerializeField] private Animator blinkAnimator;

    private bool talking;
    private float timer;
    private bool haveMouth;

    private Animator BlinkAnimator
    {
        get
        {
            if (blinkAnimator == null) blinkAnimator = GetComponent<Animator>();
            return blinkAnimator;
        }
    }
    public void SetPortrait(NpcPortrait r)
    {
        baseImg.sprite = r.baseSprite;
        openEyes.sprite = r.openEyes;
        mediumEyes.sprite = r.mediumEyes;
        closeEyes.sprite = r.closeEyes;
        openMouth.sprite = r.openMouth;
        closeMouth.sprite = r.closeMouth;

        // Only if the NPC has a mouth
        haveMouth = r.openMouth != null && r.closeMouth != null;
        SetMouth(false);

        // The NPC god could have no eyes only if there is no sprite assigned for them, otherwise they should have eyes. 
        bool nameHasGods = !string.IsNullOrEmpty(baseImg.sprite.name)
        && baseImg.sprite.name.Contains("gods", StringComparison.OrdinalIgnoreCase);

        bool hasNotEyesAssigned = r.openEyes == null && r.mediumEyes == null && r.closeEyes == null;


        bool hasNotEyes = nameHasGods && hasNotEyesAssigned;

        Debug.Log($"NPC {baseImg.sprite.name} has no eyes assigned: {hasNotEyesAssigned}, name has gods: {nameHasGods}, final result (has not eyes): {hasNotEyes}");
        if (hasNotEyes)
        {
            BlinkAnimator.enabled = false;
            openEyes.gameObject.SetActive(false);
            mediumEyes.gameObject.SetActive(false);
            closeEyes.gameObject.SetActive(false);
        }
        else
        {
            BlinkAnimator.enabled = true;
        }

         
    }

    public void SetTalking(bool value)
    {
        if (talking == value) return;

        talking = value;
        timer = 0f;
        if (!talking) SetMouth(false);
    }

    private void Update()
    {
        if (!talking || !haveMouth) return;

        timer += Time.deltaTime;
        if (timer >= timeBetweenMouthShapes)
        {
            timer = 0f;
            SetMouth(!openMouth.gameObject.activeSelf);
        }
    }

    private void SetMouth(bool open)
    {
        if (!haveMouth)
        {
            // If the NPC doesn't have a mouth, ensure both mouth images are inactive
            openMouth.gameObject.SetActive(false);
            closeMouth.gameObject.SetActive(false);
            return;
        }
        else
        {
            openMouth.gameObject.SetActive(open);
            closeMouth.gameObject.SetActive(!open);
        }
    }
}
