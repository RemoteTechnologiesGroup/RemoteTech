using System;
using UnityEngine;

namespace RemoteTech {
    public class LEM1802 : Tomato.Hardware.Device {
        public override uint DeviceID { get { return 0x7349f615; } }
        public override ushort Version { get { return 0x1802; } }
        public override uint ManufacturerID { get { return 0x1c6c8b36; } }
        public override string FriendlyName { get { return "LEM1802"; } }

        public Texture2D Texture {
            get {
                if (mTexture == null) {
                    mTexture = new Texture2D(Width, Height, TextureFormat.ARGB32, false);
                    mTexture.filterMode = FilterMode.Point;
                }
                UpdateTexture();
                return mTexture;
            }
        }

        private const int Width = 128;
        private const int Height = 96;
        private const int Columns = 32;
        private const int Rows = 12;
        private UInt16 mBorderColor;
        private UInt16 mVram;
        private UInt16 mFont;
        private UInt16 mPalette;
        private Texture2D mTexture;
        private int mLastFrame;

        // Left to right, column-major
        private static readonly UInt16[] DefaultFont = new UInt16[] {
            0xb79e, 0x388e, 0x722c, 0x75f4, 0x19bb, 0x7f8f, 0x85f9, 0xb158, 0x242e, 0x2400, 0x082a, 0x0800, 0x0008, 0x0000, 0x0808, 0x0808, 0x00ff,
            0x0000, 0x00f8, 0x0808, 0x08f8, 0x0000, 0x080f, 0x0000, 0x000f, 0x0808, 0x00ff, 0x0808, 0x08f8, 0x0808, 0x08ff, 0x0000, 0x080f, 0x0808,
            0x08ff, 0x0808, 0x6633, 0x99cc, 0x9933, 0x66cc, 0xfef8, 0xe080, 0x7f1f, 0x0701, 0x0107, 0x1f7f, 0x80e0, 0xf8fe, 0x5500, 0xaa00, 0x55aa,
            0x55aa, 0xffaa, 0xff55, 0x0f0f, 0x0f0f, 0xf0f0, 0xf0f0, 0x0000, 0xffff, 0xffff, 0x0000, 0xffff, 0xffff, 0x0000, 0x0000, 0x005f, 0x0000,
            0x0300, 0x0300, 0x3e14, 0x3e00, 0x266b, 0x3200, 0x611c, 0x4300, 0x3629, 0x7650, 0x0002, 0x0100, 0x1c22, 0x4100, 0x4122, 0x1c00, 0x1408,
            0x1400, 0x081c, 0x0800, 0x4020, 0x0000, 0x0808, 0x0800, 0x0040, 0x0000, 0x601c, 0x0300, 0x3e49, 0x3e00, 0x427f, 0x4000, 0x6259, 0x4600,
            0x2249, 0x3600, 0x0f08, 0x7f00, 0x2745, 0x3900, 0x3e49, 0x3200, 0x6119, 0x0700, 0x3649, 0x3600, 0x2649, 0x3e00, 0x0024, 0x0000, 0x4024,
            0x0000, 0x0814, 0x2241, 0x1414, 0x1400, 0x4122, 0x1408, 0x0259, 0x0600, 0x3e59, 0x5e00, 0x7e09, 0x7e00, 0x7f49, 0x3600, 0x3e41, 0x2200,
            0x7f41, 0x3e00, 0x7f49, 0x4100, 0x7f09, 0x0100, 0x3e41, 0x7a00, 0x7f08, 0x7f00, 0x417f, 0x4100, 0x2040, 0x3f00, 0x7f08, 0x7700, 0x7f40,
            0x4000, 0x7f06, 0x7f00, 0x7f01, 0x7e00, 0x3e41, 0x3e00, 0x7f09, 0x0600, 0x3e41, 0xbe00, 0x7f09, 0x7600, 0x2649, 0x3200, 0x017f, 0x0100,
            0x3f40, 0x3f00, 0x1f60, 0x1f00, 0x7f30, 0x7f00, 0x7708, 0x7700, 0x0778, 0x0700, 0x7149, 0x4700, 0x007f, 0x4100, 0x031c, 0x6000, 0x0041,
            0x7f00, 0x0201, 0x0200, 0x8080, 0x8000, 0x0001, 0x0200, 0x2454, 0x7800, 0x7f44, 0x3800, 0x3844, 0x2800, 0x3844, 0x7f00, 0x3854, 0x5800,
            0x087e, 0x0900, 0x4854, 0x3c00, 0x7f04, 0x7800, 0x447d, 0x4000, 0x2040, 0x3d00, 0x7f10, 0x6c00, 0x417f, 0x4000, 0x7c18, 0x7c00, 0x7c04,
            0x7800, 0x3844, 0x3800, 0x7c14, 0x0800, 0x0814, 0x7c00, 0x7c04, 0x0800, 0x4854, 0x2400, 0x043e, 0x4400, 0x3c40, 0x7c00, 0x1c60, 0x1c00,
            0x7c30, 0x7c00, 0x6c10, 0x6c00, 0x4c50, 0x3c00, 0x6454, 0x4c00, 0x0836, 0x4100, 0x0077, 0x0000, 0x4136, 0x0800, 0x0201, 0x0201, 0x0205,
            0x0200
        };

