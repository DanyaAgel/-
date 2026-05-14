var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

var app = builder.Build();

app.UseAuthorization();
app.MapControllers();

// Создаём папку для хранения файлов
string storagePath = Path.Combine(Directory.GetCurrentDirectory(), "storage");
if (!Directory.Exists(storagePath))
    Directory.CreateDirectory(storagePath);

app.Run();