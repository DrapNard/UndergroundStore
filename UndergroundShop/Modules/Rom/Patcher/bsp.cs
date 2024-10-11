using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using UndergroundShop.Management;

namespace UndergroundShop.Modules.Rom.Patcher
{
    public class BspPatcher
    {
        private ResizableMemoryBlock? fileBuffer;
        private List<Frame> frames;
        private uint currentFilePointer;
        private bool currentFilePointerLocked;
        private bool initialized;
        private bool done;
        private uint exitStatus;
        private byte[]? sha1Hash;
        private bool dirty;
        private uint selectionRangeCheck;

        public BspPatcher(byte[] bsp, byte[] input)
        {
            fileBuffer = new ResizableMemoryBlock(input.Length);
            frames = new List<Frame> { new Frame(bsp) };
            sha1Hash = new byte[20]; // Initialize the hash array
            Initialize(input);
        }

        private void Initialize(byte[] input)
        {
            fileBuffer = new ResizableMemoryBlock(input.Length);
            currentFilePointer = 0;
            currentFilePointerLocked = false;
            uint position = currentFilePointer;

            for (int i = 0; i < input.Length; i++)
                fileBuffer.SetByte((int)position + i, input[i]);  // Use input[i] as the byte value

            initialized = true;
            dirty = true;
        }

        public void Run(uint? param = null)
        {
            if (!initialized) MessageManagement.ConsoleMessage("BSP Patcher not initialized!", 3);

            while (!done)
            {
                Step();
            }
        }

        private Action[] GetOpcodeParameters(uint opcode)
        {
            switch (opcode)
            {
                case 0x00: // NOP: No arguments
                case 0x01: // RETURN: No arguments
                case 0x80: // LOCKPOS: No arguments
                case 0x81: // UNLOCKPOS: No arguments
                    return Array.Empty<Action>();

                case 0x02: // JUMP: Needs a word (address)
                case 0x04: // CALL: Needs a word (address)
                case 0x60: // SEEK: Needs a word (file position)
                case 0x1E: // TRUNCATE: Needs a word (file size)
                    return new Action[] { () => NextPatchWord() };

                case 0x03: // JUMP by variable
                case 0x05: // CALL by variable
                case 0x06: // EXIT by variable
                case 0x07: // PUSH by variable
                case 0x09: // POP by variable
                case 0x19: // WRITEBYTE by variable
                case 0x61: // SEEKFORWARD by variable
                case 0x62: // SEEKBACK by variable
                case 0x83: // JUMP by variable table
                    return new Action[] { () => NextPatchVariable() };

                case 0x10: // GETBYTE (byte address)
                case 0x12: // GETHALFWORD (byte address)
                case 0x14: // GETWORD (byte address)
                    return new Action[] { () => NextPatchByte(), () => NextPatchWord() };

                case 0x11: // GETBYTE (variable address)
                case 0x13: // GETHALFWORD (variable address)
                case 0x15: // GETWORD (variable address)
                    return new Action[] { () => NextPatchByte(), () => NextPatchVariable() };

                case 0xA0: // BUFSTRING: Needs byte (address)
                case 0xA1: // BUFCHAR: Needs byte (character)
                    return new Action[] { () => NextPatchByte() };

                case 0x40: // CONDITIONAL JUMP (condition, variables, address)
                case 0x41: // CONDITIONAL JUMP with condition check
                    return new Action[] { () => NextPatchVariable(), () => NextPatchVariable(), () => NextPatchWord() };

                // Add other opcodes similarly as per their argument types...

                default:
                    MessageManagement.ConsoleMessage($"Undefined opcode {opcode:X2}", 3);
                    return new Action[] { () => NextPatchWord() };
            }
        }

        private void Step()
        {
            try
            {
                uint opcode = NextPatchByte();
                Action[] args = GetOpcodeParameters(opcode);

                foreach (var arg in args)
                {
                    arg();
                }

                if (!ExecuteOpcode(opcode))
                {
                    done = true;
                    if (exitStatus == 0)
                    {
                        sha1Hash = fileBuffer.CalculateSHA1();
                    }
                }
            }
            catch (Exception ex)
            {
                done = true;
                MessageManagement.ConsoleMessage($"Error in BSPPatcher: {ex.Message}", 4);
            }
        }

        private string UTF8Decode(uint address)
        {
            List<byte> byteList = new List<byte>();
            while (true)
            {
                byte nextByte = fileBuffer.GetByte((int)address++);
                if (nextByte == 0) break;  // Stop when encountering a null terminator
                byteList.Add(nextByte);
            }

            return Encoding.UTF8.GetString(byteList.ToArray());
        }


