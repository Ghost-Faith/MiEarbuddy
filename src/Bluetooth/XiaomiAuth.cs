using System;

namespace MiBudsApp.Bluetooth
{
    /// <summary>
    /// Algoritmo de autenticação Xiaomi MMA E21 (desafio e resposta).
    /// </summary>
    public static class XiaomiAuth
    {
        private static readonly byte[] SBOX = Hex("64ac285ac9b337c50a10b7a3bab197463d05dc666ef69af80d589567c6aaabec"
            + "a0689b96d4ebbf434936e96a89d8c38a946399bc7bbec122bb5c71d51f92575d"
            + "8f44411d51e64017fbfd193234b8612aca236fda39f7a2017fd631e7de8004dd"
            + "2c5982afa8e00fcda1123e30d11cd03a33722e4f9002130675ce87c2efb2ad7d"
            + "3815e1529f7a6c2f27c4e281a9cf8dc0d7dfff6076148c5e5509e408c74220fc"
            + "d25091d94c629ee8b9a6f91a00210bfa359c4e4b6948cb0ec8a45bea8407b418"
            + "f4ae6bdba7cc3f8b4a0c3c25e5544d4583ed11f0b05393f27426b59d6d7cf32d"
            + "f156247e471b86bd708e1e3b731603b6ac285ac9b337c50a10b7a3bab1974688");

        private static readonly byte[] T1 = Hex("012de293be4515ae780387a4b838cf3f08670994eb26a86bbd18341bbbbf72f7"
            + "4035489c512f3b55e3c09fd8d3f38db1ffa73edc8677d7a611fbf4ba92916483"
            + "f133efda2cb5b22b88d199cb8c841d14819771ca5fa38b573c82c4525c1ce8a0"
            + "04b4854af61354b6df0c1a8edee039fc209b244ea9989eabf260d06ceafac7d9"
            + "00d41f6e43bcec5389fe7a5d49c932c2f99af86d16db599644e9cde646428f0a"
            + "c1ccb965b0d2c6ac1e4162292e0e7450025ac3257b8a2a5bf0060d476f709d7e"
            + "10ce1227d54c4fd679306836757de4ed806a9037a25e76aac57f3dafa5e51961"
            + "fd4d7cb70beead4b22f5e7732321c805e166ddb3586963560fa1319517073a28");

        private static readonly byte[] T2 = Hex("8000b00960efb9fd10129fe469baadf8c038c2654f0694fc19de6a1b5d4ea882"
            + "70ede8ec72b315c3ffabb6474401ac25c9fa8e411a21cbd30d6efe2658da320f"
            + "20a99d8498059cbb228c63e7c5e173c6af245b876627f757f496b1b75c8bd554"
            + "79dfaaf63ea3f111caf5d1177b9383bcbd521eebaeccd63508c88ab4e2cdbfd9"
            + "d050593f4d62340a4888b5564c2e6b9ed23d3c0313fb9751754a917123be762a"
            + "5ff9d4550bdc37311674d777a7e607dba42f46f3614567e30ca23b1c8518041d"
            + "29a08fb25ad8a67eee8d534ba19ac10e7a49a52c81c4c7362b7f439533f26c68"
            + "6df00228cedd9bea5e997c1486cfe542b840782d3ae9641f92907d396fe08930");

        private static readonly byte[] PLACEHOLDER_ADDR = Hex("112233332211");

        public static byte[] Hex(string s)
        {
            byte[] b = new byte[s.Length / 2];
            for (int i = 0; i < b.Length; i++)
                b[i] = Convert.ToByte(s.Substring(i * 2, 2), 16);
            return b;
        }

        private static int Rol8(int x, int n)
        {
            x &= 0xFF; n &= 7;
            return ((x << n) | (x >> (8 - n))) & 0xFF;
        }

        private static bool Sel(int j) { return ((1 << j) & 0x9999) != 0; }

