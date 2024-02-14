// Copyright (c) 2024 EchKode
// SPDX-License-Identifier: BSD-3-Clause

using System.Reflection;

using HarmonyLib;

using PhantomBrigade.Data;

using QFSW.QC;

namespace EchKode.PBMods.MutExEjectRetreatActions
{
	static partial class ConsoleCommands
	{
		[ConsoleCommand("log", "mutexejectretreat", "Toggle diagnostics logging for mod")]
		static void ToggleDiagnostics()
		{
			FlipLoggingToggle(ModLink.Settings, nameof(ModLink.ModSettings.logDiagnostics));
		}

		[ConsoleCommand("log", "path-actions", "Toggle diagnostics logging for creating path actions")]
		static void ToggleCreatePathActionLogging()
		{
			FlipLoggingToggle(DataShortcuts.sim, nameof(DataContainerSettingsSimulation.logCombatActions));
		}

		static void FlipLoggingToggle(object o, string fieldName)
		{
			var fieldInfo = AccessTools.DeclaredField(o.GetType(), fieldName);
			if (fieldInfo == null)
			{
				return;
			}

			var toggle = (bool)fieldInfo.GetValue(o);
			toggle = !toggle;
			fieldInfo.SetValue(o, toggle);

			var labelAttribute = fieldInfo.GetCustomAttribute<ConsoleOutputLabelAttribute>();
			var label = labelAttribute != null
				? labelAttribute.Label + " logging"
				: fieldName;
			QuantumConsole.Instance.LogToConsole($"{label}: " + (toggle ? "on" : "off"));
		}
	}
}
