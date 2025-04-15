using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using UndergroundShop.Management;

namespace UndergroundShop.Modules.Rom.Patcher
{
    /// <summary>
    /// Implements a Binary Script Patch (BSP) format patcher for ROM files.
    /// </summary>
    public class BspPatcher
    {
        private readonly ResizableMemoryBlock fileBuffer;
        private readonly List<Frame> frames = [];
        private uint currentFilePointer;
        private bool initialized;
        private bool done;
        private uint exitStatus;
        private bool currentFilePointerLocked;
        
        /// <summary>
        /// Gets the SHA1 hash of the patched file after successful execution.
        /// </summary>
        public string? SHA1Hash { get; private set; }

        /// <summary>
        /// Initializes a new instance of the BspPatcher class.
        /// </summary>
        /// <param name="bsp">The BSP patch data.</param>
        /// <param name="input">The input ROM data to be patched.</param>
        public BspPatcher(byte[] bsp, byte[] input)
        {
            fileBuffer = new ResizableMemoryBlock(input.Length);
            frames = [new Frame(bsp)];
            Initialize(input);
        }

        /// <summary>
        /// Initializes the patcher with the input data.
        /// </summary>
        /// <param name="input">The input ROM data to be patched.</param>
        private void Initialize(byte[] input)
        {
            currentFilePointer = 0;
            currentFilePointerLocked = false;
            uint position = currentFilePointer;

            for (int i = 0; i < input.Length; i++)
                fileBuffer.SetByte((int)position + i, input[i]);

            initialized = true;
        }

        /// <summary>
        /// Runs the patch process until completion.
        /// </summary>
        public void Run()
        {
            if (!initialized) 
            {
                MessageManagement.ConsoleMessage("BSP Patcher not initialized!", 3);
                return;
            }

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
                    return [() => NextPatchWord()];

                case 0x03: // JUMP by variable
                case 0x05: // CALL by variable
                case 0x06: // EXIT by variable
                case 0x07: // PUSH by variable
                case 0x09: // POP by variable
                case 0x19: // WRITEBYTE by variable
                case 0x61: // SEEKFORWARD by variable
                case 0x62: // SEEKBACK by variable
                case 0x83: // JUMP by variable table
                    return [() => NextPatchVariable()];

                case 0x10: // GETBYTE (byte address)
                case 0x12: // GETHALFWORD (byte address)
                case 0x14: // GETWORD (byte address)
                    return [() => NextPatchByte(), () => NextPatchWord()];

                case 0x11: // GETBYTE (variable address)
                case 0x13: // GETHALFWORD (variable address)
                case 0x15: // GETWORD (variable address)
                    return [() => NextPatchByte(), () => NextPatchVariable()];

                case 0xA0: // BUFSTRING: Needs byte (address)
                case 0xA1: // BUFCHAR: Needs byte (character)
                    return [() => NextPatchByte()];

                case 0x40: // CONDITIONAL JUMP (condition, variables, address)
                case 0x41: // CONDITIONAL JUMP with condition check
                    return [() => NextPatchVariable(), () => NextPatchVariable(), () => NextPatchWord()];

                // Add other opcodes similarly as per their argument types...

                default:
                    MessageManagement.ConsoleMessage($"Undefined opcode {opcode:X2}", 3);
                    return [() => NextPatchWord()];
            }
        }