        private bool PushPosOpcode()
        {
            frames[0].PushToStack(currentFilePointer);
            return true;
        }

        private bool PopPosOpcode()
        {
            currentFilePointer = frames[0].PopFromStack();
            return true;
        }

        private bool BufStringOpcode()
        {
            uint address = NextPatchWord();
            frames[0].MessageBuffer += UTF8Decode(address);
            return true;
        }

        private void BufCharOpcode()
        {
            uint character = NextPatchVariable();
            frames[0].MessageBuffer += (char)character;
        }

        private void BufNumberOpcode()
        {
            uint number = NextPatchVariable();
            frames[0].MessageBuffer += number.ToString();
        }

        private bool PrintBufOpcode()
        {
            MessageManagement.ConsoleMessage(frames[0].MessageBuffer, 0);
            frames[0].MessageBuffer = ""; // Clear the buffer after printing.
            return true;
        }

        private bool ClearBufOpcode()
        {
            frames[0].MessageBuffer = "";
            return true;
        }

        private bool LockPosOpcode()
        {
            currentFilePointerLocked = true;
            return true;
        }

        private bool UnlockPosOpcode()
        {
            currentFilePointerLocked = false;
            return true;
        }

        private bool SetStackSizeOpcode()
        {
            uint newSize = NextPatchVariable();
            frames[0].ResizeStack((int)newSize);
            return true;
        }

        private bool PushOpcode(uint variable)
        {
            frames[0].Stack.Insert(0, variable);  // Assuming Stack is a List<uint>
            return true;
        }

        private bool PopOpcode(uint variable)
        {
            if (frames[0].Stack.Count == 0)
                MessageManagement.ConsoleMessage("Stack is empty", 4);

            uint value = frames[0].Stack[0];
            frames[0].Stack.RemoveAt(0);  // Remove the top of the stack
            frames[0].Variables[variable] = value;
            return true;
        }

        private bool ReadByteOpcode(uint variable)
        {
            frames[0].Variables[variable] = fileBuffer.GetByte((int)currentFilePointer);
            currentFilePointer++;
            return true;
        }

        private bool ReadHalfWordOpcode(uint variable)
        {
            frames[0].Variables[variable] = fileBuffer.GetHalfWord((int)currentFilePointer);
            currentFilePointer += 2;
            return true;
        }

        private bool ReadWordOpcode(uint variable)
        {
            frames[0].Variables[variable] = fileBuffer.GetWord((int)currentFilePointer);
            currentFilePointer += 4;
            return true;
        }

        private void WriteByteOpcode()
        {
            uint value = NextPatchVariable();
            fileBuffer.SetByte((int)currentFilePointer, (byte)(value & 0xFF));
            currentFilePointer++;
        }

        private void WriteHalfWordOpcode()
        {
            uint value = NextPatchVariable();
            fileBuffer.SetHalfWord((int)currentFilePointer, (ushort)(value & 0xFFFF));
            currentFilePointer += 2;
        }

        private void WriteWordOpcode()
        {
            uint value = NextPatchVariable();
            fileBuffer.SetWord((int)currentFilePointer, value);
            currentFilePointer += 4;
        }

        private void SeekOpcode()
        {
            uint newPos = NextPatchWord();
            currentFilePointer = newPos;
        }

        private void PrintOpcode()
        {
            uint address = NextPatchWord();
            string message = UTF8Decode(address);
            MessageManagement.ConsoleMessage(message, 0);
        }

        private bool GetStackSizeOpcode(uint variable)
        {
            frames[0].Variables[variable] = (uint)frames[0].Stack.Count;
            return true;
        }

        private bool GetFileByteOpcode(uint variable)
        {
            frames[0].Variables[variable] = fileBuffer.GetByte((int)currentFilePointer);
            currentFilePointer++;
            return true;
        }

        private bool GetFileHalfWordOpcode(uint variable)
        {
            frames[0].Variables[variable] = fileBuffer.GetHalfWord((int)currentFilePointer);
            currentFilePointer += 2;
            return true;
        }

        private bool GetFileWordOpcode(uint variable)
        {
            frames[0].Variables[variable] = fileBuffer.GetWord((int)currentFilePointer);
            currentFilePointer += 4;
            return true;
        }
        private bool LengthOpcode(uint variable)
        {
            frames[0].Variables[variable] = (uint)fileBuffer.Size;  // Assuming fileBuffer has a Size property
            return true;
        }
        private bool TruncatePosOpcode()
        {
            fileBuffer.Resize((int)currentFilePointer);  // Assuming fileBuffer has a Resize method
            dirty = true;
            return true;
        }
        private bool GetVariableOpcode(uint targetVariable, uint sourceVariable)
        {
            frames[0].Variables[targetVariable] = frames[0].Variables[sourceVariable];
            return true;
        }



