using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

public class OutlineFeature : ScriptableRendererFeature
{
    // --- PASS 1: RENDER SELECTION MASK ---
    class SelectionMaskPass : ScriptableRenderPass
    {
        private Material maskMaterial;
        private LayerMask layerMask;
        private FilteringSettings filteringSettings;
        private readonly ShaderTagId[] shaderTags;
        private const string MaskShaderName = "Hidden/Outline/SelectionMask";

        public TextureHandle MaskTexture { get; private set; }

        public SelectionMaskPass()
        {
            renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
            shaderTags = new ShaderTagId[]
            {
                new ShaderTagId("UniversalForward"),
                new ShaderTagId("UniversalForwardOnly"),
                new ShaderTagId("LightweightForward"),
                new ShaderTagId("SRPDefaultUnlit")
            };
            filteringSettings = new FilteringSettings(RenderQueueRange.all);
        }

        public void Setup(LayerMask mask)
        {
            this.layerMask = mask;
            filteringSettings.layerMask = mask;
            if (maskMaterial == null) maskMaterial = CoreUtils.CreateEngineMaterial(Shader.Find(MaskShaderName));
        }

        private class MaskData
        {
            public RendererListHandle rendererList;
            public TextureHandle maskDest;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            MaskTexture = TextureHandle.nullHandle; // Reset mỗi frame

            if (maskMaterial == null || layerMask == 0) return;

            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            UniversalRenderingData renderingData = frameData.Get<UniversalRenderingData>();

            // 1. Tạo Texture Mask an toàn
            RenderTextureDescriptor desc = cameraData.cameraTargetDescriptor;
            desc.colorFormat = RenderTextureFormat.R8;
            desc.depthBufferBits = 0;
            desc.msaaSamples = 1;

            // FIX: Tạo TextureDesc bằng constructor
            TextureDesc texDesc = new TextureDesc(desc);
            texDesc.name = "_SelectionMaskTexture";
            texDesc.clearBuffer = true;
            texDesc.clearColor = Color.black;

            MaskTexture = renderGraph.CreateTexture(texDesc);

            // FIX CRITICAL: Kiểm tra Depth Texture có tồn tại không trước khi dùng
            TextureHandle depthTexture = resourceData.activeDepthTexture;
            bool useDepth = depthTexture.IsValid();

            // 2. Tạo RendererList
            RendererListParams rlParams = new RendererListParams(
                renderingData.cullResults,
                new DrawingSettings(shaderTags[0], new SortingSettings(cameraData.camera))
                {
                    overrideMaterial = maskMaterial,
                    overrideMaterialPassIndex = 0
                },
                filteringSettings
            );

            for (int i = 1; i < shaderTags.Length; ++i)
                rlParams.drawSettings.SetShaderPassName(i, shaderTags[i]);

            RendererListHandle rendererList = renderGraph.CreateRendererList(rlParams);

            using (var builder = renderGraph.AddRasterRenderPass<MaskData>("Outline Selection Mask", out var passData))
            {
                passData.rendererList = rendererList;
                passData.maskDest = MaskTexture;

                builder.UseRendererList(passData.rendererList);
                builder.SetRenderAttachment(passData.maskDest, 0, AccessFlags.Write);

                // FIX CRITICAL: Chỉ gắn Depth nếu nó hợp lệ. Nếu không hệ thống sẽ Crash.
                if (useDepth)
                {
                    builder.SetRenderAttachmentDepth(depthTexture, AccessFlags.Read);
                }

                builder.SetRenderFunc((MaskData data, RasterGraphContext context) =>
                {
                    context.cmd.DrawRendererList(data.rendererList);
                });
            }
        }

        public void Dispose() { CoreUtils.Destroy(maskMaterial); }
    }

    // --- PASS 2: FULL SCREEN OUTLINE COMPOSITION ---
    class OutlinePass : ScriptableRenderPass
    {
        private Material material;
        private OutlineVolume volumeSettings;
        private const string ShaderName = "Hidden/FullScreen/Outline";

        private static readonly int ThicknessID = Shader.PropertyToID("_Thickness");
        private static readonly int ColorID = Shader.PropertyToID("_OutlineColor");
        private static readonly int DepthThresholdID = Shader.PropertyToID("_DepthThreshold");
        private static readonly int NormalThresholdID = Shader.PropertyToID("_NormalThreshold");
        private static readonly int ColorThresholdID = Shader.PropertyToID("_ColorThreshold");
        private static readonly int DebugModeID = Shader.PropertyToID("_DebugMode");
        private static readonly int SelectionMaskID = Shader.PropertyToID("_SelectionMaskTexture");

        private SelectionMaskPass selectionMaskPass;

        private class PassData
        {
            public Material material;
            public TextureHandle source;
            public TextureHandle destination;
            public TextureHandle mask;
        }

        public OutlinePass() { renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing; }

        public void SetupReference(SelectionMaskPass maskPass) { this.selectionMaskPass = maskPass; }

