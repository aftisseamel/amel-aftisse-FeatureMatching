namespace amel.aftisse.FeatureMatching;
using OpenCvSharp;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class ObjectDetection 
{ 
    // Méthode principale qui détecte l'objet dans plusieurs scènes en parallèle
    public async Task<IList<ObjectDetectionResult>> DetectObjectInScenesAsync(byte[] objectImageData, IList<byte[]> imagesSceneData) 
    { 
        // Liste pour stocker les résultats de la détection
        var detectionResults = new List<ObjectDetectionResult>();

        // Exécuter la détection sur chaque image en parallèle avec Task.Run
        var tasks = imagesSceneData.Select(sceneData =>
            Task.Run(() => DetectObjectInScene(objectImageData, sceneData))
        );

        // Attendre que toutes les tâches se terminent
        var results = await Task.WhenAll(tasks);
        detectionResults.AddRange(results);

        return detectionResults;
    }

    // Méthode privée qui fait la détection sur une seule image
    public ObjectDetectionResult DetectObjectInScene(byte[] imageObjectData, byte[] imageSceneData)
    {
        // Charger les images (objet et scène) depuis les données binaires
        using var imgobject = Mat.FromImageData(imageObjectData, ImreadModes.Color);
        using var imgScene = Mat.FromImageData(imageSceneData, ImreadModes.Color);

        // Créer un détecteur ORB et calculer les descripteurs
        using var orb = ORB.Create(10000);
        using var descriptors1 = new Mat();
        using var descriptors2 = new Mat();
        orb.DetectAndCompute(imgobject, null, out var keyPoints1, descriptors1);
        orb.DetectAndCompute(imgScene, null, out var keyPoints2, descriptors2);

        // Faire l’appariement des descripteurs avec BFMatcher
        using var bf = new BFMatcher(NormTypes.Hamming, crossCheck: true);
        var matches = bf.Match(descriptors1, descriptors2);

        // Garder les 10 meilleures correspondances
        var goodMatches = matches
            .OrderBy(x => x.Distance)
            .Take(10)
            .ToArray();

        // Extraire les points des bonnes correspondances
        var srcPts = goodMatches.Select(m => keyPoints1[m.QueryIdx].Pt).Select(p => new Point2d(p.X, p.Y));
        var dstPts = goodMatches.Select(m => keyPoints2[m.TrainIdx].Pt).Select(p => new Point2d(p.X, p.Y));

        // Calculer l’homographie entre les points de l'objet et ceux de la scène
        using var homography = Cv2.FindHomography(srcPts, dstPts, HomographyMethods.Ransac, 5, null);

        // Déterminer les bords de l'objet et les transformer
        int h = imgobject.Height, w = imgobject.Width;
        var img2Bounds = new[]
        {
            new Point2d(0, 0),
            new Point2d(0, h - 1),
            new Point2d(w - 1, h - 1),
            new Point2d(w - 1, 0),
        };
        var img2BoundsTransformed = Cv2.PerspectiveTransform(img2Bounds, homography);

        // Dessiner un contour rouge autour de l'objet détecté
        using var view = imgScene.Clone();
        var drawingPoints = img2BoundsTransformed.Select(p => (Point)p).ToArray();
        Cv2.Polylines(view, new[] { drawingPoints }, true, Scalar.Red, 3);

        // Convertir l'image résultante en tableau de bytes
        var imageResult = view.ToBytes(".png");

        // Retourner le résultat avec les points détectés
        return new ObjectDetectionResult()
        {
            ImageData = imageResult,
            Points = drawingPoints.Select(point => new ObjectDetectionPoint() { X = point.X, Y = point.Y }).ToList()
        };
    }
}
