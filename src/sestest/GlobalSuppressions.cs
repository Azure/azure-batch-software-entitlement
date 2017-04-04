
// This file is used by Code Analysis to maintain SuppressMessage 
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given 
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage(
    "General",
    "RCS1118:Mark local variable as const.",
    Justification = "Prefer not to use const in local scope")]

[assembly: SuppressMessage(
    "Performance",
    "RCS1080:Use 'Count/Length' property instead of 'Any' method.",
    Justification = "Prefer the clarity of Any()")]
