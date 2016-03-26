using UnityEngine;
using System.Collections;

// General credit for the structure of this filter goes to the fine folks 
// in this thread: http://forum.unity3d.com/threads/fft-how-to.253192/

public class FastFourierTransformFilter : MonoBehaviour
{
	public int SourceChannelIndex = 0;

	public bool DebugEnabled = false;
	
	public int OutputSpectrumAmplitudesCount
	{
		get
		{
			return ((outputSpectrumAmplitudes != null) ? outputSpectrumAmplitudes.Length : 0);
		}
	}

	public void Awake()
	{
		int dspBufferLength;
		int dummyDPSBufferCount;
		AudioSettings.GetDSPBufferSize(
			out dspBufferLength,
			out dummyDPSBufferCount);

		if (DebugEnabled)
		{
			Debug.LogFormat(
				"DSP Buffer Length is {0} samples, thus {1} spectrum-amplitudes will be available.", 
				dspBufferLength,
				(dspBufferLength / 2));
		}

		scratchSamplesRealComponent = new float[dspBufferLength];
		scratchSamplesImaginaryComponent = new float[dspBufferLength];
		
		cachedSamplingWindowScalars =
			BuildBlackmanHarrisSamplingWindowScalars(dspBufferLength);
		
		// Given N samples, only N/2 frequencies can be determined.
		// https://en.wikipedia.org/wiki/Nyquist_frequency
		outputSpectrumAmplitudes = new float[dspBufferLength / 2];

		fastFourierTransform = new Beauregard.FastFourierTransform();
		fastFourierTransform.Init((uint)Mathf.Log(dspBufferLength, 2));
	}
	
	// NOTE: This function is executed on the audio-thread.
	public void OnAudioFilterRead(
		float[] inoutAudioSamples,
		int channelCount)
	{
		if (fastFourierTransform == null)
		{
			throw new System.InvalidOperationException();
		}

		// Copy our assigned audio-channel out of the interleaved audio samples.
		for (int sourceIndex = SourceChannelIndex, destIndex = 0;
			sourceIndex < inoutAudioSamples.Length;
			/*internal increment*/)
		{
			scratchSamplesRealComponent[destIndex] = inoutAudioSamples[sourceIndex];

			sourceIndex += channelCount;
			destIndex++;
		}

		// Apply the windowing function.
		for (int index = 0; index < scratchSamplesRealComponent.Length; index++)
		{
			scratchSamplesRealComponent[index] *= cachedSamplingWindowScalars[index];
		}

		System.Array.Clear(
			scratchSamplesImaginaryComponent,
			0, // startIndex
			scratchSamplesImaginaryComponent.Length);

		fastFourierTransform.Run(
			scratchSamplesRealComponent,
			scratchSamplesImaginaryComponent,
			false); // inverse

		// Compute the magnitude of the transform to determine the spectrum-aplitudes.
		for (int index = 0; index < scratchSamplesRealComponent.Length; index++)
		{
			scratchSamplesRealComponent[index] = 
				Mathf.Sqrt(
					(scratchSamplesRealComponent[index] * scratchSamplesRealComponent[index]) +
					(scratchSamplesImaginaryComponent[index] * scratchSamplesImaginaryComponent[index]));
		}

		// Move the results into the output-buffer.
		lock (outputSpectrumAmplitudes)
		{
			System.Array.Copy(
				scratchSamplesRealComponent,
				outputSpectrumAmplitudes,
				outputSpectrumAmplitudes.Length);
		}
	}

	public void GetCurrentSpectumAmplitudes(
		ref float[] outSpectrumAmplitudes)
	{
		if (outSpectrumAmplitudes.Length != OutputSpectrumAmplitudesCount)
		{
			throw new System.InvalidOperationException();
		}

		lock (outputSpectrumAmplitudes)
		{
			System.Array.Copy(
				outputSpectrumAmplitudes,
				outSpectrumAmplitudes,
				outSpectrumAmplitudes.Length);
		}
	}

	private Beauregard.FastFourierTransform fastFourierTransform = null;

	// For performance reasons, many in-place operations are performed in 
	// these arrays during each update, hence the vague naming.
	private float[] scratchSamplesRealComponent = null;
	private float[] scratchSamplesImaginaryComponent = null;

	private float[] outputSpectrumAmplitudes = null;

	private float[] cachedSamplingWindowScalars = null;

	private static float[] BuildBlackmanHarrisSamplingWindowScalars(
		int sampleCount)
	{
		// From: https://en.wikipedia.org/wiki/Window_function#Blackman.E2.80.93Harris_window

		// These coefficients adjust the shape of the cosine-based window, in 
		// this case to specifically form a Blackman-Harris window.
		float coefficient_a0 = 0.35875f;
		float coefficient_a1 = -0.48829f;
		float coefficient_a2 = 0.14128f;
		float coefficient_a3 = -0.01168f;

		float[] result = new float[sampleCount];

		for (int index = 0; index < sampleCount; index++)
		{
			float base_cosine_input =
				((2 * Mathf.PI * index) / (sampleCount - 1));

			result[index] = (
				coefficient_a0 +
				(coefficient_a1 * Mathf.Cos(base_cosine_input)) +
				(coefficient_a2 * Mathf.Cos(2 * base_cosine_input)) +
				(coefficient_a3 * Mathf.Cos(3 * base_cosine_input)));
		}

		return result;
	}
}

