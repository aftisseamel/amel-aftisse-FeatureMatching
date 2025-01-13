// See https://aka.ms/new-console-template for more information

using System.Text.Json;
using amel.aftisse.FeatureMatching;

using System.Collections.Generic; 
using System.IO; 
using System.Reflection; 
using System.Text.Json; 
using System.Threading.Tasks; 

Console.WriteLine("Hello, World!");

string objectImagePath = args[0]; 
string scenesDirectory = args[1];

byte[] objectImageData = File.ReadAllBytes(objectImagePath);
string[] imageSceneFiles = Directory.GetFiles(scenesDirectory, "*.jpg");
var imagesSceneData = imageSceneFiles.Select(file => File.ReadAllBytes(file)).ToList();
var objectDetection = new ObjectDetection();

// Lancer la détection d'objets en parallèle pour toutes les scènes
var detectObjectInScenesResults = await objectDetection.DetectObjectInScenesAsync(objectImageData, imagesSceneData);
    
foreach (var objectDetectionResult in detectObjectInScenesResults) 
{ 
    Console.WriteLine($"Points: {JsonSerializer.Serialize(objectDetectionResult.Points)}"); 
} 