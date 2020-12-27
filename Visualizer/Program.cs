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
    class Program {
        public static void Main(string[] args) {
            
        }
        public static void StrawberryPiano() {
            var folder = @"C:\Users\alexm\Downloads";
            string sequenceFile = @$"{folder}\Strawberry Piano.json";
            {
                HashSet<byte> pressed = new HashSet<byte>();
                using (var f = new FileStream(@$"{ folder}\Strawberry Piano.mid", FileMode.Open)) {
                    var m = MidiSequence.Open(f);
                    //Console.WriteLine(m);
                    List<NoteVoiceMidiEvent> notes = new List<NoteVoiceMidiEvent>();
                    foreach (var t in m.Tracks) {
                        notes.AddRange(t.Events.OfType<NoteVoiceMidiEvent>());
                    }
                    {
                        long previous = 0;
                        foreach (var n in notes) {
                            var dt = n.DeltaTime;
                            n.DeltaTime = (n.DeltaTime + previous) / 44;
                            previous += dt;
                        }
                        Console.WriteLine($"{notes.Last().DeltaTime / 1000} seconds");
                        previous = notes.First().DeltaTime;
                        foreach (var n in notes) {
                            var dt = n.DeltaTime;
                            n.DeltaTime -= previous;
                            previous = dt;
                        }
                    }
                    var dir = @$"{folder}\Strawberry Piano Frames";
                    Directory.CreateDirectory(dir);
                    int x2 = 2560;
                    Dictionary<string, KeyImage> keys = new Dictionary<string, KeyImage>() {
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
                    //                    Console.WriteLine(string.Join(' ', notes.OfType<OnNoteVoiceMidiEvent>().Select(n => GetNoteName(n.Note))));
                    int realElapsed = 0;
                    int index = 0;
                    using (Image cover = Bitmap.FromFile(@$"{folder}\Strawberry Piano Note.png")) {
                        using (GifWriter w = new GifWriter($@"{folder}\Strawberry Piano.gif", 1)) {
                            while (notes.Any()) {
                                var delta = notes.First().DeltaTime;
                                notes.First().DeltaTime = 0;
                                var now = notes.TakeWhile(n => n.DeltaTime == 0);
                                foreach (var n in now) {
                                    switch (n) {
                                        case OnNoteVoiceMidiEvent on:
                                            pressed.Add((on.Note));
                                            break;
                                        case OffNoteVoiceMidiEvent off:
                                            pressed.Remove((off.Note));
                                            break;
                                    }
                                }
                                notes.RemoveRange(0, now.Count());
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
                                    w.WriteFrame(frame, (int)delta);
                                }
                                index++;
                            }
                            Console.WriteLine($"Total: {realElapsed / 1000} seconds");
                        }
                    }
                }
            }
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
                if(b.GetPixel(p.X, p.Y) != replaced) {
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