        private static byte[][] KeySchedule(byte[] inp16)
        {
            byte[][] rk = new byte[17][];
            for (int i = 0; i < 17; i++) rk[i] = new byte[16];
            Array.Copy(inp16, 0, rk[0], 0, 16);
            int[] cur = new int[16];
            for (int i = 0; i < 16; i++) cur[i] = inp16[i] & 0xFF;
            int[] buf = new int[17];
            for (int r = 0; r < 16; r++)
            {
                int parity = 0;
                foreach (int b in cur) parity ^= b;
                for (int i = 0; i < 16; i++) buf[i] = Rol8(cur[i], 3);
                buf[16] = Rol8(parity, 3);
                for (int j = 0; j < 16; j++)
                {
                    int sboxIdx = 15 + 16 * r - j;
                    int bufIdx = (r + 1 + j) % 17;
                    rk[r + 1][j] = (byte)((SBOX[sboxIdx] + buf[bufIdx]) & 0xFF);
                }
                for (int i = 0; i < 16; i++) cur[i] = Rol8(cur[i], 3);
            }
            return rk;
        }

        private static int[] Mix(int[] s)
        {
            int w16 = s[0], w17 = s[1], w3 = s[2], w4 = s[3], w5 = s[4], w7 = (w17 + 2 * w16) & 0xFF, w6 = s[5];
            w16 = (w17 + w16) & 0xFF;
            int w19 = s[6], w20 = (w4 + 2 * w3) & 0xFF;
            w17 = s[7]; w3 = (w4 + w3) & 0xFF;
            int w21 = s[8], w22 = (w6 + 2 * w5) & 0xFF;
            w4 = s[9]; w5 = (w6 + w5) & 0xFF;
            int w23 = s[10], w24 = (w17 + 2 * w19) & 0xFF;
            w6 = s[11]; w17 = (w17 + w19) & 0xFF;
            int w25 = s[12], w26 = (w4 + 2 * w21) & 0xFF;
            w19 = s[13]; w4 = (w4 + w21) & 0xFF;
            int w27 = s[14], w28 = (w6 + 2 * w23) & 0xFF;
            w21 = s[15]; w6 = (w6 + w23) & 0xFF;
            int w23a = (w19 + 2 * w25) & 0xFF; w19 = (w19 + w25) & 0xFF;
            w25 = (w21 + 2 * w27) & 0xFF; w21 = (w21 + w27) & 0xFF;
            w27 = (w6 + 2 * w26) & 0xFF; w6 = (w6 + w26) & 0xFF;
            w26 = (w21 + 2 * w23a) & 0xFF; w21 = (w21 + w23a) & 0xFF;
            w23a = (w16 + 2 * w20) & 0xFF; w16 = (w20 + w16) & 0xFF;
            w20 = (w5 + 2 * w24) & 0xFF; w5 = (w24 + w5) & 0xFF;
            w24 = (w4 + 2 * w28) & 0xFF; w4 = (w28 + w4) & 0xFF;
            w28 = (w19 + 2 * w25) & 0xFF; w19 = (w25 + w19) & 0xFF;
            w25 = (w17 + 2 * w7) & 0xFF; w17 = (w17 + w7) & 0xFF;
            w7 = (w3 + 2 * w22) & 0xFF; w3 = (w22 + w3) & 0xFF;
            w22 = (w19 + 2 * w24) & 0xFF; w19 = (w19 + w24) & 0xFF;
            w24 = (w3 + 2 * w25) & 0xFF; w3 = (w25 + w3) & 0xFF;
            w25 = (w16 + 2 * w20) & 0xFF; w16 = (w20 + w16) & 0xFF;
            w20 = (w4 + 2 * w28) & 0xFF; w4 = (w28 + w4) & 0xFF;
            w28 = (w17 + 2 * w7) & 0xFF; w17 = (w17 + w7) & 0xFF;
            int w30 = (w20 + w17) & 0xFF; w17 = (w17 + 2 * w20) & 0xFF;
            w7 = (w5 + 2 * w27) & 0xFF; w5 = (w27 + w5) & 0xFF;
            w27 = (w21 + 2 * w23a) & 0xFF; w21 = (w21 + w23a) & 0xFF;
            int sp0 = w17;
            w17 = (w19 + w24) & 0xFF; int w23b = (w26 + w6) & 0xFF;
            w19 = (w19 + 2 * w24) & 0xFF; int w20b = (w21 + w7) & 0xFF;
            w7 = (w21 + 2 * w7) & 0xFF;
            int sp5 = w17; w17 = (w23b + 2 * w25) & 0xFF;
            int sp4 = w19; w19 = (w4 + w28) & 0xFF;
            w4 = (w4 + 2 * w28) & 0xFF;
            int sp2 = w7, sp6 = w17;
            w17 = (w27 + w5) & 0xFF; w7 = (w23b + w25) & 0xFF;
            w5 = (w5 + 2 * w27) & 0xFF;
            int sp8 = w4; w4 = (w22 + w16) & 0xFF;
            w16 = (w16 + 2 * w22) & 0xFF;
            int sp11 = w17; w17 = (w6 + 2 * w26) & 0xFF;
            int sp1 = w30, sp13 = w4;
            w4 = (w17 + w3) & 0xFF;
            int sp12 = w16; w16 = (w3 + 2 * w17) & 0xFF;
            return new int[] { sp0, sp1, sp2, w20b, sp4, sp5, sp6, w7, sp8, w19, w5, sp11, sp12, sp13, w16, w4 };
        }

