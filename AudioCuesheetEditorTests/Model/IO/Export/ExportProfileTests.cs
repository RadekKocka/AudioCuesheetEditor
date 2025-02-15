﻿//This file is part of AudioCuesheetEditor.

//AudioCuesheetEditor is free software: you can redistribute it and/or modify
//it under the terms of the GNU General Public License as published by
//the Free Software Foundation, either version 3 of the License, or
//(at your option) any later version.

//AudioCuesheetEditor is distributed in the hope that it will be useful,
//but WITHOUT ANY WARRANTY; without even the implied warranty of
//MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//GNU General Public License for more details.

//You should have received a copy of the GNU General Public License
//along with Foobar.  If not, see
//<http: //www.gnu.org/licenses />.
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AudioCuesheetEditorTests.Utility;
using AudioCuesheetEditor.Model.AudioCuesheet;
using System.IO;
using AudioCuesheetEditor.Model.IO.Audio;
using AudioCuesheetEditor.Model.Entity;

namespace AudioCuesheetEditor.Model.IO.Export.Tests
{
    [TestClass()]
    public class ExportProfileTests
    {
        [TestMethod()]
        public void ExportprofileTest()
        {
            var testHelper = new TestHelper();
            //Prepare cuesheet
            Cuesheet cuesheet = new()
            {
                Artist = "Demo Artist",
                Title = "Demo Title",
                Audiofile = new Audiofile("Testfile.mp3")
            };            
            var begin = TimeSpan.Zero;
            for (int i = 1; i < 25; i++)
            {
                var track = new Track
                {
                    Artist = String.Format("Demo Track Artist {0}", i),
                    Title = String.Format("Demo Track Title {0}", i),
                    Begin = begin
                };
                begin = begin.Add(new TimeSpan(0, i, i));
                track.End = begin;
                cuesheet.AddTrack(track, testHelper.ApplicationOptions);
                var rand = new Random();
                var flagsToAdd = rand.Next(0, 3);
                if (flagsToAdd > 0)
                {
                    for (int x = 0; x < flagsToAdd; x++)
                    {
                        track.SetFlag(Flag.AvailableFlags.ElementAt(x), SetFlagMode.Add);
                    }
                }
            }

            cuesheet.Cataloguenumber.Value = "Testcatalognumber";
            cuesheet.CDTextfile = new CDTextfile("Testfile.cdt");

            //Test class
            var exportProfile = new Exportprofile();
            exportProfile.SchemeHead.Scheme = "%Cuesheet.Artist%;%Cuesheet.Title%;%Cuesheet.Cataloguenumber%;%Cuesheet.CDTextfile%";
            Assert.AreEqual(ValidationStatus.Success, exportProfile.SchemeHead.Validate().Status);
            exportProfile.SchemeTracks.Scheme = "%Track.Position%;%Track.Artist%;%Track.Title%;%Track.Begin%;%Track.End%;%Track.Length%";
            Assert.AreEqual(ValidationStatus.Success, exportProfile.SchemeTracks.Validate().Status);
            exportProfile.SchemeFooter.Scheme = "Exported %Cuesheet.Title% from %Cuesheet.Artist% using AudioCuesheetEditor";
            Assert.AreEqual(ValidationStatus.Success, exportProfile.SchemeFooter.Validate().Status);
            Assert.IsTrue(exportProfile.IsExportable);
            var fileContent = exportProfile.GenerateExport(cuesheet);
            Assert.IsNotNull(fileContent);
            var tempFile = Path.GetTempFileName();
            File.WriteAllBytes(tempFile, fileContent);
            var content = File.ReadAllLines(tempFile);
            Assert.AreEqual(content[0], "Demo Artist;Demo Title;Testcatalognumber;Testfile.cdt");
            for (int i = 1; i < content.Length - 1; i++)
            {
                Assert.IsFalse(String.IsNullOrEmpty(content[i]));
                Assert.AreNotEqual(content[i], ";;;;;");
                Assert.IsTrue(content[i].StartsWith(cuesheet.Tracks.ToList()[i - 1].Position + ";"));
            }
            Assert.AreEqual(content[^1], "Exported Demo Title from Demo Artist using AudioCuesheetEditor");

            File.Delete(tempFile);

            exportProfile.SchemeHead.Scheme = "%Track.Position%;%Cuesheet.Artist%;";
            var validationResult = exportProfile.SchemeHead.Validate();
            Assert.AreEqual(ValidationStatus.Error, validationResult.Status);
            Assert.IsTrue(validationResult.ValidationMessages?.Any(x => x.Parameter != null && x.Parameter.Contains("%Track.Position%")));

            //Check multiline export
            exportProfile = new Exportprofile();
            exportProfile.SchemeHead.Scheme = "%Cuesheet.Artist%;%Cuesheet.Title%";
            Assert.AreEqual(ValidationStatus.Success, exportProfile.SchemeHead.Validate().Status);
            exportProfile.SchemeTracks.Scheme = String.Format("%Track.Position%{0}%Track.Artist%{1}%Track.Title%;%Track.Begin%;%Track.End%;%Track.Length%", Environment.NewLine, Environment.NewLine);
            Assert.AreEqual(ValidationStatus.Success, exportProfile.SchemeTracks.Validate().Status);
            exportProfile.SchemeFooter.Scheme = "Exported %Cuesheet.Title% from %Cuesheet.Artist% using AudioCuesheetEditor";
            Assert.AreEqual(ValidationStatus.Success, exportProfile.SchemeFooter.Validate().Status);
            Assert.IsTrue(exportProfile.IsExportable);
            fileContent = exportProfile.GenerateExport(cuesheet);
            Assert.IsNotNull(fileContent);
            tempFile = Path.GetTempFileName();
            File.WriteAllBytes(tempFile, fileContent);
            content = File.ReadAllLines(tempFile);
            Assert.AreEqual(content[0], "Demo Artist;Demo Title");
            var trackPosition = 0;
            for (int i = 1; i < content.Length - 1; i += 3)
            {
                Assert.IsFalse(String.IsNullOrEmpty(content[i]));
                Assert.AreNotEqual(content[i], ";;;;;");
                var track = cuesheet.Tracks.ToList()[trackPosition];
                Assert.IsNotNull(track.Position);
                var position = track.Position.ToString();
                Assert.IsNotNull(position);
                Assert.IsTrue(content[i].StartsWith(position));
                trackPosition++;
            }
            Assert.AreEqual(content[^1], "Exported Demo Title from Demo Artist using AudioCuesheetEditor");

            File.Delete(tempFile);

            //Test flags
            exportProfile = new Exportprofile();
            exportProfile.SchemeHead.Scheme = "%Cuesheet.Artist%;%Cuesheet.Title%;%Cuesheet.Cataloguenumber%;%Cuesheet.CDTextfile%";
            Assert.AreEqual(ValidationStatus.Success, exportProfile.SchemeHead.Validate().Status);
            exportProfile.SchemeTracks.Scheme = "%Track.Position%;%Track.Flags%;%Track.Artist%;%Track.Title%;%Track.Begin%;%Track.End%;%Track.Length%";
            Assert.AreEqual(ValidationStatus.Success, exportProfile.SchemeTracks.Validate().Status);
            exportProfile.SchemeFooter.Scheme = "Exported %Cuesheet.Title% from %Cuesheet.Artist% using AudioCuesheetEditor";
            Assert.AreEqual(ValidationStatus.Success, exportProfile.SchemeFooter.Validate().Status);
            Assert.IsTrue(exportProfile.IsExportable);
            fileContent = exportProfile.GenerateExport(cuesheet);
            Assert.IsNotNull(fileContent);
            tempFile = Path.GetTempFileName();
            File.WriteAllBytes(tempFile, fileContent);
            content = File.ReadAllLines(tempFile);
            Assert.AreEqual(content[0], "Demo Artist;Demo Title;Testcatalognumber;Testfile.cdt");
            for (int i = 1; i < content.Length - 1; i++)
            {
                Assert.IsFalse(String.IsNullOrEmpty(content[i]));
                Assert.AreNotEqual(content[i], ";;;;;");
                Assert.IsTrue(content[i].StartsWith(cuesheet.Tracks.ToList()[i - 1].Position + ";"));
                if (cuesheet.Tracks.ElementAt(i - 1).Flags.Count > 0)
                {
                    var flags = cuesheet.Tracks.ElementAt(i - 1).Flags;
                    Assert.IsTrue(content[i].Contains(String.Join(" ", flags.Select(x => x.CuesheetLabel))));
                }
            }
            Assert.AreEqual(content[^1], "Exported Demo Title from Demo Artist using AudioCuesheetEditor");
            File.Delete(tempFile);
        }

