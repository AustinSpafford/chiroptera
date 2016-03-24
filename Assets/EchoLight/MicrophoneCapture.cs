using UnityEngine;
using System.Collections;
using System.Text;

public class MicrophoneCapture : MonoBehaviour
{
	public string MicrophoneDeviceName = null;

	public void Start()
	{
		audioSource = GetComponent<AudioSource>();

		ConnectMicrophoneToAudioSource();
	}

	public void Update()
	{
		// If the microphone finally captured its first set of samples, then begin playback.
		// Otherwise, if we had attempted to immediately play the audio source, we'd wind up
		// perceiving a very long delay, specifically equal to the ring buffer's entire length.
		if (!audioSource.isPlaying &&
			(Microphone.GetPosition(MicrophoneDeviceName) > 0))
		{
			audioSource.Play();
		}
	}
	
	private AudioSource audioSource = null;

	private void ConnectMicrophoneToAudioSource()
	{
		string[] existingDeviceNames = Microphone.devices;

		MicrophoneDeviceName = (
			(existingDeviceNames.Length > 0) ? 
				existingDeviceNames[0] :
				null);	
		
		if (MicrophoneDeviceName != null)
		{
			audioSource.clip =
				Microphone.Start(
					MicrophoneDeviceName,
					true, // loop
					100, // lengthSec
					AudioSettings.outputSampleRate);

			// We must loop in order to continuously record.
			audioSource.loop = true;
			
			// NOTE: We'll begin playback once the microphone captures its initial samples.
		}
	}
}

