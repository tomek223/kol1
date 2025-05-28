var builder = WebApplication.CreateBuilder(args);
 
// Konfiguracja połączenia do bazy danych w appsettings.json

string connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
 
// Rejestracja repozytorium

builder.Services.AddScoped<ClientRepository>(sp => new ClientRepository(connectionString));
 
builder.Services.AddControllers();
 
var app = builder.Build();
 
app.UseRouting();

app.MapControllers();
 
app.Run();