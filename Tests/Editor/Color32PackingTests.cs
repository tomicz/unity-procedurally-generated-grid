using NUnit.Framework;
using UnityEngine;

namespace TOMICZ.Grid.Tests
{
    public class Color32PackingTests
    {
        [Test]
        public void Pack_WritesFourBytesPerColorInRgbaOrder()
        {
            var colors = new[] { new Color32(1, 2, 3, 4), new Color32(250, 251, 252, 253) };

            byte[] bytes = Color32Packing.Pack(colors);

            CollectionAssert.AreEqual(new byte[] { 1, 2, 3, 4, 250, 251, 252, 253 }, bytes);
        }

        [Test]
        public void Unpack_RoundTripsPack()
        {
            var colors = new Color32[37];
            for (int i = 0; i < colors.Length; i++)
            {
                colors[i] = new Color32((byte)i, (byte)(i * 3), (byte)(255 - i), (byte)(i * 7));
            }

            Color32[] restored = Color32Packing.Unpack(Color32Packing.Pack(colors));

            Assert.AreEqual(colors.Length, restored.Length);
            for (int i = 0; i < colors.Length; i++)
            {
                Assert.AreEqual(colors[i].r, restored[i].r);
                Assert.AreEqual(colors[i].g, restored[i].g);
                Assert.AreEqual(colors[i].b, restored[i].b);
                Assert.AreEqual(colors[i].a, restored[i].a);
            }
        }

        [Test]
        public void PackAndUnpack_ReuseCorrectlySizedBuffers()
        {
            var colors = new Color32[5];
            var bytes = new byte[20];
            var target = new Color32[5];

            Assert.AreSame(bytes, Color32Packing.Pack(colors, bytes));
            Assert.AreSame(target, Color32Packing.Unpack(bytes, target));
            Assert.AreNotSame(bytes, Color32Packing.Pack(colors, new byte[8]));
            Assert.AreNotSame(target, Color32Packing.Unpack(bytes, new Color32[2]));
        }

        [Test]
        public void NullAndEmpty_ProduceEmptyArrays()
        {
            Assert.AreEqual(0, Color32Packing.Pack(null).Length);
            Assert.AreEqual(0, Color32Packing.Unpack(null).Length);
            Assert.AreEqual(0, Color32Packing.Unpack(new byte[0]).Length);
        }
    }
}
