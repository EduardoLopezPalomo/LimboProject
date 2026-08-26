using UnityEngine;

public class GameLoader : MonoBehaviour
{
    public void LoadSaveGame()
    {
        GameData gameData = SaveSystem.LoadGame();
        if (gameData == null)
        {
            Debug.LogWarning("Couldn't load save game");
            return;
        }
        
        LevelLoader.Instance.LoadLevelAsync(gameData.worldData.sceneIndex, OnLoaded);

        void OnLoaded()
        {
            LoadPlayerData(gameData.playerData);    
        }
    }

    private void LoadPlayerData(PlayerData playerData)
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            Debug.LogWarning("Couldn't find player in scene");
            return; 
        }
        
        CharacterController cc = player.GetComponent<CharacterController>();
        
        if (cc != null) cc.enabled = false;
        
        player.transform.position = new Vector3(
            playerData.position[0],
            playerData.position[1],
            playerData.position[2]);
        
        if (cc != null) cc.enabled = true;
        
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
            effectManager.ApplyEffect(effect, attributeSystem);
        }
        Destroy(effect);
    }
}
