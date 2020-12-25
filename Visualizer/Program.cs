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

namespace Visualizer {
    class KeyImage {
        public bool black;
        public Point position;
        public KeyImage(bool black, Point position) {
            this.black = black;
            this.position = position;
        }
    }
    class Program {
        public static void Main(string[] args) {

            HashSet<byte> pressed = new HashSet<byte>();


            var folder = @"C:\Users\alexm\Downloads";
            using (var f = new FileStream(@$"{ folder}\StrawberryPiano.mid", FileMode.Open)) {
                var m = MidiSequence.Open(f);
                List<NoteVoiceMidiEvent> notes = new List<NoteVoiceMidiEvent>();
                foreach (var t in m.Tracks) {
                    notes.AddRange(t.Events.OfType<NoteVoiceMidiEvent>());
                }
                notes = new List<NoteVoiceMidiEvent>(notes.OrderBy(n => n.DeltaTime));

                int time = 0;

                var dir = @$"{folder}\Strawberry Piano Frames";
                Directory.CreateDirectory(dir);

                Dictionary<byte, KeyImage> keys = new Dictionary<byte, KeyImage>() {
                    { 60 ,      new KeyImage(false, new Point(3000, 500)) },
                    { 61 ,  new KeyImage(true,      new Point(2500, 600)) },
                    { 62,       new KeyImage(false, new Point(3000, 800)) },
                    { 63,   new KeyImage(true,      new Point(2500, 900)) },
                    { 64,       new KeyImage(false, new Point(3000, 1100)) },
                    { 65,       new KeyImage(false, new Point(3000, 1400)) },
                    { 66,   new KeyImage(true,      new Point(2500, 1300)) },
                    { 67,       new KeyImage(false, new Point(3000, 1700)) },
                    { 68,   new KeyImage(true,      new Point(2500, 1950)) },
                    { 69,       new KeyImage(false, new Point(3000, 2100)) },
                    { 70,   new KeyImage(true,      new Point(2500, 2300)) },
                    { 71,       new KeyImage(false, new Point(3000, 2400)) },
                };

                //https://stackoverflow.com/a/12376324
                //VideoFileWriter v = new VideoFileWriter();
                //v.Open(@$"{folder}\Strawberry.mp4", cover.Width, cover.Height, 30, VideoCodec.MPEG4);

                using (Image cover = Bitmap.FromFile(@$"{folder}\Strawberry Piano.png")) {
                    while (notes.Any()) {
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
                            Console.WriteLine(n.ToString());
                        }



                        notes.RemoveRange(0, now.Count());

                        notes.ForEach(n => n.DeltaTime--);
                        time++;

                        using (Bitmap frame = new Bitmap(cover.Width/4, cover.Height/4)) {
                            using (Graphics g = Graphics.FromImage(frame)) {
                                g.DrawImage(cover, new Rectangle(0, 0, frame.Width, frame.Height));
                            }
                            foreach (var n in pressed) {
                                if (keys.TryGetValue(n, out var key)) {
                                    Fill(frame, new Point(key.position.X / 4, key.position.Y / 4), Color.Pink);
                                }
                            }

                            frame.Save(@$"{dir}\{time}.png", System.Drawing.Imaging.ImageFormat.Png);
                        }
                        Console.WriteLine(string.Join(' ', pressed.OrderBy(p => p).Select(p => GetNoteName(p))));
                        Console.WriteLine();
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
