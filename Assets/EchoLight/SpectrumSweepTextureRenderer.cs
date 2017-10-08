using UnityEngine;
using System.Collections;
using System.Text;

public class SpectrumSweepTextureRenderer : MonoBehaviour
{
	public FastFourierTransformFilter SourceFastFourierTransform = null;

	public int SpectrumSweepTextureWidth = 1024;

	public float SpectrumSaturationAmplitude = 10.0f;

	public RenderTexture OutputRenderTexture = null;

	public Shader SweepTransformationShader = null;

	public bool DebugEnabled = false;
	public GameObject DebugSpectrumDisplayQuad = null;

	public void Awake()
	{
		if (SourceFastFourierTransform == null)
		{
			SourceFastFourierTransform =
				transform.GetComponentInChildren<FastFourierTransformFilter>();
		}

		spectrumAmplitudesTexturePropertyID = Shader.PropertyToID("_SpectrumAmplitudesTex");
	}

	public void Start()
	{
		latestSpectrumAmplitudes = 
			new float[SourceFastFourierTransform.OutputSpectrumAmplitudesCount];

		if (OutputRenderTexture == null)
		{
			// Just a starter texture until we know what the resolution of the transform is.
			OutputRenderTexture =
				new RenderTexture(
					SpectrumSweepTextureWidth, 
					256, // height
					0, // depthBufferBits
					RenderTextureFormat.ARGB32);
		}
	}

	public void Update()
	{
		// Make sure the textures are of an appropriate format.
		{
			MatchOutputTextureToRequirements();

			MatchScatchCopyTextureToOutputTexture();
			MatchScatchSpectrumInputTextureToOutputTexture();
		}

		// Write the current spectrum to its scratch-texture.
		{
			SourceFastFourierTransform.GetCurrentSpectumAmplitudes(
				ref latestSpectrumAmplitudes);

			float spectrumAmplitudeScalar = (1.0f / SpectrumSaturationAmplitude);
			
			for (int index = 0; index < scratchSpectrumInputColors.Length; index++)
			{
				float spectrumAmplitude = 
					Mathf.Clamp01(latestSpectrumAmplitudes[index] * spectrumAmplitudeScalar);
				
				scratchSpectrumInputColors[index].r = spectrumAmplitude;
				scratchSpectrumInputColors[index].g = spectrumAmplitude;
				scratchSpectrumInputColors[index].b = spectrumAmplitude;
				scratchSpectrumInputColors[index].a = 1.0f;
			}

			scratchSpectrumInputTexture.SetPixels(
				0, // x
				0, // y
				scratchSpectrumInputColors.Length, // blockWidth
				1, // blockHeight
				scratchSpectrumInputColors);

			scratchSpectrumInputTexture.Apply();
		}

		// Render the updated results into the scratch-copy texture, and then copy back 
		// into the output texture so any consumers can use the same texture-reference.
		{
			if (sweepTransformationMaterial == null ||
				sweepTransformationMaterial.shader != SweepTransformationShader)
			{
				sweepTransformationMaterial = new Material(SweepTransformationShader);
			}

			if (sweepTransformationMaterial.GetTexture(spectrumAmplitudesTexturePropertyID) !=
				scratchSpectrumInputTexture)
			{
				sweepTransformationMaterial.SetTexture(
					spectrumAmplitudesTexturePropertyID,
					scratchSpectrumInputTexture);
			}

			Graphics.Blit(
				OutputRenderTexture,
				scratchCopyRenderTexture,
				sweepTransformationMaterial);
			
			Graphics.Blit(
				scratchCopyRenderTexture,
				OutputRenderTexture);
		}
	}

	private float[] latestSpectrumAmplitudes = null;
	
	private RenderTexture scratchCopyRenderTexture = null;

	private Texture2D scratchSpectrumInputTexture = null;
	private Color[] scratchSpectrumInputColors = null;

	private int spectrumAmplitudesTexturePropertyID = -1;
	
	private Material sweepTransformationMaterial = null;

	private void MatchOutputTextureToRequirements()
	{
		if (OutputRenderTexture.width != SpectrumSweepTextureWidth ||
			OutputRenderTexture.height != latestSpectrumAmplitudes.Length)
		{
			if (DebugEnabled)
			{
				Debug.LogFormat(
					"To represent the full spectrum-results, changing the output texture from ({0}, {1}) to ({2}, {3}).",
					OutputRenderTexture.width,
					OutputRenderTexture.height,
					SpectrumSweepTextureWidth,
					latestSpectrumAmplitudes.Length);
			}

			OutputRenderTexture.Release();
			OutputRenderTexture.width = SpectrumSweepTextureWidth;
			OutputRenderTexture.height = latestSpectrumAmplitudes.Length;
		}
	}

	private void MatchScatchCopyTextureToOutputTexture()
	{
		if (scratchCopyRenderTexture == null)
		{
			if (DebugEnabled)
			{
				Debug.Log("Creating scratch-copy texture to match output texture.");
			}

			scratchCopyRenderTexture = 
				new RenderTexture(
					OutputRenderTexture.width, 
					OutputRenderTexture.height,
					OutputRenderTexture.depth,
					OutputRenderTexture.format);
		}
		else if (
			scratchCopyRenderTexture.width != OutputRenderTexture.width ||
			scratchCopyRenderTexture.height != OutputRenderTexture.height ||
			scratchCopyRenderTexture.depth != OutputRenderTexture.depth ||
			scratchCopyRenderTexture.format != OutputRenderTexture.format)
		{
			if (DebugEnabled)
			{
				Debug.Log("Updating scratch-copy texture to match output texture.");
			}
			
			scratchCopyRenderTexture.Release();
			scratchCopyRenderTexture.width = OutputRenderTexture.width;
			scratchCopyRenderTexture.height = OutputRenderTexture.height;
			scratchCopyRenderTexture.depth = OutputRenderTexture.depth;
			scratchCopyRenderTexture.format = OutputRenderTexture.format;
		}
	}
	
	private void MatchScatchSpectrumInputTextureToOutputTexture()
	{
		// The spectrum-input is horizontal so we can write to it efficiently on the CPU,
		// and the rotation-to-vertical will take place on the GPU where we don't care.
		int desiredScratchSpectrumInputWidth = OutputRenderTexture.height;
		int desiredScratchSpectrumInputHeight = 1;
		
		if (scratchSpectrumInputTexture == null)
		{
			if (DebugEnabled)
			{
				Debug.Log("Creating scratch-spectrum-input texture to match output texture.");
			}

			scratchSpectrumInputTexture = 
				new Texture2D(
					desiredScratchSpectrumInputWidth, 
					desiredScratchSpectrumInputHeight,
					TextureFormat.ARGB32,
					false); // mipmap

			scratchSpectrumInputColors = new Color[desiredScratchSpectrumInputWidth];
		}
		else if (
			scratchSpectrumInputTexture.width != desiredScratchSpectrumInputWidth ||
			scratchSpectrumInputTexture.height != desiredScratchSpectrumInputHeight)
		{
			if (DebugEnabled)
			{
				Debug.Log("Updating scratch-spectrum-input texture to match output texture.");
			}
			
			scratchSpectrumInputTexture.width = desiredScratchSpectrumInputWidth;
			scratchSpectrumInputTexture.height = desiredScratchSpectrumInputHeight;
			
			scratchSpectrumInputColors = new Color[desiredScratchSpectrumInputWidth];
		}
	}
}

