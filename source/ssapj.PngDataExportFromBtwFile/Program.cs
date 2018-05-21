using System;
using System.Collections.Generic;
using System.IO;

namespace ssapj.PngDataExportFromBtwFile
{
    class Program
    {
        static void Main(string[] args)
        {
            const int headerLength = 26; //<cr><lf>Bar Tender Format File<cr><lf>

            foreach (var btwFile in args)
            {
                if (File.Exists(btwFile))
                {
                    var directory = Directory.GetParent(btwFile);

                    using (var fileStream = File.OpenRead(btwFile))
                    {
                        Span<byte> buffer = new byte[fileStream.Length];

                        fileStream.Read(buffer);

                        ReadOnlySpan<byte> readOnlyBuffer = buffer.Slice(headerLength, buffer.Length - headerLength);

                        var p = 0;
                        var pngData = new List<(int start, int length)> { };

                        while (p < buffer.Length)
                        {
                            if (buffer[p] == 0x89 && buffer[p + 1] == 0x50 && buffer[p + 2] == 0x4e && buffer[p + 3] == 0x47 && buffer[p + 4] == 0x0d && buffer[p + 5] == 0x0a && buffer[p + 6] == 0x1a && buffer[p + 7] == 0x0a)
                            {
                                var start = p;
                                (bool isOK, int length) range = (false, 0);

                                //ヘッダー8バイト、IHDR25バイトとIDAT最小の12バイトはとりあえず飛ばす
                                var p4End = p + 8 + 25 + 12;

                                while (p4End < buffer.Length)
                                {
                                    if (buffer[p4End] == 0x49 && buffer[p4End + 1] == 0x45 && buffer[p4End + 2] == 0x4e && buffer[p4End + 3] == 0x44)
                                    {
                                        range = (true, p4End + 7 - start + 1);
                                        break;
                                    }

                                    p4End++;
                                }

                                if (range.isOK)
                                {
                                    pngData.Add((start, range.length));
                                    p = p + range.length;
                                }
                                else
                                {
                                    Console.WriteLine($"{Path.GetFileNameWithoutExtension(btwFile)} is broken.");
                                    break;//というかデータ壊れているのをどうするべきか
                                }
                            }
                            else
                            {
                                p = p + 1;
                            }
                        }

                        var counter = 1;

                        //First is a thumnail image.
                        //Second is mask for the thumnail image.
                        //Others are raw png data in Templates. There are only in several old versions.
                        foreach (var (s, l) in pngData)
                        {
                            var pngBytes = buffer.Slice(s, l).ToArray();
                            File.WriteAllBytes(Path.Combine(directory.FullName, $"{Path.GetFileNameWithoutExtension(btwFile)}_{counter}.png"), pngBytes);
                            counter++;
                        }

                        fileStream.Close();
                    }

                }
            }
        }
    }
}
