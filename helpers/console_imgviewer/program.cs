using System;
using System.IO;
using System.Linq;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.PixelFormats;
using static System.Net.Mime.MediaTypeNames;

class Program
{
    static void Main(string[] args)
    {
        string imagePath;
        int consoleWidth = 80;  // Default width
        int consoleHeight = 25; // Default height
        bool use256Colors = false;
        bool useAlternateBuffer = true; // Default to alternate buffer
        bool showFileInfo = false; // Default to not showing file info
        int argIndex = 0; // Define outside the if block

        if (args.Length > 0)
        {
            // Check for buffer selection flags first
            if (args[0] == "-main" || args[0] == "-alternate")
            {
                useAlternateBuffer = args[0] == "-alternate";
                argIndex = 1;
            }

            // Check for file info flag
            if (argIndex < args.Length && args[argIndex] == "-fileinfo")
            {
                showFileInfo = true;
                argIndex++;
            }

            // Check if the next argument is the -256colors flag
            if (argIndex < args.Length && args[argIndex] == "-256colors")
            {
                use256Colors = true;
                argIndex++;
            }

            if (argIndex < args.Length)
            {
                imagePath = args[argIndex];
                argIndex++;
            }
            else
            {
                Console.WriteLine("Usage: ImageViewer [-main|-alternate] [-fileinfo] [-256colors] <image_path> [<console_width> <console_height>]");
                return;
            }
        }
        else
        {
            Console.WriteLine("Usage: ImageViewer [-main|-alternate] [-fileinfo] [-256colors] <image_path> [<console_width> <console_height>]");
            return;
        }

        // Try to get console dimensions if available, otherwise use defaults
        try
        {
            consoleWidth = Console.WindowWidth;
            consoleHeight = Console.WindowHeight - 1;
        }
        catch
        {
            // Use default values if console dimensions are not available
        }

        // Check for optional arguments (after imagePath)
        if (args.Length > argIndex)
            consoleWidth = int.Parse(args[argIndex]);
        if (args.Length > argIndex + 1)
            consoleHeight = int.Parse(args[argIndex + 1]) - 1;

        // Get the directory and filename from the provided image path
        string imageDirectory = Path.GetDirectoryName(Path.GetFullPath(imagePath)) ?? Environment.CurrentDirectory;
        string imageFileName = Path.GetFileName(imagePath);

        // Get all supported image files in the directory, sorted by name
        string[] supportedExtensions = { ".jpg", ".jpeg", ".png", ".bmp", ".gif", ".tiff", ".webp", ".ico" };
        string[] imageFiles = Directory.GetFiles(imageDirectory ?? Environment.CurrentDirectory)
            .Where(file => supportedExtensions.Contains(Path.GetExtension(file).ToLowerInvariant()))
            .OrderBy(file => Path.GetFileName(file))
            .ToArray();

        // Find the index of the current image in the list
        int currentIndex = Array.IndexOf(imageFiles, Path.GetFullPath(imagePath));
        if (currentIndex == -1)
        {
            // If the file is not in the list, add it and sort again
            var allFiles = imageFiles.ToList();
            allFiles.Add(imagePath);
            allFiles = allFiles.Distinct().OrderBy(f => Path.GetFileName(f)).ToList();
            imageFiles = allFiles.ToArray();
            currentIndex = Array.IndexOf(imageFiles, Path.GetFullPath(imagePath));
        }

        // Set up Ctrl+C handler to ensure buffer cleanup
        bool isUsingAlternateBuffer = useAlternateBuffer;
        Console.CancelKeyPress += (sender, e) => {
            e.Cancel = true; // Cancel the default Ctrl+C behavior
            if (isUsingAlternateBuffer)
            {
                Console.Write("\u001b[?1049l"); // Switch back to main buffer
            }
            Environment.Exit(0); // Exit the program gracefully
        };

        try
        {
            Image<Rgba32> originalImage = SixLabors.ImageSharp.Image.Load<Rgba32>(imagePath);
            Image<Rgba32> currentImage = originalImage.CloneAs<Rgba32>(); // Create a copy
            int rotationStep = 0; // Track rotation state: 0=0°, 1=90°, 2=180°, 3=270°

            // Switch to alternate buffer if selected
            if (useAlternateBuffer)
            {
                Console.Write("\u001b[?1049h"); // Switch to alternate buffer
                Console.Write("\u001b[H"); // Move cursor to top-left
            }

            // Render the initial image
            Console.Clear();
            RenderImage(currentImage, consoleWidth, consoleHeight, imagePath, use256Colors, showFileInfo);

            // Main display loop with rotation and file navigation functionality
            bool continueDisplay = true;
            while (continueDisplay)
            {
                // Wait for key input
                ConsoleKeyInfo keyInfo = Console.ReadKey(true); // true means don't echo the key to the console

                switch (keyInfo.Key)
                {
                    case ConsoleKey.R:
                        // Rotate 90 degrees clockwise and update rotation state
                        currentImage?.Dispose(); // Dispose of the old image
                        rotationStep = (rotationStep + 1) % 4; // Cycle through 0, 1, 2, 3
                        currentImage = RotateImageBySteps(originalImage, rotationStep);

                        // Redraw the rotated image
                        Console.Clear();
                        RenderImage(currentImage, consoleWidth, consoleHeight, imagePath, use256Colors, showFileInfo);
                        ClearInputBuffer();
                        break;
                    case ConsoleKey.C:
                        // Toggle between 256 color and 24bit color mode
                        use256Colors = !use256Colors;

                        // Redraw the image with new color mode
                        Console.Clear();
                        RenderImage(currentImage, consoleWidth, consoleHeight, imagePath, use256Colors, showFileInfo);
                        ClearInputBuffer();
                        break;
                    case ConsoleKey.I:
                        // Toggle file info display
                        showFileInfo = !showFileInfo;

                        // Redraw the image with new file info setting
                        Console.Clear();
                        RenderImage(currentImage, consoleWidth, consoleHeight, imagePath, use256Colors, showFileInfo);
                        ClearInputBuffer();
                        break;
                    case ConsoleKey.UpArrow:
                        // Navigate to previous image
                        currentIndex = (currentIndex - 1 + imageFiles.Length) % imageFiles.Length;
                        originalImage?.Dispose();
                        originalImage = SixLabors.ImageSharp.Image.Load<Rgba32>(imageFiles[currentIndex]);
                        currentImage?.Dispose();
                        currentImage = originalImage.CloneAs<Rgba32>();
                        rotationStep = 0; // Reset rotation when changing files

                        // Redraw the new image
                        Console.Clear();
                        RenderImage(currentImage, consoleWidth, consoleHeight, imageFiles[currentIndex], use256Colors, showFileInfo);
                        ClearInputBuffer();
                        break;
                    case ConsoleKey.DownArrow:
                        // Navigate to next image
                        currentIndex = (currentIndex + 1) % imageFiles.Length;
                        originalImage?.Dispose();
                        originalImage = SixLabors.ImageSharp.Image.Load<Rgba32>(imageFiles[currentIndex]);
                        currentImage?.Dispose();
                        currentImage = originalImage.CloneAs<Rgba32>();
                        rotationStep = 0; // Reset rotation when changing files

                        // Redraw the new image
                        Console.Clear();
                        RenderImage(currentImage, consoleWidth, consoleHeight, imageFiles[currentIndex], use256Colors, showFileInfo);
                        ClearInputBuffer();
                        break;
                    case ConsoleKey.Q:
                    case ConsoleKey.Escape:
                        continueDisplay = false;
                        break;
                    // Ignore all other keys without redrawing
                }
            }

            // Switch back to main buffer if we were using alternate buffer
            if (useAlternateBuffer)
            {
                Console.Write("\u001b[?1049l"); // Switch back to main buffer
            }

            currentImage?.Dispose();
            originalImage?.Dispose();
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error: " + ex.Message);
        }
        finally
        {
            // Ensure we always switch back to main buffer when exiting
            if (useAlternateBuffer)
            {
                Console.Write("\u001b[?1049l"); // Switch back to main buffer
            }
        }

    }

