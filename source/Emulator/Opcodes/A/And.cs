﻿using debugger.Util;
using System.Collections.Generic;

namespace debugger.Emulator.Opcodes
{
    public class And : Opcode
    {
        readonly byte[] Result;
        readonly FlagSet ResultFlags;
        public And(DecodedTypes.IMyDecoded input, OpcodeSettings settings = OpcodeSettings.NONE) : base("AND", input, settings)
        {
            List<byte[]> DestSource = Fetch();
            ResultFlags = Bitwise.And(DestSource[0], DestSource[1], out Result); // fix bitwise stuff
        }

        public override void Execute()
        {
            Set(Result);
            ControlUnit.SetFlags(ResultFlags);
        }
    }
}
