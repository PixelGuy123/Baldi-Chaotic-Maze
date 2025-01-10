using BBSchoolMaze.Plugin;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using System;
using UnityEngine;

namespace BBSchoolMaze
{
	internal class ChaosGameManager : MainGameManager
	{
		[SerializeField]
		public int modeUsed = 0;

		[SerializeField]
		public ChallengeWin winScreen;

		public override void LoadNextLevel()
		{
			AudioListener.pause = true;
			Time.timeScale = 0f;
			Singleton<CoreGameManager>.Instance.disablePause = true;

			winScreen.gameObject.SetActive(true);
		}
	}
}