    static void RenderImage(Image<Rgba32> image, int consoleWidth, int consoleHeight, string imagePath, bool use256Colors, bool showFileInfo)
    {
        // Ensure console width is even to prevent output corruption
        if (consoleWidth % 2 != 0)
        {
            consoleWidth -= 1;
        }

        float aspectRatio = (float)image.Width / image.Height;

        int newWidth = (int)(consoleWidth * aspectRatio);
        int newHeight = (int)(newWidth / aspectRatio);

        if (newHeight  > consoleHeight)
        {
            newHeight = consoleHeight;
            newWidth = (int)(newHeight * aspectRatio * 2);
        }

        if (newWidth > consoleWidth)
        {
            newWidth = consoleWidth;
            newHeight = (int)(newWidth / aspectRatio / 2);
        }

        if ((image.Width < consoleWidth * 2) & (image.Height) < consoleHeight)
        {
                newWidth = image.Width * 2;
                newHeight = image.Height;
        }

        // Create a resized version of the image
        Image<Rgba32> resizedImage = image.Clone(ctx => ctx.Resize(newWidth, newHeight));

        Console.SetCursorPosition(0, 0); // Start drawing the image from below the overlay

        FileInfo fileInfo = new FileInfo(imagePath);
        int effectiveWidth = Math.Min(newWidth, resizedImage.Width);
        for (int y = 0; y < resizedImage.Height; y++)
        {
            string text = "";
            if (showFileInfo)
            {
                if (y == 0)
                {
                    text = Path.GetFileName(imagePath);
                }
                if (y == 1)
                {
                    text = $"Dimensions: {image.Width}x{image.Height}";
                }
                else if (y == 2)
                {
                    string fileSize = FormatFileSize(fileInfo.Length);
                    text = $"Size: {fileSize}";
                }
                else if (y == 3)
                {
                    string fileDateTime = fileInfo.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss");
                    text = $"Modified: {fileDateTime}";
                }
            }
            else
            {
                // For lines beyond 5, render as regular image line
                //PrintImageLine(y, resizedImage, use256Colors);
            }
            PrintInfoLine2(showFileInfo, text, y, resizedImage, use256Colors, consoleWidth, effectiveWidth);
        }


        // image info
        /*
        Console.WriteLine($"File: {System.IO.Path.GetFileName(imagePath)}");
        Console.WriteLine($"Image: {image.Width} x {image.Height}");
        Console.WriteLine($"resizedImage: {resizedImage.Width} x {resizedImage.Height}");
        Console.WriteLine($"Console: {consoleWidth} x {consoleHeight}");
        Console.WriteLine($"new: {newWidth} x {newHeight}");
        Console.WriteLine($"aspectRatio: {aspectRatio}");
        Console.WriteLine($"fileNameLength: {fileName.Length}");
        */

        resizedImage?.Dispose();
    }

