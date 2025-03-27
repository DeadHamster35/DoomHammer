using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Button;

namespace DoomHammer
{
    public partial class DoomHammerMain : Form
    {

        public class MapSection
        {
            public string Name { get; set; }
            public int Offset { get; set; }
            public byte[] Data { get; set; }
            public int Size { get; set; }

        }

        List<MapSection> SectionList = new List<MapSection>();
        byte[] TileData = new byte[0];
        byte[][] TileIndex = new byte[0][];
        byte[][] TileFlags = new byte[0][];

        byte[] SQMData = new byte[0];
        Int16[][] SQMTiles = new Int16[0][];
        byte[] REGMData = new byte[0];
        Int16[][] REGMTiles = new Int16[0][];


        Bitmap MainTexture = new Bitmap(1, 1);
        float Zoom;
        int Zoomies = 25;
        public DoomHammerMain()
        {
            InitializeComponent();

            
        }

        /*

                filled  clear
        0x      Dx      upper left
        1x      Cx      upper right
        2x      Bx      upper half
        3x      Ax      lower left
        4x      9x      left half
        7x      6x      lower right
        8x      5x      upper left, lower right


        */
        byte[] FlipYTable =
        {
            0x3,
            0x7,
            0xB,
            0x0,
            0x4,
            0x8,
            0xC,
            0x1,
            0x5,
            0x9,
            0xD,
            0x2,
            0x6,
            0xA
        };
        byte[] FlipYWallTable =
        {
            0x3,
            0x1,
            0x5,
            0x0,
            0x4,
            0x2,
            0x6,
            0x7,
            0xb,
            0x9,
            0xD,
            0x8,
            0xC,
            0xA
        };
        /*

               filled  clear
       0x      Dx      upper left
       1x      Cx      upper right
       2x      Bx      upper half
       3x      Ax      lower left
       4x      9x      left half
       7x      6x      lower right
       8x      5x      upper left, lower right


       */
        byte[] FlipXTable =
        {
            0x1,
            0x0,
            0x2,
            0x7,
            0x9,
            0x8,
            0xA,
            0x3,
            0x5,
            0x4,
            0x6,
            0xB,
            0xD,
            0xC
        };

        byte[] FlipXWallTable =
        {
            0x0,
            0x7,
            0x8,
            0x3,
            0x4,
            0xB,
            0xC,
            0x1,
            0x2,
            0x9,
            0xA,
            0x5,
            0x6,
            0xD
        };


        /*
                boundry tiles

        09..            orc wall
        08..            human wall
        07..            forest and grass
        06..            dark grass and grass
        05..            coast and grass
        04..            mount and coast
        03..            dark coast and coast
        02..            water and coast
        01..            dark water and water

        */


        int[][] BoundaryColors = new int[][]
        {
            new int[3] {255, 255, 255 },//N/A
            new int[3] {64, 96, 255 },//water darkwater
            new int[3] {64, 32, 255 },//water coast
            new int[3] {144, 80, 0 },//darkcoast-coast
            new int[3] {192, 128, 128 },//mount-coast
            new int[3] {192, 128, 0 },//coast-grass            
            new int[3] {0, 160, 0 },//darkgrass-grass
            new int[3] {0, 128, 0 },//forestgrass
            new int[3] {192, 192, 192 },//human
            new int[3] {64, 64, 64 },//orc
        };
        /*        
        001x            light water
        002x            dark water
        003x            light coast
        004x            dark coast
        005x            light ground
        006x            dark ground
        007x            forest
        008x            mountains
        009x            human wall
        00ax            orc walls
        00bx            human walls
        00cx            orc walls
        */
        int[][] TileColors = new int[][]
        {
            new int[3] {0, 128, 255 },//water
            new int[3] {0, 32, 255 },//darkwater
            new int[3] {176, 112, 0 },//coast
            new int[3] {128, 64, 0 },//darkcoast
            new int[3] {0, 192, 0 },//grass
            new int[3] {0, 144, 0 },//dark grass
            new int[3] {0, 96, 0 },//forest
            new int[3] {128, 128, 128 },//mountain
            new int[3] {192, 192, 192 },//human wall
            new int[3] {64, 64, 64 },//orc wall
            new int[3] {192, 192, 192 },//human wall
            new int[3] {64, 64, 64 },//orc wall
        };public void WriteTileData()
        {
            MemoryStream memoryStream = new MemoryStream();
            BinaryWriter binaryWriter = new BinaryWriter(memoryStream);

            for (int ThisY = 0; ThisY < SizeY; ThisY++)
            {
                for (int ThisX = 0; ThisX < SizeX; ThisX++)
                {
                    int Index = ThisX + (ThisY * SizeX);
                    binaryWriter.Write(TileIndex[ThisY][ThisX]);
                    binaryWriter.Write(TileFlags[ThisY][ThisX]);
                }
            }
            TileData = memoryStream.ToArray();

            memoryStream = new MemoryStream();
            binaryWriter = new BinaryWriter(memoryStream);

            for (int ThisY = 0; ThisY < SizeY; ThisY++)
            {
                for (int ThisX = 0; ThisX < SizeX; ThisX++)
                {
                    binaryWriter.Write(SQMTiles[ThisY][ThisX]);
                }
            }
            SQMData = memoryStream.ToArray();

            memoryStream = new MemoryStream();
            binaryWriter = new BinaryWriter(memoryStream);

            for (int ThisY = 0; ThisY < SizeY; ThisY++)
            {
                for (int ThisX = 0; ThisX < SizeX; ThisX++)
                {
                    binaryWriter.Write(REGMTiles[ThisY][ThisX]);
                }
            }
            REGMData = memoryStream.ToArray();
        }

