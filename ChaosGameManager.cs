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
		public BasePlugin.ChaosMode modeUsed = BasePlugin.ChaosMode.MazeChaos;

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
