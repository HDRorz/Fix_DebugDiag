using System;

namespace Microsoft.Diagnostics.Runtime;

[Flags]
public enum RegisterType : byte
{
	General = 1,
	Control = 2,
	Segments = 3,
	FloatingPoint = 4,
	Debug = 5,
	TypeMask = 0xF,
	ProgramCounter = 0x10,
	StackPointer = 0x20,
	FramePointer = 0x40
}
