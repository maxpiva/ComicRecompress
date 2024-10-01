using SharpCompress.Common;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using ComicRecompress.Jobs;
using Pastel;
using SharpCompress.Archives;

namespace ComicRecompress.Services
{
    public class Compression
    {
        private readonly BaseJob _job;
        public Compression(BaseJob job)
        {
            _job = job;
        }

        public bool IsCompressed(string file)
        {

            try
            {
                IArchive archive = ArchiveFactory.Open(file);
                if (archive != null)
                {
                    archive.Dispose();
                    return true;
                }
            }
            catch
            {
            }
            return false;
        }

        public bool Compress(string directory, string file)
        {
            try
            {
                using (var archive = SharpCompress.Archives.Zip.ZipArchive.Create())
                {
                    foreach (string? path in Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories))
                    {
                        try
                        {
                            string fileName = path[directory.Length..];
                            _job.WriteLine($"Compressing {fileName}");
                            FileInfo? fileInfo = new FileInfo(path);
                            archive.AddEntry(fileName, fileInfo.OpenRead(), true, fileInfo.Length, fileInfo.LastWriteTime);
                        }
                        catch (Exception ex)
                        {
                            _job.WriteError($"ERROR:executing compressor: {ex.Message}");
                        }
                    }

                    archive.SaveTo(file, CompressionType.None);
                }
            }
            catch (Exception e)
            {
                _job.WriteError($"ERROR:executing compressor: {e.Message}");
                return false;
            }
            return true;
        }

        public bool Decompress(string directory, string file, bool flatten)
        {
            try
            {
                using (var archive = ArchiveFactory.Open(file))
                {
                    if (flatten)
                    {
                        HashSet<string> existings = new HashSet<string>();
                        foreach(var entry in archive.Entries.Where(a=>!a.IsDirectory))
                        {
                            string? fileName = entry.Key;
                            if (entry.Key != null)
                            {
                                string filename = Path.GetFileName(entry.Key);
                                if (existings.Contains(filename))
                                {
                                    _job.WriteError($"WARNING:Unable to flat archive, the following filename exists at least two times '{filename}'.");
                                    flatten = false;
                                    break;
                                }
                            }
                        }
                    }

                    foreach (var entry in archive.Entries.Where(a=>!a.IsDirectory))
                    {
                        if (!entry.IsDirectory)
                        {
                            _job.WriteLine($"Decompressing {entry.Key}");
                            entry.WriteToDirectory(directory, new ExtractionOptions()
                            {
                                ExtractFullPath = !flatten,
                                Overwrite = true
                            });
                        }
                    }
                }
            }
            catch (Exception e)
            {
                _job.WriteError($"ERROR:executing decompression: {e.Message}");
                return false;
            }
            return true;
        }
    }

}
