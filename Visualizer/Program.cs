using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
//using AForge.Video.FFMPEG;
using MidiSharp;
using MidiSharp.Events.Voice.Note;
using static MidiSharp.Events.MidiEvent;
using Point = System.Drawing.Point;

using Newtonsoft.Json;
using System.Drawing.Imaging;
using System.Diagnostics;

namespace Visualizer {
    class KeyImage {
        public bool black;
        public Point position;
        public KeyImage(bool black, Point position) {
            this.black = black;
            this.position = position;
        }
    }

    public class ImageSequence {
        public List<Bitmap> frames;
        public List<int> timing;
        public ImageSequence(List<Bitmap> frames, List<int> timing) {
            this.frames = frames;
            this.timing = timing;
        }
    }

    class Note {
        public byte note;
        public bool on;
        public int time;
    }
    class Program {
        public static void Main(string[] args) {
            StrawberryPiano();
        }
        public static void StrawberryPiano() {

            //File.WriteAllText("StrawberryPiano.bat", $"ffmpeg.exe -framerate 25 -i {Path.GetFullPath("StrawberryPianoFrames")}/frame*.png -r 25   -pix_fmt yuv420p out.mp4");

            var folder = @"C:\Users\alexm\Downloads";

            HashSet<byte> pressed = new HashSet<byte>();
            using (var f = new FileStream(@$"{ folder}\Strawberry Piano.mid", FileMode.Open)) {
                var m = MidiSequence.Open(f);
                List<NoteVoiceMidiEvent> notes = new List<NoteVoiceMidiEvent>();
                foreach (var t in m.Tracks) {
                    notes.AddRange(t.Events.OfType<NoteVoiceMidiEvent>());
                }
                {
                    //Convert to absolute time
                    long previous = 0;
                    foreach (var n in notes) {
                        var dt = n.DeltaTime;
                        n.DeltaTime = (dt + previous) / 30;
                        previous += dt;
                    }
                    Console.WriteLine($"Note count: {notes.Count} ");
                    Console.WriteLine($"Last note: {notes.Last().DeltaTime} ms");
                    Console.WriteLine($"Last note: {notes.Last().DeltaTime / 1000} seconds");
                }
                var dir = @$"{folder}\Strawberry Piano Frames";
                Directory.CreateDirectory(dir);

                int frameRate = 25;
                Console.WriteLine($"Frames: {notes.Last().DeltaTime * frameRate / 1000} frames");
                using (StrawberryVisualizer v = new StrawberryVisualizer(@$"Strawberry Piano Note.png", $"StrawberryPianoFrames")) {
                    KeyTracker keys = new KeyTracker();

                    int index = 0;
                    int i = 0;
                    while(index < notes.Count) {
                        double time = i * 1000 / frameRate;
                        i++;
                        var now = notes.Skip(index).TakeWhile(n => n.DeltaTime <= time);
                        keys.Process(now);
                        index += now.Count();
                        v.AddFrame(keys.pressed);
                        //Console.WriteLine($"Time: {time}");
                        //Console.WriteLine($"Index: {index}");
                    }


                    Process.Start(Path.GetFullPath("StrawberryPiano.bat"));
                }
            }
        }
    }
    class KeyTracker {
        public HashSet<byte> pressed;
        public KeyTracker() {
            pressed = new HashSet<byte>();
        }
        public void Process(params NoteVoiceMidiEvent[] notes) {
            foreach (var n in notes) {
                Process(n);
            }
        }
        public void Process(IEnumerable<NoteVoiceMidiEvent> notes) {
            foreach (var n in notes) {
                Process(n);
            }
        }
        public void Process(NoteVoiceMidiEvent n) {
            switch (n) {
                case OnNoteVoiceMidiEvent on:
                    pressed.Add((on.Note));
                    break;
                case OffNoteVoiceMidiEvent off:
                    pressed.Remove((off.Note));
                    break;
            }
        }
    }
    class StrawberryVisualizer : IDisposable {
        Image cover;
        public string folder;
        int index = 0;
        Dictionary<string, KeyImage> keys;
        public StrawberryVisualizer(string coverFile, string folder) {
            this.cover = Bitmap.FromFile(coverFile);
            this.folder = folder;
            int x2 = 2560;
            keys = new Dictionary<string, KeyImage>() {
                { "B5",         new KeyImage(false, new Point(3000, 500)) },    //B
                { "A#5",    new KeyImage(true,      new Point(x2, 600)) },    //A#
                { "A5",         new KeyImage(false, new Point(3000, 800)) },    //A
                { "G#5",    new KeyImage(true,      new Point(x2, 900)) },    //G#
                { "G5",         new KeyImage(false, new Point(3000, 1100)) },   //G
                { "F#5",    new KeyImage(true,      new Point(x2, 1280)) },   //F#
                { "F5",         new KeyImage(false, new Point(3000, 1300)) },   //F
                { "E5",         new KeyImage(false, new Point(3000, 1700)) },   //E
                { "D#5",   new KeyImage(true,       new Point(x2, 1950)) },   //D#
                { "D5",         new KeyImage(false, new Point(3000, 2100)) },   //D
                { "C#5",   new KeyImage(true,       new Point(x2, 2300)) },   //C#
                { "C5",         new KeyImage(false, new Point(3000, 2400)) },   //C
            };

            Directory.CreateDirectory(folder);
        }
        public void AddFrame(HashSet<byte> pressed) {
            index++;
            int scale = 4;
            using (Bitmap frame = new Bitmap(cover.Width / scale, cover.Height / scale)) {
                using (Graphics g = Graphics.FromImage(frame)) {
                    g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
                    g.DrawImage(cover, new Rectangle(0, 0, frame.Width, frame.Height));
                    foreach (var n in pressed) {
                        if (keys.TryGetValue(GetNoteName(n), out var key)) {
                            var pos = key.position;
                            Fill(frame, new Point(pos.X / scale, pos.Y / scale), Color.Pink);
                        }
                    }
                }
                frame.Save($"{folder}/frame{index}.png", ImageFormat.Png);
                //gif.WriteFrame(frame);
            }
        }
        public void Dispose() {
            cover.Dispose();
        }


        public static void Fill(Bitmap b, Point start, Color c) {
            HashSet<Point> seen = new HashSet<Point>();
            Queue<Point> points = new Queue<Point>();
            points.Enqueue(start);

            Color replaced = b.GetPixel(start.X, start.Y);
            while (points.Any()) {
                var p = points.Dequeue();
                if (seen.Contains(p)) {
                    continue;
                }
                if (p.X < 0 || p.Y < 0 || p.X >= b.Width || p.Y >= b.Height) {
                    continue;
                }
                if (b.GetPixel(p.X, p.Y) != replaced) {
                    continue;
                }
                seen.Add(p);
                b.SetPixel(p.X, p.Y, c);
                points.Enqueue(new Point(p.X + 1, p.Y));
                points.Enqueue(new Point(p.X - 1, p.Y));
                points.Enqueue(new Point(p.X, p.Y + 1));
                points.Enqueue(new Point(p.X, p.Y - 1));
            }
        }
    }
}
