﻿// The MemorySpace class provides the core way to input data into the program. Currently, the only one way to load the MemorySpace with data,
// which it to initialise a code segment. In the future, users will be able to initialise other segments, such as the stack segment
// with their own data. MemorySpace stores data extremely efficiently, and operates similar to the processor itself. Firstly, memory is only 
// allocated as it is used. This creates the illusion that a 4GB address space is available, however the data used will scale from the
// initial size of a dictionary proportionally with the amount of addresses with data stored.  Compared to the processor itself, which requires
// entire pages to be allocated at one time, usually with a minimum of 4KB size(4096 addresses, can differ, very hardware specific), yet at the
// same time, the processor does not have to store the address of a byte along with it. Being as efficient as a low level technology is what
// this program encourages, not implements. Given what is at my disposal, it makes very good use of its resources. See the index accessor
// for specific implementation.
using debugger.Util;
using System.Collections.Generic;

namespace debugger.Emulator
{
    public class MemorySpace
    {
        public readonly AddressMap RangeTable = new AddressMap();
        private Dictionary<ulong, byte> AddressDict = new Dictionary<ulong, byte>();
        public Dictionary<string, Segment> SegmentMap = new Dictionary<string, Segment>();
        public ulong EntryPoint;
        public ulong End;
        public class Segment
        {
            // Addresses in the MemorySpace also support a somewhat basic implementation of segmentation
            // A segment with predefined data can be loaded in to memory when the MemorySpace is created.
            // Segments can also be read from the MemorySpace, for example, the stack pointer is first initialised
            // to SegmentMap[".stack"].
            // For ease of use, segmentation is not strict, meaning that memory can be set outside of any segment.            
            public AddressRange Range;
            public byte[] Data = null;
        }
        public static implicit operator Dictionary<ulong, byte>(MemorySpace m) => m.AddressDict;
        public void AddSegment(string name, Segment segment)
        {
            // Add the segment to the map so it can be accessed by its name later.
            SegmentMap.Add(name, segment);

            // If the segment has data associated, write it into memory. It will be null if no data
            // was set.
            if(segment.Data != null)
            {
                SetRange(segment.Range.Start, segment.Data);
            }
            
        }
        public MemorySpace(byte[] memory)
        {
            // For simplicity a MemorySpace starts at 0 because there is no kernel implementation.
            EntryPoint = 0;

            // Define $End to tell when there are no more addresses to be read, anything after $End would return 0x00.
            // It is used to set a breakpoint after all instructions to avoid this.
            End = (ulong)memory.LongLength;

            // ".main" is where the ControlUnit will read the instructions from initially. It contains the data passed in as $memory.
            AddSegment(".main", new Segment() { Range = new AddressRange(EntryPoint, EntryPoint + (ulong)memory.LongLength), Data = memory });

            // ".stack" holds the start address of the stack. There is no defined $End of said stack. A manually crafted stack could be added
            // by setting $Segment.Data 
            AddSegment(".stack", new Segment() { Range = new AddressRange(0x800000, 0x800001) }); }

        private MemorySpace(MemorySpace toClone)
        {
            // A deep cloning constructor, so that editing addresses in $this does not change the addresses in $toClone.
            // Classes are object orientated, so C# will try to use a reference where ever possible, but this can get in the way.
            AddressDict = toClone.AddressDict.DeepCopy();
            SegmentMap = toClone.SegmentMap.DeepCopy();
            RangeTable = toClone.RangeTable.DeepCopy();

            // Value types do not need to be deep copied, by default they are not passed by reference.
            EntryPoint = toClone.EntryPoint;
            End = toClone.End;
        }
        public MemorySpace DeepCopy() => new MemorySpace(this);
        public byte this[ulong address]
        {
            // A memory space can use an index accessor which will return AddressMap[$address] if it exists, otherwise a 0. This is where a seg fault would occur if segmentation was strict.
            get => AddressDict.TryGetValue(address, out byte Fetched) ? Fetched : (byte)0x00;

            set
            {
                // Register the new value in the address map
                RangeTable.TryMerge(address);
                Set(address, value);
            }
        }

        public void SetRange(ulong address, byte[] data)
        {
            // To avoid doing a binary search on each $data, adding at one position means that only one has to take place.
            RangeTable.AddRange(new AddressRange(address, address + (ulong)data.Length));

            for (ulong i = 0; i < (ulong)data.Length; i++)
            {
                Set(address + i, data[i]);
            }
        }

        private void Set(ulong address, byte value)
        {
            // This is private because it does not affect AddressTable. This could be dangerous to an outside class. Use the indexer or SetRange().

            // The address map doesn't need to be filled with 0s at initialisation. That would be massive waste of space. So instead, addresses are added to the dictionary as they are given values,
            // if the address is already in $AddressMap, its value is changed.
            // By default a 0 byte is returned if the address is not used. This means that an address can be removed if a 0 byte is assigned to it, or never added to the address table at all.
            if (AddressDict.ContainsKey(address))
            {
                if (value == 0x00)
                {
                    AddressDict.Remove(address);
                }
                else
                {
                    AddressDict[address] = value;
                }
            }
            else if (value != 0x00)
            {
                AddressDict.Add(address, value);
            }
        }
    }
}
