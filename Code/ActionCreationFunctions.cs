// Copyright (c) 2024 EchKode
// SPDX-License-Identifier: BSD-3-Clause

using System.Collections;

using HarmonyLib;

using PhantomBrigade;
using PhantomBrigade.Data;
using PhantomBrigade.Functions;

namespace EchKode.PBMods.MutExEjectRetreatActions
{
	public static class ActionCreationFunctions
	{
		public class EjectOrRetreat : ICombatActionExecutionFunction
		{
			public void Run(CombatEntity combatant, ActionEntity action)
			{
				if (!ModLink.Settings.useActionSwap)
				{
					return;
				}
				if (!Contexts.sharedInstance.combat.hasUnitSelected)
				{
					return;
				}
				if (Contexts.sharedInstance.combat.unitSelected.id != combatant.id.id)
				{
					return;
				}
				if (!combatant.isPlayerControllable)
				{
					return;
				}

				var actionKey = action.dataKeyAction.s;
				if (actionKey != "eject" && actionKey != "retreat")
				{
					return;
				}

				var time = ActionUtility.GetLastActionTime(combatant, true);
				PathUtility.GetProcessedPathDataAtTime(
					combatant,
					time,
					out var position,
					out var _,
					out var _,
					out var _,
					out var _);
				var inRetreat = ScenarioUtility.IsRetreatAvailableAtPosition(position);
				if (inRetreat && actionKey == "retreat")
				{
					return;
				}
				if (!inRetreat && actionKey == "eject")
				{
					return;
				}

				actionKey = inRetreat ? "retreat" : "eject";

				if (!IsActionAvailable(actionKey))
				{
					return;
				}

				DataHelperAction.InstantiateAction(
					combatant,
					actionKey,
					action.startTime.f,
					out var ok,
					refreshScenarioState: false);
				if (!ok)
				{
					return;
				}
				action.CompletedAction = true;
				action.isDisposed = true;
			}
		}

		static bool IsActionAvailable(string actionKey)
		{
			EnsureVerifiedActions();
			CIViewCombatAction.ins.RefreshSelectedUnitActions();
			foreach (var o in verifiedActions)
			{
				var t = new Traverse(o);
				if (actionKey == t.Field<DataContainerAction>("actionData").Value.key)
				{
					return t.Field<bool>("available").Value;
				}
			}

			return false;
		}

		static void EnsureVerifiedActions()
		{
			if (verifiedActions != null)
			{
				return;
			}

			var fi = AccessTools.DeclaredField(CIViewCombatAction.ins.GetType(), "actionLinksVerified");
			verifiedActions = (IList)fi.GetValue(CIViewCombatAction.ins);
		}

		static IList verifiedActions;
	}
}