    static int GetClosestAnsiColor(Rgba32 color)
    {
        int r = (color.R * 6) / 255;
        int g = (color.G * 6) / 255;
        int b = (color.B * 6) / 255;
        return 16 + (r * 36) + (g * 6) + b;
    }

    static Image<Rgba32> RotateImageBySteps(Image<Rgba32> originalImage, int rotationStep)
    {
        // Clone the original image and rotate it based on the step
        var rotatedImage = originalImage.CloneAs<Rgba32>();

        switch (rotationStep)
        {
            case 0: // 0 degrees (no rotation)
                // Return the original image as is
                break;
            case 1: // 90 degrees clockwise
                rotatedImage.Mutate(x => x.Rotate(RotateMode.Rotate90));
                break;
            case 2: // 180 degrees clockwise
                rotatedImage.Mutate(x => x.Rotate(RotateMode.Rotate180));
                break;
            case 3: // 270 degrees clockwise (or 90 degrees counterclockwise)
                rotatedImage.Mutate(x => x.Rotate(RotateMode.Rotate270));
                break;
        }

        return rotatedImage;
    }

    /// <summary>
    /// Gets the display width of a single character
    /// </summary>
    /// <param name="c">Character to measure</param>
    /// <returns>Display width: 0 for zero-width, 1 for single-width, 2 for CJK</returns>
    static int GetCharWidth(char c)
    {
        // Check for zero-width characters first
        if (IsZeroWidthCharacter(c))
        {
            return 0;
        }

        // Check for CJK characters (width 2)
        if (IsCJKCharacter(c))
        {
            return 2;
        }

        // Default to single-width for ASCII and most other characters
        return 1;
    }

