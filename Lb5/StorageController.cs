using Microsoft.AspNetCore.Mvc;

namespace File_Storage.Controllers
{
    [ApiController]
    [Route("/")]
    public class StorageController : ControllerBase
    {
        private readonly string _storagePath;

        public StorageController()
        {
            _storagePath = Path.Combine(Directory.GetCurrentDirectory(), "storage");
            if (!Directory.Exists(_storagePath))
                Directory.CreateDirectory(_storagePath);
        }

        // GET / - список файлов
        // GET /file.txt - содержимое файла
        [HttpGet]
        [HttpGet("{*path}")]
        public IActionResult Get(string? path)
        {
            string fullPath = string.IsNullOrEmpty(path)
                ? _storagePath
                : Path.GetFullPath(Path.Combine(_storagePath, path.Replace('/', Path.DirectorySeparatorChar)));

            if (!fullPath.StartsWith(_storagePath))
                return BadRequest("Недопустимый путь");

            if (Directory.Exists(fullPath))
            {
                var items = new List<object>();
                foreach (var d in Directory.GetDirectories(fullPath))
                    items.Add(new { type = "dir", name = Path.GetFileName(d) });
                foreach (var f in Directory.GetFiles(fullPath))
                {
                    var info = new FileInfo(f);
                    items.Add(new { type = "file", name = Path.GetFileName(f), size = info.Length });
                }
                return Ok(items);
            }

            if (System.IO.File.Exists(fullPath))
            {
                var stream = System.IO.File.OpenRead(fullPath);
                return File(stream, "application/octet-stream");
            }

            return NotFound($"Файл или папка '{path}' не найден");
        }

        // PUT /file.txt - загрузка файла
        [HttpPut("{*path}")]
        public async Task<IActionResult> Put(string path)
        {
            // ДИАГНОСТИКА: читаем ВСЁ, что пришло
            string rawRequest = "";
            using (var reader = new StreamReader(Request.Body))
            {
                rawRequest = await reader.ReadToEndAsync();
            }

            // Выводим в консоль сервера (ОЧЕНЬ ВАЖНО!)
            Console.WriteLine($"=== ДИАГНОСТИКА ===");
            Console.WriteLine($"Путь: {path}");
            Console.WriteLine($"Длина тела: {rawRequest.Length}");
            Console.WriteLine($"Содержимое: '{rawRequest}'");
            Console.WriteLine($"Заголовок Content-Length: {Request.ContentLength}");
            Console.WriteLine($"==================");

            if (string.IsNullOrEmpty(path))
                return BadRequest("Укажите путь к файлу");

            string fullPath = Path.GetFullPath(Path.Combine(_storagePath, path.Replace('/', Path.DirectorySeparatorChar)));

            if (!fullPath.StartsWith(_storagePath))
                return BadRequest("Недопустимый путь");

            string? dir = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            // Записываем то, что пришло
            await System.IO.File.WriteAllTextAsync(fullPath, rawRequest);

            return Ok($"Файл '{path}' загружен");
        }

        // DELETE /file.txt - удаление файла или папки
        [HttpDelete("{*path}")]
        public IActionResult Delete(string path)
        {
            if (string.IsNullOrEmpty(path))
                return BadRequest("Укажите путь");

            string fullPath = Path.GetFullPath(Path.Combine(_storagePath, path.Replace('/', Path.DirectorySeparatorChar)));

            if (!fullPath.StartsWith(_storagePath))
                return BadRequest("Недопустимый путь");

            if (System.IO.File.Exists(fullPath))
            {
                System.IO.File.Delete(fullPath);
                return Ok($"Файл '{path}' удалён");
            }

            if (Directory.Exists(fullPath))
            {
                Directory.Delete(fullPath, true);
                return Ok($"Папка '{path}' удалена");
            }

            return NotFound($"Файл или папка '{path}' не найден");
        }

        // HEAD /file.txt - информация о файле
        [HttpHead("{*path}")]
        public IActionResult Head(string path)
        {
            if (string.IsNullOrEmpty(path))
                return BadRequest("Укажите путь к файлу");

            string fullPath = Path.GetFullPath(Path.Combine(_storagePath, path.Replace('/', Path.DirectorySeparatorChar)));

            if (!fullPath.StartsWith(_storagePath))
                return BadRequest("Недопустимый путь");

            if (!System.IO.File.Exists(fullPath))
                return NotFound($"Файл '{path}' не найден");

            var info = new FileInfo(fullPath);
            Response.Headers.Append("X-File-Size", info.Length.ToString());
            Response.Headers.Append("X-File-Last-Modified", info.LastWriteTimeUtc.ToString("o"));
            return Ok();
        }
    }
}