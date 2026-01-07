using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using Cysharp.Threading.Tasks;
using Michsky.UI.Heat;
using UnityEngine;

public class LevelRemoteConfig : Singleton<LevelRemoteConfig>
{
    [Header("Remote CSV Settings")]
    [SerializeField] private string csvUrl = "";
    [SerializeField] private bool loadFromLocal;

    [Header("Level Data")]
    [SerializeField] private List<LevelConfig> levelConfigs = new List<LevelConfig>();

    [Header("Loading scene")]
    [SerializeField] private ProgressBar myProgressBar;

    public Action<List<LevelConfig>> OnLoadComplete;

    private static readonly HttpClient httpClient = new HttpClient();

    // Start vẫn là void; gọi async task an toàn bằng .Forget()
    void Start()
    {
        // chạy async, tự huỷ khi object bị destroy
        if (loadFromLocal)
        {
            //LoadFromLocalFiles();
            OnLoadComplete?.Invoke(levelConfigs);
            return;
        }
        var ct = this.GetCancellationTokenOnDestroy();

        //LoadCsvWithHttpClientAsync(csvUrl, ct).Forget();

        // tạo progress handler (cập nhật UI)
        var progress = new Progress<float>(p =>
        {
            // đây chạy trên main thread vì hàm LoadCsvWithHttpClientAsync đảm bảo Report được gọi từ main
            // ví dụ: update slider hoặc TMP text
            myProgressBar.SetValue(p * 100f);
        });

        // fire-and-forget nhưng an toàn
        LoadCsvWithHttpClientAsync(csvUrl, ct, progress).Forget();

    }

    // Public nếu cần gọi lại từ ngoài
    public UniTask ReloadAsync(CancellationToken ct = default)
    {
        return LoadCsvWithHttpClientAsync(csvUrl, ct);
    }

    private async UniTask LoadCsvWithHttpClientAsync(string url, CancellationToken ct, IProgress<float> progress = null)
    {
        levelConfigs.Clear();

        // try
        // {
        // 1) Download stream with progress on thread-pool
        string csv = await UniTask.RunOnThreadPool(async () =>
        {
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct);
            var linkedToken = linked.Token;

            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            // Ask for headers first so we can stream
            var resp = await httpClient.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, linkedToken).ConfigureAwait(false);
            resp.EnsureSuccessStatusCode();

            var contentLength = resp.Content.Headers.ContentLength ?? -1L;
            using var stream = await resp.Content.ReadAsStreamAsync().ConfigureAwait(false);

            const int bufferSize = 81920;
            var buffer = new byte[bufferSize];
            long totalRead = 0;
            using var ms = new System.IO.MemoryStream();

            while (true)
            {
                linkedToken.ThrowIfCancellationRequested();

                int read = await stream.ReadAsync(buffer, 0, buffer.Length, linkedToken).ConfigureAwait(false);
                if (read == 0) break;

                ms.Write(buffer, 0, read);
                totalRead += read;

                // Nếu biết content-length, báo progress; nếu không biết, bạn có thể báo -1 hoặc skip
                if (contentLength > 0 && progress != null)
                {
                    float p = Mathf.Clamp01((float)totalRead / (float)contentLength);

                    // Report must be done on main thread so UI handlers are safe.
                    // We switch to main briefly, report, then switch back to threadpool to continue.
                    await UniTask.SwitchToMainThread(linkedToken);
                    progress.Report(p);
                    await UniTask.SwitchToThreadPool();
                }
            }

            // Nếu content-length không có, ta vẫn có dữ liệu đầy đủ ở ms
            var bytes = ms.ToArray();
            string text = System.Text.Encoding.UTF8.GetString(bytes);

            // Final progress = 100%
            if (progress != null)
            {
                await UniTask.SwitchToMainThread(linkedToken);
                progress.Report(1f);
                await UniTask.SwitchToThreadPool();
            }

            return text;
        }, cancellationToken: ct);

        // 2) Parse on thread-pool (heavy CPU work)
        Debug.Log(csv);
        await UniTask.RunOnThreadPool(() => Parse(csv), cancellationToken: ct);

        // 3) Back to main thread: notify listeners
        await UniTask.SwitchToMainThread(ct);
        OnLoadComplete?.Invoke(levelConfigs);
    }

    /// <summary>
    /// Phân tích CSV và điền vào levelConfigs
    /// </summary>
    /// <param name="csv"></param>
    public void Parse(string csv)
    {
        if (string.IsNullOrWhiteSpace(csv))
            return;

        // Tách dòng (hỗ trợ cả Windows & Unix)
        string[] lines = csv.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

        if (lines.Length <= 1)
            return;

        // Bắt đầu từ dòng 1 để bỏ header
        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i];
            Debug.Log(line);

            // Skip dòng trống hoặc chỉ chứa whitespace
            if (string.IsNullOrWhiteSpace(line))
                continue;

            // Tách cột
            string[] cols = line.Split(',');

            // Nếu cả row toàn empty -> skip
            bool allEmpty = true;
            for (int c = 0; c < cols.Length; c++)
            {
                if (!string.IsNullOrWhiteSpace(cols[c]))
                {
                    allEmpty = false;
                    break;
                }
            }

            if (allEmpty)
                continue;

            // Cần tối thiểu LEVEL_ID và MAP_SIZE
            if (cols.Length < 2)
                continue;

            // Parse LEVEL_ID
            if (!int.TryParse(cols[0].Trim(), out int levelId))
                continue;

            // Parse MAP_SIZE
            if (!int.TryParse(cols[1].Trim(), out int mapSize))
                continue;

            if (!int.TryParse(cols[2].Trim(), out int maxMoves))
                continue;

            // FACE data: từ cột thứ 2 trở đi
            int faceCount = 6;
            string[] faces = new string[faceCount];

            for (int f = 0; f < faceCount; f++)
            {
                faces[f] = cols[f + 3].Trim();
            }

            // Col: last col = background color hex (optional)
            string bgColorHex = "";
            if (cols.Length >= 10)
            {
                bgColorHex = cols[9].Trim();
            }

            LevelConfig config = new LevelConfig
            {
                levelID = levelId,
                mapSize = mapSize,
                maxMoves = maxMoves,
                levelRawData = faces,
                backgroundColorHex = bgColorHex
            };

            levelConfigs.Add(config);
        }
    }
}

[Serializable]
public class LevelConfig
{
    public int levelID;
    public int mapSize;
    public int maxMoves;
    public string[] levelRawData;
    public string backgroundColorHex;

    public string ToCSVRow()
    {
        List<string> cols = new List<string>
        {
            levelID.ToString(),
            mapSize.ToString(),
            maxMoves.ToString()
        };

        if (levelRawData != null)
        {
            cols.AddRange(levelRawData);
        }

        return string.Join("\t", cols);
    }
}
