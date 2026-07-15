using System;
using UnityEngine;
using UnityEngine.Rendering;

public class ChangeMainMenuPC : MonoBehaviour
{
    [SerializeField] private RenderPipelineAsset menuPipeline;
    [SerializeField] private RenderPipelineAsset gamePipeline;

    private void Awake()
    {
        GraphicsSettings.defaultRenderPipeline = menuPipeline;
        QualitySettings.renderPipeline = menuPipeline;
    }

    private void OnDestroy()
    {
        GraphicsSettings.defaultRenderPipeline = gamePipeline;
        QualitySettings.renderPipeline = gamePipeline;
    }
}
