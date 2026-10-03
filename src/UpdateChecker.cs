using System;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace EbenTilerWindows
{
    internal enum UpdateCheckStatus
    {
        UpToDate,
        UpdateAvailable,
        NoRelease,
        Failed
    }

    internal sealed class UpdateCheckResult
    {
        public UpdateCheckStatus Status;
        public Version CurrentVersion;
        public Version LatestVersion;
        public string TagName;
        public string ReleaseUrl;
        public string ErrorMessage;
    }

    /// <summary>
    /// GitHub의 공개 최신 릴리스 정보만 확인한다.
    /// 업데이트 파일을 자동 다운로드하거나 설치하지 않으며, 사용자 정보도 전송하지 않는다.
    /// </summary>
    internal static class UpdateChecker
    {
        private const string LatestReleaseApi = "https://api.github.com/repos/Eben-Builds/EbenTiler-for-Windows/releases/latest";
        private const string ReleasesPage = "https://github.com/Eben-Builds/EbenTiler-for-Windows/releases/latest";
        private static readonly object Sync = new object();

        private static string StatePath
        {
            get { return Path.Combine(Config.Directory, "update-state.ini"); }
        }

        public static bool IsAutomaticCheckDue()
        {
            DateTime lastCheckUtc;
            string ignored;
            ReadState(out lastCheckUtc, out ignored);
            if (lastCheckUtc == DateTime.MinValue) return true;
            return DateTime.UtcNow - lastCheckUtc >= TimeSpan.FromHours(24);
        }

        public static bool ShouldNotify(string tagName)
        {
            if (string.IsNullOrWhiteSpace(tagName)) return false;

            DateTime ignored;
            string lastNotifiedTag;
            ReadState(out ignored, out lastNotifiedTag);
            return !string.Equals(lastNotifiedTag, tagName, StringComparison.OrdinalIgnoreCase);
        }

        public static void MarkNotified(string tagName)
        {
            if (string.IsNullOrWhiteSpace(tagName)) return;

            lock (Sync)
            {
                DateTime lastCheckUtc;
                string ignored;
                ReadStateCore(out lastCheckUtc, out ignored);
                WriteStateCore(lastCheckUtc, tagName);
            }
        }

        public static UpdateCheckResult CheckNow()
        {
            lock (Sync)
            {
                UpdateCheckResult result = new UpdateCheckResult();
                result.CurrentVersion = Assembly.GetExecutingAssembly().GetName().Version;
                result.ReleaseUrl = ReleasesPage;

                try
                {
                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(LatestReleaseApi);
                    request.Method = "GET";
                    request.UserAgent = "EbenTiler-for-Windows/" + result.CurrentVersion;
                    request.Accept = "application/vnd.github+json";
                    request.Timeout = 8000;
                    request.ReadWriteTimeout = 8000;
                    request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

                    string json;
                    using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                    using (Stream stream = response.GetResponseStream())
                    using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
                    {
                        json = reader.ReadToEnd();
                    }

                    string tagName = ExtractJsonString(json, "tag_name");
                    string htmlUrl = ExtractJsonString(json, "html_url");
                    Version latestVersion;
                    if (!TryParseReleaseVersion(tagName, out latestVersion))
                    {
                        result.Status = UpdateCheckStatus.Failed;
                        result.ErrorMessage = "최신 릴리스 버전을 확인하지 못했습니다.";
                        RecordCheckNowCore();
                        return result;
                    }

                    result.TagName = tagName;
                    result.LatestVersion = latestVersion;
                    if (!string.IsNullOrWhiteSpace(htmlUrl)) result.ReleaseUrl = htmlUrl;
                    result.Status = latestVersion > result.CurrentVersion
                        ? UpdateCheckStatus.UpdateAvailable
                        : UpdateCheckStatus.UpToDate;

                    RecordCheckNowCore();
                    return result;
                }
                catch (WebException ex)
                {
                    HttpWebResponse response = ex.Response as HttpWebResponse;
                    if (response != null && response.StatusCode == HttpStatusCode.NotFound)
                    {
                        response.Dispose();
                        result.Status = UpdateCheckStatus.NoRelease;
                        RecordCheckNowCore();
                        return result;
                    }

                    if (response != null) response.Dispose();
                    result.Status = UpdateCheckStatus.Failed;
                    result.ErrorMessage = "인터넷 연결 또는 GitHub 응답을 확인하지 못했습니다.";
                    RecordCheckNowCore();
                    return result;
                }
                catch (IOException)
                {
                    result.Status = UpdateCheckStatus.Failed;
                    result.ErrorMessage = "업데이트 정보를 읽는 중 연결이 끊어졌습니다.";
                    RecordCheckNowCore();
                    return result;
                }
                catch (Exception)
                {
                    result.Status = UpdateCheckStatus.Failed;
                    result.ErrorMessage = "업데이트 정보를 확인하지 못했습니다.";
                    RecordCheckNowCore();
                    return result;
                }
            }
        }

        private static bool TryParseReleaseVersion(string tagName, out Version version)
        {
            version = null;
            if (string.IsNullOrWhiteSpace(tagName)) return false;

            string value = tagName.Trim();
            if (value.StartsWith("v", StringComparison.OrdinalIgnoreCase)) value = value.Substring(1);
            int dash = value.IndexOf('-');
            if (dash >= 0) value = value.Substring(0, dash);
            return Version.TryParse(value, out version);
        }

        private static string ExtractJsonString(string json, string propertyName)
        {
            if (string.IsNullOrEmpty(json) || string.IsNullOrEmpty(propertyName)) return null;

            Match match = Regex.Match(
                json,
                "\\\"" + Regex.Escape(propertyName) + "\\\"\\s*:\\s*\\\"(?<value>(?:\\\\.|[^\\\"])*)\\\"",
                RegexOptions.CultureInvariant);
            if (!match.Success) return null;

            string value = match.Groups["value"].Value;
            return value.Replace("\\/", "/").Replace("\\\"", "\"").Replace("\\\\", "\\");
        }

        private static void RecordCheckNowCore()
        {
            DateTime ignored;
            string lastNotifiedTag;
            ReadStateCore(out ignored, out lastNotifiedTag);
            WriteStateCore(DateTime.UtcNow, lastNotifiedTag);
        }

        private static void ReadState(out DateTime lastCheckUtc, out string lastNotifiedTag)
        {
            lock (Sync)
            {
                ReadStateCore(out lastCheckUtc, out lastNotifiedTag);
            }
        }

        private static void ReadStateCore(out DateTime lastCheckUtc, out string lastNotifiedTag)
        {
            lastCheckUtc = DateTime.MinValue;
            lastNotifiedTag = "";

            try
            {
                if (!File.Exists(StatePath)) return;
                string[] lines = File.ReadAllLines(StatePath, Encoding.UTF8);
                for (int i = 0; i < lines.Length; i++)
                {
                    string line = lines[i].Trim();
                    int eq = line.IndexOf('=');
                    if (eq <= 0) continue;

                    string key = line.Substring(0, eq).Trim();
                    string value = line.Substring(eq + 1).Trim();
                    if (string.Equals(key, "LastCheckUtc", StringComparison.OrdinalIgnoreCase))
                    {
                        DateTime parsed;
                        if (DateTime.TryParse(value, null, System.Globalization.DateTimeStyles.RoundtripKind, out parsed))
                            lastCheckUtc = parsed.ToUniversalTime();
                    }
                    else if (string.Equals(key, "LastNotifiedTag", StringComparison.OrdinalIgnoreCase))
                    {
                        lastNotifiedTag = value;
                    }
                }
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }

        private static void WriteStateCore(DateTime lastCheckUtc, string lastNotifiedTag)
        {
            try
            {
                if (!Directory.Exists(Config.Directory)) Directory.CreateDirectory(Config.Directory);

                StringBuilder sb = new StringBuilder();
                sb.AppendLine("; EbenTiler 업데이트 확인 상태");
                if (lastCheckUtc != DateTime.MinValue)
                    sb.AppendLine("LastCheckUtc=" + lastCheckUtc.ToUniversalTime().ToString("o"));
                if (!string.IsNullOrWhiteSpace(lastNotifiedTag))
                    sb.AppendLine("LastNotifiedTag=" + lastNotifiedTag);
                File.WriteAllText(StatePath, sb.ToString(), new UTF8Encoding(false));
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }
}