        bool CheckWallBoundary(int Tile)
        {
            /*
                       09..            orc wall
                        08..            human wall
            */
            if ((Tile == 8) || (Tile == 9))
            {
                return true;
            }


            return false;
        }


        private void RefreshDraw()
        {
            Zoom = Zoomies * .01f;

            MainTexture = new Bitmap((int)(SizeX * (int)(32 * Zoom)), (int)(SizeY * (int)(32 * Zoom)));
            using (Graphics graphics = Graphics.FromImage(MainTexture))
            {
                // Clear the bitmap with a white background
                graphics.Clear(Color.White);

                // Define the font, brush, and position
                Font font = new Font("Arial", (Convert.ToSingle(Zoom * 5)), GraphicsUnit.Pixel);
                Brush brush = Brushes.Black;


                // Draw the text onto the bitmap

                MemoryStream memoryStream = new MemoryStream(TileData);
                BinaryReader binaryReader = new BinaryReader(memoryStream);

                for (int ThisY = 0; ThisY < SizeY; ThisY++)
                {
                    TileIndex[ThisY] = new byte[SizeX];
                    TileFlags[ThisY] = new byte[SizeX];
                    for (int ThisX = 0; ThisX < SizeX; ThisX++)
                    {
                        int Index = ThisX + (ThisY * SizeX);
                        TileIndex[ThisY][ThisX] = binaryReader.ReadByte();
                        TileFlags[ThisY][ThisX] = binaryReader.ReadByte();

                        int Offset = TileIndex[ThisY][ThisX].ToString().Length;
                        if (TileFlags[ThisY][ThisX] == 0)
                        {
                            int[] ColorArray = TileColors[(int)(TileIndex[ThisY][ThisX] / 16) - 1];

                            Color color = Color.FromArgb(ColorArray[0], ColorArray[1], ColorArray[2]);
                            Brush BrushColor = new SolidBrush(color);
                            Rectangle square = new Rectangle((ThisX * (int)(Zoom * 32)), (ThisY * (int)(Zoom * 32)), (int)(Zoom * 32), (int)(Zoom * 32));
                            graphics.FillRectangle(BrushColor, square);
                        }
                        else
                        {
                            int[] ColorArray = BoundaryColors[(int)(TileFlags[ThisY][ThisX])];

                            Color color = Color.FromArgb(ColorArray[0], ColorArray[1], ColorArray[2]);
                            Brush BrushColor = new SolidBrush(color);
                            Rectangle square = new Rectangle((ThisX * (int)(Zoom * 32)), (ThisY * (int)(Zoom * 32)), (int)(Zoom * 32), (int)(Zoom * 32));
                            graphics.FillRectangle(BrushColor, square);
                        }

                    }
                }
            }
        }

        int SizeX = 0, SizeY = 0;

