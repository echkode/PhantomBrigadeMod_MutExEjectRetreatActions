// Copyright (c) 2024 EchKode
// SPDX-License-Identifier: BSD-3-Clause

using System.Collections.Generic;
using System.IO;

using UnityEngine;

namespace EchKode.PBMods.MutExEjectRetreatActions
{
	partial class ModLink
	{
		internal sealed class ModSettings
		{
#pragma warning disable CS0649
			public string defaultRetreatZoneStateKey = "retreat_default";
			public Dictionary<string, string> retreatZoneStateKeys = new Dictionary<string, string>();
			public bool usePatch;
			public bool useActionSwap;
			public bool registerCommands;
			[ConsoleOutputLabel("Diagnostics")]
			public bool logDiagnostics;
#pragma warning restore CS0649
		}

		internal static ModSettings Settings;

		static void LoadSettings()
		{
			var settingsPath = Path.Combine(modPath, "settings.yaml");
			Settings = UtilitiesYAML.ReadFromFile<ModSettings>(settingsPath, false);
			if (Settings == null)
			{
				Settings = new ModSettings();
			}
			Debug.LogFormat(
				"Mod {0} ({1}) settings | path: {2}"
					+ "\n  default state key: {3}\n  state key map: {4}\n  use patch: {5}"
					+ "\n  use action swap: {6}\n  console commands: {7}\n  diagnostics logging: {8}",
				modIndex,
				modID,
				settingsPath,
				Settings.defaultRetreatZoneStateKey,
				Settings.retreatZoneStateKeys.ToStringFormatted(),
				Settings.usePatch,
				Settings.useActionSwap,
				Settings.registerCommands ? "register" : "-",
				Settings.logDiagnostics ? "on" : "off");
		}
	}
}
