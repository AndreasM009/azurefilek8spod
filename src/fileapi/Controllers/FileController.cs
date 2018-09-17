using fileapi.Dal;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace fileapi.Controllers
{
    [Route("api/[controller]")]
    public class FileController : Controller
    {
        private readonly FileRepository _repository;

        public FileController(FileRepository fileRepository)
        {
            _repository = fileRepository;
        }

        [HttpPost]
        [ProducesResponseType(typeof(List<string>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
        public async Task<IActionResult> UploadFile(List<IFormFile> dummy)
        {
            var result = new List<string>();

            foreach (var im in Request.Form.Files)
            {
                await _repository.AddFile(im.OpenReadStream(), im.FileName);
            }
            return Ok(result);
        }

        [HttpGet]
        [ProducesResponseType(typeof(List<string>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
        public async Task<IActionResult> GetFiles()
        {
            var files = await _repository.GetFiles();
            return Ok(files);
        }

        [HttpDelete]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
        public async Task DeleteFile(string filename)
        {
            await _repository.DeleteFile(filename);
        }
    }
}