        private static readonly UInt16[] DefaultPalette = new UInt16[] { 
            0x000, 0x00A, 0x0A0, 0x0AA, 0xA00, 0xA0A, 0xA50, 0xAAA,
            0x555, 0x55F, 0x5F5, 0x5FF, 0xF55, 0xF5F, 0xFF5, 0x0FF,
        };

        public override int HandleInterrupt() {
            switch (AttachedCPU.A) {
                case 0:
                    mVram = AttachedCPU.B;
                    return 0;
                case 1:
                    mFont = AttachedCPU.B;
                    return 0;
                case 2:
                    mPalette = AttachedCPU.B;
                    return 0;
                case 3:
                    mBorderColor = (UInt16)(AttachedCPU.B & 0xF);
                    return 0;
                case 4:
                    Array.Copy(DefaultFont, 0, AttachedCPU.Memory, AttachedCPU.B, DefaultFont.Length);
                    return 256;
                case 5:
                    Array.Copy(DefaultPalette, 0, AttachedCPU.Memory, AttachedCPU.B, DefaultPalette.Length);
                    return 16;
                default:
                    return 0;
            }
        }

        public override void Reset() {
            mVram = 0x8000;
            mFont = 0;
            mPalette = 0;
        }

        private Color32 GetColor(byte colorIndex) {
            UInt16 data = (mPalette == 0)
                ? DefaultPalette[colorIndex]
                : AttachedCPU.Memory[(UInt16)(mPalette + colorIndex)];
            return new Color32(
                r: (byte)(((data >> 8) & 0x0F) | ((data >> 4) & 0xF0)),
                g: (byte)(((data >> 4) & 0x0F) | ((data >> 0) & 0xF0)),
                b: (byte)(((data >> 0) & 0x0F) | ((data << 4) & 0xF0)),
                a: 0xFF);
        }

        private UInt32 GetFont(byte fontIndex) {
            return mFont == 0 
                ? (UInt32) ((DefaultFont[(UInt16)(fontIndex * 2)]) |
                            (DefaultFont[(UInt16)(fontIndex * 2 + 1)] << 16))
                : (UInt32)((AttachedCPU.Memory[(UInt16)(mFont + fontIndex * 2)]) |
                            (AttachedCPU.Memory[(UInt16)(mFont + fontIndex * 2 + 1)] << 16));
        }

        public override void Tick() {
            return;
        }

        private Color32 GetForeground(UInt16 cell) {
            return GetColor((byte) ((cell >> 12) & 0xF));
        }

        private Color32 GetBackground(UInt16 cell) {
            return GetColor((byte) ((cell >> 8) & 0xF));
        }

        private static byte GetCharacter(UInt16 cell) {
            return (byte) (cell & 0x007F);
        }

        private static bool IsBlinking(UInt16 cell) {
            return (cell & 0x0080) > 0;
        }

        private static void Clear(Texture2D tex) {
            for (int i = 0; i < tex.width; i++) {
                for (int j = 0; j < tex.height; j++) {
                    tex.SetPixel(i, j, Color.black);
                }
            }
            tex.Apply();
        }

        private void UpdateTexture() {
            var pixels = new Color32[Width * Height];
            for (int i = 0; i < Columns * Rows; i++) {
                var cell = (mVram == 0) ? (UInt16)0x0000 : AttachedCPU.Memory[(UInt16)(mVram + i)];
                WriteCharacter(pixels, i, cell);
            }
            mTexture.SetPixels32(pixels);
            mTexture.Apply();
        }

        private void WriteCharacter(Color32[] pixels, int i, UInt16 cell) {
            var c_width = (Width / Columns);
            var c_height = (Height / Rows);
            var chr = GetCharacter(cell);
            var blink = IsBlinking(cell);
            var font = GetFont(chr);

            int offset_x = (i % Columns) * c_width;
            for (int x = 0; x < c_width; x++) {
                int offset_y = (i / Columns) * c_height;
                for (int y = 0; y < c_height; y++) {
                    int j = x + offset_x + (Height - (y + offset_y) - 1) * Width;
                    Color32 c = (font >> (y + c_height*((1-x)%4)) & 0x1) == 1 
                        ? GetForeground(cell) 
                        : GetBackground(cell);
                    pixels[j] = c;
                }
            }
        }
    }
}