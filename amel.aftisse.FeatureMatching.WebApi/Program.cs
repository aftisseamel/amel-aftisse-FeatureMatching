using amel.aftisse.FeatureMatching;
using Microsoft.AspNetCore.Mvc;
using OpenCvSharp;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast =  Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast")
.WithOpenApi();

app.MapPost("/FeatureMatching", async ([FromForm] IFormFileCollection files) =>
{
    if (files.Count != 2)
    {
        return Results.BadRequest("Veuillez télécharger deux images.");
    }

    // Charger l'image de l'objet
    using var objectSourceStream = files[0].OpenReadStream();
    using var objectMemoryStream = new MemoryStream();
    objectSourceStream.CopyTo(objectMemoryStream);
    var objectImageData = objectMemoryStream.ToArray();

    // Charger l'image de la scène
    using var sceneSourceStream = files[1].OpenReadStream();
    using var sceneMemoryStream = new MemoryStream();
    sceneSourceStream.CopyTo(sceneMemoryStream);
    var sceneImageData = sceneMemoryStream.ToArray();

    // Initialiser l'objet de détection
    var objectDetection = new ObjectDetection();

    // Appeler la méthode de détection pour récupérer les résultats (points détectés)
    var detectionResults = await objectDetection.DetectObjectInScenesAsync(objectImageData, new List<byte[]> { sceneImageData });

    // Récupérer l'image de la scène dans OpenCV
    var sceneMat = Cv2.ImDecode(sceneImageData, ImreadModes.Color);

    // Si des points sont détectés, dessiner un rectangle autour des zones détectées
    foreach (var result in detectionResults)
    {
        foreach (var point in result.Points)
        {
            // Exemple de dessiner un rectangle autour des points détectés (à adapter selon votre logique)
            var rect = new Rect((int)(point.X - 10), (int)(point.Y - 10), 20, 20);  // Ajuster la taille du rectangle selon vos besoins
            Cv2.Rectangle(sceneMat, rect, new Scalar(0, 0, 255), 2); // Dessin du rectangle en rouge
        }
    }

    // Encoder l'image résultante (avec les rectangles) en PNG
    var resultImageBytes = sceneMat.ImEncode(".png");

    // Retourner l'image modifiée (avec les détections)
    return Results.File(resultImageBytes, "image/png");
}).DisableAntiforgery();


app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
