using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BeauRoutine;

public static class PlayerInformation
{
	public static bool isNewGame;
	public static bool isMuted;
	public static string versionNum = "Version 1.1";
}

public class FadeOutTitleMusic : MonoBehaviour {

	public AudioSource source;
	public AudioClip stampIn;
	public AudioClip wooshOut;

	Routine m_routine;

	private void Start()
	{
		m_routine.Replace(this, FadeInRoutine());
	}
	public void FadeOut()
	{
		m_routine.Replace(this, FadeOutRoutine());
	}

	public IEnumerator FadeOutRoutine()
	{
		source.PlayOneShot(wooshOut);
		yield return 0.3f;
		yield return source.VolumeTo(0.2f, 0.4f);
	}

	public IEnumerator FadeInRoutine()
	{
		yield return source.VolumeTo(1, 2f);
	}
}
