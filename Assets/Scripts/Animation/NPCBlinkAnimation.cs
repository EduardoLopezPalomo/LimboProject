using System;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public struct RetratoNPC
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

    private bool talking;
    private float timer;
    private bool haveMouth;

    public void SetRetrato(RetratoNPC r)
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
