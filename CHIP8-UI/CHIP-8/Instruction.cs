using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CHIPInterpreter
{
    internal class Instruction
    {
        public ushort Code;
        // Last three nibbles of op
        public ushort NNN;
        public byte N, NN, X, Y;

        public override string ToString()
        {
            return $"{Code:X4} (N: {N:X}, NN: {NN:X2}, NNN: {NNN:X3}, X: {X:X}, Y: {Y:X})";
        }
    }
}
