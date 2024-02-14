// Copyright (c) 2024 EchKode
// SPDX-License-Identifier: BSD-3-Clause

using System.Collections.Generic;
using System.Reflection.Emit;
using System.Text;

using Entitas;

using HarmonyLib;

using PhantomBrigade;
using PhantomBrigade.Data;
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

		[HarmonyPatch(typeof(ActionPlaybackSystem), "CleanActionsList")]
		[HarmonyTranspiler]
		static IEnumerable<CodeInstruction> Aps_CleanActionsListTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
		{
			var cm = new CodeMatcher(instructions, generator);
			var branchMatch = new CodeMatch(OpCodes.Br);
			var storeMatch = new CodeMatch(OpCodes.Stloc_3);
			var loadSimulationTime = new CodeInstruction(OpCodes.Ldloc_0);
			var loadAction = new CodeInstruction(OpCodes.Ldloc_3);
			var log = CodeInstruction.Call(typeof(Patch), nameof(LogAction));

			cm.MatchEndForward(branchMatch)
				.MatchEndForward(storeMatch)
				.Advance(1)
				.InsertAndAdvance(loadSimulationTime)
				.InsertAndAdvance(loadAction)
				.InsertAndAdvance(log);

			return cm.InstructionEnumeration();
		}

		[HarmonyPatch(typeof(ScenarioUtility), nameof(ScenarioUtility.IsRetreatAvailableAtPosition))]
		[HarmonyTranspiler]
		static IEnumerable<CodeInstruction> Civca_RefreshTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
		{
			var cm = new CodeMatcher(instructions, generator);
			var getLocationMethodInfo = AccessTools.DeclaredMethod(typeof(DataContainerCombatArea), nameof(DataContainerCombatArea.GetLocation));
			var rectFieldInfo = AccessTools.DeclaredField(typeof(DataBlockAreaLocation), nameof(DataBlockAreaLocation.rect));
			var getLocationMatch = new CodeMatch(OpCodes.Callvirt, getLocationMethodInfo);
			var rectMatch = new CodeMatch(OpCodes.Ldfld, rectFieldInfo);
			var subMatch = new CodeMatch(OpCodes.Sub);
			var storeMatch = new CodeMatch(OpCodes.Stloc_S);
			var loadPosition = new CodeInstruction(OpCodes.Ldarg_0);
			var log = CodeInstruction.Call(typeof(Patch), nameof(LogRetreatZone));

			cm.MatchEndForward(getLocationMatch)
				.MatchEndForward(rectMatch)
				.MatchEndForward(subMatch)
				.Advance(1);
			var loadLeft = new CodeInstruction(OpCodes.Ldloc_S, cm.Operand);

			cm.Advance(1)
				.MatchStartForward(storeMatch);
			var loadRight = new CodeInstruction(OpCodes.Ldloc_S, cm.Operand);

			cm.Advance(1)
				.MatchStartForward(storeMatch);
			var loadBottom = new CodeInstruction(OpCodes.Ldloc_S, cm.Operand);

			cm.Advance(1)
				.MatchStartForward(storeMatch);
			var loadTop = new CodeInstruction(OpCodes.Ldloc_S, cm.Operand);

			cm.Advance(1)
				.InsertAndAdvance(loadPosition)
				.InsertAndAdvance(loadLeft)
				.InsertAndAdvance(loadBottom)
				.InsertAndAdvance(loadRight)
				.InsertAndAdvance(loadTop)
				.InsertAndAdvance(log);

			return cm.InstructionEnumeration();
		}

		public static void LogAction(float time, ActionEntity action)
		{
			if (!ModLink.Settings.logDiagnostics)
			{
				return;
			}
			if (action.dataKeyAction.s != "eject" && action.dataKeyAction.s != "retreat")
			{
				return;
			}

			sb.Clear();
			sb.AppendFormat("Mod {0} ({1}) playback | time: {2:F3}s", ModLink.modIndex, ModLink.modID, time);
			sb.AppendFormat(" | action: A-{0} {1}", action.id.id, action.dataKeyAction.s);
			if (action.hasActionOwner)
			{
				sb.AppendFormat(" | owner: C-{0}", action.actionOwner.combatID);
			}
			sb.AppendFormat("\n  disposed: {0} | completed: {1} | valid: {2}", action.isDisposed, action.CompletedAction, DataHelperAction.IsValid(action));
			sb.AppendFormat("\n  started: {0} | ended: {1}", action.isStarted, action.isEnded);
			sb.AppendFormat("\n  start: {0:F3}s | duration: {1:F3}s", action.startTime.f, action.duration.f);
			Debug.Log(sb.ToString());
		}

		public static void LogRetreatZone(
			Vector3 position,
			float left,
			float bottom,
			float right,
			float top)
		{
			if (!ModLink.Settings.logDiagnostics)
			{
				return;
			}

			sb.Clear();
			sb.AppendFormat("Mod {0} ({1}) retreat zone | position: {2}", ModLink.modIndex, ModLink.modID, position);
			sb.AppendFormat(" | zone: ({0:F1}, {1:F1})x({2:F1}, {3:F1})", left, bottom, right, top);
			Debug.Log(sb.ToString());
		}

		static readonly StringBuilder sb = new StringBuilder();
	}
}
