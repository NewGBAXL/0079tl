﻿using InMemoryBinaryFile.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Formats.Tar;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using U8;
using XBFLib;

namespace Tests
{
    public class XbfTests
    {
        //string testfile = @"C:\DEV\0079tl\Anim_BRF_PhotoA_00.xbf.bin";
        //string testfile = @"E:\0079_jp\DATA\files\parameter\result_param_format.xbf";
        //string testfile = @"C:\DEV\0079tl\FlagCtrl.xbf.bin";
        //string testfile = @"C:\DEV\0079tl\combat_param.xbf.bin";
        string testfile = @"C:\games\wii\0079\0079_en\DATA\files\_2d\Title\Title_text.arc._tempdir\arc\ACE.arc\arc\BlockText.xbf";

        [Fact]
        public void EncodingTest()
        {
            //d_cannon_<0xef><0xbd><0x92> gets malformed upon save
            var expected = File.ReadAllBytes(@"C:\DEV\0079tl\combat_param.xbf.bin");

            var csharpstr = "d_cannon\xEF\xBD\x92"; //probably Windows 1250

            var bytes = expected.AsSpan().FindNullTerminator(0x01_C8_D2);

            var asciiStr = bytes.ToAsciiString();
            var shiftJisStr = bytes.ToShiftJisString();
            var w1250 = bytes.ToW1250String();

            var b1 = bytes.ToArray();

            string encod = "";

            foreach (var ei in System.Text.Encoding.GetEncodings())
            {
                var s = ei.GetEncoding().GetString(bytes);
                var b2 = ei.GetEncoding().GetBytes(s);
                var b3 = ei.GetEncoding().GetBytes(csharpstr);

                if (s == csharpstr)
                {
                    Debugger.Break();
                }

                if (Enumerable.SequenceEqual(b1, b2))
                {
                    encod += $"{ei.Name} {ei.CodePage}{Environment.NewLine}";
                }

                if (Enumerable.SequenceEqual(b1, b3))
                {
                    Debugger.Break();
                }

            }

            var b4 = Encoding.GetEncoding(1250).GetBytes(Encoding.GetEncoding(1250).GetString(bytes));
            var sss = Encoding.GetEncoding(1250).GetString(bytes);
            var b5 = Encoding.GetEncoding(1250).GetBytes(sss);

            var s6 = EncodingHelper.Windows1250.GetString(bytes);
            var b6 = EncodingHelper.Windows1250.GetBytes(s6);
            var s7 = EncodingHelper.Shift_JIS.GetString(bytes);
            var b7 = EncodingHelper.Shift_JIS.GetBytes(s6);

            //can't represent that legacy crap in crappy c# unicode
            Assert.NotEqual(csharpstr, asciiStr);
            Assert.NotEqual(csharpstr, shiftJisStr);
            Assert.NotEqual(csharpstr, w1250);
            Assert.Equal(b1, b4);
            Assert.Equal(b1, b5);
            Assert.Equal(b1, b6);
            //Assert.Equal(b1, b7);
        }

        [Fact]
        public void ReadWriteLoop()
        {
            var expected = File.ReadAllBytes(testfile);

            var parsed = new XbfRootSegment();
            parsed.Parse(expected.AsSpan());

            var dumpedBytes = parsed.GetBytes().ToArray();

            //int n = 0;
            //foreach (var c in parsed.Children)
            //{
            //    File.WriteAllBytes(@$"C:\DEV\0079tl\Anim_BRF_PhotoA_00.xbf_{n}.bin", c.GetBytes().ToArray());
            //    n++;
            //}
            //File.WriteAllBytes(@$"C:\DEV\0079tl\Anim_BRF_PhotoA_00.xbf_recreated.bin", dumpedBytes);

            Assert.Equal(expected.Length, dumpedBytes.Length);
            Assert.Equal(expected, dumpedBytes);
        }

        [Fact]
        public void RecreateFromXml()
        {
            var expected = File.ReadAllBytes(testfile);

            var parsed = new XbfRootSegment();
            parsed.Parse(expected.AsSpan());

            var dumpedBytes = parsed.GetBytes().ToArray();

            //check if just dumping works
            Assert.Equal(expected.Length, dumpedBytes.Length);
            Assert.Equal(expected, dumpedBytes);

            var recreated = new XbfRootSegment(parsed.NodeTree.XmlDocument);

            dumpedBytes = recreated.GetBytes().ToArray();
            //File.WriteAllBytes(@$"C:\DEV\0079tl\FlagCtrl.xbf.recreated.bin", dumpedBytes);

            Assert.Equal(expected.Length, dumpedBytes.Length);
            Assert.Equal(expected, dumpedBytes);

            //var xml = parsed.NodeTree.ToString();
            //var doc = new System.Xml.XmlDocument();
            //doc.LoadXml(xml);
            //recreated = new XbfRootSegment(doc);

            //dumpedBytes = recreated.GetBytes().ToArray();

            //Assert.Equal(expected.Length, dumpedBytes.Length);
            //Assert.Equal(expected, dumpedBytes);
        }