        private void button2_Click(object sender, EventArgs e)
        {
            SaveFileDialog FileSave = new SaveFileDialog();
            if (FileSave.ShowDialog() == DialogResult.OK)
            {
                MemoryStream memoryStream = new MemoryStream();
                BinaryWriter binaryWriter = new BinaryWriter(memoryStream);
                for (int ThisSection = 0; ThisSection < SectionList.Count; ThisSection++)
                {
                    if (SectionList[ThisSection].Name == "MTXM")
                    {
                        SectionList[ThisSection].Data = TileData;
                    }
                    if (SectionList[ThisSection].Name == "SQM ")
                    {
                        SectionList[ThisSection].Data = SQMData;
                    }
                    if (SectionList[ThisSection].Name == "REGM")
                    {
                        SectionList[ThisSection].Data = REGMData;
                    }
                    char[] parse = SectionList[ThisSection].Name.ToCharArray();
                    binaryWriter.Write(parse[0]);
                    binaryWriter.Write(parse[1]);
                    binaryWriter.Write(parse[2]);
                    binaryWriter.Write(parse[3]);

                    binaryWriter.Write(SectionList[ThisSection].Size);

                    binaryWriter.Write(SectionList[ThisSection].Data);
                }


                File.WriteAllBytes(FileSave.FileName, memoryStream.ToArray());
            }
        }

        private void button6_Click(object sender, EventArgs e)
        {
            Zoomies += 3;
            if (Zoomies <= 0)
            {
                Zoomies -= 3;
            }

            RefreshDraw();
            ImageHolster.Height = MainTexture.Height;
            ImageHolster.Width = MainTexture.Width;
            ImageHolster.Image = MainTexture;
            ImageHolster.Refresh();
        }

