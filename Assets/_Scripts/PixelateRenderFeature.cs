using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class PixelateRenderFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public class PixelateSettings
    {
        // A ordem em que o efeito é aplicado. 'BeforeRenderingPostProcessing' é uma boa escolha.
        public RenderPassEvent renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;

        [Tooltip("O material que usa o nosso PixelateShader")]
        public Material material;

        [Tooltip("Valores maiores = mais pixelado")]
        [Range(1, 20)] public int pixelationAmount = 8;
    }

    public PixelateSettings settings;
    private PixelatePass _pixelatePass;

    // Cria o nosso "passo de renderização"
    public override void Create()
    {
        if (settings.material == null) {
            Debug.LogWarning("Material de Pixelate não foi atribuído na Render Feature.");
            return;
        }
        // Passa as configurações para o "cozinheiro"
        _pixelatePass = new PixelatePass(settings.renderPassEvent, settings.material, settings.pixelationAmount);
    }

    // Adiciona o nosso passo à fila de renderização do URP
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (_pixelatePass != null)
        {
            renderer.EnqueuePass(_pixelatePass);
        }
    }
}

// --- A Classe do Pass (o "cozinheiro") ---
public class PixelatePass : ScriptableRenderPass
{
    private Material _material;
    private int _pixelationAmount;
    private RenderTargetIdentifier _source;

    public PixelatePass(RenderPassEvent passEvent, Material material, int pixelation)
    {
        renderPassEvent = passEvent;
        _material = material;
        _pixelationAmount = pixelation;
    }

    // Antes de executar, o URP nos dá a referência da imagem da câmera
    [System.Obsolete]
    public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
    {
        _source = renderingData.cameraData.renderer.cameraColorTargetHandle;
    }

    [System.Obsolete]
    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        if (_material == null) return;

        CommandBuffer cmd = CommandBufferPool.Get("PixelatePass");
        var cameraData = renderingData.cameraData;

        int width = cameraData.camera.scaledPixelWidth / _pixelationAmount;
        int height = cameraData.camera.scaledPixelHeight / _pixelationAmount;

        // Cria um nome único para nossa textura temporária
        int tempTexId = Shader.PropertyToID("_TempPixelTex");
        cmd.GetTemporaryRT(tempTexId, width, height, 0, FilterMode.Point, RenderTextureFormat.Default);

        // Blit 1: Copia a imagem da tela (source) para a textura pequena (tempTexId)
        cmd.Blit(_source, tempTexId);
        // Blit 2: Copia a textura pequena de volta para a tela (source), aplicando o material (que não faz nada, mas é necessário)
        cmd.Blit(tempTexId, _source);

        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
        cmd.ReleaseTemporaryRT(tempTexId);
    }
}