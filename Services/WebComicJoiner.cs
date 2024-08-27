
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ComicRecompress.Jobs;
using Pastel;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.Drawing;
using Color = System.Drawing.Color;
using Point = SixLabors.ImageSharp.Point;

namespace ComicRecompress.Services
{
    public class WebComicJoiner
    {
        class JoinGroup
        {
            public string FirstFileName { get; set; }
            public string ReplacementFileName { get; set; }
            public List<Image> Images { get; set; } = new List<Image>();

            public int Width { get; set; }

            public int Height { get; set; }
            public static string ReplaceLastOccurrence(string source, string find, string replace)
            {
                int place = source.LastIndexOf(find);

                if (place == -1)
                    return source;

                return source.Remove(place, find.Length).Insert(place, replace);
            }
            public JoinGroup(string source_dir, string dest_dir, string firstFileName, string firstFileNameDigitsPart, int group_count, int maxdigits)
            {
                string replPart = group_count.ToString().PadLeft(maxdigits, '0');
                string l = ReplaceLastOccurrence(firstFileName, firstFileNameDigitsPart, replPart);
                string relative = l.Substring(source_dir.Length + 1);
                string dest = Path.Combine(dest_dir, relative);
                ReplacementFileName = Path.ChangeExtension(dest, "png");
            }

        }

        private readonly BaseJob _job;
        public WebComicJoiner(BaseJob job)
        {
            _job = job;
        }
        private async Task<string> SaveJoinGroup(JoinGroup grp)
        {
            Image img = new Image<Rgb48>(grp.Width, grp.Height);
            int y = 0;
            foreach (var i in grp.Images)
            {
                img.Mutate(o => o.DrawImage(i, new Point(0, y), PixelColorBlendingMode.Normal, 1));
                y += i.Height;
            }
            Directory.CreateDirectory(Path.GetDirectoryName(grp.ReplacementFileName));
            await img.SaveAsPngAsync(grp.ReplacementFileName).ConfigureAwait(false);
            _job.WriteLine($"Saved {Path.GetFileName(grp.ReplacementFileName)}");
            return grp.ReplacementFileName;
        }
        private JoinGroup CreateGroup(Image im, string source_dir, string destination_dir, string current_file, string key, int pos, int digits)
        {
            JoinGroup grp = new JoinGroup(source_dir, destination_dir, current_file, key, pos, digits);
            grp.Width = im.Width;
            grp.Height = im.Height;
            grp.Images.Add(im);
            return grp;
        }
        public async Task<bool> Join(string source_dir, string destination_dir, int max_height)
        {
            try
            {
                List<(string, string)> files = Directory.GetFiles(source_dir, "*", SearchOption.AllDirectories).Where(Extensions.IsImage).OrderByNatural(Path.GetFileNameWithoutExtension).ToList();
                List<string> joins = new List<string>();
                int pos = 0;

                int digits = files.Select(a => a.Item1.Length).Max();
                JoinGroup grp = null;
                foreach ((string key, string current_file) in files)
                {
                    Image im = await Image.LoadAsync(current_file).ConfigureAwait(false);
                    if (grp == null)
                    {
                        grp = CreateGroup(im, source_dir, destination_dir, current_file, key, joins.Count, digits);
                        continue;
                    }
                    if (grp.Width != im.Width || grp.Height + im.Height > max_height)
                    {
                        string dest = await SaveJoinGroup(grp).ConfigureAwait(false);

                        joins.Add(dest);
                        grp = CreateGroup(im, source_dir, destination_dir, current_file, key, joins.Count, digits);
                    }
                    else
                    {
                        grp.Height += im.Height;
                        grp.Images.Add(im);
                    }
                }
                if (grp != null)
                {
                    string dest = await SaveJoinGroup(grp).ConfigureAwait(false);
                    joins.Add(dest);
                }
                return true;
            }
            catch (Exception e)
            {
                _job.WriteError($"Error:Joining images {e.Message}");
                return false;
            }


        }

    }
}