        private static int[] Subst(int[] x)
        {
            int[] o = new int[16];
            for (int i = 0; i < 16; i++)
            {
                if (i == 0 || i == 3 || i == 4 || i == 7 || i == 8 || i == 11 || i == 12 || i == 15)
                    o[i] = T1[x[i]] & 0xFF;
                else
                    o[i] = T2[x[i]] & 0xFF;
            }
            return o;
        }

        private static int[] KmCommon(int[] x, byte[] key)
        {
            int[] o = (int[])x.Clone();
            for (int j = 0; j < 16; j++)
            {
                if (Sel(j)) o[j] ^= (key[j] & 0xFF);
                else o[j] = (o[j] + (key[j] & 0xFF)) & 0xFF;
            }
            return o;
        }

        private static int[] KmSpecial(int[] x, int[] backup)
        {
            int[] o = (int[])x.Clone();
            for (int j = 0; j < 16; j++)
            {
                if (Sel(j)) o[j] ^= backup[j];
                else o[j] = (o[j] + backup[j]) & 0xFF;
            }
            return o;
        }

        private static int[] Km2(int[] x, byte[] key)
        {
            int[] o = (int[])x.Clone();
            for (int j = 0; j < 16; j++)
            {
                if (Sel(j)) o[j] = (o[j] + (key[j] & 0xFF)) & 0xFF;
                else o[j] ^= (key[j] & 0xFF);
            }
            return o;
        }

        private static byte[] RoundFunc(int[] state, byte[][] rk)
        {
            int[] x = (int[])state.Clone();
            int[] backup = (int[])x.Clone();
            for (int r = 0; r < 8; r++)
            {
                if (r == 2) x = KmSpecial(x, backup);
                x = KmCommon(x, rk[2 * r]);
                x = Subst(x);
                x = Km2(x, rk[2 * r + 1]);
                x = Mix(x);
            }
            x = KmCommon(x, rk[16]);
            byte[] o = new byte[16];
            for (int i = 0; i < 16; i++) o[i] = (byte)x[i];
            return o;
        }

        /// <summary>
        /// Gera a resposta de 16 bytes a partir do desafio recebido.
        /// </summary>
        public static byte[] E21(byte[] inp16)
        {
            byte[] ctx = PLACEHOLDER_ADDR;
            int[] o = new int[16];
            for (int i = 0; i < 6; i++) o[i] = ctx[i] & 0xFF;
            for (int i = 0; i < 6; i++) o[6 + i] = ctx[i] & 0xFF;
            for (int i = 0; i < 4; i++) o[12 + i] = ctx[i] & 0xFF;
            byte[] ksIn = (byte[])inp16.Clone();
            ksIn[15] ^= 6;
            byte[][] rk = KeySchedule(ksIn);
            return RoundFunc(o, rk);
        }
    }
}