        private bool ExecuteOpcode(uint opcode)
        {
            switch (opcode)
            {
                case 0x00: return true;  // NOP
                case 0x01: return ReturnOpcode();
                case 0x02: return JumpOpcode(NextPatchWord());
                case 0x03: return CallOpcode(NextPatchWord());
                case 0x04: return ExitOpcode(NextPatchWord());
                case 0x08: return PushOpcode(NextPatchVariable());
                case 0x0A: return PopOpcode(NextPatchVariable());
                case 0x0B: return LengthOpcode(NextPatchVariable());
                case 0x0C: return ReadByteOpcode(NextPatchVariable());
                case 0x0D: return ReadHalfWordOpcode(NextPatchVariable());
                case 0x0E: return ReadWordOpcode(NextPatchVariable());
                case 0x18: WriteByteOpcode(); return true;
                case 0x1A: WriteHalfWordOpcode(); return true;
                case 0x1C: WriteWordOpcode(); return true;
                case 0x20: AddOpcode(); return true;
                case 0x28: MultiplyOpcode(); return true;
                case 0x2C: DivideOpcode(); return true;
                case 0x40: return ConditionalJumpOpcode("<");
                case 0x50: return ConditionalJumpOpcode("==");
                case 0x58: return JumpIfZeroOpcode(true);
                case 0x60: SeekOpcode(); return true;
                case 0x68: PrintOpcode(); return false;
                case 0x80: LockPosOpcode(); return true;
                case 0x81: UnlockPosOpcode(); return true;
                case 0x82: TruncatePosOpcode(); return true;
                case 0x90: return ReturnIfZeroOpcode(true);
                case 0x92: PushPosOpcode(); return true;
                case 0x93: PopPosOpcode(); return true;
                case 0xA0: return BufStringOpcode();
                case 0xA2: BufCharOpcode(); return true;
                case 0xA4: BufNumberOpcode(); return true;
                case 0xA6: PrintBufOpcode(); return false;
                case 0xA7: ClearBufOpcode(); return true;
                case 0xA8: SetStackSizeOpcode(); return true;
                case 0xAA: GetStackSizeOpcode(NextPatchVariable()); return true;
                case 0xAC: GetFileByteOpcode(NextPatchVariable()); return true;
                case 0xAD: GetFileHalfWordOpcode(NextPatchVariable()); return true;
                case 0xAE: GetFileWordOpcode(NextPatchVariable()); return true;
                case 0xAF: GetVariableOpcode(NextPatchVariable(), NextPatchVariable()); return true;
                case 0xB0: AddCarryOpcode(); return true;
                case 0xB4: SubBorrowOpcode(); return true;
                case 0xB8: LongMulOpcode(); return true;
                case 0xBC: LongMulAccumOpcode(); return true;
                case 0xC0: XORDataOpcode(); return true;
                case 0xD0: WriteDataOpcode(); return true;
                case 0xE0: IPSPatchOpcode(); return true;
                default:
                    MessageManagement.ConsoleMessage($"Undefined opcode {opcode:X2}", 3);
                    return false;
            }
        }

        private bool ReturnOpcode()
        {
            if (frames[0].Stack.Count < 1) return false;
            frames[0].InstructionPointer = frames[0].PopFromStack();
            return true;
        }

        private bool JumpOpcode(uint target)
        {
            frames[0].InstructionPointer = target;
            return true;
        }

        private bool CallOpcode(uint target)
        {
            frames[0].PushToStack(frames[0].InstructionPointer);
            return JumpOpcode(target);
        }

        private bool ExitOpcode(uint value)
        {
            exitStatus = value;
            return false;
        }

        private bool ConditionalJumpOpcode(string condition)
        {
            uint first = NextPatchVariable();
            uint second = NextPatchVariable();
            uint address = NextPatchWord();
            bool result = EvalCondition(first, second, condition);
            if (result) JumpOpcode(address);
            return true;
        }

        private bool JumpIfZeroOpcode(bool condition)
        {
            uint value = NextPatchVariable();
            uint address = NextPatchWord();
            if ((value == 0) == condition)
            {
                JumpOpcode(address);
            }
            return true;
        }

        private bool ReturnIfZeroOpcode(bool condition)
        {
            uint comparand = NextPatchVariable();
            if ((comparand == 0) == condition)
            {
                return ReturnOpcode();
            }
            return true;
        }

