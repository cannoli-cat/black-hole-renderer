using System;
using UnityEngine;

namespace CannoliCat.Space {
    public static class BlackbodyLUT {
        private const double SecondRadiationConstant = 1.4388e-2;
        private const int Size = 256;
        public const double MinKelvin = 500.0;
        public const double MaxKelvin = 100000.0;
        
        private static double Planck(double lambdaMeters, double kelvin) {
            if (kelvin <= 0) return 0.0;
            
            var exponent = SecondRadiationConstant / (lambdaMeters * kelvin);
            var denominator = Math.Pow(lambdaMeters, 5) * (Math.Exp(exponent) - 1);
            return 1.0 / denominator;
        }

        private static double Bump(double lambdaNm, double mu, double sigmaLeft, double sigmaRight) {
            var sigma = lambdaNm < mu ? sigmaLeft : sigmaRight;
            var t = (lambdaNm - mu) / sigma;

            return Math.Exp(-0.5 * t * t);
        }

        private static double CieX(double lambdaNm) {
            return 1.056 * Bump(lambdaNm, 599.8, 37.9, 31.0) +
                   0.362 * Bump(lambdaNm, 442.0, 16.0, 26.7) -
                   0.065 * Bump(lambdaNm, 501.1, 20.4, 26.2);
        }
        
        private static double CieY(double lambdaNm) {
            return 0.821 * Bump(lambdaNm, 568.8, 46.9, 40.5) +
                   0.286 * Bump(lambdaNm, 530.9, 16.3, 31.1);
        }
        
        private static double CieZ(double lambdaNm) {
            return 1.217 * Bump(lambdaNm, 437.0, 11.8, 36.0) +
                   0.681 * Bump(lambdaNm, 459.0, 26.0, 13.8);
        }

        private static (double x, double y, double z) BlackbodyXyz(double kelvin) {
            double x = 0.0, y = 0.0, z = 0.0;
            
            for (var lambdaNm = 380; lambdaNm <= 780; lambdaNm += 5) {
                var lambdaMeters = lambdaNm * 1e-9;
                var spectralRadiance = Planck(lambdaMeters, kelvin);
                
                x += spectralRadiance * CieX(lambdaNm);
                y += spectralRadiance * CieY(lambdaNm);
                z += spectralRadiance * CieZ(lambdaNm);
            }
            
            return (x, y, z);
        }

        private static (double r, double g, double b) XyzToLinearSrgb(double x, double y, double z) {
            var r = Math.Max(0.0, 3.2406 * x - 1.5372 * y - 0.4986 * z);
            var g = Math.Max(0.0, -0.9689 * x + 1.8758 * y + 0.0415 * z);
            var b = Math.Max(0.0, 0.0557 * x - 0.2040 * y + 1.0570 * z);

            return (r, g, b);
        }

        public static Texture2D Build() {
            var referenceY = BlackbodyXyz(6500).y;
            var tex = new Texture2D(Size, 1, TextureFormat.RGBAHalf, mipChain: false, linear: true) {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                name = "BlackbodyLUT"
            };

            for (var i = 0; i < Size; i++) {
                var u = i / (Size - 1.0);
                var kelvin = MinKelvin * Math.Pow(MaxKelvin / MinKelvin, u);
                
                var (x, y, z) = BlackbodyXyz(kelvin);
                var (r, g, b) = XyzToLinearSrgb(x, y, z);
                
                tex.SetPixel(i, 0, new Color((float)(r / referenceY), (float)(g / referenceY), (float)(b / referenceY), 1f));
            }
            
            tex.Apply();
            return tex;
        }
    }
}