        private bool UpdateMaterial()
        {
            var stack = VolumeManager.instance.stack;
            volumeSettings = stack.GetComponent<OutlineVolume>();
            if (volumeSettings == null || !volumeSettings.IsActive()) return false;
            if (material == null) material = CoreUtils.CreateEngineMaterial(Shader.Find(ShaderName));
            if (material == null) return false;

            material.SetFloat(ThicknessID, volumeSettings.thickness.value);
            material.SetColor(ColorID, volumeSettings.outlineColor.value);
            material.SetFloat(DepthThresholdID, volumeSettings.depthThreshold.value);
            material.SetFloat(NormalThresholdID, volumeSettings.normalThreshold.value);
            material.SetFloat(ColorThresholdID, volumeSettings.colorThreshold.value);
            material.SetInt(DebugModeID, (int)volumeSettings.debugMode.value);

            SetKeyword("USE_DEPTH", volumeSettings.useDepth.value);
            SetKeyword("USE_NORMALS", volumeSettings.useNormals.value);
            SetKeyword("USE_COLOR", volumeSettings.useColor.value);
            SetKeyword("ALGO_SOBEL", volumeSettings.algorithm.value == OutlineVolume.OutlineAlgorithm.Sobel);
            SetKeyword("ALGO_ROBERTS", volumeSettings.algorithm.value == OutlineVolume.OutlineAlgorithm.RobertsCross);

            var mode = volumeSettings.mode.value;
            SetKeyword("OUTLINE_FULL", mode == OutlineVolume.OutlineMode.FullScreen);
            SetKeyword("OUTLINE_SELECTION", mode == OutlineVolume.OutlineMode.SelectionOnly);
            SetKeyword("OUTLINE_MIXED", mode == OutlineVolume.OutlineMode.Mixed);

            return true;
        }

        private void SetKeyword(string k, bool v) { if (v) material.EnableKeyword(k); else material.DisableKeyword(k); }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            if (!UpdateMaterial()) return;

            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            if (cameraData.cameraType == CameraType.Preview) return;

            TextureHandle source = resourceData.activeColorTexture;

            // Tạo Temp Texture
            RenderTextureDescriptor desc = cameraData.cameraTargetDescriptor;
            desc.depthBufferBits = 0;

            // FIX: Constructor TextureDesc
            TextureDesc texDesc = new TextureDesc(desc);
            texDesc.name = "OutlineTemp";
            texDesc.clearBuffer = true;
            texDesc.clearColor = Color.black;

            TextureHandle tempTexture = renderGraph.CreateTexture(texDesc);

            TextureHandle maskHandle = (selectionMaskPass != null) ? selectionMaskPass.MaskTexture : TextureHandle.nullHandle;

            using (var builder = renderGraph.AddRasterRenderPass<PassData>("Outline Composite", out var passData))
            {
                passData.material = material;
                passData.source = source;
                passData.destination = tempTexture;
                passData.mask = maskHandle;

                builder.UseTexture(passData.source, AccessFlags.Read);
                if (passData.mask.IsValid()) builder.UseTexture(passData.mask, AccessFlags.Read);

                builder.SetRenderAttachment(passData.destination, 0, AccessFlags.Write);

                builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                {
                    if (data.mask.IsValid()) data.material.SetTexture(SelectionMaskID, data.mask);
                    Blitter.BlitTexture(context.cmd, data.source, new Vector4(1, 1, 0, 0), data.material, 0);
                });
            }

            using (var builder = renderGraph.AddRasterRenderPass<PassData>("Outline Copy Back", out var passData))
            {
                passData.source = tempTexture;
                passData.destination = source;
                builder.UseTexture(passData.source, AccessFlags.Read);
                builder.SetRenderAttachment(passData.destination, 0, AccessFlags.Write);
                builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                {
                    Blitter.BlitTexture(context.cmd, data.source, new Vector4(1, 1, 0, 0), 0, false);
                });
            }
        }
        public void Dispose() { CoreUtils.Destroy(material); }
    }

    private SelectionMaskPass maskPass;
    private OutlinePass outlinePass;

    public override void Create()
    {
        maskPass = new SelectionMaskPass();
        outlinePass = new OutlinePass();
        outlinePass.ConfigureInput(ScriptableRenderPassInput.Color | ScriptableRenderPassInput.Depth | ScriptableRenderPassInput.Normal);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        var stack = VolumeManager.instance.stack;
        var settings = stack.GetComponent<OutlineVolume>();

        if (settings != null && settings.IsActive())
        {
            outlinePass.SetupReference(maskPass);
            if (settings.selectionLayer.value != 0)
            {
                maskPass.Setup(settings.selectionLayer.value);
                renderer.EnqueuePass(maskPass);
            }
            renderer.EnqueuePass(outlinePass);
        }
    }

    protected override void Dispose(bool disposing)
    {
        maskPass.Dispose();
        outlinePass.Dispose();
    }
}