        private void button5_Click(object sender, EventArgs e)
        {
            Zoomies -= 3;
            if (Zoomies <= 0)
            {
                Zoomies += 3;
            }

            RefreshDraw();
            ImageHolster.Height = MainTexture.Height;
            ImageHolster.Width = MainTexture.Width;
            ImageHolster.Image = MainTexture;
            ImageHolster.Refresh();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            //flipx
            for (int ThisY = 0; ThisY < SizeY; ThisY++)
            {

                for (int ThisX = 0; ThisX < (SizeX / 2); ThisX++)
                {
                    int Target = (SizeX - ThisX - 1);
                    if (TileFlags[ThisY][ThisX] == 0)
                    {
                        TileIndex[ThisY][Target] = TileIndex[ThisY][ThisX];
                        TileFlags[ThisY][Target] = TileFlags[ThisY][ThisX];
                        SQMTiles[ThisY][Target] = SQMTiles[ThisY][ThisX];
                        REGMTiles[ThisY][Target] = SQMTiles[ThisY][ThisX];
                    }
                    else
                    {
                        int Targ;
                        if (CheckWallBoundary(TileFlags[ThisY][ThisX]))
                        {
                            Targ = Convert.ToInt32(FlipXWallTable[(TileIndex[ThisY][ThisX] / 16)]);
                        }
                        else
                        {
                            Targ = Convert.ToInt32(FlipXTable[(TileIndex[ThisY][ThisX] / 16)]);
                        }


                        TileIndex[ThisY][Target] = Convert.ToByte(Convert.ToInt32(16 * Targ));
                        TileFlags[ThisY][Target] = TileFlags[ThisY][ThisX];
                        SQMTiles[ThisY][Target] = SQMTiles[ThisY][ThisX];
                        REGMTiles[ThisY][Target] = SQMTiles[ThisY][ThisX];

                    }

                }
            }


            WriteTileData();
            RefreshDraw();
            ImageHolster.Height = MainTexture.Height;
            ImageHolster.Width = MainTexture.Width;
            ImageHolster.Image = MainTexture;
            ImageHolster.Refresh();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            //flipy
            for (int ThisY = 0; ThisY < (SizeY / 2); ThisY++)
            {
                int Target = SizeY - ThisY - 1;
                for (int ThisX = 0; ThisX < (SizeX); ThisX++)
                {

                    if (TileFlags[ThisY][ThisX] == 0)
                    {
                        TileIndex[Target][ThisX] = TileIndex[ThisY][ThisX];
                        TileFlags[Target][ThisX] = TileFlags[ThisY][ThisX];
                        SQMTiles[Target][ThisX] = SQMTiles[ThisY][ThisX];
                        REGMTiles[Target][ThisX] = SQMTiles[ThisY][ThisX];
                    }
                    else
                    {
                        int Targ;
                        if (CheckWallBoundary(TileFlags[ThisY][ThisX]))
                        {
                            Targ = Convert.ToInt32(FlipYWallTable[(TileIndex[ThisY][ThisX] / 16)]);
                        }
                        else
                        {
                            Targ = Convert.ToInt32(FlipYTable[(TileIndex[ThisY][ThisX] / 16)]);
                        }

                        TileIndex[Target][ThisX] = Convert.ToByte(Convert.ToInt32(16 * Targ));
                        TileFlags[Target][ThisX] = TileFlags[ThisY][ThisX];
                        SQMTiles[Target][ThisX] = SQMTiles[ThisY][ThisX];
                        REGMTiles[Target][ThisX] = SQMTiles[ThisY][ThisX];
                    }

                }
            }


            WriteTileData();
            RefreshDraw();
            ImageHolster.Height = MainTexture.Height;
            ImageHolster.Width = MainTexture.Width;
            ImageHolster.Image = MainTexture;
            ImageHolster.Refresh();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Zoom = Zoomies * .01f;
            OpenFileDialog FileOpen = new OpenFileDialog();
            if (FileOpen.ShowDialog() == DialogResult.OK)
            {
                TitleBar.Text = Path.GetFileNameWithoutExtension(FileOpen.FileName);
                byte[] FileData = File.ReadAllBytes(FileOpen.FileName);
                //var MapData = Kaitai.Warcraft2Pud.FromFile(FileOpen.FileName);
                SectionList = new List<MapSection>();

                MemoryStream memoryStream = new MemoryStream(FileData);
                BinaryReader binaryReader = new BinaryReader(memoryStream);
                while (true)
                {
                    MapSection Section = new MapSection();
                    Section.Offset = Convert.ToInt32(binaryReader.BaseStream.Position);
                    char[] parse = binaryReader.ReadChars(4);
                    Section.Name = new string(parse);
                    Section.Size = binaryReader.ReadInt32();
                    Section.Data = binaryReader.ReadBytes(Section.Size);
                    SectionList.Add(Section);
                    if (binaryReader.BaseStream.Position == binaryReader.BaseStream.Length)
                    {
                        break;
                    }
                }


                if (FileData != null)
                {

                    for (int ThisSection = 0; ThisSection < SectionList.Count; ThisSection++)
                    {

                        string Name = SectionList[ThisSection].Name;

                        if (Name == "DIM ")
                        {
                            memoryStream = new MemoryStream(SectionList[ThisSection].Data);
                            binaryReader = new BinaryReader(memoryStream);

                            SizeX = binaryReader.ReadInt16();
                            SizeY = binaryReader.ReadInt16();
                            MainTexture = new Bitmap((int)(SizeX * 32 * Zoom), (int)(SizeY * 32 * Zoom));
                        }

                        if (Name == "SQM ")
                        {
                            SQMData = SectionList[ThisSection].Data;


                            memoryStream = new MemoryStream(SQMData);
                            binaryReader = new BinaryReader(memoryStream);

                            SQMTiles = new Int16[SizeY][];

                            for (int ThisY = 0; ThisY < SizeY; ThisY++)
                            {
                                SQMTiles[ThisY] = new Int16[SizeX];
                                for (int ThisX = 0; ThisX < SizeX; ThisX++)
                                {
                                    SQMTiles[ThisY][ThisX] = binaryReader.ReadInt16();
                                }
                            }
                        }

                        if (Name == "REGM")
                        {
                            REGMData = SectionList[ThisSection].Data;


                            memoryStream = new MemoryStream(REGMData);
                            binaryReader = new BinaryReader(memoryStream);

                            REGMTiles = new Int16[SizeY][];

                            for (int ThisY = 0; ThisY < SizeY; ThisY++)
                            {
                                REGMTiles[ThisY] = new Int16[SizeX];
                                for (int ThisX = 0; ThisX < SizeX; ThisX++)
                                {
                                    REGMTiles[ThisY][ThisX] = binaryReader.ReadInt16();
                                }
                            }
                        }


                        if (Name == "MTXM")
                        {
                            byte[] Local = (byte[])SectionList[ThisSection].Data;
                            TileData = Local;
                            TileIndex = new byte[SizeY][];
                            TileFlags = new byte[SizeY][];
                            RefreshDraw();

                            ImageHolster.Height = MainTexture.Height;
                            ImageHolster.Width = MainTexture.Width;
                            ImageHolster.Image = MainTexture;
                        }
                    }
                }
            }
        }
    }
}
