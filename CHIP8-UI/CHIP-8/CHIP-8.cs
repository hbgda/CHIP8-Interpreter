using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CHIPInterpreter
{
    internal class CHIP_8
    {
        const int SCREEN_WIDTH = 64;
        const int SCREEN_HEIGHT = 32;

        Action<bool[,]> Draw;
        bool[,] buffer = new bool[SCREEN_WIDTH, SCREEN_HEIGHT];
        bool shouldRedraw = true;
        
        // Not implemented
        Action<int> Beep;


        byte Delay;

        byte SP;
        ushort[] Stack = new ushort[16];

        // Program Counter
        ushort programCounter = 0x200;

        // 16bit register to store memory addresses during sprite loading.
        ushort I;

        // 16x 8-bit registers
        byte[] V = new byte[16];

        // 4K RAM
        byte[] RAM = new byte[0x1000];


        Dictionary<byte, Action<Instruction>> instructions;
        Dictionary<byte, Action<Instruction>> miscInstructions;
        // Pressed Keys
        HashSet<byte> keys = new HashSet<byte>();

        readonly Random rand = new Random();

        public CHIP_8(Action<bool[,]> Draw, Action<int> Beep)
        {
            instructions = new Dictionary<byte, Action<Instruction>>
            {
                { 
                    0x0, 
                    ReturnOrClear 
                },
                {
                    0x1,
                    Jump
                },
                {
                    0x2,
                    RunSubroutine
                },
                {
                    0x3,
                    SkipXEqual
                },
                {
                    0x4,
                    SkipXNotEqual
                },
                {
                    0x5,
                    SkipXEqualY
                },
                {
                    0x6, 
                    Store
                },
                {
                    0x7,
                    Add
                },
                {
                    0x8,
                    StoreXY
                },
                {
                    0x9,
                    SkipXNotEqualY
                },
                {
                    0xA,
                    SetI
                },
                {
                    0xB,
                    OffsetJump
                },
                {
                    0xC,
                    RandMask
                },
                {
                    0xD,
                    DrawSprite
                },
                {
                    0xE,
                    SkipIfKeyPressed
                },
                {
                    0xF,
                    MiscInstruction
                }
            };
            miscInstructions = new Dictionary<byte, Action<Instruction>>
            {
                {
                    0x07,
                    StoreDelay
                },
                {
                    0x0A,
                    WaitForKeyPressed
                },
                {
                    0x15,
                    SetDelayTimer
                },
                {
                    0x18,
                    SetSoundTimer
                },
                {
                    0x1E,
                    AddXToI
                },
                {
                    0x29,
                    SetIFontSprite
                },
                {
                    0x33,
                    StoreBinaryCoded
                },
                {
                    0x55,
                    StoreUntilX
                },
                {
                    0x65,
                    LoadUntilX
                }
            };

            this.Draw = Draw;
            this.Beep = Beep;

            SetFontDefault();
        }


        void SetFontDefault()
        {
            int offset = 0x0;
            SetFontData(5 * offset++, FontData.D0);
            SetFontData(5 * offset++, FontData.D1);
            SetFontData(5 * offset++, FontData.D2);
            SetFontData(5 * offset++, FontData.D3);
            SetFontData(5 * offset++, FontData.D4);
            SetFontData(5 * offset++, FontData.D5);
            SetFontData(5 * offset++, FontData.D6);
            SetFontData(5 * offset++, FontData.D7);
            SetFontData(5 * offset++, FontData.D8);
            SetFontData(5 * offset++, FontData.D9);
            SetFontData(5 * offset++, FontData.DA);
            SetFontData(5 * offset++, FontData.DB);
            SetFontData(5 * offset++, FontData.DC);
            SetFontData(5 * offset++, FontData.DD);
            SetFontData(5 * offset++, FontData.DE);
            SetFontData(5 * offset++, FontData.DF);
        }

        void SetFontData(int addr, long data)
        {
            RAM[addr++] = (byte)((data & 0xF000000000) >> 32);
            RAM[addr++] = (byte)((data & 0x00F0000000) >> 24);
            RAM[addr++] = (byte)((data & 0x0000F00000) >> 16);
            RAM[addr++] = (byte)((data & 0x000000F000) >> 8);
            RAM[addr++] = (byte)((data & 0x00000000F0) >> 0);
        }

        public void Load(byte[] data)
        {
            Array.Copy(data, 0, RAM, 0x200, data.Length);
        }

        public void MiscInstruction(Instruction data)
        {
            if(miscInstructions.ContainsKey(data.NN))
            {
                miscInstructions[data.NN](data);
            }
        }

        public void Tick()
        {
            ushort code = (ushort)(RAM[programCounter++] << 8 | RAM[programCounter++]);

            Instruction op = new Instruction()
            {
                Code = code,
                N = (byte)(code & 0x000F),
                NN = (byte)(code & 0x00FF),
                NNN = (ushort)(code & 0x0FFF),
                X = (byte)((code & 0x0F00) >> 8),
                Y = (byte)((code & 0x00F0) >> 4)
            };

            byte bCode = (byte)(code >> 12);
            //Console.WriteLine(bCode);
            instructions[bCode](op);
        }

        public void Tick60hz()
        {
            if (Delay > 0)
            {
                Delay--;
            }
            if (shouldRedraw)
            {
                shouldRedraw = false;
                Draw(buffer);
            }
        }

        public void KeyPressed(byte k) => keys.Add(k);
        public void KeyReleased(byte k) => keys.Remove(k);

        // https://github.com/mattmikolay/chip-8/wiki/CHIP%E2%80%908-Instruction-Set used as reference.

        // 0x00E0 and 0x00EE
        void ReturnOrClear(Instruction data)
        {
            if(data.NN == 0xE0)
            {
                for(int x = 0; x < SCREEN_WIDTH; x++)
                {
                    for (int y = 0; y < SCREEN_HEIGHT; y++)
                    {
                        buffer[x, y] = false;
                    }
                }
            }
            else if (data.NN == 0xEE)
            {
                programCounter = Pop();
            }
        }
        // 0x1
        void Jump(Instruction data)
        {
            programCounter = data.NNN;
        }
        // 0xB
        void OffsetJump(Instruction data)
        {
            programCounter = (ushort)(data.NNN + V[0]);
        }
        // 0x2
        void RunSubroutine(Instruction data)
        {
            Push(programCounter);
            programCounter = data.NNN;
        }
        // 0x3
        void SkipXEqual(Instruction data)
        {
            if (V[data.X] == data.NN)
                programCounter += 2;
        }
        // 0x4
        void SkipXNotEqual(Instruction data)
        {
            if (V[data.X] != data.NN)
                programCounter += 2;
        }
        // 0x5
        void SkipXEqualY(Instruction data)
        {
            if (V[data.X] == V[data.Y])
                programCounter += 2;
        }
        // 0x9
        void SkipXNotEqualY(Instruction data)
        {
            if (V[data.X] != V[data.Y])
                programCounter += 2;
        }
        // 0x6
        void Store(Instruction data)
        {
            V[data.X] = data.NN;
        }
        // 0x7
        void Add(Instruction data)
        {
            V[data.X] += data.NN;
        }
        // 0x8 0-E
        void StoreXY(Instruction data)
        {
            switch(data.N)
            {
                case 0x0:
                    V[data.X] = V[data.Y];
                    break;
                case 0x1:
                    V[data.X] |= V[data.Y];
                    break;
                case 0x2:
                    V[data.X] &= V[data.Y];
                    break;
                case 0x3:
                    V[data.X] ^= V[data.Y];
                    break;
                case 0x4:
                    V[data.X] += V[data.Y];
                    V[0xF] = (byte)(V[data.X] + V[data.Y] > 0xFF ? 1 : 0);
                    break;
                case 0x5:
                    V[data.X] -= V[data.Y];
                    V[0xF] = (byte)(V[data.Y] > V[data.X] ? 0 : 1);
                    break;
                case 0x6:
                    V[0xF] = (byte)((V[data.X] & 0x1) != 0 ? 1 : 0);
                    V[data.X] = (byte)(V[data.Y] / 2);
                    break;
                case 0x7:
                    V[data.X] = (byte)(V[data.Y] - V[data.X]);
                    V[0xF] = (byte)(V[data.X] > V[data.Y] ? 1 : 0);
                    break;
                case 0xE:
                    V[0xF] = (byte)((V[data.X] & 0xF) != 0 ? 1 : 0);
                    V[data.X] = (byte)(V[data.Y] * 2);
                    break;
            }
        }
        // 0xA
        void SetI(Instruction data)
        {
            I = data.NNN;
        }
        // 0xC
        void RandMask(Instruction data)
        {
            V[data.X] = (byte)(rand.Next(0, 256) & data.NN);
        }

        // 0xD
        void DrawSprite(Instruction data)
        {
            byte x = V[data.X];
            byte y = V[data.Y];

            V[0xF] = 0;
            for (byte i = 0; i < data.N; i++)
            {
                byte s = RAM[I + i];
                for (int bit = 0; bit < 8; bit++)
                {
                    int posX = (x + bit) % SCREEN_WIDTH;
                    int posY = (y + i) % SCREEN_HEIGHT;

                    int sBit = (s >> (7 - bit)) & 1;
                    int old = buffer[posX, posY] ? 1 : 0;

                    if (old != sBit)
                        shouldRedraw = true;

                    int newBit = old ^ sBit;

                    if (newBit != 0)
                        buffer[posX, posY] = true;
                    else
                        buffer[posX, posY] = false;

                    if(old == 1 && newBit == 0)
                    {
                        V[0xF] = 1;
                    }
                }
            }
            //Draw(buffer);
        }
        // 0xE 
        void SkipIfKeyPressed(Instruction data)
        {
            if (data.NN == 0x9E && keys.Contains(V[data.X]) || data.NN == 0xA1 && !keys.Contains(V[data.X]))
            {
                programCounter += 2;
            }
        }


        // Misc Instructions

        // 0xF 07
        void StoreDelay(Instruction data)
        {
            V[data.X] = Delay;
        }
        // 0xF 0A
        void WaitForKeyPressed(Instruction data)
        {
            if (keys.Count > 0)
            {
                V[data.X] = keys.First();
            }
            else
            {
                programCounter -= 2;
            }
        }
        // 0xF 15
        void SetDelayTimer(Instruction data)
        {
            Delay = V[data.X];
        }
        // 0xF 18
        void SetSoundTimer(Instruction data)
        {
            // Not implemented.
        }
        // 0xF 1E
        void AddXToI(Instruction data)
        {
            I += V[data.X];
        }
        // 0xF 29
        void SetIFontSprite(Instruction data)
        {
            I = (ushort)(V[data.X] * 5);
        }
        // 0xF 33
        void StoreBinaryCoded(Instruction data)
        {
            RAM[I + 0] = (byte)(V[data.X] / 100 % 10);
            RAM[I + 1] = (byte)(V[data.X] / 10 % 10);
            RAM[I + 2] = (byte)(V[data.X] % 10);
        }
        // 0xF 55
        void StoreUntilX(Instruction data)
        {
            for(byte i = 0; i <= data.X; i++)
            {
                RAM[I + i] = V[i];
            }
        }
        // 0xF 65
        void LoadUntilX(Instruction data)
        {
            for(byte i = 0; i <= data.X; i++)
            {
                V[i] = RAM[I + i];
            }
        }

        // Stack
        void Push(ushort val) => Stack[SP++] = val;
        ushort Pop() => Stack[--SP];
    }
}