        private void AddOpcode()
        {
            uint variable = NextPatchVariable();
            uint first = NextPatchVariable();
            uint second = NextPatchVariable();
            frames[0].Variables[variable] = first + second;
        }

        private void MultiplyOpcode()
        {
            uint variable = NextPatchVariable();
            uint first = NextPatchVariable();
            uint second = NextPatchVariable();
            frames[0].Variables[variable] = first * second;
        }

        private void DivideOpcode()
        {
            uint variable = NextPatchVariable();
            uint first = NextPatchVariable();
            uint second = NextPatchVariable();
            if (second == 0) throw new DivideByZeroException();
            frames[0].Variables[variable] = first / second;
        }

        private void AddCarryOpcode()
        {
            uint variable = NextPatchVariable();
            uint carry = NextPatchVariable();
            uint first = NextPatchVariable();
            uint second = NextPatchVariable();
            uint result = first + second;
            if (result < first)
            {
                frames[0].Variables[carry] += 1;
            }
            frames[0].Variables[variable] = result;
        }

        private void SubBorrowOpcode()
        {
            uint variable = NextPatchVariable();
            uint borrow = NextPatchVariable();
            uint first = NextPatchVariable();
            uint second = NextPatchVariable();
            if (first < second)
            {
                frames[0].Variables[borrow] -= 1;
            }
            frames[0].Variables[variable] = first - second;
        }

        private void LongMulOpcode()
        {
            uint low = NextPatchVariable();
            uint high = NextPatchVariable();
            uint first = NextPatchVariable();
            uint second = NextPatchVariable();
            var result = LongMultiply(first, second);
            frames[0].Variables[low] = result.low;
            frames[0].Variables[high] = result.high;
        }

        private void LongMulAccumOpcode()
        {
            uint low = NextPatchVariable();
            uint high = NextPatchVariable();
            uint first = NextPatchVariable();
            uint second = NextPatchVariable();
            var result = LongMultiply(first, second);
            frames[0].Variables[low] += result.low;
            frames[0].Variables[high] += result.high;
        }

        private (uint high, uint low) LongMultiply(uint first, uint second)
        {
            ulong result = (ulong)first * (ulong)second;
            uint low = (uint)(result & 0xFFFFFFFF);
            uint high = (uint)(result >> 32);
            return (high, low);
        }

        private void XORDataOpcode()
        {
            uint start = NextPatchVariable();
            uint len = NextPatchVariable();
            for (uint i = 0; i < len; i++)
            {
                fileBuffer.SetByte((int)(currentFilePointer + i), (byte)(fileBuffer.GetByte((int)(start + i)) ^ fileBuffer.GetByte((int)(currentFilePointer + i))));
            }
        }

        private void WriteDataOpcode()
        {
            uint start = NextPatchVariable();
            uint len = NextPatchVariable();
            for (uint i = 0; i < len; i++)
            {
                fileBuffer.SetByte((int)(currentFilePointer + i), fileBuffer.GetByte((int)(start + i)));
            }
        }

        private void IPSPatchOpcode()
        {
            // IPS file format header "PATCH"
            uint currentAddress = currentFilePointer;
            byte[] header = { 0x50, 0x41, 0x54, 0x43, 0x48 }; // "PATCH"

            for (int i = 0; i < header.Length; i++)
            {
                if (fileBuffer.GetByte((int)currentAddress++) != header[i])
                {
                    MessageManagement.ConsoleMessage("Invalid IPS header.", 4);
                }
            }

            while (true)
            {
                // Read 3-byte offset
                uint position = NextPatchValue(3);

                // If we reach "EOF" (0x454f46), we stop the patching process.
                if (position == 0x454f46)
                {
                    break;
                }

                // Read 2-byte length
                ushort size = (ushort)NextPatchValue(2);

                if (size == 0)  // RLE (Run Length Encoding) if size is zero
                {
                    ushort rleSize = (ushort)NextPatchValue(2);
                    byte value = (byte)NextPatchByte(); // RLE value

                    for (int i = 0; i < rleSize; i++)
                    {
                        fileBuffer.SetByte((int)(currentFilePointer + position + (uint)i), value);
                    }
                }
                else  // Normal data patching
                {
                    for (int i = 0; i < size; i++)
                    {
                        byte value = (byte)NextPatchByte();
                        fileBuffer.SetByte((int)(currentFilePointer + position + (uint)i), value);
                    }
                }
            }

            dirty = true; // Mark fileBuffer as modified
        }

        private uint NextPatchValue(int size)
        {
            uint result = 0;
            for (int i = 0; i < size; i++)
            {
                result = (result << 8) | NextPatchByte();
            }
            return result;
        }


