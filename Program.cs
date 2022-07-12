using Cocona;
using PoCSteganography.Helpers;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace PoCSteganography
{
    class Program
    {
        static void Main(string[] args)
        {
            CoconaLiteApp.Run<Program>(args);
        }

        public void Hide([Option('f')] string imageFile, [Option('t')] string textFile, [Option('p')] string password = "")
        {
            if (File.Exists(imageFile) && File.Exists(textFile))
            {
                using (Image<Rgba32> inputImage = Image.Load<Rgba32>(imageFile))
                {
                    Console.WriteLine("Read image file...");
                    using (StreamReader sr = File.OpenText(textFile))
                    {
                        string text = sr.ReadToEnd();
                        Console.WriteLine("Read text file...");

                        if (inputImage.Height * inputImage.Width * 3 / 8 >= text.Length + 1)
                        {
                            if (!string.IsNullOrEmpty(password))
                                text = AESEncryptionHelper.Encrypt(text, password);

                            Console.WriteLine("Start the main process HIDE...");
                            Image outImage = SteganographyHelper.EmbedText(text, inputImage);
                            Console.WriteLine("Finished the main process HIDE.");

                            outImage.SaveAsPng("hidden.png");
                            Console.WriteLine("Your output image file is: hidden.png");
                        }
                        else
                            Console.WriteLine("Insufficient pixels to hide text. Larger image size required.");
                    }
                }
            }
            else
                Console.WriteLine("File not found");
        }

        public void Unhide([Option('f')] string file, [Option('p')] string password = "")
        {
            if (File.Exists(file))
            {
                using Image<Rgba32> inputImage = Image.Load<Rgba32>(file);
                Console.WriteLine("Read image file...");

                Console.WriteLine("Start the main process UNHIDE...");
                string text = SteganographyHelper.ExtractText(inputImage);

                if (!string.IsNullOrEmpty(password))
                    text = AESEncryptionHelper.Decrypt(text, password);
                Console.WriteLine("Finished the main process UNHIDE...");

                File.WriteAllText("secret.txt", text);
                Console.WriteLine("Your output text file is: secret.txt");
            }
            else
                Console.WriteLine("File not found");
        }
    }
}
