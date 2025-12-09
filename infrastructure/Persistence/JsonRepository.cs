using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FSM.Infrastructure.Persistence
{
    public class JsonRepository<T>
    {
        private readonly string _filePath;

        public JsonRepository(string filename)
        {
            // Saves files in the bin/Debug folder for now
            _filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, filename);
        }

        public void Save(List<T> data)
        {
            var options = new JsonSerializerOptions 
            { 
                WriteIndented = true,
                ReferenceHandler = ReferenceHandler.IgnoreCycles // Prevents crashes on your Technician <-> Task loops
            };
            
            string json = JsonSerializer.Serialize(data, options);
            File.WriteAllText(_filePath, json);
        }

        public List<T> Load()
        {
            if (!File.Exists(_filePath)) return new List<T>();

            var json = File.ReadAllText(_filePath);
            var options = new JsonSerializerOptions 
            { 
                PropertyNameCaseInsensitive = true,
                ReferenceHandler = ReferenceHandler.IgnoreCycles 
            };
            
            return JsonSerializer.Deserialize<List<T>>(json) ?? new List<T>();
        }
    }
}