using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;
using UnityEngine;

public class Define
{
	public enum SceneNum
	{
		Consent,
		Awake,
		Main,
		ARCamera,
	}

	public enum UIEvent
	{
		Click,
		Pressed,
		PointerDown,
		PointerUp,
	}

	public enum SceneType
	{
		Unknown,
		Main,
		Navigation,
		ARZone,
		Street,
		ARNavigation,
	}

	public enum Sound
	{
		Bgm,
		Effect,
		Max,
	}

	public enum MonsterType
	{
		Normal,
		Hollow,
		Special,
	}
}