    /// <summary>
    /// Checks if a character is a CJK (Chinese, Japanese, Korean) character
    /// </summary>
    /// <param name="c">Character to check</param>
    /// <returns>True if the character is CJK</returns>
    static bool IsCJKCharacter(char c)
    {
        int code = c;

        // CJK Unified Ideographs
        if (code >= 0x4E00 && code <= 0x9FFF) return true;

        // CJK Extension A
        if (code >= 0x3400 && code <= 0x4DBF) return true;

        // Hiragana
        if (code >= 0x3040 && code <= 0x309F) return true;

        // Katakana
        if (code >= 0x30A0 && code <= 0x30FF) return true;

        // Katakana Phonetic Extensions
        if (code >= 0x31F0 && code <= 0x31FF) return true;

        // Hangul Syllables
        if (code >= 0xAC00 && code <= 0xD7AF) return true;

        // Hangul Jamo
        if (code >= 0x1100 && code <= 0x11FF) return true;

        // Fullwidth Forms (fullwidth ASCII variants)
        if (code >= 0xFF00 && code <= 0xFFEF) return true;

        // CJK Compatibility Ideographs
        if (code >= 0xF900 && code <= 0xFAFF) return true;

        // CJK Radicals Supplement
        if (code >= 0x2E80 && code <= 0x2EFF) return true;

        // CJK Symbols and Punctuation
        if (code >= 0x3000 && code <= 0x303F) return true;

        return false;
    }

    /// <summary>
    /// Checks if a character is zero-width (combining marks, zero-width joiners, etc.)
    /// </summary>
    /// <param name="c">Character to check</param>
    /// <returns>True if the character is zero-width</returns>
    static bool IsZeroWidthCharacter(char c)
    {
        int code = c;

        // Combining Diacritical Marks
        if (code >= 0x0300 && code <= 0x036F) return true;

        // Combining Diacritical Marks Extended
        if (code >= 0x1AB0 && code <= 0x1AFF) return true;

        // Combining Diacritical Marks Supplement
        if (code >= 0x1DC0 && code <= 0x1DFF) return true;

        // Combining Half Marks
        if (code >= 0xFE20 && code <= 0xFE2F) return true;

        // Zero Width Space, Zero Width Non-Joiner, Zero Width Joiner
        if (code == 0x200B || code == 0x200C || code == 0x200D) return true;

        // Variation Selectors
        if (code >= 0xFE00 && code <= 0xFE0F) return true;

        return false;
    }

    /// <summary>
    /// Formats file size in human-readable format
    /// </summary>
    /// <param name="bytes">File size in bytes</param>
    /// <returns>Formatted file size string</returns>
    static string FormatFileSize(long bytes)
    {
        string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
        int counter = 0;
        decimal number = bytes;

        while (Math.Round(number / 1024) >= 1)
        {
            number = number / 1024;
            counter++;
        }

        return string.Format("{0:n1}{1}", number, suffixes[counter]);
    }

