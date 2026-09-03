using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine;

public class GameLoader : MonoBehaviour
{
    public static event Action<string> OnGameLoadFail;
    public void LoadSaveGame()
    {
        GameData gameData = SaveSystem.LoadGame();
        if (gameData == null)
        {
            Debug.LogWarning("Couldn't load save game");
            return;
        }

        bool gameDataLoaded = LoadQuestData(gameData.worldData);
        if (!gameDataLoaded)
        {
            OnGameLoadFail?.Invoke("Save loaded with inconsistent quest data.");
            return;
        }
        LevelLoader.Instance.LoadLevelAsync(gameData.worldData.sceneName, OnLoaded, false);

        void OnLoaded()
        {
            bool playerDataLoaded = LoadPlayerData(gameData.playerData);

            if (!playerDataLoaded)
            {
                OnGameLoadFail?.Invoke("Save loaded with inconsistent player data.");
                LevelLoader.Instance.LoadLevelAsync("MainMenu");
            }
        }
    }

    private bool LoadPlayerData(PlayerData playerData)
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            Debug.LogWarning("Couldn't find player in scene");
            return false;
        }

        CharacterController cc = player.GetComponent<CharacterController>();

        if (cc != null) cc.enabled = false;

        player.transform.position = new Vector3(
            playerData.position[0],
            playerData.position[1],
            playerData.position[2]);

        if (cc != null) cc.enabled = true;

        ApplyHealthEffect(player, playerData);

        AbilityHolder abilityHolder = player.GetComponent<AbilityHolder>();

        if (abilityHolder == null) return false;

        if (playerData.slot1Id != -1)
        {
            EquipAbility(abilityHolder, playerData.slot1Id, 1);
        }
        else
        {
            abilityHolder.UnequipAbility(1);
        }

        if (playerData.slot2Id != -1)
        {
            EquipAbility(abilityHolder, playerData.slot2Id, 2);
        }
        else
        {
            abilityHolder.UnequipAbility(2);
        }

        return true;
    }

    private void ApplyHealthEffect(GameObject player, PlayerData playerData)
    {
        AttributeSystem attributeSystem = player.GetComponent<AttributeSystem>();
        GameplayEffectManager effectManager = player.GetComponent<GameplayEffectManager>();

        GameplayEffect effect = ScriptableObject.CreateInstance<GameplayEffect>();
        effect.TargetAttribute = "PlayerHealth";
        effect.Value = playerData.health;
        effect.Modifier = Modifier.Override;
        effect.EffectTime = EffectTime.Infinite;
        effect.Duration = 0f;
        effect.CanAttributeResponse = false;

        if (attributeSystem != null && effectManager != null)
        {
            effectManager.ApplyEffect(effect, attributeSystem, false);
        }
        Destroy(effect);
    }

    private void EquipAbility(AbilityHolder abilityHolder, int Id, int slot)
    {
        List<GameplayAbility> abilities = abilityHolder.GetAbilityList();

        GameplayAbility ability = abilities.Find(x => x.Id == Id);

        if (ability != null)
        {
            abilityHolder.EquipAbility(ability, slot);
        }
    }

    private bool LoadQuestData(WorldData worldData)
    {
        if (QuestManager.instance.GetQuestNumber() != worldData.questId.Length)
        {
            Debug.LogWarning("Quest number has change so it will not load");
            return false;
        }

        if (!QuestManager.instance.GetIdOfQuests().SequenceEqual(worldData.questId))
        {
            Debug.LogWarning("Quest order has change so it will not load");
            return false;
        }
        QuestManager.instance.SetQuestIndex(worldData.questIndex);
        QuestManager.instance.LoadGoalData(worldData.currentAmount, worldData.requiredAmount, worldData.completed);

        return true;
    }
}
