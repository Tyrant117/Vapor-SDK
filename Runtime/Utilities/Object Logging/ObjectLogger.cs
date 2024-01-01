using System.Collections.Generic;
using UnityEngine;
using System;
using System.Text;
using System.Runtime.CompilerServices;

namespace Vapor.ObjectLogging
{
    public enum LogLevel
    {
        Debug,
        Info,
        Warn,
        Error,
        Fatal
    }

    [Serializable]
    public class ObjectLogger
    {
        [SerializeField] private List<RichStringLog> _logs = new(1000);
        [SerializeField] private int _infoCount;
        [SerializeField] private int _warningCount;
        [SerializeField] private int _errorCount;
        public List<RichStringLog> Logs = new(1000);

        private readonly StringBuilder _sb = new();
        private const int InfoStraceCount = 5;
        private const int WarningStraceCount = 10;
        private const int ErrorStraceCount = 20;
        private readonly bool _autoClear;

        public ObjectLogger()
        {
            _autoClear = false;
        }

        public ObjectLogger(bool autoClear)
        {
            _autoClear = autoClear;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Log(LogLevel logLevel, string log, int traceSkipFrames = 1)
        {
            if (_autoClear)
            {
                if (_logs.Count > 100)
                {
                    _logs.Clear();
                    Logs.Clear();
                }
            }

            ToLogger(logLevel, log, traceSkipFrames);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void LogWithConsole(LogLevel logLevel, string log, int traceSkipFrames = 1)
        {
            if (_autoClear)
            {
                if (_logs.Count > 100)
                {
                    _logs.Clear();
                    Logs.Clear();
                }
            }

            ToLogger(logLevel, log, traceSkipFrames);

            switch (logLevel)
            {
                case LogLevel.Debug:
                    Debug.Log(log);
                    break;
                case LogLevel.Info:
                    Debug.Log(log);
                    break;
                case LogLevel.Warn:
                    Debug.LogWarning(log);
                    break;
                case LogLevel.Error:
                    Debug.LogError(log);
                    break;
                case LogLevel.Fatal:
                    Debug.LogError(log);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(logLevel), logLevel, null);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ToLogger(LogLevel logLevel, string log, int traceSkipFrames)
        {
            switch (logLevel)
            {
                case LogLevel.Debug:
                    CreateStackTrace(InfoStraceCount, traceSkipFrames);
                    _logs.Add(new RichStringLog()
                    {
                        Type = 0,
                        Content = log,
                        StackTrace = _sb.ToString(),
                        TimeStamp = DateTimeOffset.UtcNow,
                    });
                    _infoCount++;
                    break;
                case LogLevel.Info:
                    CreateStackTrace(InfoStraceCount, traceSkipFrames);
                    _logs.Add(new RichStringLog()
                    {
                        Type = 0,
                        Content = log,
                        StackTrace = _sb.ToString(),
                        TimeStamp = DateTimeOffset.UtcNow,
                    });
                    _infoCount++;
                    break;
                case LogLevel.Warn:
                    CreateStackTrace(WarningStraceCount, traceSkipFrames);
                    _logs.Add(new RichStringLog
                    {
                        Type = 1,
                        Content = log,
                        StackTrace = _sb.ToString(),
                        TimeStamp = DateTimeOffset.UtcNow,
                    });
                    _warningCount++;
                    break;
                case LogLevel.Error:
                    CreateStackTrace(ErrorStraceCount, traceSkipFrames);
                    _logs.Add(new RichStringLog()
                    {
                        Type = 2,
                        Content = log,
                        StackTrace = _sb.ToString(),
                        TimeStamp = DateTimeOffset.UtcNow,
                    });
                    _errorCount++;
                    break;
                case LogLevel.Fatal:
                    CreateStackTrace(ErrorStraceCount, traceSkipFrames);
                    _logs.Add(new RichStringLog()
                    {
                        Type = 2,
                        Content = log,
                        StackTrace = _sb.ToString(),
                        TimeStamp = DateTimeOffset.UtcNow,
                    });
                    _errorCount++;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(logLevel), logLevel, null);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CreateStackTrace(int traceCount, int traceSkipFrames)
        {
            System.Diagnostics.StackTrace t = new(traceSkipFrames, true);
            var count = Mathf.Min(traceCount, t.FrameCount);
            _sb.Clear();
            for (var i = 0; i < count; i++)
            {
                var frame = t.GetFrame(i);
                var lineNumber = frame.GetFileLineNumber();
                if (lineNumber <= 0) continue;

                var filePathAndName = frame.GetFileName();
                if (string.IsNullOrEmpty(filePathAndName)) continue;

                var fileName = System.IO.Path.GetFileName(filePathAndName);
                if (filePathAndName.StartsWith(".\\Packages"))
                {
#if UNITY_EDITOR
                    var declaringType = frame.GetMethod().DeclaringType;
                    if (declaringType != null)
                    {
                        var pi = UnityEditor.PackageManager.PackageInfo.FindForAssembly(declaringType.Assembly);
                        _sb.AppendLine($"<b>{frame.GetMethod().Name}</b> | {fileName} <link=\"{filePathAndName}%{lineNumber}%{frame.GetFileColumnNumber()}%{pi.name}\"><color=#4B79F0>[{lineNumber}]</color></link>");
                    }
                    else
#endif
                    {
                        _sb.AppendLine($"<b>{frame.GetMethod().Name}</b> | {fileName} <link=\"{filePathAndName}%{lineNumber}%{frame.GetFileColumnNumber()}\"><color=#4B79F0>[{lineNumber}]</color></link>");
                    }
                }
                else
                {
                    _sb.AppendLine($"<b>{frame.GetMethod().Name}</b> | {fileName} <link=\"{filePathAndName}%{lineNumber}%{frame.GetFileColumnNumber()}\"><color=#4B79F0>[{lineNumber}]</color></link>");
                }
                // _sb.AppendLine($"<b>{frame.GetMethod().Name}</b> | {fileName} <a cs=\"{frame.GetFileName()}\" ln=\"{frame.GetFileLineNumber()}\" cn=\"{frame.GetFileColumnNumber()}\"><b>[{frame.GetFileLineNumber()}]</b></a>");
            }
        }
    }
}
