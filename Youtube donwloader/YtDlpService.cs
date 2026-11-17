using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Youtube_donwloader.Models;

namespace Youtube_donwloader.Services
{
    public class YtDlpService
    {
        private static readonly string ApplicationDirectory = AppContext.BaseDirectory;
        private static readonly string YtDlpPath = Path.Combine(ApplicationDirectory, "yt-dlp.exe");

        public event Action<string>? LogReceived;
        public event Action<double>? ProgressChanged;

        public async Task<VideoMetadata?> GetVideoMetadataAsync(string url)
        {
            var startInfo = CreateStartInfo();
            startInfo.ArgumentList.Add("--dump-single-json");
            startInfo.ArgumentList.Add("--no-warnings");
            AddJavaScriptRuntime(startInfo);
            startInfo.ArgumentList.Add(url);

            using var process = new Process { StartInfo = startInfo };
            process.Start();
            Task<string> outputTask = process.StandardOutput.ReadToEndAsync();
            Task<string> errorTask = process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();
            string output = await outputTask;
            string error = await errorTask;

            if (process.ExitCode != 0)
                throw new InvalidOperationException(string.IsNullOrWhiteSpace(error)
                    ? $"yt-dlp exited with code {process.ExitCode}."
                    : error.Trim());

            if (string.IsNullOrWhiteSpace(output)) return null;

            try
            {
                using var document = JsonDocument.Parse(output);
                var root = document.RootElement;
                return new VideoMetadata
                {
                    Title = root.TryGetProperty("title", out var title) ? title.GetString() ?? "Unknown" : "Unknown",
                    Thumbnail = root.TryGetProperty("thumbnail", out var thumbnail) ? thumbnail.GetString() ?? string.Empty : string.Empty,
                    Uploader = root.TryGetProperty("uploader", out var uploader) ? uploader.GetString() ?? string.Empty : string.Empty,
                    Duration_String = root.TryGetProperty("duration_string", out var duration) ? duration.GetString() ?? string.Empty : string.Empty
                };
            }
            catch (JsonException)
            {
                return null;
            }
        }

        public async Task DownloadVideoAsync(string url, string folderPath, string formatMode, CancellationToken token)
        {
            if (!Directory.Exists(folderPath))
                throw new DirectoryNotFoundException($"The download folder does not exist: {folderPath}");

            string stagingFolder = Path.Combine(folderPath, $".ytdlp-{Guid.NewGuid():N}");
            Directory.CreateDirectory(stagingFolder);
            Process? process = null;
            try
            {
                var startInfo = CreateStartInfo();
                startInfo.ArgumentList.Add("--ignore-config");
                ConfigureFormat(startInfo, formatMode);
                startInfo.ArgumentList.Add("-o");
                startInfo.ArgumentList.Add(Path.Combine(stagingFolder, "%(title)s.%(ext)s"));
                startInfo.ArgumentList.Add("--no-keep-video");
                startInfo.ArgumentList.Add("--no-mtime");
                startInfo.ArgumentList.Add("--no-continue");
                startInfo.ArgumentList.Add("--retries");
                startInfo.ArgumentList.Add("10");
                startInfo.ArgumentList.Add("--fragment-retries");
                startInfo.ArgumentList.Add("10");
                AddJavaScriptRuntime(startInfo);
                startInfo.ArgumentList.Add(url);

                process = new Process { StartInfo = startInfo, EnableRaisingEvents = true };
                process.OutputDataReceived += (_, e) => HandleOutput(e.Data);
                process.ErrorDataReceived += (_, e) => HandleOutput(e.Data);
                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                await process.WaitForExitAsync(token);

                if (process.ExitCode != 0)
                    throw new InvalidOperationException($"yt-dlp failed with exit code {process.ExitCode}. Check the log for details.");

                string expectedExtension = formatMode == "Audio Only (MP3)" ? ".mp3" : ".mp4";
                string[] createdFiles = Directory.GetFiles(stagingFolder, $"*{expectedExtension}", SearchOption.TopDirectoryOnly);
                if (createdFiles.Length == 0)
                    throw new InvalidOperationException($"The download did not produce a final {expectedExtension} file. The audio/video streams may not have merged; check that ffmpeg.exe is installed and working.");

                foreach (string createdFile in createdFiles)
                {
                    string destination = GetAvailableDestination(folderPath, Path.GetFileName(createdFile));
                    File.Move(createdFile, destination);
                    LogReceived?.Invoke($"[System] Saved merged file: {destination}");
                }
            }
            catch (OperationCanceledException)
            {
                if (process is not null && !process.HasExited)
                    process.Kill(entireProcessTree: true);
                if (process is not null && !process.HasExited)
                    await process.WaitForExitAsync();
                LogReceived?.Invoke("[System] Download cancelled.");
                throw;
            }
            finally
            {
                process?.Dispose();
                try
                {
                    if (Directory.Exists(stagingFolder))
                        Directory.Delete(stagingFolder, recursive: true);
                }
                catch (IOException)
                {
                    LogReceived?.Invoke($"[Warning] Could not remove temporary download folder: {stagingFolder}");
                }
                catch (UnauthorizedAccessException)
                {
                    LogReceived?.Invoke($"[Warning] Could not remove temporary download folder: {stagingFolder}");
                }
            }
        }

