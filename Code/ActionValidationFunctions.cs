// Copyright (c) 2024 EchKode
// SPDX-License-Identifier: BSD-3-Clause

using PhantomBrigade;
using PhantomBrigade.Functions;
using PhantomBrigade.Input.Components;

using UnityEngine;

namespace EchKode.PBMods.MutExEjectRetreatActions
{
	public static class ActionValidationFunctions
	{
		public class InRetreatZone : ICombatActionValidationFunction
		{
			public bool IsValid(CombatEntity combatant) => IsSelectedUnitAndPlayerControlled(combatant)
				? IsInRetreatZone(combatant)
				: true;
		}

		public class NotInRetreatZone : ICombatActionValidationFunction
		{
			public bool IsValid(CombatEntity combatant) => IsSelectedUnitAndPlayerControlled(combatant)
				? !IsInRetreatZone(combatant)
				: true;
		}

		static bool IsSelectedUnitAndPlayerControlled(CombatEntity combatant)
		{
			if (!Contexts.sharedInstance.combat.hasUnitSelected)
			{
				return false;
			}
			if (Contexts.sharedInstance.combat.unitSelected.id != combatant.id.id)
			{
				return false;
			}
			if (!combatant.isPlayerControllable)
			{
				return false;
			}
			if (Contexts.sharedInstance.input.combatUIMode.e != CombatUIModes.Unit_Selection)
			{
				return false;
			}
			return true;
		}

		static bool IsInRetreatZone(CombatEntity combatant)
		{
			if (ModLink.Settings.logDiagnostics)
			{
				Debug.LogFormat(
					"Mod {0} ({1}) retreat zone | frame: {2} | combatant: {3}"
					+ "\n  combat mode: {4} | tags: {5}",
					ModLink.modIndex,
					ModLink.modID,
					Time.frameCount,
					combatant.ToLog(),
					Contexts.sharedInstance.input.combatUIMode.e,
					combatant.hasUnitScenarioTags
						? combatant.unitScenarioTags.tags.ToStringFormatted()
						: "null");
			}

			var startPosition = combatant.position.v;
			var startsInZone = HasRetreatZoneTag(combatant)
				|| ScenarioUtility.IsRetreatAvailableAtPosition(startPosition);

			var (moved, changed) = HasMovement(combatant);
			if (!moved)
			{
				if (ModLink.Settings.logDiagnostics)
				{
					Debug.LogFormat(
						"Mod {0} ({1}) retreat zone -- no movement | frame: {2} | combatant: {3} | in zone: {4}",
						ModLink.modIndex,
						ModLink.modID,
						Time.frameCount,
						combatant.ToLog(),
						startsInZone);
				}
				return startsInZone;
			}
			if (changed && Patch.ProcessPathForUnit == null)
			{
				// PathUtility.GetProcessedPathDataAtTime() will return the correct final position of
				// a path when the painted path is still around. However, InputUILinkModeSync removes
				// the painted path and then GetProcessedPathDataAtTime() will return an incorrect
				// position until the path is processed by PathLinker.
				//
				// Don't waste cycles on an incorrect path.

				if (ModLink.Settings.logDiagnostics)
				{
					Debug.LogFormat(
						"Mod {0} ({1}) retreat zone -- moved but no function to process path | frame: {2} | combatant: {3} | in zone: {4}",
						ModLink.modIndex,
						ModLink.modID,
						Time.frameCount,
						combatant.ToLog(),
						startsInZone);
				}

				return startsInZone;
			}

			var time = ActionUtility.GetLastActionTime(combatant, true);
			var pathProcessed = false;
			if (changed)
			{
				Patch.ProcessPathForUnit(combatant.id.id);
				MovementProcessed(combatant);
				pathProcessed = true;
			}
			PathUtility.GetProcessedPathDataAtTime(
				combatant,
				time,
				out var position,
				out var _,
				out var _,
				out var _,
				out var _);

			if (ModLink.Settings.logDiagnostics)
			{
				Debug.LogFormat(
					"Mod {0} ({1}) retreat zone | frame: {2} | combatant: {3}"
					+ "\n  time: {4:F3}s | processed: {5} | start: {6} | end: {7}",
					ModLink.modIndex,
					ModLink.modID,
					Time.frameCount,
					combatant.ToLog(),
					time,
					pathProcessed,
					startPosition,
					position);
			}

			var distance = Vector3.Distance(startPosition, position);
			if (distance < 1f)
			{
				return startsInZone;
			}

			return ScenarioUtility.IsRetreatAvailableAtPosition(position);
		}

		public static bool HasRetreatZoneTag(CombatEntity combatant)
		{
			if (!combatant.hasUnitScenarioTags)
			{
				return false;
			}

			var retreatStateKey = ModLink.Settings.defaultRetreatZoneStateKey;
			if (ModLink.Settings.retreatZoneStateKeys.Count != 0)
			{
				var currentScenario = ScenarioUtility.GetCurrentScenario();
				if (ModLink.Settings.retreatZoneStateKeys.TryGetValue(currentScenario.key, out var stateKey))
				{
					retreatStateKey = stateKey;
				}
			}

			if (combatant.unitScenarioTags.tags.Contains(inStatePrefix + retreatStateKey))
			{
				return true;
			}

			return false;
		}

		static (bool, bool) HasMovement(CombatEntity combatant)
		{
			var moved = false;
			var changed = false;
			foreach (var action in Contexts.sharedInstance.action.GetEntitiesWithActionOwner(combatant.id.id))
			{
				if (action.isDestroyed)
				{
					continue;
				}
				if (action.isDisposed)
				{
					continue;
				}
				if (action.CompletedAction)
				{
					continue;
				}
				if (action.hasMovementPath)
				{
					moved = true;
				}
				if (action.isMovementPathChanged)
				{
					changed = true;
				}
			}
			return (moved, changed);
		}

		static void MovementProcessed(CombatEntity combatant)
		{
			foreach (var action in Contexts.sharedInstance.action.GetEntitiesWithActionOwner(combatant.id.id))
			{
				if (action.isDestroyed)
				{
					continue;
				}
				if (action.isDisposed)
				{
					continue;
				}
				if (action.CompletedAction)
				{
					continue;
				}
				if (action.isMovementPathChanged)
				{
					action.isMovementPathChanged = false;
				}
			}
		}

		const string inStatePrefix = "in_state_";
	}
}
