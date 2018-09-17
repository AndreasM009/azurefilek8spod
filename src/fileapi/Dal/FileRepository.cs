using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace fileapi.Dal
{
    public class FileRepository
    {
        public async Task AddFile(Stream stream, string filename)
        {
            if (File.Exists($"/share/{filename}"))
                throw new InvalidOperationException("File already exists");

            using (var filestream = File.OpenWrite($"/share/{filename}"))
            {
                await stream.CopyToAsync(filestream);
                await filestream.FlushAsync();
            }
        }

        public async Task<List<string>> GetFiles()
        {
            return await Task.Run(() => Directory.GetFiles("/share").ToList());
        }

        public async Task DeleteFile(string filename)
        {
            await Task.Run(() => 
            { 
                if (!File.Exists(filename))
                    throw new InvalidOperationException();

                File.Delete(filename);
            });
        }
    }
}