        private static string GetAvailableDestination(string folderPath, string fileName)
        {
            string destination = Path.Combine(folderPath, fileName);
            if (!File.Exists(destination)) return destination;

            string name = Path.GetFileNameWithoutExtension(fileName);
            string extension = Path.GetExtension(fileName);
            for (int suffix = 1; ; suffix++)
            {
                destination = Path.Combine(folderPath, $"{name} ({suffix}){extension}");
                if (!File.Exists(destination)) return destination;
            }
        }

        private static ProcessStartInfo CreateStartInfo()
        {
            if (!File.Exists(YtDlpPath))
                throw new FileNotFoundException($"yt-dlp.exe was not found in the application folder: {ApplicationDirectory}", YtDlpPath);

            return new ProcessStartInfo
            {
                FileName = YtDlpPath,
                WorkingDirectory = ApplicationDirectory,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = System.Text.Encoding.UTF8,
                StandardErrorEncoding = System.Text.Encoding.UTF8
            };
        }

        private static void ConfigureFormat(ProcessStartInfo startInfo, string mode)
        {
            switch (mode)
            {
                case "Audio Only (MP3)":
                    startInfo.ArgumentList.Add("-x");
                    startInfo.ArgumentList.Add("--audio-format");
                    startInfo.ArgumentList.Add("mp3");
                    break;
                case "Max resolution (4K)":
                    startInfo.ArgumentList.Add("-f");
                    startInfo.ArgumentList.Add("bestvideo[height>=2160]+bestaudio/bestvideo+bestaudio/best");
                    startInfo.ArgumentList.Add("--merge-output-format");
                    startInfo.ArgumentList.Add("mp4");
                    break;
                case "720p":
                    startInfo.ArgumentList.Add("-f");
                    startInfo.ArgumentList.Add("bestvideo[height<=720]+bestaudio/best[height<=720]/bestvideo+bestaudio/best");
                    startInfo.ArgumentList.Add("--merge-output-format");
                    startInfo.ArgumentList.Add("mp4");
                    break;
                case "1080p":
                    startInfo.ArgumentList.Add("-f");
                    startInfo.ArgumentList.Add("bestvideo[height<=1080]+bestaudio/best[height<=1080]/bestvideo+bestaudio/best");
                    startInfo.ArgumentList.Add("--merge-output-format");
                    startInfo.ArgumentList.Add("mp4");
                    break;
                default:
                    startInfo.ArgumentList.Add("-f");
                    startInfo.ArgumentList.Add("bestvideo[height<=2160]+bestaudio/best[height<=2160]/bestvideo+bestaudio/best");
                    startInfo.ArgumentList.Add("--merge-output-format");
                    startInfo.ArgumentList.Add("mp4");
                    break;
            }

            if (mode != "Audio Only (MP3)")
            {
                startInfo.ArgumentList.Add("--remux-video");
                startInfo.ArgumentList.Add("mp4");
            }


            string ffmpegPath = ValidateFfmpeg();


            startInfo.ArgumentList.Add("--ffmpeg-location");
            startInfo.ArgumentList.Add(ffmpegPath);
        }

        private static string ValidateFfmpeg()
        {
            string? ffmpegPath = FindExecutable("ffmpeg.exe");
            if (ffmpegPath is null)
                throw new FileNotFoundException("ffmpeg.exe is required to combine video and audio. Put a working ffmpeg.exe next to the app, or add its folder to PATH.");

            using (var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = ffmpegPath,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                }
            })
            {
                process.StartInfo.ArgumentList.Add("-version");

                try
                {
                    process.Start();
                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();

                    if (!process.WaitForExit(5000))
                    {
                        process.Kill(entireProcessTree: true);
                        throw new InvalidOperationException("ffmpeg.exe did not respond to the version check within 5 seconds.");
                    }

                    if (process.ExitCode != 0)
                        throw new InvalidOperationException($"ffmpeg.exe could not run: {error.Trim()}");
                }
                catch (Exception ex) when (ex is System.ComponentModel.Win32Exception || ex is InvalidOperationException)
                {
                    throw new InvalidOperationException($"The ffmpeg executable was found at '{ffmpegPath}', but it is not working.", ex);
                }
            }

            return ffmpegPath;
        }

        private void AddJavaScriptRuntime(ProcessStartInfo startInfo)
        {
            string? denoPath = FindExecutable("deno.exe");
            if (denoPath is null)
            {
                LogReceived?.Invoke("[Warning] Deno was not found. YouTube may omit formats or reject downloads. Put deno.exe next to the application or add it to PATH.");
                return;
            }

            startInfo.ArgumentList.Add("--js-runtimes");
            startInfo.ArgumentList.Add($"deno:{denoPath}");
            startInfo.ArgumentList.Add("--remote-components");
            startInfo.ArgumentList.Add("ejs:github");
        }

        private static string? FindExecutable(string fileName)
        {
            string appLocalPath = Path.Combine(ApplicationDirectory, fileName);
            if (File.Exists(appLocalPath)) return appLocalPath;

            string? pathValue = Environment.GetEnvironmentVariable("PATH");
            if (string.IsNullOrWhiteSpace(pathValue)) return null;

            foreach (string directory in pathValue.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
            {
                string candidate = Path.Combine(directory.Trim(), fileName);
                if (File.Exists(candidate)) return candidate;
            }

            return null;
        }

        private void HandleOutput(string? data)
        {
            if (string.IsNullOrEmpty(data)) return;

            LogReceived?.Invoke(data);
            var match = Regex.Match(data, @"\[download\]\s+(\d+(?:\.\d+)?)%");
            if (match.Success && double.TryParse(match.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out double progress))
                ProgressChanged?.Invoke(progress);
        }
    }
}