    /// <summary>
    /// Prints an info line with appropriate background colors
    /// </summary>
    /// <param name="showFileInfot">true to show FileInfo</param>
    /// <param name="text">Text to display</param>
    /// <param name="line">Line number (0-based)</param>
    /// <param name="image">The image to get background colors from</param>
    /// <param name="use256Colors">Whether to use 256-color mode</param>
    /// <param name="consoleWidth">Console width</param>
    /// <param name="effectiveWidth">min (newWidth,Image width)</param>
    static void PrintInfoLine2(bool showFileInfo, string text, int line, Image<Rgba32> image, bool use256Colors, int consoleWidth, int effectiveWidth)
    {
        // Draw the filename at the top-left with dimmed background if showFileInfo is true
        int pixelPosition = 0; // Track pixel position considering character widths

        if (showFileInfo)
        {
            int charIndex = 0;
            for (; charIndex < text.Length && pixelPosition < consoleWidth; charIndex++)
            {
                char currentChar = text[charIndex];
                int charWidth = GetCharWidth(currentChar);

                // Skip zero-width characters
                if (charWidth == 0)
                {
                    continue;
                }

                // Check if this character would exceed the available width
                if (pixelPosition + charWidth > consoleWidth)
                {
                    break;
                }

                // Get the pixel color that would be at this position (for the first of the pair)
                // Use the resized image width to avoid out-of-bounds, but allow text to go to console width
                //int effectiveWidth = Math.Min(newWidth, resizedImage.Width);
                if (pixelPosition >= effectiveWidth)
                {
                    // If we're beyond the image width, just print the character with a default background
                    byte dimR = 0, dimG = 0, dimB = 0; // Use black as default background

                    // Print the character with white text on default background
                    if (use256Colors)
                    {
                        int ansiColor = GetClosestAnsiColor(new Rgba32(dimR, dimG, dimB, 255));
                        Console.Write($"\u001b[48;5;{ansiColor};38;5;15m{currentChar}");
                    }
                    else
                    {
                        Console.Write($"\u001b[48;2;{dimR};{dimG};{dimB};38;2;255;255;255m{currentChar}");
                    }
                }
                else
                {
                    Rgba32 pixelColor = image[pixelPosition, line];

                    // For CJK characters that take 2 positions, we'll use the first pixel's color for the entire character space
                    if (charWidth == 2 && pixelPosition + 1 < effectiveWidth)
                    {
                        // For full-width characters, we'll use the first pixel's color for the entire character space
                        Rgba32 firstPixelColor = image[pixelPosition, line];

                        // Dim the color by 30% for the background
                        byte dimR = (byte)(firstPixelColor.R * 0.7);
                        byte dimG = (byte)(firstPixelColor.G * 0.7);
                        byte dimB = (byte)(firstPixelColor.B * 0.7);

                        // Print the character with white text on dimmed background
                        if (use256Colors)
                        {
                            int ansiColor = GetClosestAnsiColor(new Rgba32(dimR, dimG, dimB, 255));
                            Console.Write($"\u001b[48;5;{ansiColor};38;5;15m{currentChar}");
                        }
                        else
                        {
                            Console.Write($"\u001b[48;2;{dimR};{dimG};{dimB};38;2;255;255;255m{currentChar}");
                        }
                    }
                    else
                    {
                        // For single-width characters
                        // Dim the color by 30% for the background
                        byte dimR = (byte)(pixelColor.R * 0.7);
                        byte dimG = (byte)(pixelColor.G * 0.7);
                        byte dimB = (byte)(pixelColor.B * 0.7);

                        // Print the character with white text on dimmed background
                        if (use256Colors)
                        {
                            int ansiColor = GetClosestAnsiColor(new Rgba32(dimR, dimG, dimB, 255));
                            Console.Write($"\u001b[48;5;{ansiColor};38;5;15m{currentChar}");
                        }
                        else
                        {
                            Console.Write($"\u001b[48;2;{dimR};{dimG};{dimB};38;2;255;255;255m{currentChar}");
                        }
                    }
                }

                pixelPosition += charWidth; // Move by the character width (1 for single-width, 2 for full-width)
            }
        }

        // Continue with the rest of the first line after the filename text
        for (int x = pixelPosition; x < image.Width; x += 2)
        {
            Rgba32 pixelColor = image[x, line];

            if (use256Colors)
            {
                int ansiColor = GetClosestAnsiColor(pixelColor);
                // Print the first half-space
                Console.Write($"\u001b[48;5;{ansiColor}m ");
            }
            else
            {
                // Print the first half-space with 24-bit RGB color
                Console.Write($"\u001b[48;2;{pixelColor.R};{pixelColor.G};{pixelColor.B}m ");
            }

            // Print the second half-space if in bounds
            if (x + 1 < image.Width)
            {
                Rgba32 nextPixelColor = image[x + 1, line];

                if (use256Colors)
                {
                    int nextAnsiColor = GetClosestAnsiColor(nextPixelColor);
                    // Print the second half-space with its own color
                    Console.Write($"\u001b[48;5;{nextAnsiColor}m ");
                }
                else
                {
                    // Print the second half-space with its own 24-bit RGB color
                    Console.Write($"\u001b[48;2;{nextPixelColor.R};{nextPixelColor.G};{nextPixelColor.B}m ");
                }
            }
        }

        // Reset the line and continue with the rest of the image
        Console.WriteLine("\u001b[0m"); // Reset color at the end of the first line

    }

    static void ClearInputBuffer()
    {
        while (Console.KeyAvailable) // Check if a key is available
        {
            Console.ReadKey(true); // Read and discard the key press
        }
    }

}
