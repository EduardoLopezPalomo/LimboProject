using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerUIController : MonoBehaviour
{
    public GameObject playerUI;
    public GameObject PauseMenu;
    public GameObject AblityMenu;
    public GameObject OptionsMenu;
    public GameObject DeathMenu;
    public float TotalVolumeDB = -6f;
    public float DurationMusicFade = 2f;

    private PlayerInput playerInput;
    private MenuNavigation optionsMenuNavigation;

    // T-013: SoundMixerManager is not on this object - it arrives on the GlobalManager prefab - so it is null in
    // any scene that does not carry that prefab, and during the frames before its Awake has run. This class used
    // to dereference it four times, which turned a pause keypress into a NullReferenceException in exactly the
    // menu the player is trying to reach. See FadePauseVolume for what happens instead.
    private bool warnedMissingMixer;

    // T-056: one line per missing reference for the rest of the scene, not one per keypress - the same shape as
    // warnedMissingMixer, for the same reason: a path that cannot work runs again on every press of the button.
    private readonly HashSet<string> warnedReferences = new HashSet<string>(StringComparer.Ordinal);

    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
    }

    private void Start()
    {
        // T-056: say what is missing while the scene loads, instead of leaving it to be discovered as a dead
        // keypress minutes later. These five slots plus the PlayerInput component are everything this class
        // touches and nothing else in the project ever looked at them: Assets/Characters/Player.prefab ships all
        // five empty on purpose and every playable scene fills them in on its own Player instance, so a copied
        // scene, a deleted HUD canvas or the wrong prefab dropped into a slot was a silent, scene-wide break.
        // HasSlot IS the audit here (its return value is not what these five calls are for) and WarnMissing
        // prints each reference once, so a fully wired scene - all eight built ones are - still prints nothing.
        HasSlot("playerUI", playerUI);
        HasSlot("PauseMenu", PauseMenu);
        HasSlot("AblityMenu", AblityMenu);
        HasSlot("OptionsMenu", OptionsMenu);
        HasSlot("DeathMenu", DeathMenu);
        if (playerInput == null)
            WarnMissing("the PlayerInput component on this GameObject",
                "there is nothing to switch the action map on and the player keeps driving the game while a menu is open");

        if (PauseMenu)
        {
            PauseMenu menu = PauseMenu.GetComponent<PauseMenu>();
            // T-056: the guard here tested the SLOT and then dereferenced the COMPONENT, so a PauseMenu slot
            // pointing at a GameObject without the PauseMenu script threw on the first statement of Start() and
            // took the options-menu wiring below it down with it.
            if (menu) menu.player = gameObject;
            else WarnMissing("the PauseMenu script on \"" + PauseMenu.name + "\"",
                "the pause menu's own buttons have no player to save or to hand back");
        }

        if (OptionsMenu)
        {
            optionsMenuNavigation = OptionsMenu.GetComponent<MenuNavigation>();
            if (optionsMenuNavigation == null)
                WarnMissing("the MenuNavigation script on \"" + OptionsMenu.name + "\"",
                    "the back button inside the options menu does nothing");
        }
    }

    public void PressStartButton()
    {
        // T-056: the condition is the one this method already had - every panel this path touches must exist,
        // because opening a menu half way is worse than not opening it (HUD hidden with nothing to look at, game
        // paused in front of an empty screen). What it never had was a reason: it just returned, console clean.
        // Every slot is tested rather than short-circuited, so a scene missing three of them names all three.
        var ready = HasSlot("playerUI", playerUI);
        ready &= HasSlot("PauseMenu", PauseMenu);
        ready &= HasSlot("AblityMenu", AblityMenu);
        if (!ready) return;

        playerUI.SetActive(false);
        PauseMenu.SetActive(true);
        SwitchActionMap("UI");
        TimeEffectsManager.PauseGame();

        FadePauseVolume(TotalVolumeDB);
    }

    public void PressAbilityMenuButton()
    {
        var ready = HasSlot("playerUI", playerUI);
        ready &= HasSlot("PauseMenu", PauseMenu);
        ready &= HasSlot("AblityMenu", AblityMenu);
        ready &= HasSlot("OptionsMenu", OptionsMenu);
        if (!ready) return;
        if (PauseMenu.activeSelf) return;

        playerUI.SetActive(false);
        AblityMenu.SetActive(true);
        SwitchActionMap("UI");
        TimeEffectsManager.PauseGame();

        FadePauseVolume(TotalVolumeDB);

    }

    public void OnBackMenu()
    {
        // T-056: this was the one menu path that assumed all three panels exist. It read
        // AblityMenu.activeSelf / PauseMenu.activeSelf / OptionsMenu.activeSelf in turn with no null check while
        // both Press methods above bailed on those same slots, so in a scene with no options menu a plain Back
        // press with nothing open threw on OptionsMenu.activeSelf. Each branch now tests only what it touches,
        // which also stops one unassigned panel from taking the other two with it: closing is the path that has
        // to survive, the alternative being a game stuck paused behind a menu that is already closed.
        if (AblityMenu != null && AblityMenu.activeSelf)
        {
            RestoreFrom(AblityMenu);
        }
        else if (PauseMenu != null && PauseMenu.activeSelf)
        {
            RestoreFrom(PauseMenu);
        }
        else if (OptionsMenu != null && OptionsMenu.activeSelf)
        {
            if (optionsMenuNavigation == null) return;   // named by the audit in Start
            optionsMenuNavigation.GoBack();
        }
    }

    public void ShowDeathMenu()
    {
        // T-056: the only caller is the animation event in Assets/Animations/Other/PlayerDeath.anim, so the
        // throw this used to allow is invisible: nothing to press again, no stack trace anyone reads, just a
        // player who is dead with no death screen and - because the action map switch sat BELOW the throw - no
        // switch either. The two statements now stand on their own. The animation-event wiring itself is T-011.
        if (HasSlot("DeathMenu", DeathMenu)) DeathMenu.SetActive(true);
        SwitchActionMap("UI");
    }

    /// <summary>
    /// T-056: the two "back" branches were the same four statements written out twice, and those copies are where
    /// the unguarded dereferences lived. The order is the original one, so a fully wired scene behaves exactly as
    /// before; a missing reference now costs that one statement and its warning, never the resume - the game is
    /// never left paused behind a panel that was already closed.
    /// </summary>
    private void RestoreFrom(GameObject panel)
    {
        if (HasSlot("playerUI", playerUI)) playerUI.SetActive(true);
        panel.SetActive(false);
        SwitchActionMap("Player");
        TimeEffectsManager.ResumeGame();

        FadePauseVolume(0f);
    }

    /// <summary>
    /// T-056: the action map switch is the statement the player feels most and the one that cannot be recovered
    /// from at this level - with no PlayerInput there is nothing to switch, and a player with no PlayerInput is
    /// already broken. Skipping it lets the caller finish the statements that still work, where the old code threw
    /// and abandoned them: PressStartButton threw before pausing the game, ShowDeathMenu threw before handing the
    /// controls over. It still names the reference once, so this is no path in the class that fails in silence;
    /// the key matches the one the Start audit uses, so a scene missing the component is one line, not four.
    /// </summary>
    private void SwitchActionMap(string mapName)
    {
        if (playerInput == null)
        {
            WarnMissing("the PlayerInput component on this GameObject",
                "there is nothing to switch the action map on, leaving player and menu on the same controls");
            return;
        }
        playerInput.SwitchCurrentActionMap(mapName);
    }

    /// <summary>
    /// T-056: true when the slot is filled; false, once and by name, when it is not. These five slots are set on
    /// the Player instance of each scene (the prefab leaves them empty), so the usual way one goes missing is a
    /// copied scene or a deleted HUD canvas, and Unity reports neither of those.
    /// </summary>
    private bool HasSlot(string slot, GameObject value)
    {
        if (value != null) return true;
        WarnMissing("the " + slot + " slot on this component", "the menu path that needs it is skipped");
        return false;
    }

    /// <summary>
    /// T-056: the second family of bare dereferences in this file, found while T-013 removed the first. These are
    /// not managers a scene might not carry - they are this component's own five slots and its own GetComponent
    /// results, and no validator covered any of them, so a wrong reference was a NullReferenceException instead
    /// of a name. One line per reference per scene naming the reference, what it was needed for and where it is
    /// set: the loud-once version of Working Agreement 4 for a component that is queried once per keypress.
    /// </summary>
    private void WarnMissing(string reference, string impact)
    {
        if (!warnedReferences.Add(reference)) return;
        Debug.LogWarning("PlayerUIController on " + name + ": " + reference + " is not available, so " + impact
            + ". These are set per scene on the Player instance, not in Assets/Characters/Player.prefab.", this);
    }

    /// <summary>
    /// T-013: the one place this class touches the audio mixer. The fade is cosmetic, so a scene without
    /// SoundMixerManager skips the fade and the menu transition continues - the old direct call threw instead, and
    /// because it sat last in each method it threw after the panel was already swapped, leaving the game paused
    /// with the UI half changed. Warned once (see warnedMissingMixer) rather than once per keypress.
    /// </summary>
    private void FadePauseVolume(float targetDb)
    {
        SoundMixerManager mixer = SoundMixerManager.instance;
        if (mixer == null)
        {
            if (!warnedMissingMixer)
            {
                warnedMissingMixer = true;
                Debug.LogWarning("PlayerUIController: no SoundMixerManager in this scene (it arrives on the "
                    + "GlobalManager prefab), so the pause music fade is skipped. The menus still open and close.",
                    this);
            }
            return;
        }

        mixer.FadeVolumeToTarget(MixerNames.pausedVolume, targetDb, DurationMusicFade);
    }

}