        private uint NextPatchByte() => fileBuffer.GetByte((int)frames[0].InstructionPointer++);
        private uint NextPatchHalfWord() => fileBuffer.GetHalfWord((int)frames[0].InstructionPointer++);
        private uint NextPatchWord() => fileBuffer.GetWord((int)frames[0].InstructionPointer++);
        private uint NextPatchVariable() => frames[0].Variables[NextPatchByte()];

        private bool EvalCondition(uint first, uint second, string condition)
        {
            return condition switch
            {
                "<" => first < second,
                "<=" => first <= second,
                ">" => first > second,
                ">=" => first >= second,
                "==" => first == second,
                "!=" => first != second,
                _ => false,
            };
        }

        // Opcodes for file reading/writing and other operations omitted for brevity
    }

    public class ResizableMemoryBlock
    {
        private List<byte[]> parts;
        private int currentSize;
        private const int BlockSize = 8192;

        public ResizableMemoryBlock(int initialSize = 0)
        {
            parts = new List<byte[]>();
            Resize(initialSize);
        }

        public int Size => currentSize;

        public void Resize(int size)
        {
            if (size > currentSize)
                Expand(size);
            else
                Shrink(size);
        }

        private void Expand(int size)
        {
            while (parts.Count < ((size + BlockSize - 1) / BlockSize))
            {
                parts.Add(new byte[BlockSize]);
            }
            currentSize = size;
        }

        private void Shrink(int size)
        {
            int newPartsCount = (size + BlockSize - 1) / BlockSize;
            parts.RemoveRange(newPartsCount, parts.Count - newPartsCount);
            currentSize = size;
        }

        public void SetByte(int position, byte value)
        {
            EnsurePosition(position);
            parts[position / BlockSize][position % BlockSize] = value;
        }

        public byte GetByte(int position)
        {
            EnsurePosition(position);
            return parts[position / BlockSize][position % BlockSize];
        }

        public void SetHalfWord(int position, ushort value)
        {
            SetByte(position, (byte)(value & 0xFF));
            SetByte(position + 1, (byte)((value >> 8) & 0xFF));
        }

        public ushort GetHalfWord(int position)
        {
            return (ushort)(GetByte(position) | (GetByte(position + 1) << 8));
        }

        public void SetWord(int position, uint value)
        {
            SetByte(position, (byte)(value & 0xFF));
            SetByte(position + 1, (byte)((value >> 8) & 0xFF));
            SetByte(position + 2, (byte)((value >> 16) & 0xFF));
            SetByte(position + 3, (byte)((value >> 24) & 0xFF));
        }

        public uint GetWord(int position)
        {
            return (uint)(GetByte(position) | (GetByte(position + 1) << 8) |
                          (GetByte(position + 2) << 16) | (GetByte(position + 3) << 24));
        }

        private void EnsurePosition(int position)
        {
            if (position >= currentSize)
            {
                Resize(position + 1);
            }
        }

        public byte[] CalculateSHA1()
        {
            using (SHA1 sha1 = SHA1.Create())
            {
                // Flatten the memory block into a single byte array.
                byte[] fullData = new byte[currentSize];
                int offset = 0;
                foreach (var part in parts)
                {
                    Array.Copy(part, 0, fullData, offset, part.Length);
                    offset += part.Length;
                }

                return sha1.ComputeHash(fullData);
            }
        }
    }

    public class Stack
    {
        private List<uint> stackList;

        public Stack()
        {
            stackList = new List<uint>();
        }

        public void Push(uint value)
        {
            stackList.Insert(0, value);
        }

        public uint Pop()
        {
            if (stackList.Count == 0)
                MessageManagement.ConsoleMessage("Stack is empty", 3);
            uint value = stackList[0];
            stackList.RemoveAt(0);
            return value;
        }

        public int Size => stackList.Count;
    }


    public class Frame
    {
        public uint InstructionPointer;
        public byte[] PatchSpace;
        public List<uint> Stack = new List<uint>();
        public uint[] Variables = new uint[256];
        public string MessageBuffer = "";

        public Frame(byte[] patchSpace)
        {
            PatchSpace = patchSpace;
        }

        public void PushToStack(uint value)
        {
            Stack.Insert(0, value);
        }

        public uint PopFromStack()
        {
            uint value = Stack[0];
            Stack.RemoveAt(0);
            return value;
        }


        public void ResizeStack(int newSize)
        {
            if (newSize < Stack.Count)
            {
                Stack.RemoveRange(newSize, Stack.Count - newSize);
            }
            else
            {
                while (Stack.Count < newSize)
                {
                    Stack.Add(0); // Add default values to grow the stack
                }
            }
        }
    }
}
