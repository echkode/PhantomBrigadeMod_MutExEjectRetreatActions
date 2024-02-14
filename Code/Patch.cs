// Copyright (c) 2024 EchKode
// SPDX-License-Identifier: BSD-3-Clause

using System.Collections.Generic;

using Entitas;

using HarmonyLib;

using PhantomBrigade;
using PhantomBrigade.Combat.Systems;

using UnityEngine;

namespace EchKode.PBMods.MutExEjectRetreatActions
{
	[HarmonyPatch]
	public static class Patch
	{
		internal static System.Action<int> ProcessPathForUnit;

		[HarmonyPatch(typeof(Heartbeat), "Start")]
		[HarmonyPrefix]
		static void Hb_StartPrefix()
		{
			// Dig out the method from PathLinker that processes the movement paths in actions.

			try
			{
				var fi = AccessTools.DeclaredField(typeof(Heartbeat), "_gameController");
				if (fi == null)
				{
					if (ModLink.Settings.logDiagnostics)
					{
						Debug.LogFormat(
							"Mod {0} ({1}) Heartbeat.Start -- no _gameController field on Heartbeat",
							ModLink.modIndex,
							ModLink.modID);
					}
					return;
				}

				var gameController = (GameController)fi.GetValue(null);
				var gcs = gameController.m_stateDict[GameStates.combat];
				Systems combatSystems = null;
				foreach (var feature in gcs.m_systems)
				{
					if (feature is CombatSystems cs)
					{
						combatSystems = cs;
						break;
					}
				}
				if (combatSystems == null)
				{
					if (ModLink.Settings.logDiagnostics)
					{
						Debug.LogFormat(
							"Mod {0} ({1}) Heartbeat.Start -- no CombatSystems in game state combat",
							ModLink.modIndex,
							ModLink.modID);
					}
					return;
				}
				fi = AccessTools.Field(combatSystems.GetType(), "_executeSystems");
				if (fi == null)
				{
					if (ModLink.Settings.logDiagnostics)
					{
						Debug.LogFormat(
							"Mod {0} ({1}) Heartbeat.Start -- no _executeSystems field in CombatSystems",
							ModLink.modIndex,
							ModLink.modID);
					}
					return;
				}

				var systems = (List<IExecuteSystem>)fi.GetValue(combatSystems);
				if (systems == null)
				{
					if (ModLink.Settings.logDiagnostics)
					{
						Debug.LogFormat(
							"Mod {0} ({1}) Heartbeat.Start -- no systems list from CombatSystems",
							ModLink.modIndex,
							ModLink.modID);
					}
					return;
				}
				foreach (var system in systems)
				{
					if (system is PathLinker pl)
					{
						var mi = AccessTools.DeclaredMethod(pl.GetType(), "ProcessPathForUnit");
						if (mi == null)
						{
							if (ModLink.Settings.logDiagnostics)
							{
								Debug.LogFormat(
									"Mod {0} ({1}) Heartbeat.Start -- can't find method ProcessPathForUnit",
									ModLink.modIndex,
									ModLink.modID);
							}
							return;
						}

						ProcessPathForUnit = (System.Action<int>)mi.CreateDelegate(typeof(System.Action<int>), pl);

						if (ModLink.Settings.logDiagnostics)
						{
							Debug.LogFormat(
								"Mod {0} ({1}) Heartbeat.Start -- created delegate for ProcessPathForUnit",
								ModLink.modIndex,
								ModLink.modID);
						}

						return;
					}
				}

				if (ModLink.Settings.logDiagnostics)
				{
					Debug.LogFormat(
						"Mod {0} ({1}) Heartbeat.Start -- didn't find PathLinker system",
						ModLink.modIndex,
						ModLink.modID);
				}
			}
			catch (System.Exception ex)
			{
				Debug.LogErrorFormat(
					"Mod {0} ({1}) Heartbeat.Start -- exception during reflection",
					ModLink.modIndex,
					ModLink.modID);
				Debug.LogException(ex);
			}
		}
	}
}