        [TestMethod()]
        public void TestExportWithPregapAndPostgap()
        {
            var testHelper = new TestHelper();
            //Prepare cuesheet
            Cuesheet cuesheet = new()
            {
                Artist = "Demo Artist",
                Title = "Demo Title",
                Audiofile = new Audiofile("Testfile.mp3")
            };
            var begin = TimeSpan.Zero;
            for (int i = 1; i < 25; i++)
            {
                var track = new Track
                {
                    Artist = String.Format("Demo Track Artist {0}", i),
                    Title = String.Format("Demo Track Title {0}", i),
                    Begin = begin,
                    PostGap = new TimeSpan(0, 0, 1),
                    PreGap = new TimeSpan(0, 0, 3)
                };
                begin = begin.Add(new TimeSpan(0, i, i));
                track.End = begin;
                cuesheet.AddTrack(track, testHelper.ApplicationOptions);
                var rand = new Random();
                var flagsToAdd = rand.Next(0, 3);
                if (flagsToAdd > 0)
                {
                    for (int x = 0; x < flagsToAdd; x++)
                    {
                        track.SetFlag(Flag.AvailableFlags.ElementAt(x), SetFlagMode.Add);
                    }
                }
            }

            cuesheet.Cataloguenumber.Value = "Testcatalognumber";
            cuesheet.CDTextfile = new CDTextfile("Testfile.cdt");

            var exportProfile = new Exportprofile();
            exportProfile.SchemeHead.Scheme = "%Cuesheet.Artist%;%Cuesheet.Title%;%Cuesheet.Cataloguenumber%;%Cuesheet.CDTextfile%";
            Assert.AreEqual(ValidationStatus.Success, exportProfile.SchemeHead.Validate().Status);
            exportProfile.SchemeTracks.Scheme = "%Track.Position%;%Track.Artist%;%Track.Title%;%Track.Begin%;%Track.End%;%Track.Length%;%Track.PreGap%;%Track.PostGap%";
            Assert.AreEqual(ValidationStatus.Success, exportProfile.SchemeTracks.Validate().Status);
            exportProfile.SchemeFooter.Scheme = "Exported %Cuesheet.Title% from %Cuesheet.Artist% using AudioCuesheetEditor at %Date%";
            Assert.AreEqual(ValidationStatus.Success, exportProfile.SchemeFooter.Validate().Status);
            Assert.IsTrue(exportProfile.IsExportable);
            var fileContent = exportProfile.GenerateExport(cuesheet);
            Assert.IsNotNull(fileContent);
            var tempFile = Path.GetTempFileName();
            File.WriteAllBytes(tempFile, fileContent);
            var content = File.ReadAllLines(tempFile);
            Assert.AreEqual(content[0], "Demo Artist;Demo Title;Testcatalognumber;Testfile.cdt");
            for (int i = 1; i < content.Length - 1; i++)
            {
                Assert.IsFalse(String.IsNullOrEmpty(content[i]));
                Assert.AreNotEqual(content[i], ";;;;;");
                Assert.IsTrue(content[i].StartsWith(cuesheet.Tracks.ToList()[i - 1].Position + ";"));
            }
            Assert.AreEqual(content[^1], String.Format("Exported Demo Title from Demo Artist using AudioCuesheetEditor at {0}", DateTime.Now.ToShortDateString()));
            File.Delete(tempFile);
        }

