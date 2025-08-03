using System;
using System.IO;
using UnityEngine;

public static class ShopDebugLogger
{
    private static string logFilePath;
    private static bool isInitialized = false;
    
    private static void Initialize()
    {
        if (isInitialized) return;
        
        // Create debug logs folder in Assets
        string debugFolder = Path.Combine(Application.dataPath, "DebugLogs");
        if (!Directory.Exists(debugFolder))
        {
            Directory.CreateDirectory(debugFolder);
        }
        
        // Create log file with timestamp
        string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        string fileName = $"ShopDebug_{timestamp}.txt";
        logFilePath = Path.Combine(debugFolder, fileName);
        
        // Write header
        File.WriteAllText(logFilePath, $"Shop Debug Log - {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n");
        File.AppendAllText(logFilePath, "================================================\n\n");
        
        isInitialized = true;
        
        Debug.Log($"Shop Debug Logger initialized. Log file: {logFilePath}");
    }
    
    public static void Log(string message, string category = "General")
    {
        Initialize();
        
        string timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
        string logEntry = $"[{timestamp}] [{category}] {message}\n";
        
        // Write to file
        try
        {
            File.AppendAllText(logFilePath, logEntry);
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to write to log file: {e.Message}");
        }
        
        // Also output to Unity console
        Debug.Log($"[ShopDebug] {message}");
    }
    
    public static void LogError(string message, string category = "Error")
    {
        Initialize();
        
        string timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
        string logEntry = $"[{timestamp}] [ERROR] [{category}] {message}\n";
        
        // Write to file
        try
        {
            File.AppendAllText(logFilePath, logEntry);
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to write to log file: {e.Message}");
        }
        
        // Also output to Unity console
        Debug.LogError($"[ShopDebug] {message}");
    }
    
    public static void LogWarning(string message, string category = "Warning")
    {
        Initialize();
        
        string timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
        string logEntry = $"[{timestamp}] [WARNING] [{category}] {message}\n";
        
        // Write to file
        try
        {
            File.AppendAllText(logFilePath, logEntry);
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to write to log file: {e.Message}");
        }
        
        // Also output to Unity console
        Debug.LogWarning($"[ShopDebug] {message}");
    }
    
    public static void LogSeparator()
    {
        Initialize();
        
        try
        {
            File.AppendAllText(logFilePath, "\n------------------------\n\n");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to write to log file: {e.Message}");
        }
    }
    
    public static string GetLogFilePath()
    {
        Initialize();
        return logFilePath;
    }
}