        [Fact]
        public void TestAllXbfFiles()
        {
            var files = new System.IO.DirectoryInfo(@"C:\games\wii\0079\0079_jp")
                .GetFiles("*.xbf", SearchOption.AllDirectories);

            int c = 0;
            foreach (var file in files)
            {
                var expected = File.ReadAllBytes(file.FullName);

                var parsed = new XbfRootSegment();
                parsed.Parse(expected.AsSpan());

                var dumpedBytes = parsed.GetBytes().ToArray();

                var xml = parsed.NodeTree.ToString();

                //check if just dumping works
                Assert.Equal(expected.Length, dumpedBytes.Length);
                Assert.Equal(expected, dumpedBytes);

                var recreated = new XbfRootSegment(parsed.NodeTree.XmlDocument);

                dumpedBytes = recreated.GetBytes().ToArray();

                //check if recreating from xml returns identical bytes
                Assert.Equal(expected.Length, dumpedBytes.Length);
                Assert.Equal(expected, dumpedBytes);

                c++;
            }
        }

        [Fact]
        public void AllXbfToXml()
        {
            var files = new System.IO.DirectoryInfo(@"C:\games\wii\0079\0079_jp")
                .GetFiles("*.xbf", SearchOption.AllDirectories);
            //.Where(f => f.FullName.Contains(@"Title_text.arc._tempdir\arc\ACE.arc\arc\BlockText"));

            int c = 0;
            foreach (var file in files)
            {
                var xmlFile = file.FullName
                    .Replace("0079_jp", "0079_en")
                    .Replace(".xbf", ".xml");
                var expected = File.ReadAllBytes(file.FullName);

                var parsed = new XbfRootSegment(XbfRootSegment.ShouldBeUTF8(file.Name));
                parsed.Parse(expected.AsSpan());

                parsed.DumpToDisk(xmlFile);

                c++;
            }
        }

        [Fact]
        public void AllXmlToXbf()
        {
            var files = new System.IO.DirectoryInfo(@"C:\games\wii\0079\0079_jp")
                .GetFiles("*.xbf", SearchOption.AllDirectories);
            //.Where(f => f.FullName.Contains(@"Title_text.arc._tempdir\arc\ACE.arc\arc\BlockText"));

            int c = 0;
            foreach (var file in files)
            {
                var xmlFile = file.FullName
                    .Replace("0079_jp", "0079_en")
                    .Replace(".xbf", ".en.xml");

                if (File.Exists(xmlFile))
                {
                    var outputFile = file.FullName
                        .Replace("0079_jp", "0079_en");
                    var expected = File.ReadAllBytes(file.FullName);

                    var xml = File.ReadAllText(xmlFile);
                    XmlDocument doc = new XmlDocument();
                    doc.LoadXml(xml);

                    var parsed = new XbfRootSegment(doc, XbfRootSegment.ShouldBeUTF8(file.Name));

                    var bytes = parsed.GetBytes().ToArray();
                    File.WriteAllBytes(outputFile, bytes);
                }
            }
        }

        [Fact]
        public void ArcUnpack()
        {
            //var arcjp = @"C:\games\wii\0079\0079_jp\DATA\files\_2d\Title\Title_text.arc";
            //var tempdir = @"C:\games\wii\0079\0079_en\DATA\files\_2d\Title\Title_text.arc_dir";

            var files = new System.IO.DirectoryInfo(@"C:\games\wii\0079\0079_jp\DATA")
                .GetFiles("*.arc", SearchOption.AllDirectories);
            foreach (var file in files)
            {
                var arcjp = file.FullName;
                var tempdir = arcjp.Replace("0079_jp", "0079_unpacked") + "_dir";

                using (var stream = File.OpenRead(arcjp))
                using (var reader = new BinaryReader(stream))
                {
                    var U8 = new U8Unpacker(reader, tempdir);
                    U8.Unpack();
                }
            }
        }

        [Fact]
        public void ArcRepack()
        {
            var files = new System.IO.DirectoryInfo(@"C:\games\wii\0079\0079_jp\DATA")
                .GetFiles("*.arc", SearchOption.AllDirectories);

            files = files.Where(i => 
                //i.Name == "EVC_TU_000.arc"
                //||
                i.Name == "Title_text.arc"
            ).ToArray();

            foreach (var file in files)
            {
                var arcjp = file.FullName;
                //var arcjp = @"C:\games\wii\0079\0079_jp\DATA\files\evc\EVC_TU_000.arc";
                var arcen = arcjp.Replace("0079_jp", "0079_en");
                var tempdir = arcjp.Replace("0079_jp", "0079_unpacked") + "_dir";

                var bytes = File.ReadAllBytes(arcjp).AsSpan();
                var parser = new U8Parser();
                parser.Parse(bytes);

                using (var input = new MemoryStream(File.ReadAllBytes(arcjp))) //in case of updating source file work on memcopy
                using (var output = System.IO.File.Open(arcen, FileMode.Create, FileAccess.Write))
                using (var reader = new BinaryReader(input))
                using (var writer = new BinaryWriter(output))
                {
                    var U8 = new U8Updater(tempdir, reader, writer);
                    U8.Update();
                }
            }
        }
    }
}
