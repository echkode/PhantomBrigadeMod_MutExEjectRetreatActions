﻿// Copyright (c) 2024 EchKode
// SPDX-License-Identifier: BSD-3-Clause

namespace EchKode.PBMods.MutExEjectRetreatActions
{
	public class ConsoleCommandAttribute : System.Attribute
	{
		public ConsoleCommandAttribute(string prefix, string name, string description)
		{
			Prefix = prefix;
			Description = description;
			Name = name;
		}

		public string Prefix { get; private set; }
		public string Description { get; private set; }
		public string Name { get; private set; }
	}

	public class ConsoleOutputLabelAttribute : System.Attribute
	{
		public ConsoleOutputLabelAttribute(string label)
		{
			Label = label;
		}

		public string Label { get; private set; }
	}
}