        /// <summary>
        /// Executes a single step of the patch process.
        /// </summary>
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
                        SHA1Hash = fileBuffer?.CalculateSHA1();
                    }
                }
            }
            catch (Exception ex)
            {
                done = true;
                MessageManagement.ConsoleMessage($"Error in BSPPatcher: {ex.Message}", 4);
            }
        }

        /// <summary>
        /// Decodes a null-terminated UTF-8 string from the specified address in the file buffer.
        /// </summary>
        /// <param name="address">The starting address of the string.</param>
        /// <returns>The decoded UTF-8 string.</returns>
        private string UTF8Decode(uint address)
        {
            List<byte> byteList = [];
            while (true)
            {
                byte nextByte = fileBuffer?.GetByte((int)address++) ?? 0;
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

        /// <summary>
        /// Locks the current file pointer position.
        /// </summary>
        private void LockPosOpcode()
        {
            currentFilePointerLocked = true;
        }

        /// <summary>
        /// Unlocks the current file pointer position.
        /// </summary>
        private void UnlockPosOpcode()
        {
            currentFilePointerLocked = false;
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
            frames[0].Variables[variable] = fileBuffer?.GetByte((int)currentFilePointer) ?? 0;
            currentFilePointer++;
            return true;
        }

        private bool ReadHalfWordOpcode(uint variable)
        {
            frames[0].Variables[variable] = fileBuffer?.GetHalfWord((int)currentFilePointer) ?? 0;
            currentFilePointer += 2;
            return true;
        }

        private bool ReadWordOpcode(uint variable)
        {
            frames[0].Variables[variable] = fileBuffer?.GetWord((int)currentFilePointer) ?? 0;
            currentFilePointer += 4;
            return true;
        }

        private void WriteByteOpcode()
        {
            uint value = NextPatchVariable();
            fileBuffer?.SetByte((int)currentFilePointer, (byte)(value & 0xFF));
            currentFilePointer++;
        }

        private void WriteHalfWordOpcode()
        {
            uint value = NextPatchVariable();
            fileBuffer?.SetHalfWord((int)currentFilePointer, (ushort)(value & 0xFFFF));
            currentFilePointer += 2;
        }

        private void WriteWordOpcode()
        {
            uint value = NextPatchVariable();
            fileBuffer?.SetWord((int)currentFilePointer, value);
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
            frames[0].Variables[variable] = fileBuffer?.GetByte((int)currentFilePointer) ?? 0;
            currentFilePointer++;
            return true;
        }

        private bool GetFileHalfWordOpcode(uint variable)
        {
            frames[0].Variables[variable] = fileBuffer?.GetHalfWord((int)currentFilePointer) ?? 0;
            currentFilePointer += 2;
            return true;
        }

        private bool GetFileWordOpcode(uint variable)
        {
            frames[0].Variables[variable] = fileBuffer?.GetWord((int)currentFilePointer) ?? 0;
            currentFilePointer += 4;
            return true;
        }
        private bool LengthOpcode(uint variable)
        {
            frames[0].Variables[variable] = fileBuffer != null ? (uint)fileBuffer.Size : 0;  // Assuming fileBuffer has a Size property
            return true;
        }
        private bool TruncatePosOpcode()
        {
            fileBuffer?.Resize((int)currentFilePointer);  // Assuming fileBuffer has a Resize method
            // Variable dirty supprimée car jamais utilisée
            return true;
        }
        private bool GetVariableOpcode(uint targetVariable, uint sourceVariable)
        {
            frames[0].Variables[targetVariable] = frames[0].Variables[sourceVariable];
            return true;
        }



        /// <summary>
        /// Executes the specified opcode.
        /// </summary>
        /// <param name="opcode">The opcode to execute.</param>
        /// <returns>True if execution should continue, false if it should pause.</returns>
        private bool ExecuteOpcode(uint opcode)
        {
            try
            {
                return opcode switch
                {
                    // Control flow opcodes
                    0x00 => true,                                  // NOP
                    0x01 => ReturnOpcode(),                        // RETURN
                    0x02 => JumpOpcode(NextPatchWord()),          // JUMP
                    0x03 => CallOpcode(NextPatchWord()),          // CALL
                    0x04 => ExitOpcode(NextPatchWord()),          // EXIT
                    
                    // Stack manipulation opcodes
                    0x08 => PushOpcode(NextPatchVariable()),      // PUSH
                    0x0A => PopOpcode(NextPatchVariable()),       // POP
                    0x92 => PushPosOpcode(),                      // PUSHPOS
                    0x93 => PopPosOpcode(),                       // POPPOS
                    0xA8 => SetStackSizeOpcode(),                 // SETSTACKSIZE
                    0xAA => GetStackSizeOpcode(NextPatchVariable()), // GETSTACKSIZE
                    
                    // File information opcodes
                    0x0B => LengthOpcode(NextPatchVariable()),    // LENGTH
                    
                    // Read opcodes
                    0x0C => ReadByteOpcode(NextPatchVariable()),     // READBYTE
                    0x0D => ReadHalfWordOpcode(NextPatchVariable()), // READHALFWORD
                    0x0E => ReadWordOpcode(NextPatchVariable()),     // READWORD
                    0xAC => GetFileByteOpcode(NextPatchVariable()),  // GETFILEBYTE
                    0xAD => GetFileHalfWordOpcode(NextPatchVariable()), // GETFILEHALFWORD
                    0xAE => GetFileWordOpcode(NextPatchVariable()),  // GETFILEWORD
                    
                    // Write opcodes
                    0x18 => ExecuteAndContinue(WriteByteOpcode),     // WRITEBYTE
                    0x1A => ExecuteAndContinue(WriteHalfWordOpcode), // WRITEHALFWORD
                    0x1C => ExecuteAndContinue(WriteWordOpcode),     // WRITEWORD
                    0xD0 => ExecuteAndContinue(WriteDataOpcode),     // WRITEDATA
                    
                    // Arithmetic opcodes
                    0x20 => ExecuteAndContinue(AddOpcode),         // ADD
                    0x28 => ExecuteAndContinue(MultiplyOpcode),    // MULTIPLY
                    0x2C => ExecuteAndContinue(DivideOpcode),      // DIVIDE
                    0xB0 => ExecuteAndContinue(AddCarryOpcode),    // ADDCARRY
                    0xB4 => ExecuteAndContinue(SubBorrowOpcode),   // SUBBORROW
                    0xB8 => ExecuteAndContinue(LongMulOpcode),     // LONGMUL
                    0xBC => ExecuteAndContinue(LongMulAccumOpcode), // LONGMULACCUM
                    0xAF => GetVariableOpcode(NextPatchVariable(), NextPatchVariable()), // GETVARIABLE
                    
                    // Conditional opcodes
                    0x40 => ConditionalJumpOpcode("<"),           // JUMPLT
                    0x50 => ConditionalJumpOpcode("=="),         // JUMPEQ
                    0x58 => JumpIfZeroOpcode(true),               // JUMPIFZERO
                    0x90 => ReturnIfZeroOpcode(true),             // RETURNIFZERO
                    
                    // File pointer opcodes
                    0x60 => ExecuteAndContinue(SeekOpcode),        // SEEK
                    0x80 => ExecuteAndContinue(LockPosOpcode),     // LOCKPOS
                    0x81 => ExecuteAndContinue(UnlockPosOpcode),   // UNLOCKPOS
                    0x82 => TruncatePosOpcode(),                  // TRUNCATEPOS
                    
                    // Output opcodes
                    0x68 => ExecuteWithPause(PrintOpcode),         // PRINT
                    0xA0 => BufStringOpcode(),                     // BUFSTRING
                    0xA2 => ExecuteAndContinue(BufCharOpcode),     // BUFCHAR
                    0xA4 => ExecuteAndContinue(BufNumberOpcode),   // BUFNUMBER
                    0xA6 => PrintBufOpcode(),                      // PRINTBUF
                    0xA7 => ClearBufOpcode(),                      // CLEARBUF
                    
                    // Data manipulation opcodes
                    0xC0 => ExecuteAndContinue(XORDataOpcode),     // XORDATA
                    0xE0 => ExecuteAndContinue(IPSPatchOpcode),    // IPSPATCH
                    
                    // Default case for undefined opcodes
                    _ => HandleUndefinedOpcode(opcode)
                };
            }
            catch (Exception ex)
            {
                MessageManagement.ConsoleMessage($"Error executing opcode {opcode:X2}: {ex.Message}", 3);
                return false;
            }
        }
        
        /// <summary>
        /// Handles an undefined opcode by logging an error message.
        /// </summary>
        /// <param name="opcode">The undefined opcode.</param>
        /// <returns>False to indicate execution should stop.</returns>
        private bool HandleUndefinedOpcode(uint opcode)
        {
            MessageManagement.ConsoleMessage($"Undefined opcode {opcode:X2}", 3);
            return false;
        }
        
        /// <summary>
        /// Executes an action and returns true to continue execution.
        /// </summary>
        /// <param name="action">The action to execute.</param>
        /// <returns>True to indicate execution should continue.</returns>
        private bool ExecuteAndContinue(Action action)
        {
            action();
            return true;
        }
        
        /// <summary>
        /// Executes an action and returns false to pause execution.
        /// </summary>
        /// <param name="action">The action to execute.</param>
        /// <returns>False to indicate execution should pause.</returns>
        private bool ExecuteWithPause(Action action)
        {
            action();
            return false;
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

        private static (uint high, uint low) LongMultiply(uint first, uint second)
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
            if (fileBuffer is not null)
            {
                for (uint i = 0; i < len; i++)
                {
                    fileBuffer.SetByte((int)(currentFilePointer + i), (byte)(fileBuffer.GetByte((int)(start + i)) ^ fileBuffer.GetByte((int)(currentFilePointer + i))));
                }
            }
        }

        private void WriteDataOpcode()
        {
            uint start = NextPatchVariable();
            uint len = NextPatchVariable();
            for (uint i = 0; i < len; i++)
            {
                if (fileBuffer != null)
                {
                    fileBuffer.SetByte((int)(currentFilePointer + i), fileBuffer.GetByte((int)(start + i)));
                }
            }
        }

        private void IPSPatchOpcode()
        {
            if (fileBuffer == null) return;
            
            // IPS file format header "PATCH"
            uint currentAddress = currentFilePointer;
            byte[] header = [ 0x50, 0x41, 0x54, 0x43, 0x48 ]; // "PATCH"

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

            // Variable dirty supprimée car jamais utilisée // Mark fileBuffer as modified
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


        /// <summary>
        /// Reads the next byte from the patch data and advances the instruction pointer.
        /// </summary>
        /// <returns>The byte value read.</returns>
        private uint NextPatchByte() => fileBuffer?.GetByte((int)frames[0].InstructionPointer++) ?? 0;
        
        /// <summary>
        /// Reads the next half-word (2 bytes) from the patch data and advances the instruction pointer.
        /// </summary>
        /// <returns>The half-word value read.</returns>
        private uint NextPatchHalfWord() => fileBuffer?.GetHalfWord((int)frames[0].InstructionPointer++) ?? 0;
        
        /// <summary>
        /// Reads the next word (4 bytes) from the patch data and advances the instruction pointer.
        /// </summary>
        /// <returns>The word value read.</returns>
        private uint NextPatchWord() => fileBuffer?.GetWord((int)frames[0].InstructionPointer++) ?? 0;
        
        /// <summary>
        /// Reads a variable value using the next byte as an index.
        /// </summary>
        /// <returns>The variable value.</returns>
        private uint NextPatchVariable() => frames[0].Variables[NextPatchByte()];

        /// <summary>
        /// Evaluates a condition between two values.
        /// </summary>
        /// <param name="first">The first value to compare.</param>
        /// <param name="second">The second value to compare.</param>
        /// <param name="condition">The condition operator ("<", "<=", ">", ">=", "==", "!=").</param>
        /// <returns>True if the condition is satisfied, false otherwise.</returns>
        private static bool EvalCondition(uint first, uint second, string condition)
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

    /// <summary>
    /// A memory block that can be dynamically resized, implemented as a list of fixed-size blocks.
    /// </summary>
    public class ResizableMemoryBlock
    {
        private List<byte[]> parts;
        private int currentSize;
        private const int BlockSize = 8192;

        public ResizableMemoryBlock(int initialSize = 0)
        {
            parts = [];
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

        public string CalculateSHA1()
        {
            // Flatten the memory block into a single byte array.
            byte[] fullData = new byte[currentSize];
            int offset = 0;
            foreach (var part in parts)
            {
                Array.Copy(part, 0, fullData, offset, part.Length);
                offset += part.Length;
            }

            // Calculate SHA1 hash and convert to hexadecimal string
            using (var sha1 = SHA1.Create())
            {
                byte[] hashBytes = sha1.ComputeHash(fullData);
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
            }
        }
    }

    /// <summary>
    /// A stack implementation for the BSP patcher.
    /// </summary>
    public class Stack
    {
        private readonly List<uint> stackList = [];

        /// <summary>
        /// Initializes a new instance of the Stack class.
        /// </summary>
        public Stack()
        {
        }

        /// <summary>
        /// Pushes a value onto the stack.
        /// </summary>
        /// <param name="value">The value to push.</param>
        public void Push(uint value)
        {
            stackList.Insert(0, value);
        }

        /// <summary>
        /// Pops a value from the stack.
        /// </summary>
        /// <returns>The popped value.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the stack is empty.</exception>
        public uint Pop()
        {
            if (stackList.Count == 0)
                MessageManagement.ConsoleMessage("Stack is empty", 3);
            uint value = stackList[0];
            stackList.RemoveAt(0);
            return value;
        }

        /// <summary>
        /// Gets the number of elements in the stack.
        /// </summary>
        public int Size => stackList.Count;
    }


    /// <summary>
    /// Represents an execution frame for the BSP patcher.
    /// </summary>
    public class Frame
    {
        /// <summary>
        /// Gets or sets the current instruction pointer.
        /// </summary>
        public uint InstructionPointer { get; set; }
        
        /// <summary>
        /// Gets the patch data.
        /// </summary>
        public byte[] PatchSpace { get; }
        
        /// <summary>
        /// Gets the stack for this frame.
        /// </summary>
        public List<uint> Stack { get; } = new List<uint>();
        
        /// <summary>
        /// Gets the variables array for this frame.
        /// </summary>
        public uint[] Variables { get; } = new uint[256];
        
        /// <summary>
        /// Gets or sets the message buffer for this frame.
        /// </summary>
        public string MessageBuffer { get; set; } = "";

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
