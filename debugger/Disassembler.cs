﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static debugger.Primitives;
using static debugger.ControlUnit;
namespace debugger
{
    public class Disassembler : EmulatorBase
    {
        public struct DisassembledItem
        {
            [Flags]
            public enum AddressState
            {
                Default = 0,
            }
            public string DisassembledLine;
            public ulong Address;
            public AddressState AddressInfo;
        }
        private Handle TargetHandle;
        private Context TargetContext { get => ContextHandler.CloneContext(TargetHandle); }
        public Disassembler(Handle targetHandle) : base("Disassembler", ContextHandler.CloneContext(targetHandle))
        {
            TargetHandle = targetHandle;
        }
        public async Task<List<DisassembledItem>> Step(ulong count)
        {            
            List<DisassembledItem> Output = new List<DisassembledItem>();
            for (ulong i = 0; i < count; i++)
            {
                string ExtraInfo;
                if(InstructionPointer == TargetContext.InstructionPointer)
                {
                    ExtraInfo = "←RIP";
                } else
                {
                    ExtraInfo = "    ";
                }
                ulong CurrentAddr = 0;
                Handle.Invoke(() => CurrentAddr = GetContext().InstructionPointer);
                Output.Add(new DisassembledItem()
                {
                    Address = CurrentAddr,                                         // } 1 space (←rip/4 spaces) 15 spaces {                    
                    DisassembledLine = $"{Util.Core.FormatNumber(CurrentAddr, FormatType.Hex)} {ExtraInfo}               {(await RunAsync(true)).LastDisassembled}"
                }); ;
               
            }
            return Output;
        }
        public async Task<List<DisassembledItem>> StepAll()
        {

            Context DisasContext = ContextHandler.FetchContext(Handle);
            DisasContext.InstructionPointer = DisasContext.Memory.EntryPoint;
            return await Step(DisasContext.Memory.SegmentMap[".main"].LastAddr - DisasContext.Memory.EntryPoint);
        }
    }

}