        [TestMethod()]
        public void IsExportableTest()
        {
            var exportProfile = new Exportprofile();
            exportProfile.SchemeHead.Scheme = "%Cuesheet.Artist%;%Cuesheet.Title%;%Cuesheet.Cataloguenumber%;%Cuesheet.CDTextfile%";
            Assert.AreEqual(ValidationStatus.Success, exportProfile.SchemeHead.Validate().Status);
            exportProfile.SchemeTracks.Scheme = "%Track.Position%;%Track.Artist%;%Track.Title%;%Track.Begin%;%Track.End%;%Track.Length%;%Track.PreGap%;%Track.PostGap%";
            Assert.AreEqual(ValidationStatus.Success, exportProfile.SchemeTracks.Validate().Status);
            exportProfile.SchemeFooter.Scheme = "Exported %Cuesheet.Title% from %Cuesheet.Artist% using AudioCuesheetEditor at %Date%";
            Assert.AreEqual(ValidationStatus.Success, exportProfile.SchemeFooter.Validate().Status);
            Assert.IsTrue(exportProfile.IsExportable);
            exportProfile.SchemeFooter.Scheme = "Exported %Track.Title% from %Cuesheet.Artist% using AudioCuesheetEditor at %Date%";
            Assert.AreEqual(ValidationStatus.Error, exportProfile.SchemeFooter.Validate().Status);
            Assert.IsFalse(exportProfile.IsExportable);
        }
    }
}