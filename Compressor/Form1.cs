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

namespace Compressor
{
    public class HuffmanNode
    {
        public byte mValue;
        public int mFreq;
        public HuffmanNode mRight;
        public HuffmanNode mLeft;

        public HuffmanNode()
        {
            mValue = 0;
            mFreq = 0;
            mRight = null;
            mLeft = null;
        }

        public void DeleteNode()
        {
            mValue = 0;
            mFreq = 0;
            mRight = null;
            mLeft = null;
        }
    }

    public partial class Form1 : Form
    {
        HuffmanNode[] huffList = new HuffmanNode[256];
        HuffmanNode headNode = new HuffmanNode();

        FileStream fsin;
        FileStream fsout;

        int[] freq = new int[256];
        int nodeCount;

        uint[] huffCode = new uint[256];
        int[] huffLength = new int[256];

        string fileName;

        uint cmpByte = 0;
        int cmpBit = 7;

        uint extByte = 0;
        int extBit = -1;

        long oriFileSize;

        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            OpenFileDialog fd = new OpenFileDialog();

            if (fd.ShowDialog() == DialogResult.OK)
            {
                label1.Text = fd.FileName;
                fileName = fd.FileName.Substring(fd.FileName.LastIndexOf("\\") + 1, fd.FileName.IndexOf(".") - fd.FileName.LastIndexOf("\\") - 1);

                fsin = new FileStream(fd.FileName, FileMode.Open);

                fsin.Seek(0, SeekOrigin.Begin);
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            SaveFileDialog fd = new SaveFileDialog();
            fd.FileName = fileName;
            fd.DefaultExt = "msrcmp";

            if (fd.ShowDialog() == DialogResult.OK)
            {
                fsout = new FileStream(fd.FileName, FileMode.Create);
                fsout.Seek(0, SeekOrigin.Begin);
                Compress();

                DeleteHuffTree();

                fsin.Dispose();
                fsout.Dispose();

                fsin.Close();
                fsout.Close();

                MessageBox.Show("압축이 완료되었습니다.");
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {

            SaveFileDialog fd = new SaveFileDialog();
            fd.FileName = fileName;

            if (fd.ShowDialog() == DialogResult.OK)
            {
                fsout = new FileStream(fd.FileName, FileMode.Create);
                fsout.Seek(0, SeekOrigin.Begin);
                Extract();

                DeleteHuffTree();

                fsin.Dispose();
                fsout.Dispose();

                fsin.Close();
                fsout.Close();

                MessageBox.Show("압축 해제가 완료되었습니다.");
            }
        }

        private void DeleteHuffTree()
        {
            Stack<HuffmanNode> nodes = new Stack<HuffmanNode>();
            nodes.Push(headNode);

            while (nodes.Count > 0)
            {
                HuffmanNode i = nodes.Pop();

                if (i.mRight !=  null)
                    nodes.Push(i.mRight);

                if (i.mLeft != null)
                    nodes.Push(i.mLeft);

                i.DeleteNode();
            }

            for (int i = 0; i < 256; i++)
            {
                huffList[i] = null;
                freq[i] = 0;
                huffCode[i] = 0;
                huffLength[i] = 0;
            }

            cmpByte = 0;
            cmpBit = 7;

            extByte = 0;
            extBit = -1;
        }

        private void Calcfrequency()
        {
            do
            {
                int value = fsin.ReadByte();
                freq[value]++;
            }
            while (fsin.Position < fsin.Length);


            fsin.Seek(0, SeekOrigin.Begin);
        }

        private int FindMinFrequency(int index)
        {
            int minIndex = 0;

            for (int i = 0; i < index; i++)
            {
                if (huffList[i].mFreq < huffList[minIndex].mFreq)
                    minIndex = i;
            }

            return minIndex;
        }

        public void MakeHuffmanTree()
        {
            nodeCount = 0;

            for (int i = 0; i < 256; i++)
            {
                if (freq[i] > 0)
                {
                    HuffmanNode node = new HuffmanNode();
                    node.mValue = (byte)i;
                    node.mFreq = freq[i];
                    node.mRight = null;
                    node.mLeft = null;

                    nodeCount++;

                    huffList[nodeCount - 1] = node;
                }
            }

            int head = nodeCount;

            while (head > 1)
            {
                int min = FindMinFrequency(head);
                HuffmanNode node1 = huffList[min];

                head--;
                huffList[min] = huffList[head];

                min = FindMinFrequency(head);
                HuffmanNode node2 = huffList[min];

                HuffmanNode node = new HuffmanNode();

                node.mValue = 0;
                node.mFreq = node1.mFreq + node2.mFreq;
                node.mLeft = node1;
                node.mRight = node2;

                huffList[min] = node;
            }

            headNode = huffList[0];
        }

        public void MakeCode(HuffmanNode node, uint code, int length)
        {
            if (node.mLeft == null && node.mRight == null)
            {
                huffCode[node.mValue] = code;
                huffLength[node.mValue] = length;
            }
            else
            {
                code = code << 1;
                length++;
                MakeCode(node.mLeft, code, length);

                code = code | 1u;
                MakeCode(node.mRight, code, length);
                code = code >> 1;
                length--;
            }
        }

        public void WriteByteFromBit(uint code, bool flush)
        {
            if (cmpBit < 0 || flush == true)
            {
                fsout.WriteByte((byte)cmpByte);
                cmpBit = 7;
                cmpByte = 0;
            }

            cmpByte = cmpByte | code << (cmpBit--);
        }

        public bool Compress()
        {
            Calcfrequency();

            MakeHuffmanTree();

            MakeCode(headNode, 0u, 0);

            for (int i = 0; i < 256; i++)
            {
                fsout.WriteByte((byte)huffLength[i]);
                fsout.WriteByte((byte)huffCode[i]);
            }

            long fileSize = fsin.Length;
            byte[] result = new byte[8];

            for (int i = 7; i >= 0; i--)
            {
                result[i] = (byte)(fileSize & 0xFF);
                fileSize >>= 8;
            }

            fsout.Write(result,0, result.Length);

            do
            {
                int index = fsin.ReadByte();

                for (int i = huffLength[index] - 1; i >= 0; i--)
                {
                    uint bit = 0;
                    bit = (huffCode[index] >> i) & ~(~0 << 1);
                    WriteByteFromBit(bit, false);
                }
            }
            while (fsin.Position < fsin.Length);


            WriteByteFromBit(0, true);

            return true;
        }

        bool ReadBitFromFile(ref uint uBit)
        {
            if (extBit < 0)
            {
                extByte = (uint)fsin.ReadByte();
                extBit = 7;
            }

            if (fsout.Position > oriFileSize)
                return false;

            uint bit = 0;
            bit = (extByte >> extBit) & ~(~0 << 1);
            extBit--;

            uBit = bit;

            return true;
        }

        void InsertIntoHuffTree(int value)
        {
            try
            {
                if (headNode == null)
                {
                    headNode = new HuffmanNode();
                }

                HuffmanNode curNode = headNode;

                int length = huffLength[value];
                uint code = huffCode[value];

                while (length > 0)
                {
                    uint bit = (code >> length - 1) & ~(~0 << 1);

                    if (bit == 0)
                    {
                        if (curNode.mLeft == null)
                        {
                            curNode.mLeft = new HuffmanNode();
                        }
                        curNode = curNode.mLeft;
                    }
                    else
                    {
                        if (curNode.mRight == null)
                        {
                            curNode.mRight = new HuffmanNode();
                        }
                        curNode = curNode.mRight;
                    }

                    length--;
                }

                curNode.mValue = (byte)value;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace + ", (" + ex.Message + ")");
            }
        }

        bool Extract()
        {
            fsin.Seek(0, SeekOrigin.Begin);

            for (int i = 0; i < 256; i++)
            {
                huffLength[i] = fsin.ReadByte();
                huffCode[i] = (byte)fsin.ReadByte();

                if (huffLength[i] > 0)
                    InsertIntoHuffTree(i);
            }

            oriFileSize = 0;

            for (int i = 0; i < 8; i++)
            {
                oriFileSize <<= 8;
                oriFileSize |= (long)(fsin.ReadByte() & 0xFF);
            }

            uint bit = 0;
            HuffmanNode node = headNode;

            ReadBitFromFile(ref bit);

            do
            {
                while (node.mLeft != null && node.mRight != null)
                {
                    if (bit == 0)
                        node = node.mLeft;
                    else
                        node = node.mRight;

                    ReadBitFromFile(ref bit);
                }

                fsout.WriteByte(node.mValue);
                node = headNode;
            }
            while (fsout.Position < oriFileSize);

            return true;
        }
    }
}
